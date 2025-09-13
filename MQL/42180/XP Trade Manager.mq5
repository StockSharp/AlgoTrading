//+------------------------------------------------------------------+
//|                                              Trading Manager.mq5 |
//|                                               Copyright MQL BLUE |
//|                                           http://www.mqlblue.com |
//+------------------------------------------------------------------+
#property copyright "Copyright MQL BLUE"
#property link      "https://www.mqlblue.com"
#property version   "6.00"
#property strict

//---- definitions
#define Version                           "6.00"

#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>

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
input   bool          OnlyCurrentPairInit   = true; //OnlyCurrentPair
bool OnlyCurrentPair = false;

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
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   ShortName = MQLInfoString(MQL_PROGRAM_NAME);
   ObjectSignature = "["+ShortName+"]";
   Print ("Init "+ShortName+" ver. "+Version);         

   OnlyCurrentPair = OnlyCurrentPairInit;
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
   ObjectSetString(chartID, nameProfitCurrency, OBJPROP_TEXT, "Profit currency = 0,00 "+AccountInfoString(ACCOUNT_CURRENCY));

   ordersHistoryCount=0;
   Print("init");
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
   m_trade.SetExpertMagicNumber(0);
   //Comment(TimeCurrent()+" "+PositionsTotal());
   for(int i=PositionsTotal()-1;i>=0;i--){ // returns the number of current positions
      if(m_position.SelectByIndex(i))  {   // selects the position by index for further access to its properties
         if(!OnlyCurrentPair || m_position.Symbol()==_Symbol){ 
            ulong ticket = m_position.Ticket(); 
            string symbol = m_position.Symbol(); 
            double point = SymbolInfoDouble(symbol, SYMBOL_POINT);
            int digits =  (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
            if (digits==3 || digits==5) point *=10;
            double slCurrentLevel = 0;
            double tpCurrentLevel = 0;
            bool closed = false;
            double bid =  SymbolInfoDouble(symbol, SYMBOL_BID);
            double ask = SymbolInfoDouble(symbol, SYMBOL_ASK);
            if (StealthMode)
            {
               slCurrentLevel = ObjectGetDouble(chartID, ObjectSignature+"sl"+IntegerToString(ticket), OBJPROP_PRICE);
               tpCurrentLevel = ObjectGetDouble(chartID, ObjectSignature+"tp"+IntegerToString(ticket), OBJPROP_PRICE);
               if (m_position.PositionType()==POSITION_TYPE_BUY)
               {
                  if (tpCurrentLevel > 0)
                  {
                     if (bid >= tpCurrentLevel)
                     {
                        closed = m_trade.PositionClose(ticket);
                     }
                  }
                  if (slCurrentLevel > 0)
                  {
                     if (bid <= slCurrentLevel) 
                        closed = m_trade.PositionClose(ticket);
                  } 
               }
               else if (m_position.PositionType()==POSITION_TYPE_SELL)
               {
                  if (tpCurrentLevel > 0)
                  {
                     if (ask <= tpCurrentLevel) 
                        closed = m_trade.PositionClose(ticket);
                  } 
                  if (slCurrentLevel > 0)
                  {
                     if (ask >= slCurrentLevel) 
                        closed = m_trade.PositionClose(ticket);
                  }
               }
               if (closed)
               {
                  ObjectDelete(chartID, ObjectSignature+"sl"+IntegerToString(ticket));
                  ObjectDelete(chartID, ObjectSignature+"tp"+IntegerToString(ticket));
                  continue;
               }
            }
            if (!closed)
            {
               // **************** check SL & TP *********************
               if ( (StopLoss > 0 && ((!StealthMode && m_position.StopLoss() == 0)  || (StealthMode && slCurrentLevel==0))) || 
               (TakeProfit > 0 && ((!StealthMode && m_position.TakeProfit() == 0) || (StealthMode &&  tpCurrentLevel==0)))
               ){
                  double stopLevel = SymbolInfoInteger(symbol, SYMBOL_TRADE_STOPS_LEVEL)*point;
                  double distTP = MathMax(TakeProfit*point, stopLevel);
                  double distSL = MathMax(StopLoss*point, stopLevel);
                  double takeProfit = 0;
                  double stopLoss = 0;
                  if (m_position.PositionType()==POSITION_TYPE_BUY)
                  {
                     if (TakeProfit > 0) takeProfit =m_position.PriceOpen() + distTP;
                     if (StopLoss > 0) stopLoss = m_position.PriceOpen() - distSL;
                  }
                  else if (m_position.PositionType()==POSITION_TYPE_SELL)
                  {
                     if (TakeProfit > 0) takeProfit = m_position.PriceOpen()- distTP;
                     if (StopLoss > 0) stopLoss = m_position.PriceOpen() + distSL;
                  }
                  stopLoss = normPrice(stopLoss);
                  takeProfit = normPrice(takeProfit);
                  if (!StealthMode)
                  {
                     ResetLastError();
                     if(!m_trade.PositionModify(ticket, NormalizeDouble(stopLoss, digits), NormalizeDouble(takeProfit, digits)))
                        Print(ShortName +" (OrderModify Error) "+ IntegerToString(GetLastError())); 
                  }
                  else
                  {
                     SetLevel(ObjectSignature+"sl"+IntegerToString(ticket), NormalizeDouble(stopLoss,digits), clrRed, STYLE_DASH, 1);
                     SetLevel(ObjectSignature+"tp"+IntegerToString(ticket), NormalizeDouble(takeProfit,digits), clrGreen, STYLE_DASH,1);
                  }
               }
               // ****** CHECK Trailing Stop && Break Even **************************
               if (UseTrailingStop)
               {
                  int priceDistance = 0;
                  int multi = 0;
                  double stopLoss=0;
                  double be;
                  if (m_position.PositionType()==POSITION_TYPE_BUY)
                  {
                     priceDistance = (int)(NormalizeDouble(bid - m_position.PriceOpen(), digits)/point);
                     if (priceDistance > TSStart)
                     {
                        multi = (int)MathFloor(priceDistance / TSStep);
                        if (multi > 0)
                        {
                           stopLoss = NormalizeDouble(m_position.PriceOpen() + TSStep*multi*point, digits) - TSDistance*point;
                           if (TSEndBE)
                           {
                              be = NormalizeDouble(m_position.PriceOpen() + BELevel*point, digits);
                              if (stopLoss > be) stopLoss = be;
                           }
                        }
                        stopLoss = normPrice(stopLoss);
                        if (!StealthMode)
                        {
                           if (NormalizeDouble(m_position.StopLoss(), digits) < NormalizeDouble(stopLoss, digits) || m_position.StopLoss()==0)
                           {
                              ResetLastError();
                              if(!m_trade.PositionModify(ticket, NormalizeDouble(stopLoss, digits), m_position.TakeProfit()))
                                 Print(ShortName +" (OrderModify Error) "+ IntegerToString(GetLastError())); 
                           }
                        }
                        else
                        {
                           if (NormalizeDouble(slCurrentLevel, digits) < NormalizeDouble(stopLoss, digits) || slCurrentLevel==0) SetLevel(ObjectSignature+"sl"+IntegerToString(ticket), NormalizeDouble(stopLoss,digits), clrRed, STYLE_DASH, 1);
                        }
                     }
                  }
                  else if (m_position.PositionType()==POSITION_TYPE_SELL)
                  {
                     priceDistance = (int)(NormalizeDouble(m_position.PriceOpen()- ask, digits)/point);
                     if (priceDistance > TSStart)
                     {
                        multi = (int)MathFloor(priceDistance / TSStep);
                        if (multi > 0)
                        {
                           stopLoss = NormalizeDouble(m_position.PriceOpen() - TSStep*multi*point, digits) + TSDistance*point;
                        }
                        if (TSEndBE)
                        {
                           be = NormalizeDouble(m_position.PriceOpen() - BELevel*point, digits);
                           if (stopLoss < be) stopLoss = be;
                        }   
                        stopLoss = normPrice(stopLoss);     
                        if (!StealthMode)
                        {                
                           if (NormalizeDouble(m_position.StopLoss(), digits) > NormalizeDouble(stopLoss, digits) || m_position.StopLoss()==0)
                           {
                              ResetLastError();
                             if(!m_trade.PositionModify(ticket, NormalizeDouble(stopLoss, digits), m_position.TakeProfit()))
                                 Print(ShortName +" (OrderModify Error) "+ IntegerToString(GetLastError())); 
                           }                        
                        }
                        else
                        {
                           if (NormalizeDouble(slCurrentLevel, digits) > NormalizeDouble(stopLoss, digits) || slCurrentLevel==0) SetLevel(ObjectSignature+"sl"+IntegerToString(ticket), NormalizeDouble(stopLoss,digits), clrRed, STYLE_DASH, 1);
                        }
                     }
                  }
               }
               else if (UseBreakEven)
               {
                  int slDistance = 0;
                  int priceDistance = 0;
                  double stopLoss=0;
                  if (m_position.PositionType()==POSITION_TYPE_BUY)
                  {
                     slDistance = (int)(NormalizeDouble(m_position.StopLoss() - m_position.PriceOpen(), digits)/point);
                     if (slDistance < BELevel)
                     {
                        priceDistance = (int)(NormalizeDouble(bid - m_position.PriceOpen(), digits)/point);
                        if (priceDistance >= BEActivation)
                        {
                           stopLoss = m_position.PriceOpen() + BELevel*point;
                           stopLoss = normPrice(stopLoss);
                           if (!StealthMode)
                           {
                              if(NormalizeDouble(m_position.StopLoss(), digits) <NormalizeDouble(stopLoss,digits) || m_position.StopLoss()==0){
                                 ResetLastError();
                                if(!m_trade.PositionModify(ticket, NormalizeDouble(stopLoss, digits), m_position.TakeProfit()))
                                    Print(ShortName +" (OrderModify Error) "+ IntegerToString(GetLastError())); 
                              }
                           }
                           else
                           {
                              if (NormalizeDouble(slCurrentLevel, digits) < NormalizeDouble(stopLoss, digits) || slCurrentLevel==0)  SetLevel(ObjectSignature+"sl"+IntegerToString(ticket), NormalizeDouble(stopLoss,digits), clrRed, STYLE_DASH, 1);                                                                         
                           }
                        }
                     }
                  }
                  else if (m_position.PositionType()==POSITION_TYPE_SELL)
                  {
                     slDistance = (int)(NormalizeDouble(m_position.PriceOpen()-m_position.StopLoss(),  digits)/point);
                     if (slDistance < BELevel)
                     {
                        priceDistance = (int)(NormalizeDouble(m_position.PriceOpen() - ask, digits)/point);
                        if (priceDistance >= BEActivation)
                        {
                           stopLoss = m_position.PriceOpen() - BELevel*point;
                           stopLoss = normPrice(stopLoss);
                           if (!StealthMode)
                           {
                              if(NormalizeDouble(m_position.StopLoss(), digits) >NormalizeDouble(stopLoss,digits) || m_position.StopLoss()==0){
                              
                                 ResetLastError();
                                if(!m_trade.PositionModify(ticket, NormalizeDouble(stopLoss, digits), m_position.TakeProfit()))
                                    Print(ShortName +" (OrderModify Error) "+ IntegerToString(GetLastError())); 
                              }
                           }
                           else
                           {
                              if (NormalizeDouble(slCurrentLevel, digits) > NormalizeDouble(stopLoss, digits) || slCurrentLevel==0)  SetLevel(ObjectSignature+"sl"+IntegerToString(ticket), NormalizeDouble(stopLoss,digits), clrRed, STYLE_DASH, 1);                           
                           }
                        }
                     }                  
                  }
               }
            }
         }
      }  
   }
   
   for(int i=OrdersTotal()-1;i>=0;i--){ // returns the number of current positions
    ulong ticket=OrderGetTicket(i);
    if((ticket-1)>0) {
         if(!OnlyCurrentPair || OrderGetString(ORDER_SYMBOL)==_Symbol){
            double slCurrentLevel = 0;
            double tpCurrentLevel = 0;
            string symbol = OrderGetString(ORDER_SYMBOL);
            double point = SymbolInfoDouble(symbol, SYMBOL_POINT);
            int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
            if (digits==3 || digits==5) point *=10;
            if (StealthMode)
            {
               slCurrentLevel = ObjectGetDouble(chartID, ObjectSignature+"sl"+IntegerToString(ticket), OBJPROP_PRICE);
               tpCurrentLevel = ObjectGetDouble(chartID, ObjectSignature+"tp"+IntegerToString(ticket), OBJPROP_PRICE);
            }
              
               if ( (StopLoss > 0 && ((!StealthMode && OrderGetDouble(ORDER_SL)== 0)  || (StealthMode && slCurrentLevel==0))) || 
               (TakeProfit > 0 && ((!StealthMode && OrderGetDouble(ORDER_TP) == 0) || (StealthMode &&  tpCurrentLevel==0)))
               ){
                  double stopLevel = SymbolInfoInteger(symbol, SYMBOL_TRADE_STOPS_LEVEL)*point;
                  double distTP = MathMax(TakeProfit*point, stopLevel);
                  double distSL = MathMax(StopLoss*point, stopLevel);
                  double takeProfit = 0;
                  double stopLoss = 0;
                  if (MathMod(OrderGetInteger(ORDER_TYPE),2)==0)
                  {
                     if (TakeProfit > 0) takeProfit =OrderGetDouble(ORDER_PRICE_OPEN) + distTP;
                     if (StopLoss > 0) stopLoss = OrderGetDouble(ORDER_PRICE_OPEN)  - distSL;
                  }
                  else
                  {
                     if (TakeProfit > 0) takeProfit = OrderGetDouble(ORDER_PRICE_OPEN) - distTP;
                     if (StopLoss > 0) stopLoss = OrderGetDouble(ORDER_PRICE_OPEN)  + distSL;
                  }
                  
                  stopLoss = normPrice(stopLoss);
                  takeProfit = normPrice(takeProfit);
                  if (!StealthMode)
                  {
                     ResetLastError();
                     if(!m_trade.OrderModify(ticket,OrderGetDouble(ORDER_PRICE_OPEN),NormalizeDouble(stopLoss, digits), NormalizeDouble(takeProfit, digits),ORDER_TIME_GTC,0))
                        Print(ShortName +" (OrderModify Error) "+ IntegerToString(GetLastError())); 
                  }
                  else
                  {
                     SetLevel(ObjectSignature+"sl"+IntegerToString(ticket), NormalizeDouble(stopLoss,digits), clrRed, STYLE_DASH, 1);
                     SetLevel(ObjectSignature+"tp"+IntegerToString(ticket), NormalizeDouble(takeProfit,digits), clrGreen, STYLE_DASH,1);
                  }
               }
           }
        }
     }
}

