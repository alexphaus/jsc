using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpTree;

namespace jsc
{
    partial class Parser
    {
        /// <summary>
        /// parse non-math expressions
        /// </summary>
        public static Exp ParseExp(List<Token> tokens)
        {
            if (tokens.Count == 0)
            {
                return Exp.Null;
            }
            else if (tokens.Count == 1)
            {
                Token tok = tokens[0];
                switch (tok.Type)
                {
                    case TokenType.String:
                        return Exp.Constant(tok.Value);

                    case TokenType.Char:
                        return Exp.Constant(tok.Value[0]);

                    case TokenType.Number:
                        if (tok.Value.IndexOf('.') == -1)
                            return Exp.Constant(int.Parse(tok.Value));
                        else
                            return Exp.Constant(double.Parse(tok.Value));

                    case TokenType.Identifier:
                        switch (tok.Value)
                        {
                            case "null":
                                return Exp.Null;
                            case "true":
                                return Exp.True;
                            case "false":
                                return Exp.False;

                            case "int":
                                return Exp.Int;
                            case "float":
                                return Exp.Float;
                            case "char":
                                return Exp.Char;
                            case "bool":
                                return Exp.Bool;
                            case "String":
                                return Exp.String;
                            case "Object":
                                return Exp.Object;
                            case "List":
                                return Exp.List;

                            default:
                                Exp c = locals.Find(tok.Value);
                                if (c is null)
                                {
                                    return locals.NewVar(tok.Value);
                                }
                                else
                                {
                                    return c;
                                }
                        }
                    case TokenType.Parenthesis: // parenthesis
                        if (tok.Expressions.Count == 1)
                        {
                            return ParseExp(tok.Expressions[0]);
                        }
                        break;
                    case TokenType.Brackets: // array
                        return new NewList { items = ParseArg(tok) };

                    case TokenType.Braces: // dictionary
                        return new NewDict { elements = ParseArg(tok) };
                }
            }
            else
            {
                // starts with
                Token first = tokens[0];
                if (first.Type == TokenType.Identifier)
                {
                    if (first.Value == "new")
                    {
                        tokens.RemoveAt(0); // new
                        int lastindex = tokens.Count - 1;
                        Exp[] arg = ParseArg(tokens[lastindex]);
                        tokens.RemoveAt(lastindex); // (..)
                        return new New { type = ParseExp(tokens), arg = arg };
                    }
                    else if (first.Value == "function")
                    {
                        return ParseLambda(tokens);
                    }
                }

                // ends with
                Token last = tokens[tokens.Count - 1];
                tokens.RemoveAt(tokens.Count - 1);

                if (last.Type == TokenType.Parenthesis)
                {
                    Token pre = tokens[tokens.Count - 1];
                    if (pre.Type == TokenType.Member)
                    {
                        tokens.RemoveAt(tokens.Count - 1);
                        return new Call { obj = ParseExp(tokens), name = pre.Value, arg = ParseArg(last) };
                    }
                    else if (pre.Type == TokenType.Brackets) // generic call
                    {
                        Token pre2 = tokens[tokens.Count - 2];
                        if (pre2.Type == TokenType.Member)
                        {
                            tokens.RemoveAt(tokens.Count - 1);
                            tokens.RemoveAt(tokens.Count - 1);
                            return new Call { obj = ParseExp(tokens), name = pre2.Value, arg = ParseArg(last),
                                typeArgs = pre.Expressions.Select(x => ((TypeI)ParseExp(x)).type).ToArray()
                            };
                        }
                    }
                    return new Invoke { obj = ParseExp(tokens), arg = ParseArg(last) };
                }
                else if (last.Type == TokenType.Brackets)
                {
                    Exp collection = ParseExp(tokens);
                    Exp[] indeces = ParseArg(last);

                    // set generic type
                    if (collection is TypeI t)
                    {
                        Type[] typeArgs = indeces.Select(x => ((TypeI)x).type).ToArray();
                        if (typeArgs.Length == 0)
                        {
                            return new TypeI { type = t.type.MakeArrayType() };
                        }
                        else
                        {
                            return new TypeI { type = t.genericTypes[indeces.Length].MakeGenericType(typeArgs) };
                        }
                    }

                    if (indeces.Length == 0)
                    {
                        // [] operator
                        return new EmptyIndex { obj = collection };
                    }
                    else if (indeces.Length > 1)
                    {
                        throw new Exception("syntax error, unexpected ',', expecting ']'");
                    }
                    Exp index = indeces[0];

                    // slice notation
                    if (index is Operation.Colon colon1)
                    {
                        if (colon1.left is Operation.Colon colon2) // step
                        {
                            return new SliceIndex
                            {
                                obj = collection,
                                start = colon2.left,
                                end = colon2.right,
                                step = colon1.right
                            };
                        }
                        else // step omitted
                        {
                            return new SliceIndex
                            {
                                obj = collection,
                                start = colon1.left,
                                end = colon1.right,
                                step = Exp.Null
                            };
                        }
                    }

                    return new ExpTree.Index { obj = collection, index = index };
                }
                else if (last.Type == TokenType.Member)
                {
                    Exp obj = ParseExp(tokens);
                    string name = last.Value;
                    if (obj is NamespaceI ns)
                    {
                        if (ns.fields.TryGetValue(name, out Exp field))
                            return field;
                        else
                            throw new Exception($"The type or namespace name '{name}' does not exist in the namespace '{ns.name}'");
                    }
                    return Exp.Member(obj, name);
                }
            }
            throw new Exception("Unable to parse expression");
        }
    }
}
