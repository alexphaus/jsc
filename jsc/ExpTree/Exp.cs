using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using jsc;

namespace ExpTree
{
    [Flags]
    public enum GotoStatement
    {
        Return = 1,
        Break = 2,
        Continue = 4,
        Loop = Break | Continue
    }

    public struct Goto
    {
        public GotoStatement type;
        public Exp value;
    }

    public class Exp
    {
        public static Exp Null = Exp.Constant(null);
        public static Exp True = Exp.Constant(true);
        public static Exp False = Exp.Constant(false);
        public static Exp Echo = Exp.Constant((Action<object>)(value => Console.WriteLine(value)));
        /* types */
        public static Exp Int = new TypeI { type = typeof(int) };
        public static Exp Float = new TypeI { type = typeof(double) };
        public static Exp Char = new TypeI { type = typeof(char) };
        public static Exp Bool = new TypeI { type = typeof(bool) };
        public static Exp String = new TypeI { type = typeof(string) };
        public static Exp Object = new TypeI { type = typeof(object) };
        public static Exp List = new TypeI { type = typeof(jsc.List) };

        public static Stack<Goto> flow = new Stack<Goto>();

        //public static Dictionary<(Type type, string name), Lambda> prototypes;
        
        public virtual dynamic Eval()
        {
            return null;
        }

        public virtual void SetValue(dynamic value)
        {
            throw new Exception("The left-hand side of an assignment must be a variable, property or indexer");
        }

        public static object[] EvalArg(Exp[] arg)
        {
            var r = new object[arg.Length];
            for (int i = 0; i < arg.Length; i++)
            {
                r[i] = arg[i].Eval();
            }
            return r;
        }

        public static Constant Constant(object value)
        {
            return new Constant { value = value };
        }

        public static Member Member(Exp obj, string name)
        {
            return new Member { obj = obj, name = name };
        }
    }

    public class Block : Exp
    {
        public string Name;
        public List<Exp> expressions;

        public Variable[] variables;
        public dynamic[] loc;
        public int lcsize;

        public void SetVariables()
        {
            var variables = Parser.locals.variables.ToArray();
            for (int i = 0; i < variables.Length; i++)
            {
                variables[i].lcid = i;
                variables[i].block = this;
            }
            this.variables = variables;
            lcsize = variables.Length;
        }
        
        public override dynamic Eval()
        {
            if (lcsize == 0)
            {
                return Exec();
            }
            else
            {
                var lc = loc;
                loc = new dynamic[lcsize];
                var ret = Exec();
                loc = lc;
                return ret;
            }
        }

        public dynamic Exec()
        {
            for (int i = 0; i < expressions.Count; i++)
            {
                expressions[i].Eval();
                if (flow.Count > 0)
                {
                    break;
                }
            }
            return null;
        }
    }

    public class Return : Exp
    {
        public Exp value;
        public override dynamic Eval()
        {
            flow.Push(new Goto { type = GotoStatement.Return, value = value });
            return null;
        }
    }

    public class Break : Exp
    {
        public override dynamic Eval()
        {
            flow.Push(new Goto { type = GotoStatement.Break });
            return null;
        }
    }

    public class Continue : Exp
    {
        public override dynamic Eval()
        {
            flow.Push(new Goto { type = GotoStatement.Continue });
            return null;
        }
    }

    public class Lambda : Exp
    {
        public Reflection.ParameterInfo[] parameters;
        public Block body;

