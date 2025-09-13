//+------------------------------------------------------------------+
//|                                                           su.mqh |
//|                        Copyright 2010, Alf.                      |
//|        http://forum.liteforex.org/showthread.php?p=6210#post6210 |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, Alf"
#property link      "http://forum.liteforex.org/showthread.php?p=6210#post6210"
//+------------------------------------------------------------------+
//| max_series_loss                                                  |
//+------------------------------------------------------------------+
double max_series_loss()
  {
   HistorySelect(0,TimeCurrent());
   int ser=0;
   double max=0;
   double o,c;
   long t;
   for(int i=0;i<HistoryOrdersTotal()-1;i=i+2)
     {
      o=HistoryOrderGetDouble(HistoryOrderGetTicket(i),ORDER_PRICE_OPEN);
      c=HistoryOrderGetDouble(HistoryOrderGetTicket(i+1),ORDER_PRICE_OPEN);
      t=HistoryOrderGetInteger(HistoryOrderGetTicket(i),ORDER_TYPE);

      if(t==ORDER_TYPE_BUY)
        {
         if(c-o>0)
           {
            if(ser>max)max=ser;
            ser=0;
           }
         else ser++;
        }
      else
        {
         if(c-o<0)
           {
            if(ser>max)max=ser;
            ser=0;
           }
         else ser++;
        }
     }
   return max;
  }
//+------------------------------------------------------------------+
//| profitc_divide_lossc                                             |
//+------------------------------------------------------------------+
double profitc_divide_lossc()
  {
   HistorySelect(0,TimeCurrent());
   double pr=0,ls=1;

   double o,c,p=0;
   long t;
   for(int i=0;i<HistoryOrdersTotal()-1;i=i+2)
     {
      o=HistoryOrderGetDouble(HistoryOrderGetTicket(i),ORDER_PRICE_OPEN);
      c=HistoryOrderGetDouble(HistoryOrderGetTicket(i+1),ORDER_PRICE_OPEN);
      t=HistoryOrderGetInteger(HistoryOrderGetTicket(i),ORDER_TYPE);

      if(t==ORDER_TYPE_BUY)
        {
         if(c-o>0)
           {
            pr=pr+1;
           }
         else ls=ls+1;
        }
      else
        {
         if(c-o<0)
           {
            pr=pr+1;
           }
         else ls=ls+1;
        }
     }
   p=pr/ls;

   return p;
  }
//+------------------------------------------------------------------+
