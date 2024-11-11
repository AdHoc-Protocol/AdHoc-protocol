

//  MIT License
//
//  Copyright © 2020 Chikirev Sirguy, Unirail Group. All rights reserved.
//  For inquiries, please contact:  al8v5C6HU4UtqE9@gmail.com
//  GitHub Repository: https://github.com/AdHoc-Protocol
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to use,
//  copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
//  the Software, and to permit others to do so, under the following conditions:
//
//  1. The above copyright notice and this permission notice must be included in all
//     copies or substantial portions of the Software.
//
//  2. Users of the Software must provide a clear acknowledgment in their user
//     documentation or other materials that their solution includes or is based on
//     this Software. This acknowledgment should be prominent and easily visible,
//     and can be formatted as follows:
//     "This product includes software developed by Chikirev Sirguy and the Unirail Group
//     (https://github.com/AdHoc-Protocol)."
//
//  3. If you modify the Software and distribute it, you must include a prominent notice
//     stating that you have changed the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE, AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES, OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT, OR OTHERWISE, ARISING FROM,
//  OUT OF, OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

using System;
using System.Collections.Generic;
using Agent = org.unirail.Agent;
using AdHocProtocol = org.unirail.Agent.AdHocProtocol;

using org.unirail.collections;

namespace org.unirail
{
    public interface Context
    {
        const int VALUE_OF_NULL_KEY = int.MaxValue - 100;
        class Transmitter : AdHoc
        {
            public Dictionary<ulong, byte>.Enumerator _Dictionary_ulong_byte__Enumerator;
            public Dictionary<ulong, ushort>.Enumerator _Dictionary_ulong_ushort__Enumerator;
            public Dictionary<uint, ushort>.Enumerator _Dictionary_uint_ushort__Enumerator;
            public byte[]? _byte_arrayN;
            public Dictionary<ushort, byte>.Enumerator _Dictionary_ushort_byte__Enumerator;


            public void clear()
            {
                _byte_arrayN = null;
                #region > Clear custom code
                #endregion > Context.Transmitter.Clear.Code

            }
            #region > Custom code
            #endregion > Context.Transmitter.Code

            public class Slot
            {


                public void clear()
                {
                    #region > Clear custom code
                    #endregion > Context.Transmitter.Slot.Clear.Code

                }
                #region > Custom code
                #endregion > Context.Transmitter.Slot.Code

                public uint state;
                private readonly AdHoc.Transmitter dst;
                public Slot(AdHoc.Transmitter dst) { this.dst = dst; }
                #region 0
                public int index0;
                public int index_fix0;
                public int index_max0;

                public int index_max_0(int max0)
                {
                    index0 = 0;
                    return index_max0 = max0;
                }

                public bool next_index0() => ++index0 < index_max0;


                public bool next_index_fix0() => ++index0 < index_fix0;
                #endregion
                #region 1
                public int index1;
                public int index_fix1;
                public int index_max1;

                public int index_max_1(int max1)
                {
                    index1 = 0;
                    return index_max1 = max1;
                }

                public bool next_index1() => ++index1 < index_max1;


                public bool next_index_fix1() => ++index1 < index_fix1;
                #endregion
                #region 2
                public int index2;
                public int index_fix2;
                public int index_max2;

                public int index_max_2(int max2)
                {
                    index2 = 0;
                    return index_max2 = max2;
                }

                public bool next_index2() => ++index2 < index_max2;


                public bool next_index_fix2() => ++index2 < index_fix2;
                #endregion




                int tmp;
                private static int bytes4value(int value) => value < 0xFFFF ?
                                                                  value < 0xFF ?
                                                                      value == 0 ?
                                                                          0 :
                                                                          1 :
                                                                      2 :
                                                                  value < 0xFFFFFF ?
                                                                      3 :
                                                                      4;

                public bool no_null_values()
                {
                    if (tmp == 0) return true;
                    index_max_1(tmp);
                    return false;
                }

                public bool no_items(int items, int items_max)
                {
                    if (items == 0)
                    {
                        dst.put((byte)0);
                        return true;
                    }
                    if (items_max < items)
                    {
                        AdHoc.Receiver.error_handler_.error(dst, AdHoc.Receiver.OnError.OVERFLOW, new ArgumentOutOfRangeException("In no_items(uint items, uint items_max, uint next_field_case)     items_max < items : " + items_max + " < " + items));
                        index_max_1(items_max);
                    }
                    else index_max_1(items);

                    return false;
                }
                #region Key does not contain null

