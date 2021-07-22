namespace tomenglertde.Wax.Model.Tools
{
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    using JetBrains.Annotations;

    public static class SerializerExtensions
    {
        [NotNull]
        public static T Deserialize<T>(this string? data) where T : class, new()
        {
            if (string.IsNullOrEmpty(data))
                return new T();

            var serializer = new XmlSerializer(typeof(T));

            try
            {
                var xmlReader = new XmlTextReader(new StringReader(data));

                if (serializer.CanDeserialize(xmlReader))
                    return (serializer.Deserialize(xmlReader) as T) ?? new T();
            }
            catch
            {
                // file is corrupt
            }

            return new T();
        }

        [NotNull]
        public static string Serialize<T>([NotNull] this T value)
        {
            var result = new StringBuilder();

            var serializer = new XmlSerializer(typeof(T));

            using (var stringWriter = new StringWriter(result, CultureInfo.InvariantCulture))
            {
                serializer.Serialize(stringWriter, value);
            }

            return result.ToString();
        }
    }
}
