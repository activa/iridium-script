#region License
//=============================================================================
// VeloxDB Core - Portable .NET Productivity Library 
//
// Copyright (c) 2008-2015 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Iridium.Script.CSharp
{
    public static class CSharpEvaluator
    {
        private static readonly NumberFormatInfo _numberFormat;

        static CSharpEvaluator()
        {
            _numberFormat = new NumberFormatInfo
                                {
                                    NumberDecimalSeparator = ".",
                                    NumberGroupSeparator = ",",
                                    NumberGroupSizes = new[] {3}
                                };
        }

        public static Expression TypeCast(string token, Expression[] terms)
        {
            return new TypeCastExpression(VarName(token.Substring(1, token.Length - 2).Trim(),new Expression[0]), terms[0]);
        }

        public static Expression IsAsOperator(string token, Expression[] terms)
        {
            if (token == "as")
                return new AsExpression(terms[0], terms[1]);

            if (token == "is")
                return new IsExpression(terms[0], terms[1]);

            return null;
        }

        public static Expression InOperator(string token, Expression[] terms)
        {
            VariableExpression varExpression = terms[0] as VariableExpression;

            return new InExpression(varExpression, terms[1]);
        }


        public static Expression Ternary(string token, Expression[] terms)
        {
            return new ConditionalExpression(terms[0], terms[1], terms[2]);
        }

        private static char UnEscape(string s)
        {
            if (s.Length == 1)
                return s[0];

            if (s.Length == 2)
            {
                switch (s[1])
                {
                    case '\\':
                    case '\"':
                    case '\'':
                        return s[1];
                    case '0':
                        return (char)0;
                    case 'a':
                        return '\a';
                    case 'b':
                        return '\b';
                    case 'f':
                        return '\f';
                    case 'n':
                        return '\n';
                    case 'r':
                        return '\r';
                    case 't':
                        return '\t';
                    case 'v':
                        return '\v';
                    default:
                        throw new UnknownTokenException(s);
                }
            }
            else
            {
                return (char)Convert.ToUInt16(s.Substring(2), 16);
            }
        }

        public static Expression TypeOf(string token,Expression[] terms)
        {
            return new TypeOfExpression();
        }

        public static Expression CharLiteral(string token, Expression[] terms)
        {
            return Exp.Value(UnEscape(token.Substring(1, token.Length - 2)));
        }

        public static Expression Number(string token, Expression[] terms)
        {
            string s = token;

            Type type = null;

            if (!char.IsDigit(s[s.Length - 1]))
            {
                string suffix = "" + char.ToUpper(s[s.Length - 1]);

                s = s.Remove(s.Length - 1);

                if (!char.IsDigit(s[s.Length - 1]))
                {
                    suffix = char.ToUpper(s[s.Length - 1]) + suffix;

                    s = s.Remove(s.Length - 1);
                }

                switch (suffix)
                {
                    case "M":
                        type = typeof(decimal);
                        break;
                    case "D":
                        type = typeof(double);
                        break;
                    case "F":
                        type = typeof(float);
                        break;
                    case "L":
                        type = typeof(long);
                        break;
                    case "U":
                        type = typeof(uint);
                        break;
                    case "LU":
                    case "UL":
                        type = typeof(ulong);
                        break;
                }
            }

            if (type != null)
                return Exp.Value(Convert.ChangeType(s, type, _numberFormat), type);

            if (s.LastIndexOf('.') >= 0)
            {
                return Exp.Value(Convert.ToDouble(s, _numberFormat));
            }
            else
            {
                long n = Convert.ToInt64(s);

                if (n > Int32.MaxValue || n < Int32.MinValue)
                    return Exp.Value(n);
                else
                    return Exp.Value((int)n);
            }
        }

        public static Expression VarName(string token, Expression[] terms)
        {
            Expression exp;

            if (_keywords.TryGetValue(token, out exp))
                return exp;

            return new VariableExpression(token);
        }

        public static Expression Function(string token, Expression[] terms)
        {
            Expression[] parameters = new Expression[terms.Length - 1];

            Array.Copy(terms, 1, parameters, 0, parameters.Length);

            if (token == "[")
            {
                return new IndexExpression(terms[0], parameters);
            }
            else
            {
                return new CallExpression(terms[0], parameters);
            }
        }

        public static Expression Coalesce(string token, Expression[] terms)
        {
            return new CoalesceExpression(terms[0], terms[1]);
        }

        public static Expression DefaultValueOperator(string token, Expression[] terms)
        {
            return new DefaultValueExpression(terms[0], terms[1]);
        }

        public static Expression ValueOrNullOperator(string token, Expression[] terms)
        {
            return new ValueOrNullExpression(terms[0], terms[1]);
        }

        public static Expression ShortcutOperator(string token, Expression[] terms)
        {
            if (token == "&&")
                return new AndAlsoExpression(terms[0], terms[1]);

            if (token == "||")
                return new OrElseExpression(terms[0], terms[1]);

            return null;
        }

        public static Expression Unary(string token, Expression[] terms)
        {
            if (token == "!")
                return new NegationExpression(terms[0]);

            if (token == "-")
                return new UnaryMinusExpression(terms[0]);

            if (token == "~")
                return new BitwiseComplementExpression(terms[0]);

            return null;
        }

        public static Expression StatementSeperator(string token, Expression[] terms)
        {
            return new SequenceExpression(terms);
        }

        public static Expression Operator(string token, Expression[] terms)
        {
            return Exp.Op(token, terms[0], terms[1]);
        }

        public static Expression Assignment(string token,Expression[] terms)
        {
            return new AssignmentExpression(terms[0], terms[1]);
        }

        public static Expression StringLiteral(string token, Expression[] terms)
        {
            string s = token.Substring(1, token.Length - 2);

            if (s.IndexOf('\\') < 0)
                return Exp.Value(s);

            StringBuilder output = new StringBuilder(token.Length);

            bool inEscape = false;
            string hexString = null;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (inEscape)
                {
                    if (c == 'x')
                    {
                        hexString = "";
                        continue;
                    }

                    if (hexString == null && (c != 'x' || c != 'X'))
                    {
                        output.Append(UnEscape("\\" + c));
                        inEscape = false;
                        continue;
                    }

                    if (hexString == null)
                    {
                        inEscape = false;
                    }
                    else
                    {
                        if (((char.ToLower(c) < 'a' || char.ToLower(c) > 'f') && (c < '0' || c > '9')) || hexString.Length == 4)
                        {
                            output.Append(UnEscape("\\x" + hexString));
                            inEscape = false;
                            hexString = null;
                        }
                        else
                        {
                            hexString += c;
                            continue;
                        }
                    }
                }

                if (c != '\\')
                {
                    output.Append(c);

                    continue;
                }

                inEscape = true;
            }

            return Exp.Value(output.ToString());
        }

        public static Expression DotOperator(string token, Expression[] terms)
        {
            VariableExpression varExpression = terms[1] as VariableExpression;

            if (varExpression == null)
                throw new UnknownPropertyException("Unkown member " + terms[1], terms[1]);

            return new FieldExpression(terms[0], varExpression.VarName);
        }

        public static Expression Constructor(string token, Expression[] terms)
        {
            string className = token.Substring(3).Trim();

            return new ConstructorExpression(new VariableExpression(className), terms);
        }

        public static Expression NumericRange(string token,  Expression[] terms)
        {
			return new RangeExpression(terms[0], terms[1], token.StartsWith(">"), token.EndsWith("<"));
        }

        private static readonly Dictionary<string, Expression> _keywords = new Dictionary<string, Expression>()
        {
            { "int",  Exp.Value(ContextFactory.CreateType(typeof(int))) },
            { "uint", Exp.Value(ContextFactory.CreateType(typeof(uint))) },
            { "long", Exp.Value(ContextFactory.CreateType(typeof(long))) },
            { "ulong", Exp.Value(ContextFactory.CreateType(typeof(ulong))) },
            { "short", Exp.Value(ContextFactory.CreateType(typeof(short))) },
            { "ushort", Exp.Value(ContextFactory.CreateType(typeof(ushort))) },
            { "double", Exp.Value(ContextFactory.CreateType(typeof(double))) },
            { "float", Exp.Value(ContextFactory.CreateType(typeof(float))) },
            { "bool", Exp.Value(ContextFactory.CreateType(typeof(bool))) },
            { "char", Exp.Value(ContextFactory.CreateType(typeof(char))) },
            { "byte", Exp.Value(ContextFactory.CreateType(typeof(byte))) },
            { "sbyte", Exp.Value(ContextFactory.CreateType(typeof(sbyte))) },
            { "decimal", Exp.Value(ContextFactory.CreateType(typeof(decimal))) },
            { "string", Exp.Value(ContextFactory.CreateType(typeof(string))) },
            { "null", Exp.Value(null, typeof(object)) },
            { "true", Exp.Value(true) },
            { "false", Exp.Value(false) }

        };
    }
}