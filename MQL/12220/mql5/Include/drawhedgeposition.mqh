//+------------------------------------------------------------------+
//| DrawHedgePosition.mqh                                            |
//| Copyright 2015, Vasiliy Sokolov.                                 |
//| http://www.mql5.com                                              |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, Vasiliy Sokolov."
#property link "http://www.mql5.com"
#include <Prototypes.mqh>

/* 
   In MetaTrader 5. all inputs and outputs traceroute (displayed on
   the chart in the form of arrows) from the point of view of a netthe submission.
   HedgeTerminal position is mixed. So
   rendering offered by MetaTrader 5 does not suit him. Class
   CDrawHedgePosition solves this problem and trisomies position
   from the point of view HedgeTerminal. 
*/

//+------------------------------------------------------------------+
//| Class trace (renders) position.                                  |
//+------------------------------------------------------------------+
class CDrawHedgePosition
{
private:
   int m_obj_totals;
   bool enable;
   int m_count;       // Used to create a uniquename.
public:
   CDrawHedgePosition();
   void Enable(bool state);
   void DrawEntryPrice();
   void DrawHistoryLine(void);
   void DrawExitPrice();
   void DrawTrendLine();
   void DrawSLTP(void);
   void DeleteAutoTracing(void);
};
//+------------------------------------------------------------------+
//| The default constructor.                                         |
//+------------------------------------------------------------------+
CDrawHedgePosition::CDrawHedgePosition()
{
   enable = true;
   m_obj_totals = 0;
   m_count = 0;
}

//+------------------------------------------------------------------+
//| Enables or disables the rendering.                               |
//| INPUT PARAMETERS                                                 |
//| state - True if you want to enable rendering. False if           |
//| need to shut down rendering.                                     |
//+------------------------------------------------------------------+
CDrawHedgePosition::Enable(bool state)
{
   enable = state;
}
//+------------------------------------------------------------------+
//| Renders the input current position.                              |
//+------------------------------------------------------------------+
void CDrawHedgePosition::DrawEntryPrice()
{
   if(!enable)return;
   double open = HedgePositionGetDouble(HEDGE_POSITION_PRICE_OPEN);
   datetime time = (datetime)(HedgePositionGetInteger(HEDGE_POSITION_ENTRY_TIME_EXECUTED_MSC)/1000);
   string name_open = "entry_price " + TimeToString(TimeCurrent(), TIME_DATE|TIME_MINUTES|TIME_SECONDS);
   color clr = clrBlack;
   ENUM_OBJECT obj = OBJ_ARROW_BUY;
   IF_SHORT
      obj = OBJ_ARROW_SELL;
   ObjectCreate(ChartID(), name_open, obj, 0, time, open);
}
//+------------------------------------------------------------------+
//| Displays the SL/TP on the chart for the current selected position.|
//+------------------------------------------------------------------+
void CDrawHedgePosition::DrawSLTP(void)
{
   if(!enable)return;
   double sl = HedgePositionGetDouble(HEDGE_POSITION_SL);
   double tp = HedgePositionGetDouble(HEDGE_POSITION_TP);
   string symbol = HedgePositionGetString(HEDGE_POSITION_SYMBOL);
   double points = SymbolInfoDouble(symbol, SYMBOL_POINT);
   string name_tp = "tp " + TimeToString(TimeCurrent(), TIME_DATE|TIME_MINUTES|TIME_SECONDS);
   string name_sl = "sl " + TimeToString(TimeCurrent(), TIME_DATE|TIME_MINUTES|TIME_SECONDS);
   datetime times[];
   CopyTime(symbol, Period(), 0, 1, times);
   if(ArraySize(times) == 0)return ;
   datetime t1 = times[0];
   datetime t2 = times[0] + (Period()*60);
   if(tp > 0.0 && ObjectCreate(ChartID(), name_tp, OBJ_RECTANGLE, 0, t1, tp, t2, tp))
   {
      ObjectSetInteger(ChartID(), name_tp, OBJPROP_COLOR, clrGreen);
   }
   if(sl > 0.0 && ObjectCreate(ChartID(), name_sl, OBJ_RECTANGLE, 0, t1, sl, t2, sl))
   {
      ObjectSetInteger(ChartID(), name_sl, OBJPROP_COLOR, clrRed);
   }
}
//+------------------------------------------------------------------+
//| Renders the output price historical position. Historical         |
//| position should be pre-selected function                         |
//| TransactionSelect.                                               |
//+------------------------------------------------------------------+
void CDrawHedgePosition::DrawExitPrice(void)
{
   if(!enable)return;
   if(TransactionType() != TRANS_HEDGE_POSITION)return;
   if(HedgePositionGetInteger(HEDGE_POSITION_STATUS) != HEDGE_POSITION_HISTORY)return;
   double exit_price = HedgePositionGetDouble(HEDGE_POSITION_PRICE_CLOSED);
   datetime time = (datetime)(HedgePositionGetInteger(HEDGE_POSITION_EXIT_TIME_EXECUTED_MSC)/1000.0);
   string name_price = "exit_price_" + DoubleToString(exit_price, 5) +
                       TimeToString(TimeCurrent(), TIME_DATE|TIME_MINUTES|TIME_SECONDS);
   ENUM_OBJECT obj = OBJ_ARROW_SELL;
   IF_SHORT
      obj = OBJ_ARROW_SELL;
   ObjectCreate(ChartID(), name_price, obj, 0, time, exit_price);
}
//+------------------------------------------------------------------+
//| Removes the auto traceroute deals MetaTrader 5.                  |
//+------------------------------------------------------------------+
void CDrawHedgePosition::DeleteAutoTracing(void)
{
   string name = ObjectName(ChartID(), 0);
   string auto = "#";
   string n = StringSubstr(name, 0, StringLen(auto));
   if(n == auto)
      ObjectDelete(ChartID(), name);
}

void CDrawHedgePosition::DrawHistoryLine(void)
{
   if(!enable)return;
   if(TransactionType() != TRANS_HEDGE_POSITION)return;
   if(HedgePositionGetInteger(HEDGE_POSITION_STATUS) != HEDGE_POSITION_HISTORY)return;
   double entry_price = HedgePositionGetDouble(HEDGE_POSITION_PRICE_OPEN);
   double exit_price = HedgePositionGetDouble(HEDGE_POSITION_PRICE_CLOSED);
   ulong id = HedgePositionGetInteger(HEDGE_POSITION_EXIT_ORDER_ID);
   datetime entry_time = (datetime)(HedgePositionGetInteger(HEDGE_POSITION_ENTRY_TIME_EXECUTED_MSC)/1000);
   datetime exit_time = (datetime)(HedgePositionGetInteger(HEDGE_POSITION_EXIT_TIME_EXECUTED_MSC)/1000);
   string name_line = "pos #" + (string)id + " " + (string)(m_count++);
   color clr = clrBlue;
   IF_SHORT
      clr = clrRed;
   if(ObjectCreate(ChartID(), name_line, OBJ_TREND, 0, entry_time, entry_price, exit_time, exit_price))
   {
      ObjectSetInteger(ChartID(), name_line, OBJPROP_STYLE, STYLE_DOT);
      ObjectSetInteger(ChartID(), name_line, OBJPROP_COLOR, clr);
   }
}