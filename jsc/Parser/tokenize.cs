using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jsc
{
    public enum TokenType
    {
        Identifier,
        Member,
        Number,
        String,
        Char,
        Parenthesis,
        Brackets,
        Braces
    }

    public class Token
    {
        public TokenType Type;
        public string Value;
        public Group Expressions;
    }

    public class Group : List<Expression>
    {
        public Expression Last
        {
            get
            {
                if (Count == 0)
                    Add(new Expression());
                return this[Count - 1];
            }
        }
    }

    public class Expression
    {
        public Statement cmd;
        public List<Token> tokens = new List<Token>();
        public List<List<Token>> operands;
        public List<Op> operators;
        public List<int> priority;
        static int[] precedence = {
            0, 0, // ++, --
            1, // ^
            2, 2, 2, // *, /, %
            3, 3, // +, -
            4, 4, // <<, >>
            5, 5, 5, 5, 5, 5, 5, 5, // <, >, <=, >=, is, as, in, like
            6, 6, // ==, !=
            7, 8, 9, 10, 11, 12, 13, 14, 14, 15, 16 // !, &, xor, |, &&, ||, ??, ?, :, =>, =
        };

        public void PushOp(Op op)
        {
            if (operators is null)
            {
                operands = new List<List<Token>>();
                operators = new List<Op>();
                priority = new List<int>();
            }
            operands.Add(tokens);
            tokens = new List<Token>();
            operators.Add(op);
            priority.Add(precedence[(int)op]);
        }

        public int IndexOfMin()
        {
            int min = priority[0];
            int minIndex = 0;
            for (int i = 1; i < priority.Count; i++)
            {
                if (priority[i] < min)
                {
                    min = priority[i];
                    minIndex = i;
                }
            }
            return minIndex;
        }
    }

    enum Capture
    {
        None,
        String,
        Char,
        Comment,
        CommentDoc,
        IL
    }

    partial class Parser
    {
        static Token Tokenize(string src)
        {
            src = string.Concat(src, " *");
            char c; // current char
            char f; // forward char
            bool skipNext = false;
            var cap = Capture.None;
            var value = new StringBuilder();
            bool alldigit = true;
            bool isMember = false;
            var stack = new Stack<Token>(); // for groups

            void append(Token item) =>
                stack.Peek().Expressions.Last.tokens.Add(item);

            void appendword(string val)
            {
                Token word = new Token();
                if (isMember)
                {
                    word.Type = TokenType.Member;
                    isMember = false;
                }
                else
                    word.Type = TokenType.Identifier;

                word.Value = val;
                append(word);
            }

            void setcmd(Statement state, string val)
            {
                var last = stack.Peek().Expressions.Last;
                if (last.operators is null && last.tokens.Count == 0)
                {
                    last.cmd = state;
                }
                else
                    appendword(val);
            }

            void pushOp(Op op) =>
                stack.Peek().Expressions.Last.PushOp(op);

            void pushGroup(TokenType type) =>
                stack.Push(new Token
                {
                    Type = type,
                    Expressions = new Group()
                });
            // default
            pushGroup(TokenType.Braces);

            for (int i = 0, end = src.Length - 1; i < end; i++)
            {
                c = src[i];
                f = src[i + 1];

                if (skipNext)
                {
                    skipNext = false;
                    continue;
                }

                switch (cap)
                {
                    case Capture.None:
                        if (char.IsLetter(c) || c == '_')
                        {
                            value.Append(c);
                            alldigit = false;
                        }
                        else if (char.IsDigit(c))
                        {
                            value.Append(c);
                        }
                        else
                        {
                            if (value.Length > 0)
                            {
                                if (alldigit)
                                {
                                    if (c == '.')
                                    {
                                        continue; // float point
                                    }
                                    else
                                    {
                                        append(new Token
                                        {
                                            Type = TokenType.Number,
                                            Value = value.ToString()
                                        });
                                    }
                                }
                                else
                                {
                                    string val = value.ToString();
                                    switch (val)
                                    {
                                        // operators
                                        case "is":
                                            pushOp(Op.Is);
                                            break;
                                        case "as":
                                            pushOp(Op.As);
                                            break;
                                        case "in":
                                            pushOp(Op.In);
                                            break;
                                        case "like":
                                            pushOp(Op.Like);
                                            break;
                                        case "xor":
                                            pushOp(Op.Xor);
                                            break;
                                        // keywords
                                        case "import":
                                            setcmd(Statement.Import, val);
                                            break;
                                        case "using":
                                            setcmd(Statement.Using, val);
                                            break;
                                        case "class":
                                            setcmd(Statement.Class, val);
                                            break;
                                        case "get":
                                            setcmd(Statement.Get, val);
                                            break;
                                        case "set":
                                            setcmd(Statement.Set, val);
                                            break;
                                        case "cil":
                                            setcmd(Statement.Cil, val);
                                            break;
                                        case "function":
                                            setcmd(Statement.Function, val);
                                            break;
                                        case "return":
                                            setcmd(Statement.Return, val);
                                            break;
                                        case "var":
                                            setcmd(Statement.Var, val);
                                            break;
                                        case "if":
                                            if (stack.Peek().Expressions.Last.cmd == Statement.None)
                                                setcmd(Statement.If, val);
                                            break;
                                        case "else":
                                            setcmd(Statement.Else, val);
                                            break;
                                        case "for":
                                            setcmd(Statement.For, val);
                                            break;
                                        case "while":
                                            setcmd(Statement.While, val);
                                            break;
                                        case "break":
                                            setcmd(Statement.Break, val);
                                            break;
                                        case "continue":
                                            setcmd(Statement.Continue, val);
                                            break;
                                        case "switch":
                                            setcmd(Statement.Switch, val);
                                            break;
                                        case "case":
                                            setcmd(Statement.Case, val);
                                            break;
                                        case "throw":
                                            setcmd(Statement.Throw, val);
                                            break;
                                        case "echo":
                                            setcmd(Statement.Echo, val);
                                            break;
                                        default:
                                            appendword(val);
                                            break;
                                    }
                                }
                                value.Clear();
                                alldigit = true;
                            }
                            switch (c)
                            {
                                case ' ':
                                case '\t':
                                case '\r':
                                case '\n':
                                    break;
                                #region Operators
                                case '^':
                                    pushOp(Op.Power);
                                    break;
                                case '*':
                                    pushOp(Op.Multiply);
                                    break;
                                case '/':
                                    switch (f)
                                    {
                                        case '/':
                                            cap = Capture.Comment;
                                            skipNext = true;
                                            break;
                                        case '*':
                                            cap = Capture.CommentDoc;
                                            skipNext = true;
                                            break;
                                        default:
                                            pushOp(Op.Divide);
                                            break;
                                    }
                                    break;
                                case '%':
                                    pushOp(Op.Modulo);
                                    break;
                                case '+':
                                    if (f == '+')
                                    {
                                        pushOp(Op.Increment);
                                        skipNext = true;
                                    }
                                    else
                                        pushOp(Op.Add);
                                    break;
                                case '-':
                                    if (f == '-')
                                    {
                                        pushOp(Op.Decrement);
                                        skipNext = true;
                                    }
                                    else
                                        pushOp(Op.Subtract);
                                    break;
                                case '<':
                                    if (f == '=')
                                    {
                                        pushOp(Op.LessOrEqual);
                                        skipNext = true;
                                    }
                                    else if (f == '<')
                                    {
                                        pushOp(Op.LeftShift);
                                        skipNext = true;
                                    }
                                    else
                                        pushOp(Op.Less);
                                    break;
                                case '>':
                                    if (f == '=')
                                    {
                                        pushOp(Op.GreaterOrEqual);
                                        skipNext = true;
                                    }
                                    else if (f == '>')
                                    {
                                        pushOp(Op.RightShift);
                                        skipNext = true;
                                    }
                                    else
                                        pushOp(Op.Greater);
                                    break;
                                case '=':
                                    if (f == '=')
                                    {
                                        pushOp(Op.Equal);
                                        skipNext = true;
                                    }
                                    else if (f == '>')
                                    {
                                        pushOp(Op.Lambda);
                                        skipNext = true;
                                    }
                                    else
                                        pushOp(Op.Assign);
                                    break;
                                case '&':
                                    if (f == '&')
                                    {
                                        pushOp(Op.AndAlso);
                                        skipNext = true;
                                    }
                                    else
                                        pushOp(Op.And);
                                    break;
                                case '|':
                                    if (f == '|')
                                    {
                                        pushOp(Op.OrElse);
                                        skipNext = true;
                                    }
                                    else
                                        pushOp(Op.Or);
                                    break;
                                case '!':
                                    if (f == '=')
                                    {
                                        pushOp(Op.NotEqual);
                                        skipNext = true;
                                    }
                                    else
                                        pushOp(Op.Not);
                                    break;
                                case '?':
                                    if (f == '?')
                                    {
                                        pushOp(Op.Coalesce);
                                        skipNext = true;
                                    }
                                    else
                                        pushOp(Op.Condition);
                                    break;
                                case ':':
                                    // if 'case' line convert colon to semicolon
                                    if (stack.Peek().Expressions.Last.cmd == Statement.Case)
                                    {
                                        goto case ';';
                                    }
                                    else
                                        pushOp(Op.Colon);
                                    break;
                                #endregion
                                case '\\':
                                    if (f == '"')
                                    {
                                        cap = Capture.Char;
                                        skipNext = true;
                                        break;
                                    }
                                    goto default;
                                case '#':
                                    cap = Capture.Comment;
                                    break;
                                case '"':
                                    cap = Capture.String;
                                    break;
                                case '(':
                                    pushGroup(TokenType.Parenthesis);
                                    break;
                                case '[':
                                    pushGroup(TokenType.Brackets);
                                    break;
                                case '{':
                                    pushGroup(TokenType.Braces);
                                    break;
                                case ']':
                                case ')':
                                    Token group = stack.Pop();
                                    append(group);
                                    break;
                                case '}':
                                    group = stack.Pop();
                                    append(group);
                                    // auto append ';'
                                    if (char.IsWhiteSpace(f) && stack.Peek().Type == TokenType.Braces)
                                        goto case ';';
                                    break;
                                case '.':
                                    isMember = true;
                                    break;
                                case ',':
                                case ';':
                                    stack.Peek().Expressions.Add(new Expression());
                                    break;
                                default:
                                    throw new Exception($"Unknown symbol '{c}'");
                            }
                        }
                        break;
                    case Capture.String:
                        switch (c)
                        {
                            case '"':
                                var tok = new Token();
                                tok.Type = TokenType.String;
                                tok.Value = value.ToString();
                                value.Clear();
                                append(tok);
                                cap = Capture.None;
                                break;
                            default:
                                value.Append(c);
                                break;
                        }
                        break;
                    case Capture.Char:
                        if (f == '"')
                        {
                            var tok = new Token();
                            tok.Type = TokenType.Char;
                            tok.Value = c.ToString();
                            append(tok);
                            cap = Capture.None;
                            skipNext = true;
                        }
                        else
                            throw new Exception("Too many characters in character literal");
                        break;
                    case Capture.Comment:
                        if (c == '\r')
                            cap = Capture.None;
                        break;
                    case Capture.CommentDoc:
                        if (c == '*' && f == '/')
                        {
                            cap = Capture.None;
                            skipNext = true;
                        }
                        break;
                }
            }
            return stack.Pop();
        }
    }
}