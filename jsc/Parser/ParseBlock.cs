using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpTree;

namespace jsc
{
    public enum Statement
    {
        None,
        Import,
        Using,
        Class,
        Get,
        Set,
        Cil,
        Function,
        Return,
        If,
        Else,
        For,
        While,
        Break,
        Continue,
        Switch,
        Case,
        Throw,
        Echo,
        Var
    }

    partial class Parser
    {
        public static Block ParseBlock(Token tok, bool islocal = true)
        {
            return ParseBlock(tok.Expressions, islocal);
        }

        public static Block ParseBlock(List<Expression> expressions, bool islocal = true)
        {
            Scope lcp = null;
            if (islocal)
            {
                lcp = locals;
                locals = new Scope();
                locals.previous = lcp;
            }
            var chunk = new List<Exp>();
            Expression expr;
            var ifparts = new Stack<If>();
            for (int i = 0; i < expressions.Count; i++)
            {
                expr = expressions[i];

                if (expr.cmd == Statement.None && expr.operators is null && expr.tokens.Count == 0)
                    continue;

                switch (expr.cmd)
                {
                    case Statement.Import:
                        Import(fpath(expr.tokens));
                        break;
                    case Statement.Using:
                        Using(expr.tokens);
                        break;
                    case Statement.Class:
                        var type = new Reflection.Type(expr);
                        globals.Add(type.Name, Exp.Constant(type));
                        break;
                    case Statement.Cil:
                        globals.Add(expr.tokens[0].Value, Exp.Constant(ParseCil(expr.tokens)));
                        break;
                    case Statement.Function:
                        ParseFunc(expr.tokens);
                        break;
                    case Statement.Return:
                        chunk.Add(new Return { value = ParseExp(expr) });
                        break;
                    case Statement.If:
                        Exp test = ParseExp(expr.tokens[0].Expressions[0]);
                        Block body = ParseBlock(expr.tokens[1]);
                        If ifthen = new If { test = test, body = body };
                        ifparts.Push(ifthen);

                        if (i + 1 == expressions.Count || expressions[i + 1].cmd != Statement.Else)
                        {
                            chunk.Add(IfThenElse(ifparts));
                        }

                        break;
                    case Statement.Else:
                        if (expr.tokens[0].Type == TokenType.Parenthesis) // else if
                        {
                            goto case Statement.If;
                        }
                        else // else
                        {
                            body = ParseBlock(expr.tokens[0]);
                            chunk.Add(IfThenElse(ifparts, body));
                        }
                        break;
                    case Statement.For:
                        Token paren = expr.tokens[0];
                        if (paren.Expressions.Count == 3)
                        {
                            Exp init = ParseExp(paren.Expressions[0]);
                            chunk.Add(init);
                            test = ParseExp(paren.Expressions[1]);
                            Exp step = ParseExp(paren.Expressions[2]);
                            body = ParseBlock(expr.tokens[1]);
                            var loop = new While { test = test, body = body, step = step };
                            chunk.Add(loop);
                        }
                        else if (paren.Expressions.Count == 1)
                        {
                            var forEach = new ForEach();
                            forEach.var = (Variable)ParseExp(paren.Expressions[0].operands[0]);
                            forEach.collection = ParseExp(paren.Expressions[0].tokens);
                            forEach.body = ParseBlock(expr.tokens[1]);
                            chunk.Add(forEach);
                        }
                        break;
                    case Statement.While:
                        chunk.Add(new While
                        {
                            test = ParseExp(expr.tokens[0].Expressions[0]),
                            body = ParseBlock(expr.tokens[1])
                        });
                        break;
                    case Statement.Break:
                        chunk.Add(new Break());
                        break;
                    case Statement.Continue:
                        chunk.Add(new Continue());
                        break;
                    case Statement.Switch:
                        {
                            var cases = new List<SwitchCase>();
                            Exp switchValue = ParseExp(expr.tokens[0].Expressions[0]);

                            Exp caseValue = null;
                            var bodybuilder = new List<Expression>();
                            bool jump = true;

                            foreach (Expression e in expr.tokens[1].Expressions)
                            {
                                if (e.cmd == Statement.Case)
                                {
                                    if (caseValue != null)
                                    {
                                        cases.Add(new SwitchCase
                                        {
                                            test = caseValue,
                                            body = ParseBlock(bodybuilder)
                                        });
                                    }
                                    // new case
                                    caseValue = ParseExp(e);
                                    if (jump)
                                        if (!(caseValue is Constant))
                                        {
                                            jump = false;
                                        }
                                    bodybuilder = new List<Expression>();
                                }
                                else
                                {
                                    bodybuilder.Add(e);
                                }
                            }

                            cases.Add(new SwitchCase
                            {
                                test = caseValue,
                                body = ParseBlock(bodybuilder)
                            });

                            // try convert to jumptable
                            if (jump)
                            {
                                var table = new Dictionary<dynamic, Block>();
                                foreach (SwitchCase c in cases)
                                {
                                    table.Add(((Constant)c.test).value, c.body);
                                }
                                chunk.Add(new JumpTable { value = switchValue, table = table });
                            }
                            else
                                chunk.Add(new Switch { switchValue = switchValue, cases = cases.ToArray() });
                            break;
                        }
                    case Statement.Echo:
                        Exp msg = ParseExp(expr);
                        chunk.Add(new Invoke { obj = Exp.Echo, arg = new[] { msg } });
                        break;
                    case Statement.Throw:
                        msg = ParseExp(expr);
                        chunk.Add(new Throw { message = msg });
                        break;
                    case Statement.Var:
                        if (expr.operators is null)
                        {
                            string name = expr.tokens[0].Value;
                            locals.NewVar(name);
                        }
                        else if (expr.operators[0] == Op.Assign)
                        {
                            string name = expr.operands[0][0].Value;
                            locals.NewVar(name);
                            goto default;
                        }
                        break;
                    default:
                        chunk.Add(ParseExp(expr));
                        break;
                }
            }
            if (islocal)
            {
                var body = new Block { expressions = chunk };
                body.SetVariables();
                locals = lcp;
                return body;
            }
            return new Block { expressions = chunk };
        }

        static Exp IfThenElse(Stack<If> ifparts, Block elseblock = null)
        {
            If node = ifparts.Pop();
            Exp r;

            if (elseblock is null)
                r = node;
            else
                r = new IfElse { test = node.test, ifTrue = node.body, ifFalse = elseblock };

            while (ifparts.Count > 0)
            {
                node = ifparts.Pop();
                r = new IfElse { test = node.test, ifTrue = node.body, ifFalse = r };
            }

            return r;
        }
    }
}