                public void put_info()
                {
                    var items = index_max1;
                    var items_bytes = bytes4value(items);
                    dst.put((byte)items_bytes);
                    dst.put_val((uint)items, items_bytes);
                }


                // Return `true` if leap to the new state is necessary.
                public bool put_info(int null_V_count)
                {
                    var KV_count = index_max1;
                    if (null_V_count == 0)
                    {
                        var KV_count_bytes = bytes4value(KV_count);

                        dst.put((byte)(KV_count_bytes));
                        dst.put_val((uint)KV_count, KV_count_bytes);

                        tmp = 0;
                        index_max_1(KV_count);
                        return false;
                    }
                    var null_V_count_bytes = bytes4value(null_V_count);

                    if (0 < (KV_count -= null_V_count))
                    { // has KV items

                        var KV_count_bytes = bytes4value(KV_count);
                        dst.put((byte)(null_V_count_bytes << 3 | KV_count_bytes));
                        dst.put_val((uint)KV_count, KV_count_bytes);
                        dst.put_val((uint)null_V_count, null_V_count_bytes);
                        tmp = null_V_count;//  preserve
                        index_max_1(KV_count);
                        return false;
                    }
                    dst.put((byte)(null_V_count_bytes << 3));
                    dst.put_val((uint)null_V_count, null_V_count_bytes);

                    index_max_1(null_V_count);
                    return true;
                }
                #endregion
                #region Key contains null

                // Return `true` if leap to the new state is necessary.
                public bool put_info(bool null_K_exists)
                {
                    var items = index_max1;
                    var null_key_bits = 0;
                    if (null_K_exists)
                    {
                        null_key_bits = 0b1000_0000;
                        if (--items == 0)
                        {
                            dst.put((byte)null_key_bits);
                            return true;
                        }
                    }

                    var items_bytes = bytes4value(items);
                    dst.put((byte)(null_key_bits | items_bytes));
                    dst.put_val((uint)items, items_bytes);
                    index_max_1(items);
                    return false;
                }

                public bool put_info__(uint KV_case, uint next_field_case)
                {
                    var items = index_max1;
                    var null_key_bits = 0b1100_0000;
                    if (--items == 0)
                    {
                        state = next_field_case;
                        dst.put((byte)null_key_bits);
                        return true;
                    }
                    state = KV_case;
                    var items_bytes = bytes4value(items);
                    dst.put((byte)(null_key_bits | items_bytes));
                    dst.put_val((uint)items, items_bytes);
                    index_max_1(items);
                    return false;
                }

                // Return `true` if leap to the new state is necessary.
                public void put_info__()
                {
                    var KV_count = index_max1;
                    var null_key_bits = 0b1100_0000;
                    if (--KV_count == 0)
                    {
                        dst.put((byte)null_key_bits);
                        index_max_1(0);
                    }
                    else
                    {
                        var KV_count_bytes = bytes4value(KV_count);
                        dst.put((byte)(null_key_bits | KV_count_bytes));
                        dst.put_val((uint)KV_count, KV_count_bytes);
                        index_max_1(KV_count);
                    }

                    index1 = VALUE_OF_NULL_KEY; //  indicates that sending the value of 'Map  (null)'
                }

