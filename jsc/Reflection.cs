using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpTree;
using jsc;

namespace Reflection
{
    public class Object
    {
        public Type Type; // the type that this object represents
        public dynamic[] loc;

        public override string ToString()
        {
            return Type.Name;
        }
    }

    public class Type : Dictionary<string, IMember>
    {
        public string Name { get; }
        public Reflection.Type Base { get; }
        MethodInfo ctor;
        int fldcnt = 0;

        public Type(Expression source)
        {
            // get the name
            if (source.operators is null)
                Name = source.tokens[0].Value;
            else // extends
            {
                Name = source.operands[0][0].Value;
                Base = (Parser.globals[source.tokens[0].Value] as Constant).value;
            }
            // initializer
            var init = new List<Exp>();
            // get all members
            var fields = new List<string>();
            Token body = source.tokens.Last();
            Expression expr;
            for (int i = 0; i < body.Expressions.Count; i++)
            {
                expr = body.Expressions[i];
                if (expr.operators?[0] == Op.Assign)
                {
                    string fldName = expr.operands[0][0].Value;
                    var fi = new FieldInfo(fldName, fldcnt++);
                    Add(fldName, fi);
                    fields.Add(fldName);
                    init.Add(Parser.ParseExp(expr));
                }
                else if (expr.cmd == Statement.Get)
                {
                    MethodInfo m = MethodInfo.Parse(expr.tokens, fields);
                    if (TryGetValue(m.Name, out IMember value))
                    {
                        var prop = (PropertyInfo)value;
                        prop.GetMethod = m;
                    }
                    else
                    {
                        var prop = new PropertyInfo();
                        prop.Name = m.Name;
                        prop.GetMethod = m;
                        Add(m.Name, prop);
                    }
                }
                else if (expr.cmd == Statement.Set)
                {
                    MethodInfo m = MethodInfo.Parse(expr.tokens, fields);
                    if (TryGetValue(m.Name, out IMember value))
                    {
                        var prop = (PropertyInfo)value;
                        prop.SetMethod = m;
                    }
                    else
                    {
                        var prop = new PropertyInfo();
                        prop.Name = m.Name;
                        prop.SetMethod = m;
                        Add(m.Name, prop);
                    }
                }
                else if (expr.operators is null &&
                    expr.tokens.Count == 3 &&
                    expr.tokens[0].Type == TokenType.Identifier &&
                    expr.tokens[1].Type == TokenType.Parenthesis &&
                    expr.tokens[2].Type == TokenType.Braces)
                {
                    MethodInfo m = MethodInfo.Parse(expr.tokens, fields);
                    if (m.Name == "constructor")
                        ctor = m;
                    else
                        Add(m.Name, m);
                }
            }
            // embed field initialization into constructor
            if (ctor is null)
            {
                ctor = new MethodInfo();
                ctor.body = new Block { expressions = new List<Exp>(), lcsize = 1 };
                ctor.self = new Variable { block = ctor.body };
            }
            // initialize field values
            for (int i = 0; i < fields.Count; i++)
            {
                var assign = (Operation.Assign)init[i];
                assign.left = Exp.Member(ctor.self, fields[i]);
                ctor.body.expressions.Insert(i, assign);
            }
        }

        public MethodInfo GetMethod(string name)
        {
            if (TryGetValue(name, out IMember value) && value is MethodInfo m)
            {
                return m;
            }
            return null;
        }

        public PropertyInfo GetProperty(string name)
        {
            if (TryGetValue(name, out IMember value) && value is PropertyInfo p)
            {
                return p;
            }
            return null;
        }

        public FieldInfo GetField(string name)
        {
            if (TryGetValue(name, out IMember value) && value is FieldInfo f)
            {
                return f;
            }
            return null;
        }

        public bool InstanceOf(Reflection.Type type)
        {
            if (this == type)
                return true;
            else if (Base != null)
                return Base.InstanceOf(type);
            return false;
        }

