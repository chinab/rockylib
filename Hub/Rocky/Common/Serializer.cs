using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace System
{
    public static class Serializer
    {
        public static MemoryStream Serialize(object graph)
        {
            Contract.Requires(graph != null);

            var stream = new MemoryStream();
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(stream, graph);
            stream.Position = 0L;
            return stream;
        }

        public static void SerializeTo(object graph, Stream stream)
        {
            Contract.Requires(graph != null && stream != null);

            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(stream, graph);
        }

        public static object Deserialize(Stream stream)
        {
            Contract.Requires(stream != null);

            var binaryFormatter = new BinaryFormatter();
            return binaryFormatter.Deserialize(stream);
        }
    }
}