                // Return `true` if leap to the new state is necessary.
                public bool put_info_(int null_V_count, uint V_array_case)
                {
                    var KV_count = index_max1;
                    var null_key_bits = 0b1100_0000;

                    if (--KV_count == 0)
                    {
                        dst.put((byte)null_key_bits);
                        index_max_1(0);
                        tmp = 0;
                        goto to_V_case;
                    }

                    if (null_V_count == 0)
                    {
                        var KV_count_bytes = bytes4value(KV_count);
                        dst.put((byte)(null_key_bits | KV_count_bytes));
                        dst.put_val((uint)KV_count, KV_count_bytes);
                        tmp = 0;
                        index_max_1(KV_count);
                        goto to_V_case;
                    }

                    var null_V_count_bytes = bytes4value(null_V_count);
                    if (0 < (KV_count -= null_V_count)) // has KV items
                    {
                        var KV_count_bytes = bytes4value(KV_count);
                        dst.put((byte)(null_key_bits | null_V_count_bytes << 3 | KV_count_bytes));
                        dst.put_val((uint)KV_count, KV_count_bytes);
                        dst.put_val((uint)null_V_count, null_V_count_bytes);
                        tmp = null_V_count; // keys with null values count preserve
                        index_max_1(KV_count);
                        goto to_V_case;
                    }

                    dst.put((byte)(null_key_bits | null_V_count_bytes << 3));
                    dst.put_val((uint)null_V_count, null_V_count_bytes);
                    tmp = null_V_count; // preserve
                    index_max_1(0);     // zero KV items
                to_V_case:
                    state = V_array_case;
                    index1 = VALUE_OF_NULL_KEY;
                    return true;
                }

                // Return `true` if leap to the new state is necessary.
                public bool put_info(int null_V_count,
                                     uint null_V_case,
                                     uint next_field_case) => put_info(null_V_count, null_V_case, next_field_case, 0b1000_0000);

                // Return `true` if leap to the new state is necessary.
                public bool put_info_(int null_V_count,
                                      uint KV_case,
                                      uint null_V_case,
                                      uint next_field_case)
                { state = KV_case; return put_info(null_V_count, null_V_case, next_field_case, 0b1100_0000); }

                // Return `true` if leap to the new state is necessary.
                private bool put_info(int null_V_count,
                                      uint null_V_case,
                                      uint next_field_case,
                                      uint null_key_bits)
                {
                    var KV_count = index_max1;
                    if (--KV_count == 0)
                    {
                        dst.put((byte)null_key_bits);
                        state = next_field_case;
                        tmp = 0;
                        return true;
                    }

                    if (null_V_count == 0)
                    {
                        var KV_count_bytes = bytes4value(KV_count);
                        dst.put((byte)(null_key_bits | KV_count_bytes));
                        dst.put_val((uint)KV_count, KV_count_bytes);
                        tmp = 0;
                        index_max_1(KV_count);
                        return false;
                    }

                    var null_V_count_bytes = bytes4value(null_V_count);
                    if (0 < (KV_count -= null_V_count)) // has KV items
                    {
                        var KV_count_bytes = bytes4value(KV_count);
                        dst.put((byte)(null_key_bits | null_V_count_bytes << 3 | KV_count_bytes));
                        dst.put_val((uint)KV_count, KV_count_bytes);
                        dst.put_val((uint)null_V_count, null_V_count_bytes);
                        tmp = null_V_count; // keys with null values count preserve
                        index_max_1(KV_count);
                        return false;
                    }

                    dst.put((byte)(null_key_bits | null_V_count_bytes << 3));
                    dst.put_val((uint)null_V_count, null_V_count_bytes);
                    index_max_1(null_V_count); // keys with null values count
                    state = null_V_case;
                    return true;
                }
                public bool leap_KV()
                {
                    if (index1 < VALUE_OF_NULL_KEY || index_max1 == 0) return false;
                    index1 = 0;
                    return true;
                }
                #endregion


            }
        }

        class Receiver : AdHoc
        {
            public ulong _ulong;


            public void clear()
            {
                #region > Clear custom code
                #endregion > Context.Receiver.Clear.Code

            }
            #region > Custom code
            #endregion > Context.Receiver.Code

            public class Slot
            {


                public void clear()
                {
                    #region > Clear custom code
                    #endregion > Context.Receiver.Slot.Clear.Code

                }
                #region > Custom code
                #endregion > Context.Receiver.Slot.Code

                private static int trailing8Zeros(uint i)
                {
                    var n = 7;
                    i <<= 24;
                    var y = i << 4;
                    if (y != 0)
                    {
                        n -= 4;
                        i = y;
                    }
                    y = i << 2;
                    return (int)(y == 0 ?
                                     n - (i << 1 >> 31) :
                                     n - 2 - (y << 1 >> 31));
                }

                public uint state;
                private readonly AdHoc.Receiver src;
                public Slot(AdHoc.Receiver src) { this.src = src; }
                #region 0
                public int index0;
                public int _index0;// Used to store the index of the most recent non-null value
                public int index_max0;
                public uint nulls0;

