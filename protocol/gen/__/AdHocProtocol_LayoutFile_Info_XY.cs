
using System;
using org.unirail;
using org.unirail.collections;
namespace org.unirail
{
    namespace Agent
    {
        public static partial class AdHocProtocol
        {
            public partial class LayoutFile_
            {
                public partial class Info
                {
                    public partial struct XY : IEquatable<XY>
                    {

                        public int __id => __id_;
                        public const int __id_ = -9;

                        public ulong Value = 0x0;
                        public XY() { }
                        public XY(ulong src) => this.Value = src;

                        public int x
                        {
                            get
                            {
                                var _inT = (Value & 0xFFFF_FFFFUL);
                                return (int)((long)_inT - 0x8000_0000L);
                            }

                            set => this.Value = (ulong)(Value & 0xFFFF_FFFF_0000_0000UL | (ulong)((uint)(value + 0x8000_0000L)));
                        }

                        public int y
                        {
                            get
                            {
                                var _inT = (Value >> 32 & 0xFFFF_FFFFUL);
                                return (int)((long)_inT - 0x8000_0000L);
                            }

                            set => this.Value = (ulong)(Value & 0xFFFF_FFFFUL | (ulong)((uint)(value + 0x8000_0000L)) << 32);
                        }

                        public class Handler : AdHoc.Channel.Receiver.BytesDst, AdHoc.Channel.Transmitter.BytesSrc
                        {
                            public int __id => -9;
                            public static readonly Handler ONE = new();

                            bool AdHoc.Channel.Transmitter.BytesSrc.__get_bytes(AdHoc.Channel.Transmitter __dst)
                            {
                                var __slot = __dst.slot!;
                                ulong _bits;
                                switch (__slot.state)
                                {
                                    case 0:
                                        throw new NotSupportedException();
                                    case 1:
                                        return __dst.put_val(__dst.u8, 8, 2);
                                    default:
                                        return true;
                                }
                            }

                            bool AdHoc.Channel.Receiver.BytesDst.__put_bytes(AdHoc.Channel.Receiver __src)
                            {
                                var __slot = __src.slot!;
                                switch (__slot.state)
                                {
                                    case 0:
                                        return __src.try_get8(8, 1);

                                    default:
                                        return true;
                                }
                            }
                        }

                        public bool Equals(XY other) => Value == other.Value;

                        public static bool operator ==(XY? a, XY? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.Value == b!.Value.Value);
                        public static bool operator !=(XY? a, XY? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.Value != b!.Value.Value);

                        public override bool Equals(object? other) => other is XY p && p.Value == Value;
                        public override int GetHashCode() => Value.GetHashCode();

                        public static implicit operator ulong(XY a) => a.Value;
                        public static implicit operator XY(ulong a) => new XY(a);
                    }
                }
            }
        }
    }
}
