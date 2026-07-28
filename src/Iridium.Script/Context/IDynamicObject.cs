using System;

namespace Iridium.Script;

public interface IDynamicObject
{
    bool IsArray { get; }
    bool IsValue { get; }
    bool IsObject { get; }
    bool TryGetValue(out object value, out Type type);
    bool TryGetValue(string key, out object value, out Type type);
    bool TryGetValue(int index, out object value, out Type type);
}