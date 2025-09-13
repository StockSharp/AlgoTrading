//+------------------------------------------------------------------+
//|                                              Trading Manager.mq4 |
//|                                               Copyright MQL BLUE |
//|                                           http://www.mqlblue.com |
//+------------------------------------------------------------------+
#property copyright "Copyright MQL BLUE"
#property link      "https://www.mqlblue.com"
#property version   "6.00"
#property strict

//---- definitions
#define Version                           "6.00"

#include <stderror.mqh>
#include <stdlib.mqh>

// **** EA PARAMS *****************
input  string         SLTPParams        = "--- SL & TP -------------------";
input  int            StopLoss          = 20;
input  int            TakeProfit        = 40;
input  string         BEParams          = "--- BreakEven -----------------";
input  bool           UseBreakEven      = true;
input  int            BEActivation      = 50;
input  int            BELevel           = 10;
input  string         TS1Params         = "--- Trailing Stop -------------";
input  bool           UseTrailingStop   = true;
input  int            TSStart           = 10;
input  int            TSStep            = 10;
input  int            TSDistance        = 15;
input  bool           TSEndBE           = false;
input  string         Behavior          = "--- EA Behavior ---------------";
input  bool           StealthMode       = false;
extern bool           OnlyCurrentPair   = true;

string         ShortName               = "XP Trade Manager";
string         ObjectSignature         = "";
long           chartID                 = 0;
const string nameProfitPips = "profitPips";  
const string nameProfitCurrency = "profitCurrency";

int ordersHistoryCount=0;

