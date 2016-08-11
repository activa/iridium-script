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
using System.Reflection;
using Iridium.Core;

namespace Iridium.Script
{
    public class BinaryArithmicExpression : BinaryExpression
    {
        private class OperatorMethod
        {
            public delegate object Action(string op, object v1, object v2, StringComparison stringComparison, Expression expr);

            public readonly Type Type1;
            public readonly Type Type2;
            public readonly Type ReturnType;

            public readonly Action Function;

            public OperatorMethod(Type type, Action function)
            {
                Type1 = type;
                Type2 = type;
                ReturnType = type;

                Function = function;
            }

            public OperatorMethod(Type returnType, Type type1, Type type2, Action function)
            {
                ReturnType = returnType;
                Type1 = type1;
                Type2 = type2;
                Function = function;
            }
        }

        public BinaryArithmicExpression(string op, Expression left, Expression right) : base(left, right)
        {
            Operator = op;
        }

        static BinaryArithmicExpression()
        {
            _operatorMethods["+"] = new[]
                {
                    new OperatorMethod(typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(float), BinaryExpressionHelper.CalcFloat),
                    new OperatorMethod(typeof(double), BinaryExpressionHelper.CalcDouble),
                    new OperatorMethod(typeof(decimal), BinaryExpressionHelper.CalcDecimal),
                    new OperatorMethod(typeof(string), BinaryExpressionHelper.CalcString),
                    new OperatorMethod(typeof(string), typeof(string), typeof(object), BinaryExpressionHelper.CalcStringObject),
                    new OperatorMethod(typeof(string), typeof(object), typeof(string), BinaryExpressionHelper.CalcObjectString)
                };

            _operatorMethods["-"] = new[]
                {
                    new OperatorMethod(typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(float), BinaryExpressionHelper.CalcFloat),
                    new OperatorMethod(typeof(double), BinaryExpressionHelper.CalcDouble),
                    new OperatorMethod(typeof(decimal), BinaryExpressionHelper.CalcDecimal)
                };
            _operatorMethods["*"] = new[]
                {
                    new OperatorMethod(typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(float), BinaryExpressionHelper.CalcFloat),
                    new OperatorMethod(typeof(double), BinaryExpressionHelper.CalcDouble),
                    new OperatorMethod(typeof(decimal), BinaryExpressionHelper.CalcDecimal)
                };
            _operatorMethods["/"] = new[]
                {
                    new OperatorMethod(typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(float), BinaryExpressionHelper.CalcFloat),
                    new OperatorMethod(typeof(double), BinaryExpressionHelper.CalcDouble),
                    new OperatorMethod(typeof(decimal), BinaryExpressionHelper.CalcDecimal)
                };
            _operatorMethods["%"] = new[]
                {
                    new OperatorMethod(typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(float), BinaryExpressionHelper.CalcFloat),
                    new OperatorMethod(typeof(double), BinaryExpressionHelper.CalcDouble),
                    new OperatorMethod(typeof(decimal), BinaryExpressionHelper.CalcDecimal)
                };
            _operatorMethods["<<"] = new[]
                {
                    new OperatorMethod(typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(uint), typeof(uint), typeof(int), BinaryExpressionHelper.CalcUInt32_Int32),
                    new OperatorMethod(typeof(long), typeof(long), typeof(int), BinaryExpressionHelper.CalcInt64_Int32),
                    new OperatorMethod(typeof(ulong), typeof(ulong), typeof(int), BinaryExpressionHelper.CalcUInt64_Int32)
                };
            _operatorMethods[">>"] = new[]
                {
                    new OperatorMethod(typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(uint), typeof(uint), typeof(int), BinaryExpressionHelper.CalcUInt32_Int32),
                    new OperatorMethod(typeof(long), typeof(long), typeof(int), BinaryExpressionHelper.CalcInt64_Int32),
                    new OperatorMethod(typeof(ulong), typeof(ulong), typeof(int), BinaryExpressionHelper.CalcUInt64_Int32)
                };
            _operatorMethods["=="] = new[]
                {
                    new OperatorMethod(typeof(bool), typeof(int), typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(bool), typeof(uint), typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(bool), typeof(long), typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(bool), typeof(ulong), typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(bool), typeof(float), typeof(float), BinaryExpressionHelper.CalcFloat),
                    new OperatorMethod(typeof(bool), typeof(double), typeof(double), BinaryExpressionHelper.CalcDouble),
                    new OperatorMethod(typeof(bool), typeof(decimal), typeof(decimal), BinaryExpressionHelper.CalcDecimal),
                    new OperatorMethod(typeof(bool), typeof(string), typeof(string), BinaryExpressionHelper.CalcString),
                    new OperatorMethod(typeof(bool), BinaryExpressionHelper.CalcBool)
                };
            _operatorMethods["!="] = new[]
                {
                    new OperatorMethod(typeof(bool), typeof(int), typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(bool), typeof(uint), typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(bool), typeof(long), typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(bool), typeof(ulong), typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(bool), typeof(float), typeof(float), BinaryExpressionHelper.CalcFloat),
                    new OperatorMethod(typeof(bool), typeof(double), typeof(double), BinaryExpressionHelper.CalcDouble),
                    new OperatorMethod(typeof(bool), typeof(decimal), typeof(decimal), BinaryExpressionHelper.CalcDecimal),
                    new OperatorMethod(typeof(bool), typeof(string), typeof(string), BinaryExpressionHelper.CalcString),
                    new OperatorMethod(typeof(bool), BinaryExpressionHelper.CalcBool)
                };
            _operatorMethods["<"] = new[]
                {
                    new OperatorMethod(typeof(bool), typeof(int), typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(bool), typeof(uint), typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(bool), typeof(long), typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(bool), typeof(ulong), typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(bool), typeof(float), typeof(float), BinaryExpressionHelper.CalcFloat),
                    new OperatorMethod(typeof(bool), typeof(double), typeof(double), BinaryExpressionHelper.CalcDouble),
                    new OperatorMethod(typeof(bool), typeof(decimal), typeof(decimal), BinaryExpressionHelper.CalcDecimal),
                };
            _operatorMethods[">"] = new[]
                {
                    new OperatorMethod(typeof(bool), typeof(int), typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(bool), typeof(uint), typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(bool), typeof(long), typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(bool), typeof(ulong), typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(bool), typeof(float), typeof(float), BinaryExpressionHelper.CalcFloat),
                    new OperatorMethod(typeof(bool), typeof(double), typeof(double), BinaryExpressionHelper.CalcDouble),
                    new OperatorMethod(typeof(bool), typeof(decimal), typeof(decimal), BinaryExpressionHelper.CalcDecimal),
                };
            _operatorMethods["<="] = new[]
                {
                    new OperatorMethod(typeof(bool), typeof(int), typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(bool), typeof(uint), typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(bool), typeof(long), typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(bool), typeof(ulong), typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(bool), typeof(float), typeof(float), BinaryExpressionHelper.CalcFloat),
                    new OperatorMethod(typeof(bool), typeof(double), typeof(double), BinaryExpressionHelper.CalcDouble),
                    new OperatorMethod(typeof(bool), typeof(decimal), typeof(decimal), BinaryExpressionHelper.CalcDecimal),
                };
            _operatorMethods[">="] = new[]
                {
                    new OperatorMethod(typeof(bool), typeof(int), typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(bool), typeof(uint), typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(bool), typeof(long), typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(bool), typeof(ulong), typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(bool), typeof(float), typeof(float), BinaryExpressionHelper.CalcFloat),
                    new OperatorMethod(typeof(bool), typeof(double), typeof(double), BinaryExpressionHelper.CalcDouble),
                    new OperatorMethod(typeof(bool), typeof(decimal), typeof(decimal), BinaryExpressionHelper.CalcDecimal),
                };
            _operatorMethods["&"] = new[]
                {
                    new OperatorMethod(typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(bool), BinaryExpressionHelper.CalcBool)
                };
            _operatorMethods["|"] = new[]
                {
                    new OperatorMethod(typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(bool), BinaryExpressionHelper.CalcBool)
                };
            _operatorMethods["^"] = new[]
                {
                    new OperatorMethod(typeof(int), BinaryExpressionHelper.CalcInt32),
                    new OperatorMethod(typeof(uint), BinaryExpressionHelper.CalcUInt32),
                    new OperatorMethod(typeof(long), BinaryExpressionHelper.CalcInt64),
                    new OperatorMethod(typeof(ulong), BinaryExpressionHelper.CalcUInt64),
                    new OperatorMethod(typeof(bool), BinaryExpressionHelper.CalcBool)
                };

            _operatorOverloadNames["+"] = "op_Addition";
            _operatorOverloadNames["-"] = "op_Subtraction";
            _operatorOverloadNames["<"] = "op_LessThan";
            _operatorOverloadNames["<="] = "op_LessThanOrEqual";
            _operatorOverloadNames[">"] = "op_GreaterThan";
            _operatorOverloadNames[">="] = "op_GreaterThanOrEqual";
            _operatorOverloadNames["=="] = "op_Equality";
            _operatorOverloadNames["!="] = "op_Inequality";
            
        }

