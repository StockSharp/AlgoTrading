//+------------------------------------------------------------------+
//|                                                       grr-al.mq5 |
//|                                                     Igor Volodin |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Igor Volodin"
#property version   "1.00"
#property link      "http://www.mql5.com"
//+------------------------------------------------------------------+
#define MAGIC_NUMBER 12937
#define DEV 20
#define RISK 0.0
#define BASELOT 0.1
#define SL 100
#define TP 700
#define DELTA 30
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   EventSetTimer(1);
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   EventKillTimer();
  }
//+------------------------------------------------------------------+
//| Expert timer function                                            |
//+------------------------------------------------------------------+
void OnTimer()
  {
   MqlTick tick;
   MqlTradeRequest request;
   MqlTradeResult tradeResult;
   MqlTradeCheckResult checkResult;
   ZeroMemory(request);

   static double oldtick=0.0;
   static double a1=0;
   double point=SymbolInfoDouble(Symbol(),SYMBOL_POINT);
   static bool br=false;
   if(SymbolInfoTick(Symbol(),tick))
     {
      datetime lastbar=(datetime)SeriesInfoInteger(Symbol(),0,SERIES_LASTBAR_DATE);
      if(tick.bid!=oldtick)
        {
         oldtick=tick.bid;
         if(isNewBar())
           {
            a1= tick.bid;
            br= false;
           }
         else
           {
            if(!br)
              {
               if(tick.bid-a1>DELTA*point)
                 {
                  br=true;
                  request.price=tick.bid;
                  request.sl = tick.ask+SL*point;
                  request.tp = tick.bid-TP*point;
                  request.type=ORDER_TYPE_SELL;
                 }
               else if(a1-tick.bid>DELTA*point)
                 {
                  br=true;
                  request.price=tick.ask;
                  request.sl = tick.bid-SL*point;
                  request.tp = tick.ask+TP*point;
                  request.type=ORDER_TYPE_BUY;
                 }
               if(br)
                 {
                  request.action       = TRADE_ACTION_DEAL;
                  request.symbol       = Symbol();
                  request.volume       = getLot();
                  request.deviation    = DEV;
                  request.type_filling = ORDER_FILLING_FOK;
                  request.type_time    = ORDER_TIME_GTC;
                  request.comment      = "";
                  request.magic        = MAGIC_NUMBER;
                  if(OrderCheck(request,checkResult))
                    {
                     OrderSend(request,tradeResult);
                    }
                  else
                    {
                     Print("Error: ",checkResult.retcode);
                    }
                 }
              }
           }
        }
     }

  }
//+------------------------------------------------------------------+
//| Checking of a new bar                                            |
//+------------------------------------------------------------------+
bool isNewBar()
  {
   static datetime lastTime=0;
   datetime lastbarTime=(datetime)SeriesInfoInteger(Symbol(),0,SERIES_LASTBAR_DATE);
   if(lastTime==0)
     {
      lastTime=lastbarTime;
      return(false);
     }
   if(lastTime!=lastbarTime)
     {
      lastTime=lastbarTime;
      return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//| Get trade volume                                                 |
//+------------------------------------------------------------------+
double getLot()
  {
   if(RISK==0) return(BASELOT);
   double required;
   if(RISK==0) return(BASELOT);
   double max_lot=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   OrderCalcMargin(ORDER_TYPE_BUY,Symbol(),1,SymbolInfoDouble(Symbol(),SYMBOL_ASK),required);
   double maximal_lot=(AccountInfoDouble(ACCOUNT_FREEMARGIN)*0.9/required);
   double lot=maximal_lot*RISK;
   lot=MathMin(lot,max_lot);
   return(NormalizeDouble(lot,2));
  }
//+------------------------------------------------------------------+	
