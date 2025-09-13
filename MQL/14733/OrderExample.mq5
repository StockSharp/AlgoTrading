//+------------------------------------------------------------------+
//|                                                 OrderExample.mq5 |
//|                        Copyright 2016, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2009-2016, MetaQuotes Software Corp. ~ edited By 3rjfx : 2016/01/29"
#property link      "http://www.mql5.com"
#property version   "1.00"
//--
#property description "Expert Advisor for sending trade requests using OrderSendAsync() function.\r\n"
#property description "Handling trading events using OnTrade() handler functions is displayed\r\n"
#property description "Expert Advisor parameters allow setting Magic Number (unique Expert ID)"
//---
#include <Trade\Trade.mqh>
//---
//--- input parameters 
input int      MagicNumber         = 1234567;  // Expert Advisor ID
input double   RiskPercentage      = 1.0;      // Risk Percentage
input double   TPvsSLRatio         = 1.5;      // multiplication SL to TP Ratio
input bool     CalculatedTradeSize = true;     // Automatic calculate Lots size
input string   ManualyLotsSize     = "If Set CalculatedTradeSize = False, Input TradeLotSize Below";
input double   TradeLotSize        = 0.1;      // Input Lots Size manualy
//--
//--- variable for using in HistorySelect() call 
datetime history_start;
//--
//----//
//+------------------------------------------------------------------+ 
//| Expert initialization function                                   | 
//+------------------------------------------------------------------+ 
int OnInit() 
  { 
//--- check if autotrading is allowed 
   if(!TerminalInfoInteger(TERMINAL_TRADE_ALLOWED)) 
     { 
      Alert("Autotrading in the terminal is disabled, Expert Advisor will be removed.");
       ExpertRemove(); 
      return(-1); 
     } 
//--- unable to trade on a real account // If the EA is used in the Real Account, the code at line 54 to 59 must be removed like this
   /*
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE)==ACCOUNT_TRADE_MODE_REAL) 
     { 
      Alert("Expert Advisor cannot trade on a real account!"); 
      ExpertRemove(); 
      return(-2); 
     }
   */
//--- check if it is possible to trade on this account (for example, trading is impossible when using an investor password)
    if(!AccountInfoInteger(ACCOUNT_TRADE_ALLOWED)) 
     { 
      Alert("Trading on this account is disabled"); 
      ExpertRemove(); 
      return(-3); 
     } 
//--- save the time of launching the Expert Advisor for receiving trading history
    history_start=TimeCurrent(); 
//--- 
   CreateBuySellButtons(); 
   return(INIT_SUCCEEDED); 
  }
//----//
//+------------------------------------------------------------------+ 
//| Expert deinitialization function                                 | 
//+------------------------------------------------------------------+ 
void OnDeinit(const int reason) 
  { 
//--- delete all graphical objects 
   ObjectDelete(0,"Buy"); 
   ObjectDelete(0,"Sell"); 
//--- 
  }
//----//

//+------------------------------------------------------------------+ 
//| Trade function                                                   | 
//+------------------------------------------------------------------+ 
void OnTrade() 
  { 
//--- static members for storing trading account status 
   static int prev_positions=0,prev_orders=0,prev_deals=0,prev_history_orders=0;
 //--- request trading history 
   bool update=HistorySelect(history_start,TimeCurrent()); 
   PrintFormat("HistorySelect(%s , %s) = %s", 
               TimeToString(history_start),TimeToString(TimeCurrent()),(string)update);
 //--- heading named after trading event's handler function  
   Print("=> ",__FUNCTION__," at ",TimeToString(TimeCurrent(),TIME_SECONDS)); 
//--- display handler's name and the number of orders at the moment of handling 
   int curr_positions=PositionsTotal(); 
   int curr_orders=OrdersTotal(); 
   int curr_deals=HistoryOrdersTotal(); 
   int curr_history_orders=HistoryDealsTotal(); 
//--- display the number of orders, positions, deals, as well as changes in parentheses 
   PrintFormat("PositionsTotal() = %d (%+d)", 
               curr_positions,(curr_positions-prev_positions)); 
   PrintFormat("OrdersTotal() = %d (%+d)", 
               curr_orders,curr_orders-prev_orders); 
   PrintFormat("HistoryOrdersTotal() = %d (%+d)", 
               curr_deals,curr_deals-prev_deals); 
   PrintFormat("HistoryDealsTotal() = %d (%+d)", 
               curr_history_orders,curr_history_orders-prev_history_orders); 
//--- insert a string break to view the log more conveniently 
   Print(""); 
//--- save the account status 
   prev_positions=curr_positions; 
   prev_orders=curr_orders; 
   prev_deals=curr_deals; 
   prev_history_orders=curr_history_orders; 
//--- 
  }
