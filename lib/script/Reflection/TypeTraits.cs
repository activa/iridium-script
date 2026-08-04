#region License
//=============================================================================
// Iridium Script - .NET scripting and templating engine 
//
// Copyright (c) 2008-2026 Philippe Leybaert
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

namespace Iridium.Script;

internal class TypeTraits
{
    private readonly Type _realType;
    private readonly TypeTraits? _elementTypeTraits;

    public Type Type { get; }
    public Type RealType { get; }
    public TypeTraitFlags TraitFlags { get; }

    private const TypeTraitFlags TypeDesignator = (TypeTraitFlags)((1 << 19) - 1);

    private const TypeTraitFlags TypeModifier = TypeTraitFlags.CanBeNull | TypeTraitFlags.Nullable | TypeTraitFlags.ValueType |
                                           TypeTraitFlags.ElementCanBeNull | TypeTraitFlags.ElementNullable |
                                           TypeTraitFlags.ElementValueType;

    private static readonly Dictionary<Type, TypeTraitFlags> _typeflagsMap = new()
    {
        { typeof(Byte), TypeTraitFlags.Byte },
        { typeof(SByte), TypeTraitFlags.SByte },
        { typeof(Int16), TypeTraitFlags.Int16 },
        { typeof(UInt16), TypeTraitFlags.UInt16 },
        { typeof(Int32), TypeTraitFlags.Int32 },
        { typeof(UInt32), TypeTraitFlags.UInt32 },
        { typeof(Int64), TypeTraitFlags.Int64 },
        { typeof(UInt64), TypeTraitFlags.UInt64 },
        { typeof(Single), TypeTraitFlags.Single },
        { typeof(Double), TypeTraitFlags.Double },
        { typeof(Decimal), TypeTraitFlags.Decimal },
        { typeof(Boolean), TypeTraitFlags.Boolean },
        { typeof(Char), TypeTraitFlags.Char },
        { typeof(DateTime), TypeTraitFlags.DateTime },
        { typeof(TimeSpan), TypeTraitFlags.TimeSpan },
        { typeof(DateTimeOffset), TypeTraitFlags.DateTimeOffset },
        { typeof(String), TypeTraitFlags.String },
        { typeof(Guid), TypeTraitFlags.Guid }
    };


    public TypeTraits(Type type)
    {
        Type = type;
        RealType = Nullable.GetUnderlyingType(Type) ?? Type;

        _realType = RealType;

        if (type.IsArray)
            _elementTypeTraits = new TypeTraits(type.GetElementType()!);

        TraitFlags = BuildTypeFlags();
    }

    private TypeTraitFlags BuildTypeFlags()
    {
        _typeflagsMap.TryGetValue(RealType, out var flags);

        if (Type != RealType)
            flags |= TypeTraitFlags.Nullable | TypeTraitFlags.CanBeNull;

        if (_realType.IsValueType)
            flags |= TypeTraitFlags.ValueType;
        else
            flags |= TypeTraitFlags.CanBeNull;

        if (_realType.IsEnum)
        {
            if (_typeflagsMap.TryGetValue(Enum.GetUnderlyingType(RealType), out var enumTypeFlags))
                flags |= enumTypeFlags;

            flags |= TypeTraitFlags.Enum;
        }
        else if (Type.IsArray)
        {
            flags |= TypeTraitFlags.Array;

            if (_typeflagsMap.TryGetValue(Type.GetElementType(), out var arrayTypeFlags))
                flags |= arrayTypeFlags;

            if ((_elementTypeTraits!.TraitFlags & TypeTraitFlags.CanBeNull) != 0)
                flags |= TypeTraitFlags.ElementCanBeNull;
            if ((_elementTypeTraits.TraitFlags & TypeTraitFlags.Nullable) != 0)
                flags |= TypeTraitFlags.ElementNullable;
            if ((_elementTypeTraits.TraitFlags & TypeTraitFlags.ValueType) != 0)
                flags |= TypeTraitFlags.ElementValueType;
        }

        return flags;
    }

    public bool Is(TypeTraitFlags flags)
    {
        return (
            ((flags & TypeDesignator) == 0 || (TraitFlags & flags & TypeDesignator) != 0)
            && (((flags & (TypeDesignator | TypeTraitFlags.Array)) == 0) ||
                (flags & TypeTraitFlags.Array) == (TraitFlags & TypeTraitFlags.Array))
            && (flags & TraitFlags & TypeModifier) == (flags & TypeModifier)
        );
    }


}