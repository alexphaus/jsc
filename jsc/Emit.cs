using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using ExpTree;

namespace jsc
{
    partial class Parser
    {
        public static Dictionary<string, OpCode> DictOp;

        static void InitializeDictOp()
        {
            DictOp = new Dictionary<string, OpCode>();
            var fields = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (FieldInfo f in fields)
            {
                if (f.FieldType == typeof(OpCode))
                {
                    DictOp.Add(f.Name.ToLower(), (OpCode)f.GetValue(null));
                }
            }
        }

        public static Delegate ParseCil(List<Token> tokens)
        {
            // initialize OPs
            if (DictOp is null)
                InitializeDictOp();
            
            var lcp = locals;
            locals = new Scope { previous = globals };

            string name = tokens[0].Value;
            Type[] parameterTypes = tokens[1].Expressions
                .Select(x => ((TypeI)ParseExp(x)).type)
                .ToArray();

            Type returnType;
            List<Token> retToks = tokens.Skip(2).Take(tokens.Count - 3).ToList();
            if (retToks.Count == 0)
                returnType = typeof(void);
            else
                returnType = ((TypeI)ParseExp(retToks)).type;

            var meth = new DynamicMethod(name, returnType, parameterTypes, typeof(jsc).Module);
            var il = meth.GetILGenerator();

            var body = tokens[tokens.Count - 1];

            OpCode op;
            dynamic arg = null;
            
            foreach (Expression e in body.Expressions)
            {
                if (e.tokens.Count == 0) // none
                {
                    continue;
                }
                // label
                if (e.operators != null && e.operators.Count == 1 && e.operators[0] == Op.Colon)
                {
                    string lbl = e.operands[0][0].Value;
                    Label label = il.DefineLabel();
                    locals.Add(lbl, Exp.Constant(label));
                    il.MarkLabel(label);
                }

                string opName = e.tokens[0].Value;

                // exceptions
                if (e.cmd == Statement.Var)
                {
                    e.tokens.RemoveAt(0);
                    Type type = ((TypeI)ParseExp(e.tokens)).type;
                    locals.Add(opName, Exp.Constant(il.DeclareLocal(type)));
                    continue;
                }
                else if (opName == "ldarg_ref")
                {
                    il.Emit(OpCodes.Ldarg_0);
                    e.tokens.RemoveAt(0);
                    il.Emit(OpCodes.Ldc_I4, (int)ParseExp(e.tokens).Eval());
                    il.Emit(OpCodes.Ldelem_Ref);
                    continue;
                }

                // parse opcode
                if (DictOp.TryGetValue(opName, out OpCode value))
                {
                    op = value;
                }
                else
                {
                    throw new Exception($"'{opName}' is not an OpCode");
                }
                
                // parse arg
                if (e.tokens.Count == 1)
                {
                    arg = null;
                }
                else if (e.tokens.Count >= 2)
                {
                    e.tokens.RemoveAt(0);
                    arg = ParseExp(e.tokens).Eval();
                }

                // emit
                if (arg is null)
                {
                    il.Emit(op);
                }
                else
                {
                    il.Emit(op, arg);
                }
            }

            locals = lcp;

            Type delType;
            if (returnType == typeof(void))
            {
                delType = System.Linq.Expressions.Expression.GetActionType(parameterTypes);
            }
            else
            {
                Type[] tArgs = parameterTypes.Concat(new[] { returnType }).ToArray();
                delType = System.Linq.Expressions.Expression.GetFuncType(tArgs);
            }

            return meth.CreateDelegate(delType);
        }
    }
}