//----//
//+------------------------------------------------------------------+ 
//| ChartEvent function                                              | 
//+------------------------------------------------------------------+ 
void OnChartEvent(const int id, 
                  const long &lparam, 
                  const double &dparam, 
                  const string &sparam) 
  { 
//--- handling CHARTEVENT_CLICK event ("Clicking the chart") 
   if(id==CHARTEVENT_OBJECT_CLICK) 
     { 
      Print("=> ",__FUNCTION__,": sparam = ",sparam); 
      //--- deal BUY at price
      double size=TradeSize();
      //--- if "Buy" button is pressed, then buy 
      if(sparam=="Buy") 
        { 
         PrintFormat("Buy %s %G lot",_Symbol,size); 
         BuyAsync(size); 
         //--- unpress the button 
         ObjectSetInteger(0,"Buy",OBJPROP_STATE,false); 
        }
      //---
      //--- if "Sell" button is pressed, then sell 
      if(sparam=="Sell") 
        { 
         PrintFormat("Sell %s %G lot",_Symbol,size);
         SellAsync(size); 
         //--- unpress the button 
         ObjectSetInteger(0,"Sell",OBJPROP_STATE,false); 
        } 
      ChartRedraw(); 
     } 
//---          
  }
//----//

//+------------------------------------------------------------------+ 
//| Create two buttons for buying and selling                        | 
//+------------------------------------------------------------------+ 
void CreateBuySellButtons() 
  { 
//--- check the object named "Buy" 
   if(ObjectFind(0,"Buy")>=0) 
     { 
      //--- if the found object is not a button, delete it 
      if(ObjectGetInteger(0,"Buy",OBJPROP_TYPE)!=OBJ_BUTTON) 
         ObjectDelete(0,"Buy"); 
     } 
   else 
      ObjectCreate(0,"Buy",OBJ_BUTTON,0,0,0); // create "Buy" button 
//--- configure "Buy" button 
   ObjectSetInteger(0,"Buy",OBJPROP_CORNER,CORNER_RIGHT_UPPER); 
   ObjectSetInteger(0,"Buy",OBJPROP_XDISTANCE,100); 
   ObjectSetInteger(0,"Buy",OBJPROP_YDISTANCE,50); 
   ObjectSetInteger(0,"Buy",OBJPROP_XSIZE,70); 
   ObjectSetInteger(0,"Buy",OBJPROP_YSIZE,30); 
   ObjectSetString(0,"Buy",OBJPROP_TEXT,"Buy"); 
   ObjectSetInteger(0,"Buy",OBJPROP_COLOR,clrRed);
//--- check presence of the object named "Sell" 
   if(ObjectFind(0,"Sell")>=0) 
     { 
      //--- if the found object is not a button, delete it 
      if(ObjectGetInteger(0,"Sell",OBJPROP_TYPE)!=OBJ_BUTTON) 
         ObjectDelete(0,"Sell"); 
     } 
   else 
      ObjectCreate(0,"Sell",OBJ_BUTTON,0,0,0); // create "Sell" button 
//--- configure "Sell" button 
   ObjectSetInteger(0,"Sell",OBJPROP_CORNER,CORNER_RIGHT_UPPER); 
   ObjectSetInteger(0,"Sell",OBJPROP_XDISTANCE,100); 
   ObjectSetInteger(0,"Sell",OBJPROP_YDISTANCE,100); 
   ObjectSetInteger(0,"Sell",OBJPROP_XSIZE,70); 
   ObjectSetInteger(0,"Sell",OBJPROP_YSIZE,30); 
   ObjectSetString(0,"Sell",OBJPROP_TEXT,"Sell"); 
   ObjectSetInteger(0,"Sell",OBJPROP_COLOR,clrBlue); 
//--- perform forced update of the chart to see the buttons immediately 
   ChartRedraw(); 
//--- 
  }
