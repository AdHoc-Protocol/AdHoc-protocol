
using System;
using org.unirail;
using org.unirail.collections;
namespace org.unirail
{
    namespace Agent
    {
        public static partial class AdHocProtocol
        {
            public partial class Agent_
            {
                public partial struct Version : IEquatable<Version>
                {

                    public int __id => __id_;
                    public const int __id_ = 2;

                    public uint uid = 0;

                    public Version() { }
                    public Version(uint src) => this.uid = src;

                    public class Handler : AdHoc.Channel.Receiver.BytesDst, AdHoc.Channel.Transmitter.BytesSrc
                    {
                        public int __id => 2;
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
                                    return __dst.put((uint)__dst.u8, 123);

                                default: return true;
                            }
                        }

                        bool AdHoc.Channel.Receiver.BytesDst.__put_bytes(AdHoc.Channel.Receiver __src)
                        {
                            var __slot = __src.slot!;
                            switch (__slot.state)
                            {
                                case 0:
                                    return __src.get_uint_u8(123);

                                default:
                                    return true;
                            }
                        }
                    }

                    public bool Equals(Version other) => uid == other.uid;

                    public static bool operator ==(Version? a, Version? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.uid == b!.Value.uid);
                    public static bool operator !=(Version? a, Version? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.uid != b!.Value.uid);

                    public override bool Equals(object? other) => other is Version p && p.uid == uid;
                    public override int GetHashCode() => uid.GetHashCode();

                    public static implicit operator uint(Version a) => a.uid;
                    public static implicit operator Version(uint a) => new Version(a);

                    public static implicit operator ulong(Version a) => (ulong)(a.uid);
                    public static implicit operator Version(ulong a) => new Version((uint)a);

                    public struct Nullable : IEquatable<Nullable>
                    {
                        public Nullable() { }
                        public Nullable(ulong value) => this.value = value;
                        public Nullable(Version value) => Value = value;

                        public ulong value = NULL;

                        public Version Value
                        {
                            get => new Version((uint)(value));
                            set => this.value = (ulong)value.uid;
                        }
                        public override string ToString() => hasValue ? $"{{ Value: {Value}, value: {value} }}" : "has_no_Value";
                        public bool hasValue => value != NULL;
                        public void to_null() => value = NULL;

                        public const ulong NULL = (ulong)0x1_0000_0000;

                        public bool Equals(Nullable other) => value == other.value;

                        public static bool operator ==(Nullable? a, Nullable? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.value == b!.Value.value);
                        public static bool operator !=(Nullable? a, Nullable? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.value != b!.Value.value);

                        public static bool operator ==(Nullable a, Version b) => a.value == (ulong)b.uid;
                        public static bool operator !=(Nullable a, Version b) => a.value != (ulong)b.uid;
                        public static bool operator ==(Version a, Nullable b) => (ulong)a.uid == b.value;
                        public static bool operator !=(Version a, Nullable b) => (ulong)a.uid != b.value;
                        public override bool Equals(object? other) => other is Nullable p && p.value == value;
                        public override int GetHashCode() => value.GetHashCode();
                        public static implicit operator ulong(Nullable a) => a.value;
                        public static implicit operator Nullable(ulong a) => new Nullable(a);
                        public static implicit operator Nullable(Version a) => new Nullable(a);
                    }
                }
            }
        }
    }
}
