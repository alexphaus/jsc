using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpTree;

namespace jsc
{
    class Scope : Dictionary<string, Exp>
    {
        public Scope previous;
        public List<Variable> variables = new List<Variable>();

        public Exp Find(string key)
        {
            if (TryGetValue(key, out Exp value))
            {
                return value;
            }
            else
            {
                return previous?.Find(key);
            }
        }

        public Scope Last { get => previous?.Last ?? this; }

        public Variable NewVar(string name)
        {
            Variable var = new Variable { name = name };
            variables.Add(var);
            Parser.locals.Add(name, var);
            return var;
        }
    }

    partial class Parser
    {
        public static Scope globals = new Scope();
        public static Scope locals = globals;
        public static Dictionary<(Type type, string name), Lambda> prototypes = new Dictionary<(Type type, string name), Lambda>();

        public static Block Parse(string src)
        {
            Token tok = Tokenize(src);
            Block body = ParseBlock(tok.Expressions, false);
            body.SetVariables();
            return body;
        }

        static Exp[] ParseArg(Token paren)
        {
            return paren.Expressions.Select(x => ParseExp(x)).ToArray();
        }

        static void ParseFunc(List<Token> toks)
        {
            var lcp = locals;
            locals = new Scope { previous = globals };

            string name;
            TypeI typeI = null;

            if (toks.Count > 3) // extension
            {
                // take only type
                List<Token> typetoks = toks.Take(toks.Count - 3).ToList();
                typeI = (TypeI)ParseExp(typetoks);
            }
            
            name = toks[toks.Count - 3].Value;

            Reflection.ParameterInfo[] parameterInfos = Reflection.ParameterInfo.Parse(toks[toks.Count - 2], false);
            foreach (var p in parameterInfos)
            {
                locals.NewVar(p.Name);
            }

            if (typeI != null)
            {
                locals.NewVar("this");
            }

            Block body = ParseBlock(toks[toks.Count - 1],false);
            body.SetVariables();
            Lambda lmb = new Lambda { body = body, parameters = parameterInfos };

            locals = lcp;

            if (typeI != null)
            {
                prototypes.Add((typeI.type, name), lmb);
            }
            else
            {
                locals.Add(name, lmb);
            }
        }

        static Exp ParseLambda(List<Token> toks)
        {
            var lcp = locals;
            locals = new Scope { previous = locals };

            Reflection.ParameterInfo[] parameterInfos = Reflection.ParameterInfo.Parse(toks[1], false);
            foreach (var p in parameterInfos)
            {
                locals.NewVar(p.Name);
            }

            Block body = ParseBlock(toks[2], false);
            body.SetVariables();
            var lmb = new Lambda { body = body, parameters = parameterInfos };

            locals = lcp;

            return lmb;
        }
    }
}