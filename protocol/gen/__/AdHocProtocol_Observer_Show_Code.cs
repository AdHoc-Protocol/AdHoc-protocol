
using System;
using org.unirail;
using org.unirail.collections;
namespace org.unirail
{
    namespace Agent
    {
        public static partial class AdHocProtocol
        {
            public partial class Observer_
            {
                public partial struct Show_Code : IEquatable<Show_Code>
                {

                    public int __id => __id_;
                    public const int __id_ = 11;

                    public uint Value = 0x0;
                    public Show_Code() { }
                    public Show_Code(uint src) => this.Value = src;

                    public ushort idx
                    {
                        get
                        {
                            var _inT = (Value & 0xFFFFU);
                            return (ushort)_inT;
                        }

                        set => this.Value = (uint)(Value & 0x7_0000UL | (ulong)(value));
                    }

                    public Agent.AdHocProtocol.Item.Type tYpe
                    {
                        get
                        {
                            var _inT = (Value >> 16 & 0x7U);
                            return (Agent.AdHocProtocol.Item.Type)_inT;
                        }

                        set => this.Value = (uint)(Value & 0xFFFFUL | (ulong)((byte)value) << 16);
                    }

                    public class Handler : AdHoc.Channel.Receiver.BytesDst, AdHoc.Channel.Transmitter.BytesSrc
                    {
                        public int __id => 11;
                        public static readonly Handler ONE = new();

                        bool AdHoc.Channel.Transmitter.BytesSrc.__get_bytes(AdHoc.Channel.Transmitter __dst)
                        {
                            var __slot = __dst.slot!;
                            ulong _bits;
                            switch (__slot.state)
                            {
                                case 0:
                                    if (__dst.put_val(__id_, 1, 1))
                                        goto case 1;
                                    return false;
                                case 1:
                                    return __dst.put_val(__dst.u8, 3, 2);
                                default: return true;
                            }
                        }

                        bool AdHoc.Channel.Receiver.BytesDst.__put_bytes(AdHoc.Channel.Receiver __src)
                        {
                            var __slot = __src.slot!;
                            switch (__slot.state)
                            {
                                case 0:
                                    return __src.try_get8(3, 1);

                                default:
                                    return true;
                            }
                        }
                    }

                    public bool Equals(Show_Code other) => Value == other.Value;

                    public static bool operator ==(Show_Code? a, Show_Code? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.Value == b!.Value.Value);
                    public static bool operator !=(Show_Code? a, Show_Code? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.Value != b!.Value.Value);

                    public override bool Equals(object? other) => other is Show_Code p && p.Value == Value;
                    public override int GetHashCode() => Value.GetHashCode();

                    public static implicit operator uint(Show_Code a) => a.Value;
                    public static implicit operator Show_Code(uint a) => new Show_Code(a);

                    public static implicit operator ulong(Show_Code a) => (ulong)(a.Value);
                    public static implicit operator Show_Code(ulong a) => new Show_Code((uint)a);

                    public struct Nullable : IEquatable<Nullable>
                    {
                        public Nullable() { }
                        public Nullable(uint value) => this.value = value;
                        public Nullable(Show_Code value) => Value = value;

                        public uint value = NULL;

                        public Show_Code Value
                        {
                            get => new Show_Code(value);
                            set => this.value = value.Value;
                        }
                        public override string ToString() => hasValue ? $"{{ Value: {Value}, value: {value} }}" : "has_no_Value";
                        public bool hasValue => value != NULL;
                        public void to_null() => value = NULL;

                        public const uint NULL = (uint)0x7_0000;

                        public bool Equals(Nullable other) => value == other.value;

                        public static bool operator ==(Nullable? a, Nullable? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.value == b!.Value.value);
                        public static bool operator !=(Nullable? a, Nullable? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.value != b!.Value.value);

                        public static bool operator ==(Nullable a, Show_Code b) => a.value == b;
                        public static bool operator !=(Nullable a, Show_Code b) => a.value != b;
                        public static bool operator ==(Show_Code a, Nullable b) => a == b.value;
                        public static bool operator !=(Show_Code a, Nullable b) => a != b.value;
                        public override bool Equals(object? other) => other is Nullable p && p.value == value;
                        public override int GetHashCode() => value.GetHashCode();
                        public static implicit operator uint(Nullable a) => a.value;
                        public static implicit operator Nullable(uint a) => new Nullable(a);
                        public static implicit operator Nullable(Show_Code a) => new Nullable(a);
                        public static implicit operator Nullable(uint? a) => a ?? NULL;
                    }
                }
            }
        }
    }
}