struct HistoryStat
{
   double profitPips;
   double profitCurrency;
};

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   ShortName = WindowExpertName();
   ObjectSignature = "["+ShortName+"]";
   Print ("Init "+ShortName+" ver. "+Version);         

      
   if (StealthMode) OnlyCurrentPair = true;
   chartID = ChartID();
   ObjectCreate(chartID, nameProfitPips, OBJ_LABEL,0,0,0,0);
   ObjectSetInteger(chartID, nameProfitPips, OBJPROP_XDISTANCE, 5);
   ObjectSetInteger(chartID, nameProfitPips, OBJPROP_YDISTANCE, 20);
   ObjectSetInteger(chartID, nameProfitPips, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetString(chartID, nameProfitPips, OBJPROP_TEXT, "Profit pips = 0");

   ObjectCreate(chartID, nameProfitCurrency, OBJ_LABEL,0,0,0,0);
   ObjectSetInteger(chartID, nameProfitCurrency, OBJPROP_XDISTANCE, 5);
   ObjectSetInteger(chartID, nameProfitCurrency, OBJPROP_YDISTANCE, 35);
   ObjectSetInteger(chartID, nameProfitCurrency, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetString(chartID, nameProfitCurrency, OBJPROP_TEXT, "Profit currency = 0,00 "+AccountCurrency());

   ordersHistoryCount=0;
//---
   return(INIT_SUCCEEDED);
   
  }
  
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   ObjectDelete(chartID, nameProfitPips);
   ObjectDelete(chartID, nameProfitCurrency);
  }
  
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
//--- 

   RefreshRates();
   for (int i=OrdersTotal()-1; i>=0; i--)
   {
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
      {
         if (!OnlyCurrentPair || OrderSymbol()==_Symbol)
         {
            double point = MarketInfo(OrderSymbol(), MODE_POINT);
            int digits = (int)MarketInfo(OrderSymbol(), MODE_DIGITS);
            if (digits==3 || digits==5) point *=10;
            double slCurrentLevel = 0;
            double tpCurrentLevel = 0;
            bool closed = false;
            double bid = MarketInfo(OrderSymbol(), MODE_BID);
            double ask = MarketInfo(OrderSymbol(), MODE_ASK);
            if (StealthMode)
            {
               slCurrentLevel = ObjectGet(ObjectSignature+"sl"+IntegerToString(OrderTicket()), OBJPROP_PRICE1);
               tpCurrentLevel = ObjectGet(ObjectSignature+"tp"+IntegerToString(OrderTicket()), OBJPROP_PRICE1);
               if (OrderType()==OP_BUY)
               {
                  if (tpCurrentLevel > 0)
                  {
                     if (bid >= tpCurrentLevel)
                     {
                        closed = CloseOrder(OrderTicket());
                     }
                  }
                  if (slCurrentLevel > 0)
                  {
                     if (bid <= slCurrentLevel) closed = CloseOrder(OrderTicket());
                  } 
               }
               else if (OrderType() == OP_SELL)
               {
                  if (tpCurrentLevel > 0)
                  {
                     if (ask <= tpCurrentLevel) closed = CloseOrder(OrderTicket());
                  } 
                  if (slCurrentLevel > 0)
                  {
                     if (ask >= slCurrentLevel) closed = CloseOrder(OrderTicket());
                  }
               }
               if (closed)
               {
                  ObjectDelete(ObjectSignature+"sl"+IntegerToString(OrderTicket()));
                  ObjectDelete(ObjectSignature+"tp"+IntegerToString(OrderTicket()));
               }
            }
            if (!closed)
            {
               // **************** check SL & TP *********************
               if ( (StopLoss > 0 && ((!StealthMode && OrderStopLoss() == 0)  || (StealthMode && slCurrentLevel==0))) || 
               (TakeProfit > 0 && ((!StealthMode && OrderTakeProfit() == 0) || (StealthMode &&  tpCurrentLevel==0)))
               )
               {
                  double stopLevel = MarketInfo(OrderSymbol(), MODE_STOPLEVEL)*point;
                  double distTP = MathMax(TakeProfit*point, stopLevel);
                  double distSL = MathMax(StopLoss*point, stopLevel);
                  double takeProfit = 0;
                  double stopLoss = 0;
                  if (MathMod(OrderType(),2))
                  {
                     if (TakeProfit > 0) takeProfit = OrderOpenPrice() + distTP;
                     if (StopLoss > 0) stopLoss = OrderOpenPrice() - distSL;
                  }
                  else
                  {
                     if (TakeProfit > 0) takeProfit = OrderOpenPrice() - distTP;
                     if (StopLoss > 0) stopLoss = OrderOpenPrice() + distSL;
                  }
                  
                  takeProfit = normPrice(takeProfit);
                  stopLoss = normPrice(stopLoss);
                  if (!StealthMode)
                  {
                     
                     if (!OrderModify(OrderTicket(), OrderOpenPrice(), NormalizeDouble(stopLoss, digits), NormalizeDouble(takeProfit, digits), OrderExpiration()))
                        Print(ShortName +" (OrderModify Error) "+ ErrorDescription(GetLastError())); 
                  }
                  else
                  {
                     SetLevel(ObjectSignature+"sl"+IntegerToString(OrderTicket()), NormalizeDouble(stopLoss,digits), clrRed, STYLE_DASH, 1);
                     SetLevel(ObjectSignature+"tp"+IntegerToString(OrderTicket()), NormalizeDouble(takeProfit,digits), clrGreen, STYLE_DASH,1);
                  }
               }
               // ****** CHECK Trailing Stop && Break Even **************************
               if (UseTrailingStop)
               {
                  int priceDistance = 0;
                  int multi = 0;
                  double stopLoss=0;
                  double be;
                  if (OrderType() == OP_BUY)
                  {
                     priceDistance = (int)(NormalizeDouble(bid - OrderOpenPrice(), digits)/point);
                     if (priceDistance > TSStart)
                     {
                        multi = (int)MathFloor(priceDistance / TSStep);
                        if (multi > 0)
                        {
                           stopLoss = NormalizeDouble(OrderOpenPrice() + TSStep*multi*point, digits) - TSDistance*point;
                           if (TSEndBE)
                           {
                              be = NormalizeDouble(OrderOpenPrice() + BELevel*point, digits);
                              if (stopLoss > be) stopLoss = be;
                           }
                        }
                        stopLoss = normPrice(stopLoss);
                        if (!StealthMode)
                        {
                           if (NormalizeDouble(OrderStopLoss(), digits) < NormalizeDouble(stopLoss, digits) || OrderStopLoss()==0)
                           {
                              if (!OrderModify(OrderTicket(), OrderOpenPrice(), NormalizeDouble(stopLoss, digits), OrderTakeProfit(), OrderExpiration()))
                                 Print(ShortName +" (OrderModify Error) "+ ErrorDescription(GetLastError())); 
                           }
                        }
                        else
                        {
                           if (NormalizeDouble(slCurrentLevel, digits) < NormalizeDouble(stopLoss, digits) || slCurrentLevel==0) SetLevel(ObjectSignature+"sl"+IntegerToString(OrderTicket()), NormalizeDouble(stopLoss,digits), clrRed, STYLE_DASH, 1);
                        }
                     }
                  }
                  else if (OrderType() == OP_SELL)
                  {
                     priceDistance = (int)(NormalizeDouble(OrderOpenPrice() - ask, digits)/point);
                     if (priceDistance > TSStart)
                     {
                        multi = (int)MathFloor(priceDistance / TSStep);
                        if (multi > 0)
                        {
                           stopLoss = NormalizeDouble(OrderOpenPrice() - TSStep*multi*point, digits) + TSDistance*point;
                        }
                        if (TSEndBE)
                        {
                           be = NormalizeDouble(OrderOpenPrice() - BELevel*point, digits);
                           if (stopLoss < be) stopLoss = be;
                        }       
                        stopLoss = normPrice(stopLoss); 
                        if (!StealthMode)
                        {                
                           if (NormalizeDouble(OrderStopLoss(), digits) > NormalizeDouble(stopLoss, digits) || OrderStopLoss()==0)
                           {
                              if (!OrderModify(OrderTicket(), OrderOpenPrice(), NormalizeDouble(stopLoss, digits), OrderTakeProfit(), OrderExpiration()))
                                 Print(ShortName +" (OrderModify Error) "+ ErrorDescription(GetLastError())); 
                           }                        
                        }
                        else
                        {
                           if (NormalizeDouble(slCurrentLevel, digits) > NormalizeDouble(stopLoss, digits) || slCurrentLevel==0) SetLevel(ObjectSignature+"sl"+IntegerToString(OrderTicket()), NormalizeDouble(stopLoss,digits), clrRed, STYLE_DASH, 1);
                        }
                     }
                  }
               }
               else if (UseBreakEven)
               {
                  int slDistance = 0;
                  int priceDistance = 0;
                  double stopLoss=0;
                  if (OrderType() == OP_BUY)
                  {
                     slDistance = (int)(NormalizeDouble(OrderStopLoss() - OrderOpenPrice(),digits)/point);
                     if (slDistance < BELevel)
                     {
                        priceDistance = (int)(NormalizeDouble(bid - OrderOpenPrice(), digits)/point);
                        if (priceDistance >= BEActivation)
                        {
                           stopLoss = OrderOpenPrice() + BELevel*point;
                           stopLoss = normPrice(stopLoss);
                           if (!StealthMode)
                           {
                              if(NormalizeDouble(OrderStopLoss(), digits)<NormalizeDouble(stopLoss, digits) || OrderStopLoss()==0){
                                 if (!OrderModify(OrderTicket(), OrderOpenPrice(), NormalizeDouble(stopLoss, digits), OrderTakeProfit(), OrderExpiration()))
                                    Print(ShortName +" (OrderModify Error) "+ ErrorDescription(GetLastError())); 
                              }
                           }
                           else
                           {
                              if (NormalizeDouble(slCurrentLevel, digits) < NormalizeDouble(stopLoss, digits) || slCurrentLevel==0) SetLevel(ObjectSignature+"sl"+IntegerToString(OrderTicket()), NormalizeDouble(stopLoss,digits), clrRed, STYLE_DASH, 1);                                                                         
                           }
                        }
                     }
                  }
                  else if (OrderType() == OP_SELL)
                  {
                     slDistance = (int)(NormalizeDouble(OrderOpenPrice()-OrderStopLoss(), digits)/point);
                     if (slDistance < BELevel)
                     {
                        priceDistance = (int)(NormalizeDouble(OrderOpenPrice() - ask, digits)/point);
                        if (priceDistance >= BEActivation)
                        {
                           stopLoss = OrderOpenPrice() - BELevel*point;
                           stopLoss = normPrice(stopLoss);
                           if (!StealthMode)
                           {
                              if(NormalizeDouble(OrderStopLoss(), digits)>NormalizeDouble(stopLoss, digits) || OrderStopLoss()==0){
                                 if (!OrderModify(OrderTicket(), OrderOpenPrice(), NormalizeDouble(stopLoss, digits), OrderTakeProfit(), OrderExpiration()))
                                    Print(ShortName +" (OrderModify Error) "+ ErrorDescription(GetLastError())); 
                              }
                           }
                           else
                           {
                             if (NormalizeDouble(slCurrentLevel, digits) > NormalizeDouble(stopLoss, digits) || slCurrentLevel==0) SetLevel(ObjectSignature+"sl"+IntegerToString(OrderTicket()), NormalizeDouble(stopLoss,digits), clrRed, STYLE_DASH, 1);                           
                           }
                        }
                     }                  
                  }
               }
            }
         }
      }  
   }
   
   
   if (OrdersHistoryTotal() != ordersHistoryCount)
   {
      // Refresh pips counter
      HistoryStat histStat = {0,0};
      ReadHistory(histStat);
         
      ObjectSetString(chartID, nameProfitPips, OBJPROP_TEXT, "Profit pips = "+DoubleToStr(histStat.profitPips,0));   
      ObjectSetString(chartID, nameProfitCurrency, OBJPROP_TEXT, "Profit currency = "+DoubleToStr(histStat.profitCurrency,2)+" "+AccountCurrency());

      ordersHistoryCount = OrdersHistoryTotal();
   }
   
  }
  