//----//

//+------------------------------------------------------------------+ 
//| Buy using OrderSendAsync() asynchronous function                 | 
//+------------------------------------------------------------------+ 
void BuyAsync(double volume) 
  { 
//--- prepare the request
    int stbar=0;
    int xhilo=26;
    int minstop=28;
    double prcHi=0.0;
    double prcLo=0.0;
    double high[];
    double low[];
    ArrayResize(high,100);
    ArrayResize(low,100);
    ArraySetAsSeries(high,true);
    ArraySetAsSeries(low,true);
    int copyHigh=CopyHigh(_Symbol,PERIOD_CURRENT,0,99,high);
    int copyLow=CopyLow(_Symbol,PERIOD_CURRENT,0,99,low);
    int barHi=iHighest(high,PERIOD_CURRENT,xhilo,stbar);
    int barLo=iLowest(low,PERIOD_CURRENT,xhilo,stbar);
    if(barHi!=-1) prcHi=high[barHi];
    if(barLo!=-1) prcLo=low[barLo];
    //--
    double lots=volume;
    if(!CalculatedTradeSize) lots = NormalizeDouble(TradeLotSize,2);
    //--
    double tp=NormalizeDouble(prcHi-(2*SymbolInfoInteger(_Symbol,SYMBOL_SPREAD)*_Point),_Digits);
    double sl=NormalizeDouble(prcLo-(2*SymbolInfoInteger(_Symbol,SYMBOL_SPREAD)*_Point),_Digits);
    if((tp-SymbolInfoDouble(_Symbol,SYMBOL_ASK)<20*_Point)||(SymbolInfoDouble(_Symbol,SYMBOL_BID)-sl<20*_Point))
      {
        sl=NormalizeDouble(sl-(minstop*_Point),_Digits);    
        tp=NormalizeDouble(tp+(minstop*TPvsSLRatio*_Point),_Digits);
      }
    //--
    MqlTradeRequest req={0}; 
    req.action      = TRADE_ACTION_DEAL; 
    req.symbol      = _Symbol; 
    req.magic       = MagicNumber; 
    req.volume      = lots; 
    req.type        = ORDER_TYPE_BUY; 
    req.price       = SymbolInfoDouble(_Symbol,SYMBOL_ASK);
    req.sl          = sl;
    req.tp          = tp;
    req.deviation   = 10; 
    req.comment     = "Buy using OrderSendAsync()"; 
    MqlTradeResult  res={0}; 
    if(!OrderSendAsync(req,res)) 
      { 
       Print(__FUNCTION__,": error ",GetLastError(),", retcode = ",res.retcode); 
      }
    else PlaySound("ok.wav.");
//----
  }
//----//

