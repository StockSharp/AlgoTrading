//+------------------------------------------------------------------+
//|                                                 sentiment_ea.mq5 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "5.55"
#include <trade/trade.mqh>
#define CHART_TEXT_OBJECT_NAME   "chart-text"
input int      MinVolume=20000; //Мин. объем ордеров
input int      MinTraders=1000;//Мин. кол-во ордеров
input double   DiffVolumesEx=2.0;//Разница объемов ордеров
input double   DiffTradersEx=1.5;//Разница ордеров
input double   MinDiffVolumesEx=1.5;//Мин. разница объемов ордеров
input double   MinDiffTradersEx=1.3;//Мин. разница ордеров
input int   Sleep=5;//Задержка (мин.)
input double Lots=1;   //Лот
input int TpPips=500;   //Take Profit
input int SlPips=500;   //Stop Loss
input ENUM_ORDER_TYPE_FILLING Filling=ORDER_FILLING_RETURN;  //Режим заполнения ордера

bool   tradeResult=false;
bool tradeOpened=false;
double gTickSize,DiffVolumes,DiffTraders;
int pos=false;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   DiffVolumes=DiffVolumesEx;
   DiffTraders=DiffTradersEx;
   gTickSize=SymbolInfoDouble(_Symbol,SYMBOL_TRADE_TICK_SIZE);
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   ObjectDelete(0,CHART_TEXT_OBJECT_NAME);
  }