        static readonly Dictionary<string,OperatorMethod[]> _operatorMethods = new Dictionary<string, OperatorMethod[]>();
        static readonly Dictionary<string,string> _operatorOverloadNames = new Dictionary<string, string>();

        public string Operator { get; }

        public override ValueExpression Evaluate(IParserContext context)
        {
            ValueExpression[] values = { Left.Evaluate(context), Right.Evaluate(context) };

            var type1 = values[0].Type;
            var type2 = values[1].Type;

            bool nullable1 = type1.Inspector().IsNullable;
            bool nullable2 = type2.Inspector().IsNullable;

            type1 = type1.Inspector().RealType;
            type2 = type2.Inspector().RealType;

            bool isNullable = (nullable1 || nullable2);

            OperatorMethod operatorMethod = FindOperatorMethod(type1, type2);

            if (operatorMethod == null)
            {
                MethodInfo customOperatorMethod = type1.Inspector().GetMethod(_operatorOverloadNames[Operator], new[] { type1, type2 });

                if (customOperatorMethod != null)
                {
                    return Exp.Value(customOperatorMethod.Invoke(null, new[] { values[0].Value, values[1].Value }), customOperatorMethod.ReturnType);
                }

                if (Operator == "==" || Operator == "!=")
                    return Exp.Value(BinaryExpressionHelper.CalcObject(Operator, values[0].Value, values[1].Value, context.StringComparison, this), typeof(bool));

                throw new IllegalOperandsException("Operator " + Operator + " is not supported on " + values[0] + " and " + values[1], this);
            }

            Type returnType = operatorMethod.ReturnType;

            if (isNullable)
            {
                returnType = typeof(Nullable<>).MakeGenericType(returnType);

                //TODO: check specs for bool? values

                if (values[0].Value == null || values[1].Value == null)
                    return Exp.Value(null, returnType);
            }

            object value1 = operatorMethod.Type1.Inspector().Cast(values[0].Value);
            object value2 = operatorMethod.Type2.Inspector().Cast(values[1].Value);

            return Exp.Value(operatorMethod.Function(Operator, value1, value2, context?.StringComparison ?? StringComparison.Ordinal, this), returnType);
        }