                public int index_max_0(int max0)
                {
                    index0 = 0;
                    return index_max0 = max0;
                }

                public bool next_index0() => ++index0 < index_max0;

                public bool next_index0(uint next_ok_case)
                {
                    if (++index0 < index_max0)
                    {
                        state = next_ok_case;
                        return true;
                    }
                    return false;
                }

                public bool null_at_index0 => (nulls0 & 1 << (int)(index0 & 7)) == 0;

                public void nulls_0(uint nulls, int index0)
                {

                    this.index0 = index0 + trailing8Zeros(nulls);
                    nulls0 = nulls;
                }

                public bool no_index0(uint no_index_case, int no_index_index0)
                {
                    if (0 < src.remaining) return false;
                    src.retry_at(no_index_case);
                    index0 = no_index_index0;
                    return true;
                }

                public bool find_exist0(int index0)
                {
                    var nulls = src.get_byte();
                    if (nulls == 0) return false;
                    this.index0 = index0 + trailing8Zeros(nulls);
                    nulls0 = nulls;
                    return true;
                }

                public bool get_len0(int max_items, int bytes, uint next_case)
                {
                    if (bytes == 0)
                    {
                        index_max_0(0);
                        return true;
                    }

                    this.max_items = max_items;
                    if (src.remaining < bytes)
                    {
                        src.retry_get4(bytes, next_case);
                        src.mode = LEN0;
                        return false;
                    }

                    check_len0(src.get4<int>(bytes));
                    return true;
                }

                public void check_len0(int len)
                {
                    if (max_items < index_max_0(len))
                        Receiver.error_handler.error(src, Receiver.OnError.OVERFLOW, new ArgumentOutOfRangeException("In get_len0  (uint max_items, uint bytes, uint next_case){}   max_items < index_max0 : " + max_items + " < " + index_max0));
                }
                #endregion
                #region 1
                public int index1;
                public int _index1;// Used to store the index of the most recent non-null value
                public int index_max1;
                public uint nulls1;

                public int index_max_1(int max1)
                {
                    index1 = 0;
                    return index_max1 = max1;
                }

                public bool next_index1() => ++index1 < index_max1;

                public bool next_index1(uint next_ok_case)
                {
                    if (++index1 < index_max1)
                    {
                        state = next_ok_case;
                        return true;
                    }
                    return false;
                }

                public bool null_at_index1 => (nulls1 & 1 << (int)(index1 & 7)) == 0;

                public void nulls_1(uint nulls, int index1)
                {

                    this.index1 = index1 + trailing8Zeros(nulls);
                    nulls1 = nulls;
                }

                public bool no_index1(uint no_index_case, int no_index_index1)
                {
                    if (0 < src.remaining) return false;
                    src.retry_at(no_index_case);
                    index1 = no_index_index1;
                    return true;
                }

                public bool find_exist1(int index1)
                {
                    var nulls = src.get_byte();
                    if (nulls == 0) return false;
                    this.index1 = index1 + trailing8Zeros(nulls);
                    nulls1 = nulls;
                    return true;
                }

                public bool get_len1(int max_items, int bytes, uint next_case)
                {
                    if (bytes == 0)
                    {
                        index_max_1(0);
                        return true;
                    }

                    this.max_items = max_items;
                    if (src.remaining < bytes)
                    {
                        src.retry_get4(bytes, next_case);
                        src.mode = LEN1;
                        return false;
                    }

                    check_len1(src.get4<int>(bytes));
                    return true;
                }

                public void check_len1(int len)
                {
                    if (max_items < index_max_1(len))
                        Receiver.error_handler.error(src, Receiver.OnError.OVERFLOW, new ArgumentOutOfRangeException("In get_len1  (uint max_items, uint bytes, uint next_case){}   max_items < index_max1 : " + max_items + " < " + index_max1));
                }
                #endregion
                #region 2
                public int index2;
                public int _index2;// Used to store the index of the most recent non-null value
                public int index_max2;
                public uint nulls2;

                public int index_max_2(int max2)
                {
                    index2 = 0;
                    return index_max2 = max2;
                }