void OnTrade(){   
   
  // Print("TRADE EVENSJ------------------------------------------");
//   if (OrdersHistoryTotal() != ordersHistoryCount)
   {
      // Refresh pips counter
      HistoryStat histStat = {0,0};
      ReadHistory(histStat);
         
      ObjectSetString(chartID, nameProfitPips, OBJPROP_TEXT, "Profit pips = "+DoubleToString(histStat.profitPips,0));   
      ObjectSetString(chartID, nameProfitCurrency, OBJPROP_TEXT, "Profit currency = "+DoubleToString(histStat.profitCurrency,2)+" "+AccountInfoString(ACCOUNT_CURRENCY));
      //ordersHistoryCount = OrdersHistoryTotal();
   }
   
  }
  

  
//+------------------------------------------------------------------+
//| Add or Update level line on chart                                |
//+------------------------------------------------------------------+
void SetLevel(string linename, double level, color col1, int linestyle, int thickness)
{
   int digits= _Digits;

   // create or move the horizontal line   
   if (ObjectFind(chartID, linename) != 0) {
      ObjectCreate(chartID, linename, OBJ_HLINE, 0, 0, level);
      ObjectSetInteger(chartID, linename, OBJPROP_STYLE, linestyle);
      ObjectSetInteger(chartID, linename, OBJPROP_COLOR, col1);
      ObjectSetInteger(chartID, linename, OBJPROP_WIDTH, thickness);
      
      ObjectSetInteger(chartID, linename, OBJPROP_BACK, true);
   }
   else 
   {
      ObjectMove(chartID, linename, 0, Time(PERIOD_CURRENT, 0), level);
   }
}  