        private OperatorMethod FindOperatorMethod(Type type1, Type type2)
        {
            OperatorMethod[] operatorMethods = _operatorMethods[Operator];

            HashSet<string> promotableOperators = new HashSet<string>(new[] {"+", "-", "*", "/", "%", "&", "|", "^", "==", "!=", ">", "<", ">=", "<="});

            if (type1.Inspector().Is(TypeFlags.Numeric) && type2.Inspector().Is(TypeFlags.Numeric))
            {
                if (promotableOperators.Contains(Operator))
                {
                    Type promotionType;

                    if (type1 == typeof(decimal) || type2 == typeof(decimal))
                    {
                        if (type1 == typeof(float) || type1 == typeof(double) || type2 == typeof(float) ||
                            type2 == typeof(double))
                            return null;

                        promotionType = typeof(decimal);
                    }
                    else if (type1 == typeof(double) || type2 == typeof(double))
                        promotionType = typeof(double);
                    else if (type1 == typeof(float) || type2 == typeof(float))
                        promotionType = typeof(float);
                    else if (type1 == typeof(ulong) || type2 == typeof(ulong))
                        promotionType = typeof(ulong);
                    else if (type1 == typeof(long) || type2 == typeof(long))
                        promotionType = typeof(long);
                    else if (type1 == typeof(ulong) || type2 == typeof(ulong))
                    {
                        if (type1 == typeof(sbyte) || type1 == typeof(short) || type1 == typeof(int) ||
                            type1 == typeof(long) ||
                            type2 == typeof(sbyte) || type2 == typeof(short) || type2 == typeof(int) ||
                            type2 == typeof(long))
                            return null;

                        promotionType = typeof(ulong);
                    }
                    else if (type1 == typeof(long) || type2 == typeof(long))
                        promotionType = typeof(long);
                    else if ((type1 == typeof(uint) || type2 == typeof(uint)) &&
                             (type1 == typeof(sbyte) || type2 == typeof(sbyte) || type1 == typeof(short) ||
                              type2 == typeof(short) || type1 == typeof(int) || type2 == typeof(int)))
                        promotionType = typeof(long);
                    else if (type1 == typeof(uint) || type2 == typeof(uint))
                        promotionType = typeof(uint);
                    else if (type1 == typeof(uint) || type2 == typeof(uint))
                        promotionType = typeof(uint);
                    else
                        promotionType = typeof(int);

                    type1 = type2 = promotionType;
                }
                else if ((Operator == "<<" || Operator == ">>") && type1.Inspector().Is(TypeFlags.Integer))
                {
                    if (type1.Inspector().Is(TypeFlags.Integer16|TypeFlags.Integer8))
                        type1 = typeof(int);

                    type2 = typeof(int);
                }
            }

            foreach (OperatorMethod operatorMethod in operatorMethods)
            {
                bool sameType1 = type1 == operatorMethod.Type1;
                bool sameType2 = type2 == operatorMethod.Type2;

                bool canConvert1 = operatorMethod.Type1.Inspector().IsAssignableFrom(type1);
                bool canConvert2 = operatorMethod.Type2.Inspector().IsAssignableFrom(type2);

                if ((sameType1 || canConvert1) && (sameType2 || canConvert2))
                {
                    return operatorMethod;
                }
            }

            return null;
        }

#if DEBUG
        public override string ToString()
        {
            return $"({Left} {Operator} {Right})";
        }
#endif
    }
}