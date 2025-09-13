//+------------------------------------------------------------------+
//|                                                        CRC32.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Zip, PNG, etc                                                    |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Calculate CRC32 in most simple way                               |
//+------------------------------------------------------------------+
class CRC32
{
private:
   static uint table[256]; // bit mask per byte
   
   static void initTable()
   {
      static bool tableCalculated = false;
      if(tableCalculated) return;
      tableCalculated = true;
       
      const uint polynomial = 0xEDB88320;
       
      // loop through all possible byte values
      for(int i = 0; i < 256; ++i)
      {
         uint byte = (uint)i;
         // loop through bits
         for(uchar b = 0; b < 8; ++b)
         {
            if((byte & 1) != 0)
            {
               byte = polynomial ^ (byte >> 1);
            }
            else
            {
               byte = (byte >> 1);
            }
         }
         table[i] = byte;
      }
   }

public:
   CRC32() { initTable(); }
   
   uint compute(uchar &bytes[], uint crc = 0xFFFFFFFF)
   {
      for(int i = 0; i < ArraySize(bytes); ++i)
      {
         uchar pos = (uchar)((crc ^ bytes[i]) & 0xFF);
         crc = (uint)((crc >> 8) ^ (uint)(table[pos]));
      }
   
      return crc;
   }
   
   static uint crc32(uchar &bytes[])
   {
      CRC32 temp;
      return temp.compute(bytes) ^ 0xFFFFFFFF;
   }
};

static uint CRC32::table[256];
//+------------------------------------------------------------------+
