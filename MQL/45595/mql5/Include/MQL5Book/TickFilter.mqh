//+------------------------------------------------------------------+
//|                                                   TickFilter.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Several methods of prunning ticks for faster optimization and    |
//| lower resource consumption                                       |
//+------------------------------------------------------------------+
class TickFilter
{
public:
   enum FILTER_MODE
   {
      NONE,
      SEQUENCE,
      FLUTTER,
      FRACTALS,
   };
   
   static int filter(FILTER_MODE mode, MqlTick &data[])
   {
      switch(mode)
      {
      case SEQUENCE: return filterBySequences(data);
      case FLUTTER: return filterBySpreadFlutter(data);
      case FRACTALS: return filterByFractals(data);
      }
      return ArraySize(data);
   }

   static int filterBySequences(MqlTick &data[])
   {
      const int size = ArraySize(data);
      if(size < 3) return size;
      
      int index = 2;
      bool dirUp = data[1].bid - data[0].bid + data[1].ask - data[0].ask > 0;
      
      for(int i = 2; i < size; i++)
      {
         if(dirUp)
         {
            if(data[i].bid - data[i - 1].bid + data[i].ask - data[i - 1].ask < 0)
            {
               dirUp = false;
               data[index++] = data[i];
            }
         }
         else
         {
            if(data[i].bid - data[i - 1].bid + data[i].ask - data[i - 1].ask > 0)
            {
               dirUp = true;
               data[index++] = data[i];
            }
         }
      }
      return ArrayResize(data, index);
   }
    
   static int filterBySpreadFlutter(MqlTick &data[])
   {
      const int size = ArraySize(data);
      if(size < 3) return size;

      bool dirUp = true;
      int index = 0;
      double priceMinMax = -DBL_MAX;
  
      for(int i = 0; i < size; i++)
      {
         const double bid = data[i].bid;
         const double ask = data[i].ask;
  
         if(dirUp)
         {
            if(bid > priceMinMax)
            {
               priceMinMax = bid;
               data[index++] = data[i];
            }
            else if(ask <= priceMinMax)
            {
               priceMinMax = ask;
               dirUp = false;
               data[index++] = data[i];
            }
         }
         else
         {
            if(ask < priceMinMax)
            {
               priceMinMax = ask;
               data[index++] = data[i];
             }
             else if(bid >= priceMinMax)
             {
               priceMinMax = bid;
               dirUp = true;
               data[index++] = data[i];
            }
         }
      }
  
      return ArrayResize(data, index);
   }
   
   static int filterByFractals(MqlTick &data[])
   {
      int index = 1;
      const int size = ArraySize(data);
      if(size < 3) return size;

      for(int i = 1; i < size - 2; i++)
      {
         if((data[i].bid < data[i - 1].bid && data[i].bid < data[i + 1].bid)
         || (data[i].ask > data[i - 1].ask && data[i].ask > data[i + 1].ask))
         {
            data[index++] = data[i];
         }
      }
      
      return ArrayResize(data, index);
   }
};
//+------------------------------------------------------------------+
/*
   Example outputs:
   
   Create new custom symbol 'EURUSD.TckFltr-SEQUENCE'?
   Done ticks - read: 37682157, written: 19896830, ratio: 52.8%

*/
//+------------------------------------------------------------------+