void RemoveLines()
{
   ObjectsDeleteAll(chartID, ObjectSignature);
   /*int obj_total= ObjectsTotal();
   int signatureLength = StringLen(ObjectSignature);
   for (int i= obj_total; i>=0; i--) {
      string name= ObjectName(i);
      if (StringSubstr(name,0,signatureLength)==ObjectSignature) ObjectDelete(name);
   }*/
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
   ulong ticket;
   HistorySelect(day, TimeCurrent());
   for(int i=HistoryDealsTotal()-1;i>=0;i--){ 
      if((ticket=HistoryDealGetTicket(i))>0){ 
            if(Symbol()==HistoryDealGetString(ticket,DEAL_SYMBOL) || !OnlyCurrentPair){
               if(HistoryDealGetInteger(ticket,DEAL_ENTRY)==DEAL_ENTRY_OUT){
                  if(HistoryDealGetInteger(ticket, DEAL_TIME)>=day){
                     double pv = SymbolInfoDouble(HistoryOrderGetString(ticket, ORDER_SYMBOL), SYMBOL_TRADE_TICK_VALUE)/
                                    (SymbolInfoDouble(HistoryOrderGetString(ticket, ORDER_SYMBOL), SYMBOL_TRADE_TICK_SIZE) / 
                                    SymbolInfoDouble(HistoryOrderGetString(ticket, ORDER_SYMBOL), SYMBOL_POINT));
                     if(pv==0) continue;
                     stat.profitPips += (HistoryDealGetDouble(ticket, DEAL_PROFIT)/(pv*HistoryDealGetDouble(ticket, DEAL_VOLUME)));
                     stat.profitCurrency+=HistoryDealGetDouble(ticket, DEAL_PROFIT)+HistoryDealGetDouble(ticket, DEAL_COMMISSION)+HistoryDealGetDouble(ticket, DEAL_SWAP);   
               }   
            }
         }
            
      }     
   }

   
   return (true);
}

datetime Time(ENUM_TIMEFRAMES tf, int i){
   datetime times[1];
   int copied = CopyTime(_Symbol, tf, i, 1,times);
   if(copied < 1){
      //Print("Error: Download date time array problem: "+IntegerToString(GetLastError()));
      return 0;
   }
   return times[0];
}

double normPrice(double p, string pair=""){
        // Prices to open must be a multiple of ticksize
        if(p<=0) return(0); 
    if (pair == "") pair = _Symbol;
    double ts = SymbolInfoDouble(pair, SYMBOL_TRADE_TICK_SIZE);
    if(ts==0) return(NormalizeDouble(p, (int)SymbolInfoInteger(pair, SYMBOL_DIGITS))); //no normalization if no ticksize info
    return( MathRound(p/ts) * ts );
    
}