//+------------------------------------------------------------------+
//| Close Order                                                      |
//| PARAMS: int Ticket    - ticket of closing order                  |
//+------------------------------------------------------------------+   
bool CloseOrder(int Ticket)
{
   datetime l_datetime = TimeCurrent();
   bool closed = false;
   if (OrderSelect(Ticket, SELECT_BY_TICKET, MODE_TRADES)) 
   {
            l_datetime = TimeCurrent();
            closed = false;
            while(!closed && TimeCurrent() - l_datetime < 60)
            {
               closed = OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), 10, Gold);
               int err = GetLastError();
               if (err == 150) return (closed);
               Sleep(1000);
               while (IsTradeContextBusy()) Sleep(1000);
               RefreshRates();
            }
            if (!closed) Print("OrderClose Error: "+ ErrorDescription(GetLastError()));        
   }
   return (closed);                    
}
  
//+------------------------------------------------------------------+
//| Add or Update level line on chart                                |
//+------------------------------------------------------------------+
void SetLevel(string linename, double level, color col1, int linestyle, int thickness)
{
   int digits= Digits;

   // create or move the horizontal line   
   if (ObjectFind(linename) != 0) {
      ObjectCreate(linename, OBJ_HLINE, 0, 0, level);
      ObjectSet(linename, OBJPROP_STYLE, linestyle);
      ObjectSet(linename, OBJPROP_COLOR, col1);
      ObjectSet(linename, OBJPROP_WIDTH, thickness);
      
      ObjectSet(linename, OBJPROP_BACK, True);
   }
   else 
   {
      ObjectMove(linename, 0, Time[0], level);
   }
}  