        public dynamic Invoke(Exp[] arg, object @this)
        {
            var lc = body.loc;
            body.loc = new dynamic[body.lcsize];

            // load parameters
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].IsRef)
                {
                    if (arg[i] is Variable var)
                    {
                        body.variables[i].block = var.block;
                        body.variables[i].lcid = var.lcid;
                    }
                    else
                        throw new Exception("Only variables are allowed to be passed to a reference parameter");
                }
                else
                {
                    body.loc[i] = arg[i].Eval();
                }
            }
            if (@this != null)
                body.loc[parameters.Length] = @this;

            body.Exec();

            dynamic r = null;
            
            if (flow.Count > 0)
                r = flow.Pop().value.Eval();

            body.loc = lc;
            return r;
        }

        public override dynamic Eval()
        {
            return this;
        }
    }

    public class If : Exp
    {
        public Exp test;
        public Exp body;

        public override dynamic Eval()
        {
            if (test.Eval())
            {
                body.Eval();
            }
            return null;
        }
    }

    public class IfElse : Exp
    {
        public Exp test;
        public Exp ifTrue;
        public Exp ifFalse;

        public override dynamic Eval()
        {
            if (test.Eval())
            {
                ifTrue.Eval();
            }
            else
            {
                ifFalse.Eval();
            }
            return null;
        }
    }

    public class While : Exp
    {
        public Exp test;
        public Block body;
        public Exp step;

        public override dynamic Eval()
        {
            while (test.Eval())
            {
                body.Eval();
                if (flow.Count > 0)
                {
                    if ((flow.Peek().type & GotoStatement.Loop) > 0)
                    {
                        if (flow.Pop().type == GotoStatement.Break)
                            break;
                    }
                    else
                        break;
                }
                step?.Eval();
            }
            return null;
        }
    }

    public class ForEach : Exp
    {
        public Variable var;
        public Exp collection;
        public Block body;

        public override dynamic Eval()
        {
            foreach (dynamic elem in collection.Eval())
            {
                var.SetValue(elem);
                body.Eval();
                if (flow.Count > 0)
                {
                    if ((flow.Peek().type & GotoStatement.Loop) > 0)
                    {
                        if (flow.Pop().type == GotoStatement.Break)
                            break;
                    }
                    else
                        break;
                }
            }
            return null;
        }
    }

    public class Switch : Exp
    {
        public Exp switchValue;
        public SwitchCase[] cases;

        public override dynamic Eval()
        {
            dynamic switchValue = this.switchValue.Eval();
            foreach (SwitchCase @case in cases)
            {
                dynamic caseValue = @case.test.Eval();
                if (object.Equals(switchValue, caseValue))
                {
                    @case.body.Eval();
                    break;
                }
            }
            return null;
        }
    }

    public class SwitchCase
    {
        public Exp test;
        public Block body;
    }

    public class JumpTable : Exp
    {
        public Dictionary<dynamic, Block> table;
        public Exp value;

        public override dynamic Eval()
        {
            if (table.TryGetValue(value.Eval(), out Block body))
            {
                body.Eval();
            }
            return null;
        }
    }

    public class Constant : Exp
    {
        public dynamic value;

        public override dynamic Eval()
        {
            return value;
        }
    }

    public class TypeI : Exp
    {
        public Type type;
        public Dictionary<int, Type> genericTypes;

        public override dynamic Eval()
        {
            return type;
        }
    }

    public class NamespaceI : Exp
    {
        public string name;
        public Dictionary<string, Exp> fields;

        public override dynamic Eval()
        {
            throw new Exception($"'{name}' is a namespace");
        }
    }

    class New : Exp
    {
        public Exp type;
        public Exp[] arg;

        public override dynamic Eval()
        {
            object type = this.type.Eval();
            if (type is Reflection.Type t)
                return t.CreateInstance(arg);
            else
                return Activator.CreateInstance((Type)type, EvalArg(arg));
        }
    }

    class NewList : Exp
    {
        public Exp[] items;

        public override dynamic Eval()
        {
            var lst = new jsc.List();
            foreach (Exp item in items)
            {
                if (item is Operation.Multiply oper && oper.left == Null)
                    lst.AddRange(oper.right.Eval());
                else
                    lst.Add(item.Eval());
            }
            return lst;
        }
    }

    class NewDict : Exp
    {
        public Exp[] elements;

        public override dynamic Eval()
        {
            var dct = new jsc.Dict();
            Operation.Colon colon;
            foreach (Exp elem in elements)
            {
                colon = (Operation.Colon)elem;
                dct.Add(colon.left.Eval(), colon.right.Eval());
            }
            return dct;
        }
    }

    public class Member : Exp
    {
        public Exp obj;
        public string name;

        public override dynamic Eval()
        {
            object obj = this.obj.Eval();

            if (obj is Reflection.Object instance)
            {
                return instance.Type[name].GetValue(instance);
            }
            else if (obj is Dict dct && dct.TryGetValue(name, out dynamic value))
            {
                return value;
            }
            else
            {
                Type type = obj is Type ? (Type)obj : obj.GetType();
                // property
                var pi = type.GetProperty(name);
                if (pi != null)
                    return pi.GetValue(obj);
                // field
                var fi = type.GetField(name);
                if (fi != null)
                    return fi.GetValue(obj);
                throw new Exception($"No member '{name}' exists in type '{type}'");
            }
        }

        public override void SetValue(dynamic value)
        {
            object obj = this.obj.Eval();

            if (obj is Reflection.Object instance)
            {
                instance.Type[name].SetValue(instance, value);
            }
            else if (obj is Dict dct)
            {
                dct[name] = value;
            }
            else
            {
                Type type = obj is Type ? (Type)obj : obj.GetType();
                // property
                var pi = type.GetProperty(name);
                if (pi != null)
                    pi.SetValue(obj, value);
                else
                { // field
                    var fi = type.GetField(name);
                    if (fi != null)
                        fi.SetValue(obj, value);
                    else
                        throw new Exception($"No member '{name}' exists in type '{type}'");
                }
            }
        }
    }

    class Invoke : Exp
    {
        public Exp obj;
        public Exp[] arg;

        public override dynamic Eval()
        {
            // jsc function
            object obj = this.obj.Eval();
            if (obj is Lambda lmb)
                return lmb.Invoke(this.arg, null);

            object[] arg = EvalArg(this.arg);
            // cil
            if (obj is Func<object[], object> del)
                return del.Invoke(arg);
            // default
            var m = obj.GetType().GetMethod("Invoke");
            return m.Invoke(obj, arg);
        }
    }

    class Call : Exp
    {
        public Exp obj;
        public string name;
        public Exp[] arg;
        public Type[] typeArgs;

        public override dynamic Eval()
        {
            object obj = this.obj.Eval();
            if (obj is Reflection.Object instance)
            {
                return instance.Type[name].Callvirt(instance, arg);
            }
            else
            {
                Type type = obj is Type ? (Type)obj : obj.GetType();
                // extension
                if (Parser.prototypes.TryGetValue((type, name), out Lambda lmb))
                {
                    return lmb.Invoke(this.arg, obj);
                }
                object[] arg = EvalArg(this.arg);
                Type[] types = arg.Select(x => x.GetType()).ToArray();
                var m = type.GetMethod(name, types);
                if (typeArgs != null)
                {
                    m = m.MakeGenericMethod(typeArgs);
                }
                if (m != null)
                    return m.Invoke(obj, arg);
                throw new Exception($"No method '{name}' exists in type '{type}' with the supplied arguments");
            }
        }
    }

    class Index : Exp
    {
        public Exp obj;
        public Exp index;

        public override object Eval()
        {
            return obj.Eval()[index.Eval()];
        }

        public override void SetValue(dynamic value)
        {
            obj.Eval()[index.Eval()] = value;
        }
    }

    /// <summary>
    /// zero-based element indexing
    /// </summary>
    class EmptyIndex : Exp
    {
        public Exp obj;

        public override object Eval()
        {
            throw new Exception("Cannot use [] for reading");
        }

        public override void SetValue(dynamic value)
        {
            List lst = (List)obj.Eval();
            lst.Add(value);
        }
    }

    /// <summary>
    /// Python's slice notation
    /// </summary>
    class SliceIndex : Exp
    {
        public Exp obj;
        public Exp start;
        public Exp end;
        public Exp step;

        public override object Eval()
        {
            var l = (System.Collections.IList)this.obj.Eval();
            int start;
            int end;
            int step;
            int maxint = 10000000;       // A hack to simulate PY_SSIZE_T_MAX

            if (this.step == Exp.Null)
                step = 1;
            else
                step = (int)this.step.Eval();

            if (step == 0)
                throw new Exception("slice step cannot be zero");

            if (this.start == Exp.Null)
                start = step < 0 ? maxint : 0;
            else
                start = (int)this.start.Eval();

            if (this.end == Exp.Null)
                end = step < 0 ? -maxint : maxint;
            else
                end = (int)this.end.Eval();

            // Handle negative slice indexes and bad slice indexes.
            // Compute number of elements in the slice as slice_length.
            int length = l.Count;
            int slice_length = 0;

            if (start < 0)
            {
                start += length;
                if (start < 0)
                    start = step < 0 ? -1 : 0;
            }
            else if (start >= length)
                start = step < 0 ? length - 1 : length;

            if (end < 0)
            {
                end += length;
                if (end < 0)
                    end = step < 0 ? -1 : 0;
            }
            else if (end > length)
                end = step < 0 ? length - 1 : length;

            if (step < 0)
            {
                if (end < start)
                    slice_length = (start - end - 1) / -step + 1;
            }
            else
            {
                if (start < end)
                    slice_length = (end - start - 1) / step + 1;
            }

            // Cases of step = 1 and step != 1 are treated separately
            if (slice_length <= 0)
                return new List();
            else if (step == 1)
            {
                var result = new List();
                for (int i = 0, loopTo = end - start; i < loopTo; i++)
                {
                    result.Add(l[i + start]);
                }
                return result;
            }
            else
            {
                var result = new List();
                int cur = start;
                for (int i = 0; i < slice_length; i++)
                {
                    result.Add(l[cur]);
                    cur += step;
                }
                return result;
            }
        }
    }

    public class Variable : Exp
    {
        public string name;
        public Block block;
        public int lcid;

        public override dynamic Eval()
        {
            return block.loc[lcid];
        }

        public override void SetValue(dynamic value)
        {
            block.loc[lcid] = value;
        }
    }

    class Throw : Exp
    {
        public Exp message;

        public override dynamic Eval()
        {
            throw new Exception((string)message.Eval());
        }
    }

    //class Cil : Exp
    //{
    //    public System.Reflection.Emit.ILGenerator il;
    //    public List<System.Reflection.Emit.OpCode> OpCodes;

    //    public override dynamic Eval()
    //    {
    //        foreach (var op in OpCodes)
    //        {
    //            il.Emit()
    //        }
    //    }
    //}
}