        public Object CreateInstance(Exp[] arg)
        {
            var instance = new Object
            {
                Type = this,
                loc = new dynamic[fldcnt]
            };
            ctor?.Callvirt(instance, arg);
            return instance;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class IMember
    {
        public virtual dynamic GetValue(Reflection.Object obj)
        {
            throw new Exception("Non-overridden member access");
        }

        public virtual void SetValue(Reflection.Object obj, dynamic value)
        {
            throw new Exception("Non-overridden member access");
        }

        public virtual dynamic Callvirt(Reflection.Object obj, Exp[] arg)
        {
            throw new Exception("Non-overridden member access");
        }
    }

    public class MethodInfo : IMember
    {
        public string Name { get; private set; }
        public ParameterInfo[] Parameters { get; private set; }
        public Block body;
        public Variable self;

        public static MethodInfo Parse(List<Token> toks, List<string> fields)
        {
            var m = new MethodInfo();
            var lcp = Parser.locals;
            Parser.locals = new Scope();
            Parser.locals.previous = Parser.globals;

            m.Name = toks[0].Value;
            m.Parameters = ParameterInfo.Parse(toks[1], true);

            // load parameters
            foreach (var pi in m.Parameters)
            {
                Parser.locals.NewVar(pi.Name);
            }

            // load fields
            m.self = Parser.locals.variables[0];
            foreach (string field in fields)
            {
                Parser.locals.Add(field, Exp.Member(m.self, field));
            }

            m.body = Parser.ParseBlock(toks[2], false);
            m.body.SetVariables();
            
            Parser.locals = lcp;

            return m;
        }
        
        public override dynamic Callvirt(Reflection.Object obj, Exp[] arg)
        {
            // make reference copy from previous scope
            var lcp = body.loc;
            body.loc = new dynamic[body.lcsize];
            body.loc[0] = obj; // this

            // load parameters
            for (int i = 0; i < arg.Length; i++)
                body.loc[i + 1] = arg[i].Eval();

            var ret = body.Exec();

            body.loc = lcp;

            return ret;
        }

        /// <summary>
        /// external invokation
        /// </summary>
        public dynamic Invoke(Reflection.Object obj, dynamic[] arg)
        {
            // make reference copy from previous scope
            var lcp = body.loc;
            body.loc = new dynamic[body.lcsize];
            body.loc[0] = obj; // this

            // load parameters
            for (int i = 0; i < arg?.Length; i++)
                body.loc[i + 1] = arg[i];

            var ret = body.Exec();

            body.loc = lcp;

            return ret;
        }
    }

    public class ParameterInfo
    {
        public string Name { get; private set; }
        public bool IsRef { get; private set; }

        public static ParameterInfo[] Parse(Token token, bool addthis)
        {
            var lst = new List<ParameterInfo>();
            if (addthis)
                lst.Add(new ParameterInfo { Name = "this" });
            Expression expr;
            for (int i = 0; i < token.Expressions.Count; i++)
            {
                expr = token.Expressions[i];
                var p = new ParameterInfo();
                if (expr.operators?[0] == Op.And)
                    p.IsRef = true;
                p.Name = expr.tokens[0].Value;
                lst.Add(p);
            }
            return lst.ToArray();
        }
    }

    public class PropertyInfo : IMember
    {
        public string Name { get; set; }
        public MethodInfo GetMethod { get; set; }
        public MethodInfo SetMethod { get; set; }

        public override dynamic GetValue(Reflection.Object obj)
        {
            return GetMethod.Invoke(obj, null);
        }

        public override void SetValue(Reflection.Object obj, dynamic value)
        {
            SetMethod.Invoke(obj, new[] { value });
        }
    }

    public class FieldInfo : IMember
    {
        public string Name { get; }
        int lcid;

        public FieldInfo(string name, int lcid)
        {
            this.Name = name;
            this.lcid = lcid;
        }

        public override dynamic GetValue(Reflection.Object obj)
        {
            return obj.loc[lcid];
        }

        public override void SetValue(Reflection.Object obj, dynamic value)
        {
            obj.loc[lcid] = value;
        }
    }
}