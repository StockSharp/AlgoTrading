#property copyright "Copyright 2021, mfx123 & Conor Dailey"
#property version   "1.00"
#property description "No need to tick anything below"
#property strict
#property indicator_chart_window

string total;
double total_sl, total_tp;
double prev_total_sl, prev_total_tp;
string label = "sltp";
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   total_sl = GetTotalSLValue();
   total_tp = GetTotalTPValue();
   ObjectCreate(0, label, OBJ_LABEL, 0, 0, 0);
   ObjectSetInteger(0, label, OBJPROP_CORNER, CORNER_LEFT_LOWER);
   ObjectSetInteger(0, label, OBJPROP_XDISTANCE, 0);
   ObjectSetInteger(0, label, OBJPROP_YDISTANCE, 50);
   ObjectSetInteger(0, label, OBJPROP_COLOR, clrGoldenrod);
   ObjectSetString(0, label, OBJPROP_FONT, "Arial");
   ObjectSetInteger(0, label, OBJPROP_FONTSIZE, 16);
   ObjectSetInteger(0, label, OBJPROP_HIDDEN, true);
   ObjectSetInteger(0, label, OBJPROP_BACK, false);
   ObjectSetInteger(0, label, OBJPROP_SELECTED, true);
   ObjectSetInteger(0, label, OBJPROP_SELECTABLE, true);
   ObjectSetInteger(0, label, OBJPROP_ZORDER, 0);
   Display_Info();
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   ObjectDelete(0, label);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
int GetMultiplier(string s)
  {
   int m = 0;
   int digits = SymbolInfoInteger(s, SYMBOL_DIGITS);
   if(digits == 5)
      m = 10000;
   if(digits == 4)
      m = 1000;
   if(digits == 2 || digits == 3)
      m = 100;
   if(digits == 1)
      m = 10;
   return(m);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetPips2Dbl(string s)
  {
   int digits = SymbolInfoInteger(s, SYMBOL_DIGITS);
   double p = 0;
   if(digits == 5 || digits == 3)
      p = SymbolInfoDouble(s, SYMBOL_POINT) * 10;
   else
      p = SymbolInfoDouble(s, SYMBOL_POINT);
   return(p);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetTotalSLValue()
  {
   double sl_value = 0, total_sl_value = 0, delta;

   for(int v = PositionsTotal() - 1; v >= 0; v--)
     {
      ulong positionticket = PositionGetTicket(v);
      if(PositionSelectByTicket(positionticket))
        {
         if(PositionGetDouble(POSITION_SL) != 0)
           {

            delta    = (SymbolInfoDouble(PositionGetString(POSITION_SYMBOL), SYMBOL_TRADE_TICK_VALUE) / SymbolInfoDouble(PositionGetString(POSITION_SYMBOL), SYMBOL_TRADE_TICK_SIZE)) * GetPips2Dbl(PositionGetString(POSITION_SYMBOL));
            sl_value = ((MathAbs(PositionGetDouble(POSITION_PRICE_OPEN) - PositionGetDouble(POSITION_SL)) * delta) * PositionGetDouble(POSITION_VOLUME)) * GetMultiplier(PositionGetString(POSITION_SYMBOL));
            sl_value -= PositionGetDouble(POSITION_SWAP);
            sl_value = -(sl_value);
            total_sl_value += sl_value;
           }
        }
     }
   return(NormalizeDouble(total_sl_value, 2));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetTotalTPValue()
  {
   double tp_value = 0, total_tp_value = 0, delta;

   for(int v = PositionsTotal() - 1; v >= 0; v--)
     {
      ulong positionticket = PositionGetTicket(v);
      if(PositionSelectByTicket(positionticket))
        {
         if(PositionGetDouble(POSITION_TP) != 0)
           {
            delta    = (SymbolInfoDouble(PositionGetString(POSITION_SYMBOL), SYMBOL_TRADE_TICK_VALUE) / SymbolInfoDouble(PositionGetString(POSITION_SYMBOL), SYMBOL_TRADE_TICK_SIZE)) * GetPips2Dbl(PositionGetString(POSITION_SYMBOL));
            tp_value = ((MathAbs(PositionGetDouble(POSITION_PRICE_OPEN) - PositionGetDouble(POSITION_TP)) * delta) * PositionGetDouble(POSITION_VOLUME)) * GetMultiplier(PositionGetString(POSITION_SYMBOL));
            tp_value -= PositionGetDouble(POSITION_SWAP);
            total_tp_value += tp_value;
           }
        }
     }
   return(NormalizeDouble(total_tp_value, 2));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Display_Info()
  {
   total = ""
           + "SL: $ " + DoubleToString(total_sl, 2) + "  " + "TP: $ " + DoubleToString(total_tp, 2);
   ObjectSetString(0, label, OBJPROP_TEXT, total);
   ChartRedraw(0);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   total_sl = GetTotalSLValue();
   total_tp = GetTotalTPValue();
   if((total_sl != prev_total_sl) || (total_tp != prev_total_tp))
     {
      Display_Info();
      prev_total_sl = total_sl;
      prev_total_tp = total_tp;
     }
  }
//+------------------------------------------------------------------+
