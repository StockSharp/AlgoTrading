//+------------------------------------------------------------------+
//|                                                     fibostop.mq4 |
//|                        Copyright 2014, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "dacontrader"
#property link      "http://www.mql5.com"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- create timer
   EventSetTimer(1);
   ObjectCreate("fibo",OBJ_LABEL,0, 0, 0);  // Creating obj.
   ObjectSet("fibo", OBJPROP_CORNER, 0);    // Reference corner
   ObjectSet("fibo", OBJPROP_XDISTANCE, 10*1);// X coordinate   
   ObjectSet("fibo", OBJPROP_YDISTANCE, 10*1);// Y coordinate


//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy timer
   EventKillTimer();

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---

   int total= OrdersTotal();
   for(int i=total-1;i>=0;i--)
     {
      OrderSelect(i,SELECT_BY_POS);
      int tipo=OrderType();
      double  stoploss=0;
      double fibo0=0;

      if(OrderSymbol()==Symbol())
        {
         bool result=false;
         stoploss=OrderStopLoss();
         double  pbuy=SymbolInfoDouble(OrderSymbol(),SYMBOL_BID)-(SymbolInfoInteger(OrderSymbol(),SYMBOL_TRADE_STOPS_LEVEL)*SymbolInfoDouble(OrderSymbol(),SYMBOL_POINT));
         double  psell=SymbolInfoDouble(OrderSymbol(),SYMBOL_ASK)+(SymbolInfoInteger(OrderSymbol(),SYMBOL_TRADE_STOPS_LEVEL)*SymbolInfoDouble(OrderSymbol(),SYMBOL_POINT));
         double  porden=OrderOpenPrice();
         double fibo=ObjectGetDouble(0,"fibostop",OBJPROP_PRICE,1)-ObjectGetDouble(0,"fibostop",OBJPROP_PRICE,0);
         fibo0=ObjectGetDouble(0,"fibostop",OBJPROP_PRICE,2);
         if(fibo0==0)
           {
            fibo=ObjectGetDouble(0,"fibostop",OBJPROP_PRICE,0)-ObjectGetDouble(0,"fibostop",OBJPROP_PRICE,1);
            fibo0=ObjectGetDouble(0,"fibostop",OBJPROP_PRICE,1);
           }

         double separacion=((-MathAbs(fibo)/fibo)*((SymbolInfoInteger(OrderSymbol(),SYMBOL_SPREAD)+10)*SymbolInfoDouble(OrderSymbol(),SYMBOL_POINT)));
         double fibo236=fibo0+(fibo*0.236);
         double fibo386=fibo0+(fibo*0.386);
         double fibo50=fibo0+(fibo*0.50);
         double fibo618=fibo0+(fibo*0.618);
         double fibo786=fibo0+(fibo*0.786);
         double fibo1=fibo0+(fibo*1);
         double fibo127=fibo0+(fibo*1.27);
         double ganancia=-OrderLots()*(porden-stoploss)*MarketInfo(Symbol(),MODE_TICKVALUE)/Point;
         double gananciap=100*ganancia/AccountInfoDouble(ACCOUNT_EQUITY);
         if(tipo==OP_SELL) ganancia=-ganancia;
         if(stoploss==0) stoploss=fibo0+3*separacion;

         if(tipo==OP_BUY && fibo0 < pbuy && stoploss < fibo0+2*separacion && fibo236 < SymbolInfoDouble(OrderSymbol(),SYMBOL_BID))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo0+separacion,OrderTakeProfit(),OrderExpiration(),White);
         if(tipo==OP_BUY && fibo236 < pbuy && stoploss < fibo236+2*separacion && fibo386 < SymbolInfoDouble(OrderSymbol(),SYMBOL_BID))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo236+separacion,OrderTakeProfit(),OrderExpiration(),White);
         if(tipo==OP_BUY && fibo386 < pbuy && stoploss < fibo386+2*separacion && fibo50 < SymbolInfoDouble(OrderSymbol(),SYMBOL_BID))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo386+separacion,OrderTakeProfit(),OrderExpiration(),White);
         if(tipo==OP_BUY && fibo50 < pbuy && stoploss < fibo50+2*separacion && fibo618 < SymbolInfoDouble(OrderSymbol(),SYMBOL_BID))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo50+separacion,OrderTakeProfit(),OrderExpiration(),White);
         if(tipo==OP_BUY && fibo618 < pbuy && stoploss < fibo618+2*separacion && fibo786 < SymbolInfoDouble(OrderSymbol(),SYMBOL_BID))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo618+separacion,OrderTakeProfit(),OrderExpiration(),White);
         if(tipo==OP_BUY && fibo786 < pbuy && stoploss < fibo786+2*separacion && fibo1 < SymbolInfoDouble(OrderSymbol(),SYMBOL_BID))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo786+separacion,OrderTakeProfit(),OrderExpiration(),White);
         if(tipo==OP_BUY && fibo1 < pbuy && stoploss < fibo1+2*separacion && fibo127 < SymbolInfoDouble(OrderSymbol(),SYMBOL_BID))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo1+separacion,OrderTakeProfit(),OrderExpiration(),White);


         if(tipo==OP_SELL && fibo0 > psell && stoploss > fibo0+3*separacion && fibo236 > SymbolInfoDouble(OrderSymbol(),SYMBOL_ASK))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo0+2*separacion,OrderTakeProfit(),OrderExpiration(),White);
         if(tipo==OP_SELL && fibo236 > psell && stoploss > fibo236+3*separacion && fibo386 > SymbolInfoDouble(OrderSymbol(),SYMBOL_ASK))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo236+2*separacion,OrderTakeProfit(),OrderExpiration(),White);
         if(tipo==OP_SELL && fibo386 > psell && stoploss > fibo386+3*separacion && fibo50 > SymbolInfoDouble(OrderSymbol(),SYMBOL_ASK))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo386+2*separacion,OrderTakeProfit(),OrderExpiration(),White);
         if(tipo==OP_SELL && fibo50 > psell && stoploss > fibo50+3*separacion && fibo618 > SymbolInfoDouble(OrderSymbol(),SYMBOL_ASK))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo50+2*separacion,OrderTakeProfit(),OrderExpiration(),White);
         if(tipo==OP_SELL && fibo618 > psell && stoploss > fibo618+3*separacion && fibo786 > SymbolInfoDouble(OrderSymbol(),SYMBOL_ASK))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo618+2*separacion,OrderTakeProfit(),OrderExpiration(),White);
         if(tipo==OP_SELL && fibo786 > psell && stoploss > fibo786+3*separacion && fibo1 > SymbolInfoDouble(OrderSymbol(),SYMBOL_ASK))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo786+2*separacion,OrderTakeProfit(),OrderExpiration(),White);
         if(tipo==OP_SELL && fibo1 > psell && stoploss > fibo1+3*separacion && fibo127 > SymbolInfoDouble(OrderSymbol(),SYMBOL_ASK))  OrderModify(OrderTicket(),OrderOpenPrice(),fibo1+2*separacion,OrderTakeProfit(),OrderExpiration(),White);

         ObjectSetText("fibo"," fibostop--> "+DoubleToStr(fibo0,5)+" stoploss -->$ "+DoubleToStr(ganancia,0)+"("+DoubleToStr(gananciap,2)+"%)",15*1,"Arial",White);
        }
     }
  }
//+------------------------------------------------------------------+