void RemoveLines()
{
   int obj_total= ObjectsTotal();
   int signatureLength = StringLen(ObjectSignature);
   for (int i= obj_total; i>=0; i--) {
      string name= ObjectName(i);
      if (StringSubstr(name,0,signatureLength)==ObjectSignature) ObjectDelete(name);
   }
}
  
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Read stat from history                                           |
//+------------------------------------------------------------------+
bool ReadHistory(HistoryStat &stat)
{
   stat.profitPips=0;
   stat.profitCurrency=0;

   datetime day = StringToTime(TimeToString(TimeCurrent(), TIME_DATE));
   int i=OrdersHistoryTotal()-1;
   while (i >=0 )
   {
      if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY))
      {
         if (!OnlyCurrentPair || OrderSymbol()==_Symbol){
               if(OrderCloseTime()>=day){
                  double point = MarketInfo(OrderSymbol(), MODE_POINT);
                  int digits = (int)MarketInfo(OrderSymbol(), MODE_DIGITS);
                  if (digits==3 || digits==5) point *=10;
                  stat.profitPips += MathAbs(OrderClosePrice()-OrderOpenPrice())/point;
                  stat.profitCurrency += OrderProfit()+OrderCommission()+OrderSwap();
               }
         }
      }
      i--;
   }
   
   return (true);
}

double normPrice(double p, string pair=""){
        // Prices to open must be a multiple of ticksize 
    if(p<=0) return(0); 
    if (pair == "") pair = Symbol();
    double ts = MarketInfo(pair, MODE_TICKSIZE);
    if(ts==0) return(NormalizeDouble(p, (int)MarketInfo(pair, MODE_DIGITS))); //no normalization if no ticksize info
    return( NormalizeDouble(MathRound(p/ts) * ts , (int)MarketInfo(pair, MODE_DIGITS)));
}