//+------------------------------------------------------------------+ 
//| Sell using OrderSendAsync() asynchronous function                | 
//+------------------------------------------------------------------+ 
void SellAsync(double volume) 
  { 
//--- prepare the request
    int stbar=0;
    int xhilo=26;
    int minstop=28;
    double prcHi=0.0;
    double prcLo=0.0;
    double high[];
    double low[];
    ArrayResize(high,100);
    ArrayResize(low,100);
    ArraySetAsSeries(high,true);
    ArraySetAsSeries(low,true);
    int copyHigh=CopyHigh(_Symbol,PERIOD_CURRENT,0,99,high);
    int copyLow=CopyLow(_Symbol,PERIOD_CURRENT,0,99,low);
    int barHi=iHighest(high,PERIOD_CURRENT,xhilo,stbar);
    int barLo=iLowest(low,PERIOD_CURRENT,xhilo,stbar);
    if(barHi!=-1) prcHi=high[barHi];
    if(barLo!=-1) prcLo=low[barLo];
    //--
    double lots=volume;
    if(!CalculatedTradeSize) lots = NormalizeDouble(TradeLotSize,2);
    //--
    double tp=NormalizeDouble(prcLo+(2*SymbolInfoInteger(_Symbol,SYMBOL_SPREAD)*_Point),_Digits);
    double sl=NormalizeDouble(prcHi+(2*SymbolInfoInteger(_Symbol,SYMBOL_SPREAD)*_Point),_Digits);
    if((SymbolInfoDouble(_Symbol,SYMBOL_BID)-tp<20*_Point)||(sl-SymbolInfoDouble(_Symbol,SYMBOL_ASK)<20*_Point))
      {
        sl=NormalizeDouble(sl+(minstop*_Point),_Digits);
        tp=NormalizeDouble(tp-(minstop*TPvsSLRatio*_Point),_Digits);
      }
    //--
    MqlTradeRequest req={0}; 
    req.action      = TRADE_ACTION_DEAL; 
    req.symbol      = _Symbol; 
    req.magic       = MagicNumber; 
    req.volume      = lots;
    req.type        = ORDER_TYPE_SELL; 
    req.price       = SymbolInfoDouble(_Symbol,SYMBOL_BID);
    req.sl          = sl;
    req.tp          = tp;
    req.deviation   = 10; 
    req.comment     = "Sell using OrderSendAsync()"; 
    MqlTradeResult  res={0}; 
    if(!OrderSendAsync(req,res)) 
      { 
       Print(__FUNCTION__,": error ",GetLastError(),", retcode = ",res.retcode); 
      }
    else PlaySound("ok.wav.");
//----
  }
//----//

//+-------------------------------------------------------------------------+
//|                      Money Managment                                    |   
//+-------------------------------------------------------------------------+   
double TradeSize() 
  {
    //---
    double lots_min  = SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
    double lots_max  = SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX);
    long   leverage  = AccountInfoInteger(ACCOUNT_LEVERAGE);
    double lots_size = SymbolInfoDouble(_Symbol,SYMBOL_TRADE_CONTRACT_SIZE);
    double lots_step = SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP);
    //--
    double final_account_balance =  MathMin(AccountInfoDouble(ACCOUNT_BALANCE),AccountInfoDouble(ACCOUNT_EQUITY));
    int normalization_factor = 0;
    double lots = 0.0;
    //--
    if(lots_step == 0.01) {normalization_factor = 2;}
    if(lots_step == 0.1)  {normalization_factor = 1;}
    //--
    lots = (final_account_balance*RiskPercentage/100.0)/(lots_size/leverage);
    lots = NormalizeDouble(lots,normalization_factor);
    //--
    if (lots < lots_min) {lots = lots_min;}
    if (lots > lots_max) {lots = lots_max;}
    //---
    return(lots);
//----
  }
//----//

//+------------------------------------------------------------------+
//|  searching index of the highest bar                              |
//+------------------------------------------------------------------+
int iHighest(const double &array[],
             int timeframe,
             int depth,
             int startPos)
  {
   int index=startPos;
//--- start index validation
   if(startPos<0)
     {
      Print("Invalid parameter in the function iHighest, startPos =",startPos);
      return 0;
     }
   int size=ArraySize(array);
//---
   double max=array[startPos];
//--- start searching
   for(int i=depth; i>startPos; i--)
     {
      if(array[i]>max)
        {
         index=i;
         max=array[i];
        }
     }
//--- return index of the highest bar
   return(index);
  }
//----//

//+------------------------------------------------------------------+
//|  searching index of the lowest bar                               |
//+------------------------------------------------------------------+
int iLowest(const double &array[],
            int timeframe,
            int depth,
            int startPos)
  {
   int index=startPos;
//--- start index validation
   if(startPos<0)
     {
      Print("Invalid parameter in the function iLowest, startPos =",startPos);
      return 0;
     }
   int size=ArraySize(array);
//---
   double min=array[startPos];
//--- start searching
   for(int i=depth; i>startPos; i--)
     {
      if(array[i]<min)
        {
         index=i;
         min=array[i];
        }
     }
//--- return index of the lowest bar
   return(index);
  }
//----//
//+----------------------------------------------------------------------------+