//+------------------------------------------------------------------+
//| Expert Check traiding time function                              |
//+------------------------------------------------------------------+
bool CheckTradingTime()
  {
   MqlDateTime local_time;
   TimeLocal(local_time);
   MqlDateTime tick_time;
   TimeTradeServer(tick_time);

//---  
   if(( tick_time.day_of_week==0) || (tick_time.day_of_week==6))
     {
      return( false );
     }

   if(( tick_time.hour>=0) && (tick_time.hour<10))
     {
      return( false );
     }

   uint trade_time=tick_time.hour*3600+tick_time.min*60+tick_time.sec;

   if((trade_time>=(10*3600+15*60+30)) && ( trade_time < ( 23 * 3600 + 35 * 60  + 30 ) )) 
     {
      return( true );
     }
   return( false );
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   Sleep(60000*Sleep);
//---
   long  BUYorders=SymbolInfoInteger(_Symbol,SYMBOL_SESSION_BUY_ORDERS);
   if(BUYorders==0) BUYorders=1; //for div on zero
   long  SELLorders=SymbolInfoInteger(_Symbol,SYMBOL_SESSION_SELL_ORDERS);
   if(SELLorders==0) SELLorders=1; //for div on zero
   double BUYvolume=SymbolInfoDouble(_Symbol,SYMBOL_SESSION_BUY_ORDERS_VOLUME);
   if(BUYvolume==0) BUYvolume=1; //for div on zero
   double SELLvolume=SymbolInfoDouble(_Symbol,SYMBOL_SESSION_SELL_ORDERS_VOLUME);
   if(SELLvolume==0) SELLvolume=1; //for div on zero
                                   //buy signal
   double DiffTradersCurr = double(BUYorders) / double(SELLorders);
   double DiffVolumesCurr = BUYvolume / SELLvolume;
   if(CheckTradingTime() && ((DiffVolumesCurr>=DiffVolumes && DiffTradersCurr>=DiffTraders)
      && (SymbolInfoInteger(_Symbol,SYMBOL_SESSION_BUY_ORDERS)>=MinTraders || SymbolInfoInteger(_Symbol,SYMBOL_SESSION_SELL_ORDERS)>=MinTraders)
      && (SymbolInfoDouble(_Symbol,SYMBOL_SESSION_BUY_ORDERS_VOLUME)>=MinVolume || SymbolInfoDouble(_Symbol,SYMBOL_SESSION_SELL_ORDERS_VOLUME)>=MinVolume)))
     {
      //open pos
      if(!tradeResult)
        {
         MqlTradeRequest request={0};
         MqlTradeResult result={0};
         double ask=NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK),_Digits);
         double bid=NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);
         double spread = MathAbs(ask-bid);
         request.action=TRADE_ACTION_DEAL;         // setting a pending order
         request.magic=68975;                      // ORDER_MAGIC
         request.symbol=_Symbol;                   // symbol
         request.volume=Lots;                      // volume in 0.1 lots
         request.sl=NormalizeDouble(ask - SlPips * gTickSize, _Digits);        // Stop Loss is not specified
         request.tp=NormalizeDouble(ask + TpPips * gTickSize, _Digits);        // Take Profit is not specified     
         request.type=ORDER_TYPE_BUY;              // order type
         request.price=ask;                        // open price
         request.type_filling=Filling;
         //--- send a trade request
         tradeResult=OrderSend(request,result);
         if(tradeResult)
           {
            pos=1;// BUY flag
            DiffVolumes=MinDiffVolumesEx;
            DiffTraders=MinDiffTradersEx;
           }
         else
            pos=false;
        }
      DisplayTextOnChart(CHART_TEXT_OBJECT_NAME,"Интерес: BUY",clrMidnightBlue);
     }
   else
     {
      //DisplayTextOnChart(CHART_TEXT_OBJECT_NAME,"Интерес: ---",clrLawnGreen);
      //sell signal
      DiffTradersCurr = double(SELLorders) / double(BUYorders);
      DiffVolumesCurr = SELLvolume / BUYvolume;
      if(CheckTradingTime() && ((DiffVolumesCurr>=DiffVolumes && DiffTradersCurr>=DiffTraders)
         && (SymbolInfoInteger(_Symbol,SYMBOL_SESSION_BUY_ORDERS)>=MinTraders || SymbolInfoInteger(_Symbol,SYMBOL_SESSION_SELL_ORDERS)>=MinTraders)
         && (SymbolInfoDouble(_Symbol,SYMBOL_SESSION_BUY_ORDERS_VOLUME)>=MinVolume || SymbolInfoDouble(_Symbol,SYMBOL_SESSION_SELL_ORDERS_VOLUME)>=MinVolume)))
        {
         //Open pos
         if(!tradeResult)
           {
            tradeOpened=true;
            MqlTradeRequest request={0};
            MqlTradeResult result={0};
            double bid=NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);
            request.action=TRADE_ACTION_DEAL;         // setting a pending order
            request.magic=68975;                      // ORDER_MAGIC
            request.symbol=_Symbol;                   // symbol
            request.volume=Lots;                      // volume in 0.1 lots
            request.sl=NormalizeDouble(bid + SlPips * gTickSize, _Digits);        // Stop Loss is not specified
            request.tp=NormalizeDouble(bid - TpPips * gTickSize, _Digits);        // Take Profit is not specified     
            request.type=ORDER_TYPE_SELL;             // order type
            request.price=bid;                        // open price
            request.type_filling=Filling;
            //--- send a trade request
            tradeResult=OrderSend(request,result);
            if(tradeResult)
              {
               pos=2;// SELL flag
               DiffVolumes=MinDiffVolumesEx;
               DiffTraders=MinDiffTradersEx;
              }
            else
               pos=false;
           }
         DisplayTextOnChart(CHART_TEXT_OBJECT_NAME,"Интерес: SELL",clrFireBrick);
        }
     }

   DiffTradersCurr=double(SELLorders)/double(BUYorders);
   DiffVolumesCurr=SELLvolume/BUYvolume;
   if((pos==2 && (DiffVolumesCurr<DiffVolumes || DiffTradersCurr<DiffTraders))
      || (pos==1 && (DiffVolumesCurr>1/DiffVolumes || DiffTradersCurr>1/DiffTraders)))
     {
      pos=false; //Close pos
     }

   if(pos==false || !CheckTradingTime()) //No positions
     {
      pos=false;
      tradeResult=false;
      DiffVolumes=DiffVolumesEx;
      DiffTraders=DiffTradersEx;
      //Close pos
      CTrade trade;
      trade.SetTypeFilling(Filling);
      trade.PositionClose(_Symbol);
      ObjectDelete(0,CHART_TEXT_OBJECT_NAME);
     }
  }
//+------------------------------------------------------------------+
//| BookEvent function                                               |
//+------------------------------------------------------------------+
void OnBookEvent(const string &symbol)
  {
//---
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DisplayTextOnChart(string objetName,string textToDisplay,int textColor,int xPos=10,int yPos=20)
  {
   if(ObjectFind(0,objetName)<0)
     {
      ObjectCreate(0,objetName,OBJ_LABEL,0,0,0);
     }
   ObjectSetInteger(0,objetName,OBJPROP_XDISTANCE,xPos);
   ObjectSetInteger(0,objetName,OBJPROP_YDISTANCE,yPos);
   ObjectSetString(0,objetName,OBJPROP_TEXT,textToDisplay);
   ObjectSetString(0,objetName,OBJPROP_FONT,"Verdana");
   ObjectSetInteger(0,objetName,OBJPROP_COLOR,textColor);
   ObjectSetInteger(0,objetName,OBJPROP_FONTSIZE,10);
   ObjectSetInteger(0,objetName,OBJPROP_SELECTABLE,false);
   ChartRedraw(0);
  }
//+------------------------------------------------------------------+
