using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jsc
{
    partial class Parser
    {
        const string ext = ".js";

        public static string fpath(List<Token> toks)
        {
            if (toks.Count == 1 && toks[0].Type == TokenType.String)
            {
                return toks[0].Value;
            }

            string path = string.Join(".", toks.Select(x => x.Value));
            string s;
            // current directory
            s = string.Concat(path, ext);
            if (System.IO.File.Exists(s))
            {
                return s;
            }

            // lib directory
            s = string.Concat(AppDomain.CurrentDomain.BaseDirectory, "lib/", path, ext);
            if (System.IO.File.Exists(s))
            {
                return s;
            }

            // .net framework
            s = string.Concat("C:/Windows/Microsoft.NET/Framework/v4.0.30319/", path, ".dll");
            if (System.IO.File.Exists(s))
            {
                return s;
            }

            throw new Exception($"Could not import '{path}'. Path not found");
        }

        public static void Import(string fileName)
        {
            if (fileName.EndsWith(".js", StringComparison.Ordinal))
            {
                var lcp = locals;
                locals = new Scope();

                var tk = Tokenize(System.IO.File.ReadAllText(fileName));
                var bl = ParseBlock(tk, false);
                bl.SetVariables();
                bl.loc = new dynamic[bl.lcsize];
                bl.Exec();

                lcp.Last.previous = locals;
                locals = lcp;
            }
            else if (fileName.EndsWith(".dll", StringComparison.Ordinal))
            {
                foreach (Type t in System.Reflection.Assembly.LoadFrom(fileName).GetExportedTypes())
                {
                    ExpTree.NamespaceI namespaceI = GetNamespace(t.Namespace);
                    if (t.Name.Contains("`"))
                    {
                        string[] split = t.Name.Split('`');
                        string name = split[0];
                        int typeArgsLength = int.Parse(split[1]);
                        if (namespaceI.fields.TryGetValue(name, out ExpTree.Exp exp))
                        {
                            ExpTree.TypeI typeI = (ExpTree.TypeI)exp;
                            if (typeI.genericTypes is null)
                            {
                                typeI.genericTypes = new Dictionary<int, Type>();
                            }
                            typeI.genericTypes.Add(typeArgsLength, t);
                        }
                        else
                        {
                            namespaceI.fields.Add(name, new ExpTree.TypeI { genericTypes = new Dictionary<int, Type> { { typeArgsLength, t } } });
                        }
                    }
                    else
                    {
                        if (namespaceI.fields.TryGetValue(t.Name, out ExpTree.Exp exp))
                        {
                            var typeI = (ExpTree.TypeI)exp;
                            typeI.type = t;
                        }
                        else
                        {
                            namespaceI.fields.Add(t.Name, new ExpTree.TypeI { type = t });
                        }
                    }
                }
            }
            else
                throw new Exception("Only packages with extension '.js' or '.dll' can be imported");
            //string src = System.IO.File.ReadAllText(fileName + ".js");
            //var lang = new jsc(src);
            //lang.Run();
        }

        static Dictionary<string, ExpTree.NamespaceI> namespaces = new Dictionary<string, ExpTree.NamespaceI>();
        static ExpTree.NamespaceI GetNamespace(string ns)
        {
            if (namespaces.TryGetValue(ns, out var namespaceI))
            {
                return namespaceI;
            }
            else
            {
                string[] spaces = ns.Split('.');
                namespaceI = new ExpTree.NamespaceI { fields = new Dictionary<string, ExpTree.Exp>(), name = ns };
                if (spaces.Length == 1)
                {
                    globals.Add(ns, namespaceI);
                }
                else
                {
                    var ant = GetNamespace(ns.Substring(0, ns.LastIndexOf('.')));
                    ant.fields.Add(spaces[spaces.Length - 1], namespaceI);
                }
                namespaces.Add(ns, namespaceI);
                return namespaceI;
            }
        }

        public static void Using(List<Token> toks)
        {
            foreach (Token tok in toks)
            {
                var ns = (ExpTree.NamespaceI)globals[tok.Value];
                foreach (var field in ns.fields)
                {
                    globals[field.Key] = field.Value;
                }
            }
        }

        public struct ModAs
        {
            public string Module;
            public string As;
        }

        public static void Import(ModAs[] modas)
        {

        }

        public struct IdAs
        {
            public string Id;
            public string As;
        }

        public static void From(string module, IdAs[] idas)
        {

        }

        public static void FromAll(string module)
        {

        }
    }
}