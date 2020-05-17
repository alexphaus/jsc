using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpTree
{
    public class Operation : Exp
    {
        public Exp left;
        public Exp right;

        public class Power : Operation
        {
            public override dynamic Eval()
            {
                return Math.Pow((double)left.Eval(), (double)right.Eval());
            }
        }

        public class Multiply : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() * right.Eval();
            }
        }

        public class Divide : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() / right.Eval();
            }
        }

        public class Modulo : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() % right.Eval();
            }
        }

        public class Add : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() + right.Eval();
            }
        }

        public class Subtract : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() - right.Eval();
            }
        }

        public class LeftShift : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() << right.Eval();
            }
        }

        public class RightShift : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() >> right.Eval();
            }
        }

        public class Less : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() < right.Eval();
            }
        }

        public class Greater : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() > right.Eval();
            }
        }

        public class LessOrEqual : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() <= right.Eval();
            }
        }

        public class GreaterOrEqual : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() >= right.Eval();
            }
        }

        public class Is : Operation
        {
            public override dynamic Eval()
            {
                object value = left.Eval();
                object type = right.Eval();

                if (type is Reflection.Type T)
                    if (value is Reflection.Object O)
                        return O.Type.InstanceOf(T);
                    else
                        return false;

                return value.GetType() == (Type)type;
            }
        }

        public class As : Operation
        {
            public override dynamic Eval()
            {
                object value = left.Eval();
                Type type = (Type)right.Eval();

                if (type.IsArray && value is jsc.List lst)
                {
                    var arrayvalue = lst.ToArray();
                    var elemType = type.GetElementType();
                    var destinationArray = Array.CreateInstance(elemType, arrayvalue.Length);
                    Array.Copy(arrayvalue, destinationArray, arrayvalue.Length);
                    return destinationArray;
                }

                return Convert.ChangeType(value, type);
            }
        }

        public class In : Operation
        {
            public override dynamic Eval()
            {
                object value = left.Eval();
                object seq = right.Eval();

                if (seq is System.Collections.IList lst)
                {
                    return lst.Contains(value);
                }
                else if (seq is string str)
                {
                    return str.Contains((string)value);
                }
                else if (seq is System.Collections.IDictionary dct)
                {
                    return dct.Contains(value);
                }

                throw new Exception($"Unknown sequence");
            }
        }

        public class Like : Operation
        {
            public override dynamic Eval()
            {
                return System.Text.RegularExpressions.Regex.
                    IsMatch((string)left.Eval(), (string)right.Eval());
            }
        }

        public class Equal : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() == right.Eval();
            }
        }

        public class NotEqual : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() != right.Eval();
            }
        }

        public class And : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() & right.Eval();
            }
        }

        public class Xor : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() ^ right.Eval();
            }
        }

        public class Or : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() | right.Eval();
            }
        }

        public class AndAlso : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() && right.Eval();
            }
        }

        public class OrElse : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() || right.Eval();
            }
        }

        public class Coalesce : Operation
        {
            public override dynamic Eval()
            {
                return left.Eval() ?? right.Eval();
            }
        }

        public class Condition : Operation
        {
            public Exp ifFalse;

            public override dynamic Eval()
            {
                return left.Eval() ? right.Eval() : ifFalse.Eval();
            }
        }

        public class Colon : Operation
        {
            public override dynamic Eval()
            {
                return null;
            }
        }

        public class Assign : Operation
        {
            public override dynamic Eval()
            {
                var r = right.Eval();
                left.SetValue(r);
                return r;
            }
        }

        public class Negate : Operation
        {
            public override dynamic Eval()
            {
                return -left.Eval();
            }
        }

        public class Not : Operation
        {
            public override dynamic Eval()
            {
                return !left.Eval();
            }
        }

        public class PreIncrement : Operation
        {
            public override dynamic Eval()
            {
                var r = left.Eval() + 1;
                left.SetValue(r);
                return r;
            }
        }

        public class PreDecrement : Operation
        {
            public override dynamic Eval()
            {
                var r = left.Eval() - 1;
                left.SetValue(r);
                return r;
            }
        }

        public class PostIncrement : Operation
        {
            public override dynamic Eval()
            {
                var r = left.Eval();
                left.SetValue(r + 1);
                return r;
            }
        }

        public class PostDecrement : Operation
        {
            public override dynamic Eval()
            {
                var r = left.Eval();
                left.SetValue(r - 1);
                return r;
            }
        }

        public class Cast : Operation
        {
            public override dynamic Eval()
            {
                return null;
            }
        }

        public class Ref : Operation
        {
            public override dynamic Eval()
            {
                return new jsc.Ref { value = left.Eval() };
            }
        }
    }
}