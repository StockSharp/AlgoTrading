//+------------------------------------------------------------------+
//|                                             RoNz Auto SL n TP.mq4|
//|                                   Copyright 2014, Rony Nofrianto |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, Rony Nofrianto"
#property link      ""
#property version   "1.20"
#property strict
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_CHARTSYMBOL
  {
   CurrentChartSymbol=0,AllOpenOrder=1
  };

input int   TakeProfit=500;
input int   StopLoss=250;
input ENUM_CHARTSYMBOL  ChartSymbolSelection=AllOpenOrder;
//+------------------------------------------------------------------+
//| Hitung Posisi Terbuka                                            |
//+------------------------------------------------------------------+
int CalculateCurrentOrders()
  {
   int buys=0,sells=0;
//---
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(ChartSymbolSelection==CurrentChartSymbol && OrderSymbol()!=Symbol()) continue;
      if(OrderType()==OP_BUY)
        {
         buys++;
        }
      if(OrderType()==OP_SELL)
        {
         sells++;
        }
     }

   if(buys>0) return(buys);
   else       return(-sells);
//---
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool SetSLnTP()
  {
   double SL,TP;
   SL=TP=0.00;

   for(int i=0;i<OrdersTotal();i++)
      //+------------------------------------------------------------------+
      //|                                                                  |
      //+------------------------------------------------------------------+
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(ChartSymbolSelection==CurrentChartSymbol && OrderSymbol()!=Symbol()) continue;

      double point=MarketInfo(OrderSymbol(),MODE_POINT);

      //Print("Check SL & TP : ",OrderSymbol()," SL = ",OrderStopLoss()," TP = ",OrderTakeProfit());

      if(OrderType()==OP_BUY)
        {
         SL=NormalizeDouble(OrderOpenPrice()-(StopLoss*point),(int)MarketInfo(OrderSymbol(),MODE_DIGITS));
         TP=NormalizeDouble(OrderOpenPrice()+(TakeProfit*point),(int)MarketInfo(OrderSymbol(),MODE_DIGITS));

         if(OrderStopLoss()==0.0 && OrderTakeProfit()==0.0)
            bool res=OrderModify(OrderTicket(),OrderOpenPrice(),SL,TP,0,Blue);
         else if(OrderTakeProfit()==0.0)
            bool res=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),TP,0,Blue);
         else if(OrderStopLoss()==0.0)
            bool res=OrderModify(OrderTicket(),OrderOpenPrice(),SL,OrderTakeProfit(),0,Blue);
        }
      else if(OrderType()==OP_SELL)
        {
         SL=NormalizeDouble(OrderOpenPrice()+(StopLoss*point),(int)MarketInfo(OrderSymbol(),MODE_DIGITS));
         TP=NormalizeDouble(OrderOpenPrice()-(TakeProfit*point),(int)MarketInfo(OrderSymbol(),MODE_DIGITS));

         if(OrderStopLoss()==0.0 && OrderTakeProfit()==0.0)
            bool res=OrderModify(OrderTicket(),OrderOpenPrice(),SL,TP,0,Blue);
         else if(OrderTakeProfit()==0.0)
            bool res=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),TP,0,Blue);
         else if(OrderStopLoss()==0.0)
            bool res=OrderModify(OrderTicket(),OrderOpenPrice(),SL,OrderTakeProfit(),0,Blue);
        }
     }
   return false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(Bars<100 || IsTradeAllowed()==false)
      return;

   if(CalculateCurrentOrders()!=0)
      SetSLnTP();
  }

//+------------------------------------------------------------------+
