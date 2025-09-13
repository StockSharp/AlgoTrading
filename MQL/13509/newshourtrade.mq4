//+----------------------------------------------------------------------+
//| SheriffOnline.com. Forex EA and Script Developer                     |
//| -------------------------------------------------------------------- |
//| Keywords: MT4, Forex EA builder, create EA, expert advisor developer |
//+----------------------------------------------------------------------+
#property copyright "Copyright © 2015, sheriffonline@gmail.com"
#property link      "http://www.sheriffonline.com.com/"
#property version   "3.00"
#property description "NewsHourTrade EA works best with M5 Timeframe"
#property description "Recommended to Trade All Pairs with Low Spread"
#property description "Use only with High Impact News"
#property description "The EA Created By SheriffOnline.com"
#property strict
//---
#include <stdlib.mqh>
#include <WinUser32.mqh>
//--- exported variables
extern int StartHour = 1;           // Starting Hours
extern int StartMinute = 1;         // Starting Minute News Released time (-1 minute)
extern int DelaySeconds = 5;        // Default 5 seconds
extern int MagicNumber=786;         // MagicNumber
extern double Lots = 0.1;           // Lots Size
extern int StopLoss = 20;           // StopLoss
extern int TakeProfit = 50;         // Take Profit
extern int PriceGap = 10;           // Price Offset
extern int Expiration=60;           // Pending Order Expiry time in seconds. leave 0 for no expiry
extern bool TrailStop=false;        // Enable/Disable Trailing
extern int TrailingStop = 20;       // Trailing Stop Value in Pips
int NewTakeProfit=200;
extern int TrailingGap=10;        // Trailing Gap in Pips
extern bool BuyTrade=true;
extern bool SellTrade=true;
extern string Wishes="***To Your Success,Sheriff sheriffonline@gmail.com ***";
//--- local variables
double PipValue=1;    // this variable is here to support 5-digit brokers
bool Terminated= false;
string LF = "\n";  // use this in custom or utility blocks where you need line feeds
int NDigits = 4;   // used mostly for NormalizeDouble in Flex type blocks
int ObjCount = 0;  // count of all objects created on the chart, allows creation of objects with unique names
int current=0;
//---
int newsday=0;
int Today25 = -1;
int Count15 = 0;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
   NDigits=Digits;
   if(false) ObjectsDeleteAll();      // clear the chart
   Comment("");    // clear the chart
   return (0);
  }