                public bool next_index2() => ++index2 < index_max2;

                public bool next_index2(uint next_ok_case)
                {
                    if (++index2 < index_max2)
                    {
                        state = next_ok_case;
                        return true;
                    }
                    return false;
                }

                public bool null_at_index2 => (nulls2 & 1 << (int)(index2 & 7)) == 0;

                public void nulls_2(uint nulls, int index2)
                {

                    this.index2 = index2 + trailing8Zeros(nulls);
                    nulls2 = nulls;
                }

                public bool no_index2(uint no_index_case, int no_index_index2)
                {
                    if (0 < src.remaining) return false;
                    src.retry_at(no_index_case);
                    index2 = no_index_index2;
                    return true;
                }

                public bool find_exist2(int index2)
                {
                    var nulls = src.get_byte();
                    if (nulls == 0) return false;
                    this.index2 = index2 + trailing8Zeros(nulls);
                    nulls2 = nulls;
                    return true;
                }

                public bool get_len2(int max_items, int bytes, uint next_case)
                {
                    if (bytes == 0)
                    {
                        index_max_2(0);
                        return true;
                    }

                    this.max_items = max_items;
                    if (src.remaining < bytes)
                    {
                        src.retry_get4(bytes, next_case);
                        src.mode = LEN2;
                        return false;
                    }

                    check_len2(src.get4<int>(bytes));
                    return true;
                }

                public void check_len2(int len)
                {
                    if (max_items < index_max_2(len))
                        Receiver.error_handler.error(src, Receiver.OnError.OVERFLOW, new ArgumentOutOfRangeException("In get_len2  (uint max_items, uint bytes, uint next_case){}   max_items < index_max2 : " + max_items + " < " + index_max2));
                }
                #endregion



                int max_items;
                int tmp = 0;


                public bool try_get_info(uint the_case)
                {
                    tmp = 0;
                    if (0 < src.remaining)
                    {
                        src.u8 = src.get_byte();
                        return true;
                    }
                    src.retry_at(the_case);
                    return false;
                }

                public bool try_items_count(int max_items, uint next_case) => get_len1(max_items, (int)(src.u8 & 7), next_case);

                public bool try_null_values_count(int max_items, uint next_case)
                {
                    tmp = index_max1; // preserve total items count
                    return get_len1(max_items, (int)(src.u8 >> 3 & 7), next_case);
                }
                #region Key contains null

                public bool hasNullKey() => 0x7F < src.u8;

                public bool nullKeyHasValue() => 0xBF < src.u8;

                public bool nullKey_Value()
                {
                    if (index1 < VALUE_OF_NULL_KEY) return false;
                    index1 = 0;
                    return true;
                }

                public void receiving_value_of_null_key()
                {
                    index1 = VALUE_OF_NULL_KEY;
                    index_max2 = 0;
                }

                public void receiving_value_of_null_key_()
                {
                    var KV_items = tmp;
                    var null_V_items = index_max1;
                    tmp = null_V_items;
                    index_max_1(KV_items);
                    index1 = VALUE_OF_NULL_KEY;
                }
                #endregion

                public bool leap()
                {
                    var KV_items = index_max1;
                    if (0 < KV_items) return false;
                    return true;
                }

                public bool leap(uint null_V_case, uint next_field_case)
                {
                    var KV_items = tmp;
                    var null_V_items = index_max1;
                    if (KV_items == 0)
                    {
                        state = null_V_items == 0 ?
                                    next_field_case :
                                    null_V_case;
                        return true;
                    }

                    index_max_1(KV_items);
                    tmp = null_V_items;
                    return false;
                }

                public int items_count(int max_items)
                {
                    var items = tmp + index_max1 + (hasNullKey() ?
                                                                1 :
                                                                0);
                    if (max_items < items)
                    {
                        AdHoc.Receiver.error_handler.error(src, AdHoc.Receiver.OnError.OVERFLOW, new ArgumentOutOfRangeException("In items_count  (uint max_items){}   max_items < items : " + max_items + " < " + items));
                        return 0;
                    }
                    return items;
                }

                public bool no_null_values()
                {
                    if (tmp == 0) return true;
                    index_max_1(tmp);
                    return false;
                }



            }
        }
    }
}
