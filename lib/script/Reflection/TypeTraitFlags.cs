using System;

namespace Iridium.Script;

[Flags]
public enum TypeTraitFlags
{
    Byte = 1 << 0,
    SByte = 1 << 1,
    Int16 = 1 << 2,
    UInt16 = 1 << 3,
    Int32 = 1 << 4,
    UInt32 = 1 << 5,
    Int64 = 1 << 6,
    UInt64 = 1 << 7,
    Single = 1 << 8,
    Double = 1 << 9,
    Decimal = 1 << 10,
    Boolean = 1 << 11,
    Char = 1 << 12,
    Enum = 1 << 13,
    DateTime = 1 << 14,
    TimeSpan = 1 << 15,
    DateTimeOffset = 1 << 16,
    String = 1 << 17,
    Guid = 1 << 18,
    Array = 1 << 21,
    Nullable = 1 << 24,
    ElementNullable = 1 << 25,
    ValueType = 1 << 26,
    ElementValueType = 1 << 27,
    CanBeNull = 1 << 28,
    ElementCanBeNull = 1 << 29,
    Integer8 = Byte | SByte,
    Integer16 = Int16 | UInt16 | Char,
    Integer32 = Int32 | UInt32,
    Integer64 = Int64 | UInt64,
    SignedInteger = Char | SByte | Int16 | Int32 | Int64,
    UnsignedInteger = Byte | UInt16 | UInt32 | UInt64,
    FloatingPoint = Single | Double | Decimal,
    Integer = Integer8 | Integer16 | Integer32 | Integer64,
    Numeric = Integer | FloatingPoint,
    Primitive = Integer | Boolean | Char | FloatingPoint
}