using System.Collections.Generic;

namespace ResxWeb
{
    public class ResxKey
    {
        public ResxKey(string relativeFilePath, string key)
        {
            RelativeFilePath = relativeFilePath;
            Key = key;
            Values = new Dictionary<string, ResxValue>();
        }

        public string Key { get; private set; }
        public string RelativeFilePath { get; private set; }
        public IDictionary<string, ResxValue> Values { get; private set; }
    }

    public class ResxValue
    {
        public ResxValue(string value)
        {
            Value = value;
        }

        public string Value { get; set; }
        public bool HasChangedInDelta { get; set; }
    }
}