//+------------------------------------------------------------------+
//| Expert start                                                     |
//+------------------------------------------------------------------+
int start()
  {
   if(Bars<10)
     {
      Comment("Not enough bars");
      return (0);
     }
   if(Terminated==true)
     {
      Comment("EA Terminated.");
      return (0);
     }
//---
   OnEveryTick9();
   return (0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnEveryTick9()
  {
   if(true==false && false) PipValue=10;
   if(true && (NDigits==3 || NDigits==5)) PipValue=10;
//---
   TrailOK();
   IfOrderExists32();
   IfOrderExists34();
   TechnicalAnalysis24();
   PrintInfoToChart();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TrailOK()
  {
   if(TrailStop==true)
     {
      TrailingStop();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TrailingStop()
  {
   for(int i=OrdersTotal()-1; i>=0; i--)
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
           {
            double takeprofit=OrderTakeProfit();

            if(OrderType()==OP_BUY && Ask-OrderOpenPrice()>TrailingStop*PipValue*Point)
              {
               if(OrderStopLoss()<Ask-(TrailingStop+TrailingGap)*PipValue*Point)
                 {
                  if(NewTakeProfit!=0) takeprofit=Ask+(NewTakeProfit+TrailingStop)*PipValue*Point;
                  bool ret1=OrderModify(OrderTicket(),OrderOpenPrice(),Ask-TrailingStop*PipValue*Point,takeprofit,OrderExpiration(),White);
                  if(ret1==false)
                     Print("OrderModify() error - ",ErrorDescription(GetLastError()));
                 }
              }
            if(OrderType()==OP_SELL && OrderOpenPrice()-Bid>TrailingStop*PipValue*Point)
              {
               if(OrderStopLoss()>Bid+(TrailingStop+TrailingGap)*PipValue*Point)
                 {
                  if(NewTakeProfit!=0) takeprofit=Bid-(NewTakeProfit+TrailingStop)*PipValue*Point;
                  bool ret2=OrderModify(OrderTicket(),OrderOpenPrice(),Bid+TrailingStop*PipValue*Point,takeprofit,OrderExpiration(),White);
                  if(ret2==false)
                     Print("OrderModify() error - ",ErrorDescription(GetLastError()));
                 }
              }
           }
        }
   else
      Print("OrderSelect() error - ",ErrorDescription(GetLastError()));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void IfOrderExists32()
  {
   bool exists=false;
   for(int i=OrdersTotal()-1; i>=0; i--)
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderType()==OP_BUY && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
           {
            exists=true;
           }
        }
   else
     {
      Print("OrderSelect() error - ",ErrorDescription(GetLastError()));
     }
//---
   if(exists)
     {
      DeletePendingOrder33();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DeletePendingOrder33()
  {
//---
   for(int i=OrdersTotal()-1; i>=0; i--)
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderType()==OP_SELLSTOP && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
           {
            bool ret=OrderDelete(OrderTicket(),Red);

            if(ret==false)
              {
               Print("OrderDelete() error - ",ErrorDescription(GetLastError()));
              }
           }
        }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void IfOrderExists34()
  {
   bool exists=false;
   for(int i=OrdersTotal()-1; i>=0; i--)
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderType()==OP_SELL && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
           {
            exists=true;
           }
        }
   else
     {
      Print("OrderSelect() error - ",ErrorDescription(GetLastError()));
     }
//---
   if(exists)
     {
      DeletePendingOrder35();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DeletePendingOrder35()
  {
//---
   for(int i=OrdersTotal()-1; i>=0; i--)
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderType()==OP_BUYSTOP && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
           {
            bool ret=OrderDelete(OrderTicket(),Red);

            if(ret==false)
              {
               Print("OrderDelete() error - ",ErrorDescription(GetLastError()));
              }
           }
        }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TechnicalAnalysis24()
  {
   if(newsday!=TimeDay(TimeCurrent()))
     {
      CertainTimeServer25();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CertainTimeServer25()
  {
   int datetime800=(int)TimeCurrent();
   int hour0=TimeHour(datetime800);
   int minute0=TimeMinute(datetime800);
   if(DayOfWeek()!=Today25 && hour0==StartHour && minute0==StartMinute)
     {
      Today25=DayOfWeek();
      CustomIf26();
      CustomIf22();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CustomIf26()
  {
   if(BuyTrade==true)
     {
      Sleep38();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Sleep38()
  {
   Sleep(DelaySeconds*1000);
   IfOrderDoesNotExist31();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void IfOrderDoesNotExist31()
  {
   bool exists=false;
   for(int i=OrdersTotal()-1; i>=0; i--)
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderType()==OP_BUYSTOP && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
           {
            exists=true;
           }
        }
   else
     {
      Print("OrderSelect() error - ",ErrorDescription(GetLastError()));
     }

   if(exists==false)
     {
      IfTradeContextNotBusy47();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void IfTradeContextNotBusy47()
  {
   if(!IsTradeContextBusy())
     {
      BuyPendingOrder48();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void BuyPendingOrder48()
  {
   int expire=(int)TimeCurrent()+60*Expiration;
   double price=NormalizeDouble(Ask,NDigits)+PriceGap*PipValue*Point;
   double SL=price-StopLoss*PipValue*Point;
   if(StopLoss==0) SL=0;
   double TP=price+TakeProfit*PipValue*Point;
   if(TakeProfit == 0) TP = 0;
   if(Expiration == 0) expire = 0;
   int ticket= OrderSend(Symbol(),OP_BUYSTOP,Lots,price,4,SL,TP,"NewsHourTrade",MagicNumber,expire,Blue);
   if(ticket == -1)
     {
      Print("OrderSend() error - ",ErrorDescription(GetLastError()));
     }
   CustomCode10();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CustomCode10()
  {
   newsday=TimeDay(TimeCurrent());
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CustomIf22()
  {
   if(SellTrade==true)
     {
      Sleep40();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Sleep40()
  {
   Sleep(DelaySeconds*1000);
   IfOrderDoesNotExist23();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void IfOrderDoesNotExist23()
  {
   bool exists=false;
   for(int i=OrdersTotal()-1; i>=0; i--)
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderType()==OP_SELLSTOP && OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
           {
            exists=true;
           }
        }
   else
     {
      Print("OrderSelect() error - ",ErrorDescription(GetLastError()));
     }
//---
   if(exists==false)
     {
      IfTradeContextNotBusy46();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void IfTradeContextNotBusy46()
  {
   if(!IsTradeContextBusy())
     {
      SellPendingOrder41();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SellPendingOrder41()
  {
   int expire=(int)TimeCurrent()+60*Expiration;
   double price=NormalizeDouble(Bid,NDigits)-PriceGap*PipValue*Point;
   double SL=price+StopLoss*PipValue*Point;
   if(StopLoss==0) SL=0;
   double TP=price-TakeProfit*PipValue*Point;
   if(TakeProfit == 0) TP = 0;
   if(Expiration == 0) expire = 0;
   int ticket= OrderSend(Symbol(),OP_SELLSTOP,Lots,price,4,SL,TP,"NewsHourTrade",MagicNumber,expire,Red);
   if(ticket == -1)
     {
      Print("OrderSend() error - ",ErrorDescription(GetLastError()));
     }
   CustomCode10();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void PrintInfoToChart()
  {
   string temp="News Hour Trade by SheriffOnline.com\n"
               +"Spread: "+DoubleToStr(MarketInfo(Symbol(),MODE_SPREAD)/PipValue,2)+"\n"
               +"------------------------------------------------\n"
               +"ACCOUNT INFORMATION:\n"
               +"\n"
               +"Account Name:     "+AccountName()+"\n"
               +"Account Leverage:     "+DoubleToStr(AccountLeverage(),0)+"\n"
               +"Account Balance:     "+DoubleToStr(AccountBalance(),2)+"\n"
               +"Account Equity:     "+DoubleToStr(AccountEquity(),2)+"\n"
               +"Free Margin:     "+DoubleToStr(AccountFreeMargin(),2)+"\n"
               +"Used Margin:     "+DoubleToStr(AccountMargin(),2)+"\n"
               +"Contact Author: sheriffonline@gmail.com\n"
               +"------------------------------------------------\n";
   Comment(temp);
   Count15++;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit()
  {
   if(false) ObjectsDeleteAll();
//---
   return (0);
  }
//+------------------------------------------------------------------+
