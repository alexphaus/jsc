using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpTree;

namespace jsc
{
    public enum Op
    {
        // Unary
        Increment,
        Decrement,
        // Exponentiation
        Power,
        // Multiplicative
        Multiply,
        Divide,
        Modulo,
        // Additive
        Add,
        Subtract,
        // Shift
        LeftShift,
        RightShift,
        // Relational
        Less,
        Greater,
        LessOrEqual,
        GreaterOrEqual,
        Is,
        As,
        In,
        Like,
        // Equality
        Equal,
        NotEqual,
        // Logical NOT
        Not,
        // Bitwise AND
        And,
        // Bitwise XOR
        Xor,
        // Bitwise OR
        Or,
        // Logical AND
        AndAlso,
        // Logical OR
        OrElse,
        // Null Coalescing Operator
        Coalesce,
        // Conditional
        Condition,
        Colon,
        // Lambda
        Lambda,
        // Assignment
        Assign
    }

    partial class Parser
    {
        /// <summary>
        /// parse math expressions
        /// </summary>
        public static Exp ParseExp(Expression expr)
        {
            if (expr.operators != null)
            {
                expr.operands.Add(expr.tokens);
                // parse each operand
                List<Exp> operands = expr.operands.Select(x => ParseExp(x)).ToList();
                List<Op> operators = expr.operators;
                List<int> priority = expr.priority;
                // create nodes
                while (operators.Count > 0)
                {
                    int i = expr.IndexOfMin();
                    Op op = operators[i];
                    Exp left = operands[i];
                    Exp right = operands[i + 1];
                    switch (op)
                    {
                        case Op.Increment:
                            if (right == Exp.Null) // i++
                            {
                                operands[i] = new Operation.PostIncrement { left = left };
                            }
                            else // ++i
                            {
                                operands[i] = new Operation.PreIncrement { left = right };
                            }
                            break;
                        case Op.Decrement:
                            if (right == Exp.Null) // i--
                            {
                                operands[i] = new Operation.PostDecrement { left = left };
                            }
                            else // --i
                            {
                                operands[i] = new Operation.PreDecrement { left = right };
                            }
                            break;
                        case Op.Power:
                            operands[i] = new Operation.Power { left = left, right = right };
                            break;
                        case Op.Multiply:
                            operands[i] = new Operation.Multiply { left = left, right = right };
                            break;
                        case Op.Divide:
                            operands[i] = new Operation.Divide { left = left, right = right };
                            break;
                        case Op.Modulo:
                            operands[i] = new Operation.Modulo { left = left, right = right };
                            break;
                        case Op.LeftShift:
                            operands[i] = new Operation.LeftShift { left = left, right = right };
                            break;
                        case Op.RightShift:
                            operands[i] = new Operation.RightShift { left = left, right = right };
                            break;
                        case Op.Add:
                            operands[i] = new Operation.Add { left = left, right = right };
                            break;
                        case Op.Subtract:
                            if (left == Exp.Null)
                            {
                                operands[i] = new Operation.Negate { left = right };
                            }
                            else
                            {
                                operands[i] = new Operation.Subtract { left = left, right = right };
                            }
                            break;
                        case Op.Less:
                            operands[i] = new Operation.Less { left = left, right = right };
                            break;
                        case Op.Greater:
                            operands[i] = new Operation.Greater { left = left, right = right };
                            break;
                        case Op.LessOrEqual:
                            operands[i] = new Operation.LessOrEqual { left = left, right = right };
                            break;
                        case Op.GreaterOrEqual:
                            operands[i] = new Operation.GreaterOrEqual { left = left, right = right };
                            break;

                        case Op.Is:
                            operands[i] = new Operation.Is { left = left, right = right };
                            break;

                        case Op.As:
                            operands[i] = new Operation.As { left = left, right = right };
                            break;

                        case Op.In:
                            operands[i] = new Operation.In { left = left, right = right };
                            break;
                        case Op.Like:
                            operands[i] = new Operation.Like { left = left, right = right };
                            break;
                        case Op.Equal:
                            operands[i] = new Operation.Equal { left = left, right = right };
                            break;
                        case Op.NotEqual:
                            operands[i] = new Operation.NotEqual { left = left, right = right };
                            break;
                        case Op.Not:
                            operands[i] = new Operation.Not { left = right };
                            break;
                        case Op.And:
                            if (left == Exp.Null)
                            {
                                operands[i] = new Operation.Ref { left = right };
                            }
                            else
                            {
                                operands[i] = new Operation.And { left = left, right = right };
                            }
                            break;
                        case Op.Xor:
                            operands[i] = new Operation.Xor { left = left, right = right };
                            break;
                        case Op.Or:
                            operands[i] = new Operation.Or { left = left, right = right };
                            break;
                        case Op.AndAlso:
                            operands[i] = new Operation.AndAlso { left = left, right = right };
                            break;
                        case Op.OrElse:
                            operands[i] = new Operation.OrElse { left = left, right = right };
                            break;
                        case Op.Coalesce:
                            operands[i] = new Operation.Coalesce { left = left, right = right };
                            break;
                        case Op.Condition:
                            operands[i] = new Operation.Condition { left = left, right = right };
                            break;
                        case Op.Colon:
                            if (left is Operation.Condition iif)
                            {
                                iif.ifFalse = right;
                            }
                            else
                            {
                                operands[i] = new Operation.Colon { left = left, right = right };
                            }
                            break;
                        case Op.Assign:
                            operands[i] = new Operation.Assign { left = left, right = right };
                            break;
                        case Op.Lambda:
                            throw new Exception("Lambda operator '=>' is not implemented yet in jsc1.0");
                            break;
                    }
                    operands.RemoveAt(i + 1);
                    operators.RemoveAt(i);
                    priority.RemoveAt(i);
                }
                return operands[0];
            }
            else
            {
                return ParseExp(expr.tokens);
            }
        }
    }
}