using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.IO;
using MsgPack.Serialization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace MessagePackVSJil
{
    class Program
    {
        static void Main(string[] args)
        {
            start:
            Console.WriteLine();
            Console.WriteLine();
            Console.Write("生成Student对象，数量：");
            int count = 0;
            try
            {
                count = Convert.ToInt32(Console.ReadLine());
            }
            catch {
                goto start;
            }
            if (count > 1000000)
            {
                Console.WriteLine("太多了，最多只给100万");
                goto start;
            }

            List<Student> list = new List<Student>();
            for (var i = 0; i < count; i++)
            {
                list.Add(new Student
                {
                    Name = $"student{i}",
                    Age = 12,
                    //Brith = DateTime.Now
                });
            }
            //***************公平竞争修正****************************
            using (StringWriter initOut = new StringWriter())
            {
                Jil.JSON.Serialize(list[0], initOut); 
            }

            using (MemoryStream ms = new MemoryStream())
            {
                var serializer = MessagePackSerializer.Get<Student>();
                serializer.Pack(ms, list[0]);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize<Student>(ms, list[0]); 
            }

            //********************************************************

            var s1 = serialize(list, "Jil JSON", data =>
            {
                using (StringWriter output = new StringWriter())
                {
                    Jil.JSON.Serialize(data, output);
                    return ASCIIEncoding.Default.GetBytes(output.ToString());
                }
            });
            var s = ASCIIEncoding.Default.GetString(s1);
            
            deserialize("Jil JSON", s, (data) =>
            {
                using (StringReader input = new StringReader(s))
                {
                   return Jil.JSON.Deserialize<List<Student>>(input);  
                }
            });


            var s2 = serialize(list, "BinaryFormatter", data =>
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(ms, list);
                    return ms.ToArray();
                }
            });

            deserialize("BinaryFormatter", s2, (data) =>
            {
                using (MemoryStream ms = new MemoryStream(s2))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    return formatter.Deserialize(ms) as List<Student>;
                }
            });


            var s3 = serialize(list, "MessagePack", data =>
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    var serializer = MessagePackSerializer.Get<List<Student>>();
                    serializer.Pack(ms, list);
                    return ms.ToArray();
                }
            });

            deserialize("MessagePack", s3, (data) =>
            {
                using (MemoryStream ms = new MemoryStream(s3))
                {
                    var serializer = MessagePackSerializer.Get<List<Student>>();
                    return serializer.Unpack(ms); 
                }
            });

            var d4 = serialize(list, "ProtoBuf", data =>
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ProtoBuf.Serializer.Serialize<List<Student>>(ms, data);
                    return ms.ToArray();
                }
            });

            deserialize("ProtoBuf", d4,(data) =>
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                   return ProtoBuf.Serializer.Deserialize<List<Student>>(ms); 
                }
            });


            while (true)
            {
                Console.WriteLine(" Ctrl+C to quit.");
                goto start; 
            }
        }

        static byte[] serialize<T>(List<T> list,string name, Func<List<T>, byte[]> serializeFunc)
        {
            Console.WriteLine(); 
            var count = list.Count();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var bytes = serializeFunc(list);
            watch.Stop();
            var size = bytes.Length;
            //name
            Console.WriteLine($"{name}序列化{count}条记录(Student)，费时:{watch.ElapsedMilliseconds}毫秒, 大小{size / 1024}kb");
            return bytes;
        }

        static List<TDest> deserialize<TSource,TDest>(string name, TSource data,Func<TSource, List<TDest>> deserializeFunc)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var list = deserializeFunc(data);
            watch.Stop();
            var count = list.Count();
            //name
            Console.WriteLine($"{name}反序列化{count}条记录(Student)，费时:{watch.ElapsedMilliseconds}毫秒");
           
            return list;
        }
    }



    [Serializable]
    [ProtoBuf.ProtoContract]
    public class Student
    {
        [ProtoBuf.ProtoMember(1)]
        public String Name { set; get; }

        [ProtoBuf.ProtoMember(2)]
        public int Age { set; get; }

        [ProtoBuf.ProtoMember(3)]
        public DateTime? Brith { set; get; }
    }
}
