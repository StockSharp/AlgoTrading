//+------------------------------------------------------------------+
//|                                                    Reservoir.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Use resources as universal storage (local clipboard) for         |
//| applied data (builtin types and simple structs)                  |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Arbitrary data type conversion from/to byte array                |
//+------------------------------------------------------------------+
template<typename T>
union ByteOverlay
{
   uchar buffer[sizeof(T)];
   T value;
   
   ByteOverlay(const T &v)
   {
      value = v;
   }
   
   ByteOverlay(const uchar &bytes[], const int offset = 0)
   {
      ArrayCopy(buffer, bytes, 0, offset, sizeof(T));
   }
};

//+------------------------------------------------------------------+
//| Save/Restore data to/from resource                               |
//+------------------------------------------------------------------+
class Reservoir
{
   uint storage[];
   int offset;
   
public:
   Reservoir(): offset(0) { }
   
   template<typename T>
   int packArray(const T &data[])
   {
      const int bytesize = ArraySize(data) * sizeof(T); // TODO: check overflow
      uchar buffer[];
      ArrayResize(buffer, bytesize);
      for(int i = 0; i < ArraySize(data); ++i)
      {
         ByteOverlay<T> overlay(data[i]);
         ArrayCopy(buffer, overlay.buffer, i * sizeof(T));
      }
      
      const int size = bytesize / sizeof(uint) + (bool)(bytesize % sizeof(uint));
      ArrayResize(storage, offset + size + 1);
      storage[offset] = bytesize;
      for(int i = 0; i < size; ++i)
      {
         ByteOverlay<uint> word(buffer, i * sizeof(uint));
         storage[offset + i + 1] = word.value;
      }
      
      offset = ArraySize(storage);
      
      return offset;
   }
   
   int packString(const string text)
   {
      uchar data[];
      StringToCharArray(text, data, 0, -1, CP_UTF8);
      return packArray(data);
   }
   
   template<typename T>
   int packNumber(const T &number)
   {
      T array[1];
      array[0] = number;
      return packArray(array);
   }

   template<typename T>
   int packNumber(const T number)
   {
      T array[1] = {number};
      return packArray(array);
   }
   
   template<typename T>
   int unpackArray(T &output[])
   {
      if(offset >= ArraySize(storage)) return 0; // out of bounds
      const int bytesize = (int)storage[offset];
      if(bytesize <= 0) return 0;
      if(bytesize % sizeof(T) != 0) return 0;    // inconsistent data type
      if(bytesize > (ArraySize(storage) - offset) * sizeof(uint)) return 0;
      
      uchar buffer[];
      ArrayResize(buffer, bytesize);
      for(int i = 0, k = 0; i < ArraySize(storage) - 1 - offset && k < bytesize; ++i, k += sizeof(uint))
      {
         ByteOverlay<uint> word(storage[i + 1 + offset]);
         ArrayCopy(buffer, word.buffer, k);
      }
      
      int n = bytesize / sizeof(T);
      n = ArrayResize(output, n);
      for(int i = 0; i < n; ++i)
      {
         ByteOverlay<T> overlay(buffer, i * sizeof(T));
         output[i] = overlay.value;
      }
      
      offset += 1 + bytesize / sizeof(uint) + (bool)(bytesize % sizeof(uint));
      
      return offset;
   }
   
   int unpackString(string &output)
   {
      uchar bytes[];
      const int p = unpackArray(bytes);
      if(p == offset)
      {
         output = CharArrayToString(bytes, 0, -1, CP_UTF8);
      }
      return p;
   }

   template<typename T>
   int unpackNumber(T &number)
   {
      T array[1] = {};
      const int p = unpackArray(array);
      number = array[0];
      return p;
   }
   
   bool submit(const string resource)
   {
      return ResourceCreate(resource, storage, ArraySize(storage), 1,
         0, 0, 0, COLOR_FORMAT_XRGB_NOALPHA);
   }
   
   bool acquire(const string resource)
   {
      if(ArraySize(storage) || offset) return false; // not empty already
      
      uint width, height;
      if(ResourceReadImage(resource, storage, width, height))
      {
         return true;
      }
      return false;
   }
   
   int size() const
   {
      return ArraySize(storage);
   }
   
   int cursor() const
   {
      return offset;
   }
   
   void clear()
   {
      ArrayFree(storage);
      offset = 0;
   }
   
   void rewind()
   {
      offset = 0;
   }
};
//+------------------------------------------------------------------+
