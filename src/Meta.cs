using System;

namespace org.unirail
{
    namespace Meta
    {
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
        public class MinMaxAttribute : Attribute
        {
            public MinMaxAttribute(long   Min, long   Max) { }
            public MinMaxAttribute(double Min, double Max) { }
        }

        // Field that value has up direction (higher) values dispersion gradient.
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
        public class AAttribute : Attribute
        {
            public AAttribute(long Min = 0, long Max = 0) { }
        }

        //Field that values has down direction (lower) dispersion gradient.
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
        public class VAttribute : Attribute
        {
            public VAttribute(long Max = 0, long Min = 0) { }
        }

        //Field that values have bi-direction dispersion gradient.
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
        public class XAttribute : Attribute
        {
            public XAttribute(uint Amplitude = 0, long Zero = 0) { }
        }

        //Multidimensional field.
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
        public class DimsAttribute : Attribute
        {
            public DimsAttribute(params int[] dims) { }
        }

        /**
         usage: 
         [A, MapValueParams, Dims(~65000)]
         Map<int, long> item_impl_hash_equal();
         
          [...ForKeyAtributes...., MapValueParams, ...ForValueAttributes...]
          attributes before MapValueParams are for key, and after for value 
         */
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
        public class MapValueParamsAttribute : Attribute { }


        //datatype that best fit to transmit binary data.
        //In Java it is   byte 
        //In C# it is     byte  
        public class Binary { }

        public interface Set<K> { }

        public interface Map<K, V> { }


        public interface Communication_Channel_Of<HostA_Port, HostB_Port>{ }


        public interface InCPP { };

        public interface InRS { };

        public interface InCS { };


        public interface InJAVA { };

        public interface InGO { };

        public interface InTS { };

        public interface All { };

        public interface Host { }; //mark Host struct vs Constants pack struct

        public interface _<T> { } //wrapper that let "import" non-interface-based entities (class/struct/enums)
    }
}