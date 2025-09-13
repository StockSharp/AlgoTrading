//+------------------------------------------------------------------+
//|                                        TradeTransactionRelay.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2021, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Intercept OnTradeTransaction events"
                      " and expose most important MqlTradeResult fields"
                      " in the indicator buffer addressable by request_id."

#property indicator_chart_window
#property indicator_buffers 1
#property indicator_plots   1
#property indicator_type1   DRAW_LINE
#property indicator_color1  DodgerBlue

#include <MQL5Book/ConverterT.mqh>

#define FIELD_NUM 6

double Buffer[];
Converter<ulong,double> cnv;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   SetIndexBuffer(0, Buffer, INDICATOR_DATA);
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   return rates_total;
}
//+------------------------------------------------------------------+
//| Trade transactions handler                                       |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction &transaction,
   const MqlTradeRequest &request,
   const MqlTradeResult &result)
{
   if(transaction.type == TRADE_TRANSACTION_REQUEST)
   {
      ArraySetAsSeries(Buffer, true);
      
      // save FIELD_NUM result fields in successive elements of the buffer
      const int offset = (int)((result.request_id * FIELD_NUM)
         % (Bars(_Symbol, _Period) / FIELD_NUM * FIELD_NUM));
      Buffer[offset + 1] = result.retcode;
      Buffer[offset + 2] = cnv[result.deal];
      Buffer[offset + 3] = cnv[result.order];
      Buffer[offset + 4] = result.volume;
      Buffer[offset + 5] = result.price;
      Buffer[offset + 0] = result.request_id; // this assignment must go last! it's a trigger of the 'ready' state
   }
}
//+------------------------------------------------------------------+
