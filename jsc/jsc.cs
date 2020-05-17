using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpTree;

namespace jsc
{
    class jsc
    {
        public Block script;

        public jsc(string src)
        {
            //if (!src.StartsWith("#!stdlib"))
            //{
            //    Parser.Import(AppDomain.CurrentDomain.BaseDirectory + "/lib/stdlib.js");
            //}
            script = Parser.Parse(src);
        }

        public void Run()
        {
            script.loc = new dynamic[script.lcsize];
            script.Exec();
        }
    }

    class Ref
    {
        public dynamic value;
    }

    class List : List<dynamic>
    {
        public List()
        {

        }

        public List(IEnumerable<dynamic> collection) : base(collection)
        {
            
        }

        public List(int capacity)
        {
            for (int i = 0; i < capacity; i++)
                Add(null);
        }

        public static List operator +(List a, List b)
        {
            return new List(a.Concat(b));
        }
    }

    public class Dict : Dictionary<dynamic, dynamic>
    {
        
    }
}