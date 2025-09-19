
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
                    public partial class View : IEquatable<View>, AdHoc.Channel.Receiver.BytesDst, AdHoc.Channel.Transmitter.BytesSrc
                    {

                        public int __id => __id_;
                        public const int __id_ = -8;
                        #region x

                        public int x { get; set; } = 0;
                        #endregion
                        #region y

                        public int y { get; set; } = 0;
                        #endregion
                        #region w

                        public int w { get; set; } = 0;
                        #endregion
                        #region h

                        public int h { get; set; } = 0;
                        #endregion
                        #region panX

                        public int panX { get; set; } = 0;
                        #endregion
                        #region panY

                        public int panY { get; set; } = 0;
                        #endregion
                        #region zoom

                        public float zoom { get; set; } = 0; //The zoom level for this view.
                        #endregion
                        #region hue

                        public ushort hue { get; set; } = 0;
                        #endregion

                        public int GetHashCode
                        {
                            get
                            {
                                var _hash = 3001003L;
                                #region x
                                _hash = HashCode.Combine(_hash, x);
                                #endregion
                                #region y
                                _hash = HashCode.Combine(_hash, y);
                                #endregion
                                #region w
                                _hash = HashCode.Combine(_hash, w);
                                #endregion
                                #region h
                                _hash = HashCode.Combine(_hash, h);
                                #endregion
                                #region panX
                                _hash = HashCode.Combine(_hash, panX);
                                #endregion
                                #region panY
                                _hash = HashCode.Combine(_hash, panY);
                                #endregion
                                #region zoom
                                _hash = HashCode.Combine(_hash, zoom);
                                #endregion
                                #region hue
                                _hash = HashCode.Combine(_hash, hue);
                                #endregion

                                return (int)_hash;
                            }
                        }
                        bool IEquatable<View>.Equals(View? _pack)
                        {
                            if (_pack == null)
                                return false;
                            bool __t;
                            #region x
                            if (x != _pack.x)
                                return false;
                            #endregion
                            #region y
                            if (y != _pack.y)
                                return false;
                            #endregion
                            #region w
                            if (w != _pack.w)
                                return false;
                            #endregion
                            #region h
                            if (h != _pack.h)
                                return false;
                            #endregion
                            #region panX
                            if (panX != _pack.panX)
                                return false;
                            #endregion
                            #region panY
                            if (panY != _pack.panY)
                                return false;
                            #endregion
                            #region zoom
                            if (zoom != _pack.zoom)
                                return false;
                            #endregion
                            #region hue
                            if (hue != _pack.hue)
                                return false;
                            #endregion

                            return true;
                        }

                        bool AdHoc.Channel.Transmitter.BytesSrc.__get_bytes(AdHoc.Channel.Transmitter __dst)
                        {
                            var __slot = __dst.slot!;
                            int __i = 0, __t = 0, __v = 0;
                            ulong __value = 0;
                            for (; ; )
                                switch (__slot.state)
                                {
                                    case 0:
                                        throw new NotSupportedException();
                                    case 1:

                                        if (!__dst.Allocate(30, 1))
                                            return false;
                                        __dst.put((int)x);
                                        __dst.put((int)y);
                                        __dst.put((int)w);
                                        __dst.put((int)h);
                                        __dst.put((int)panX);
                                        __dst.put((int)panY);
                                        __dst.put(zoom);
                                        __dst.put((ushort)hue);

                                        goto case 2;
                                    case 2:

                                    default:
                                        return true;
                                }
                        }

                        bool AdHoc.Channel.Receiver.BytesDst.__put_bytes(AdHoc.Channel.Receiver __src)
                        {
                            var __slot = __src.slot!;
                            int __i = 0, __t = 0, __v = 0;
                            ulong __value = 0;
                            for (; ; )
                                switch (__slot.state)
                                {
                                    case 0:

                                        if (!__src.has_4bytes(1))
                                            return false;
                                        x = (int)__src.get_int();
                                        goto case 2; //leap
                                    case 1:
                                        x = (int)__src.get_int_();
                                        goto case 2;
                                    case 2:

                                        if (!__src.has_4bytes(3))
                                            return false;
                                        y = (int)__src.get_int();
                                        goto case 4; //leap
                                    case 3:
                                        y = (int)__src.get_int_();
                                        goto case 4;
                                    case 4:

                                        if (!__src.has_4bytes(5))
                                            return false;
                                        w = (int)__src.get_int();
                                        goto case 6; //leap
                                    case 5:
                                        w = (int)__src.get_int_();
                                        goto case 6;
                                    case 6:

                                        if (!__src.has_4bytes(7))
                                            return false;
                                        h = (int)__src.get_int();
                                        goto case 8; //leap
                                    case 7:
                                        h = (int)__src.get_int_();
                                        goto case 8;
                                    case 8:

                                        if (!__src.has_4bytes(9))
                                            return false;
                                        panX = (int)__src.get_int();
                                        goto case 10; //leap
                                    case 9:
                                        panX = (int)__src.get_int_();
                                        goto case 10;
                                    case 10:

                                        if (!__src.has_4bytes(11))
                                            return false;
                                        panY = (int)__src.get_int();
                                        goto case 12; //leap
                                    case 11:
                                        panY = (int)__src.get_int_();
                                        goto case 12;
                                    case 12:

                                        if (!__src.has_4bytes(13))
                                            return false;
                                        zoom = (float)__src.get_float();
                                        goto case 14; //leap
                                    case 13:
                                        zoom = (float)__src.get_float_();
                                        goto case 14;
                                    case 14:

                                        if (!__src.has_2bytes(15))
                                            return false;
                                        hue = (ushort)__src.get_ushort();
                                        goto case 16; //leap
                                    case 15:
                                        hue = (ushort)__src.get_ushort_();
                                        goto case 16;
                                    case 16:

                                        return true;
                                    default: return true;
                                }
                        }
                    }
                }
            }
        }
    }
}
