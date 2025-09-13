//+------------------------------------------------------------------+
//|                                                   TradeUtils.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

double CorrectLot(double lot, string symbol = "")
{
   if (symbol == "") symbol = _Symbol;

   double min = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN);
   double max = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX);
   double step= SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP);
   
   double lots = MathRound(lot/step)*step;
   if (lots > max) lots = max;
   if (lots < min) lots = 0;
   
   return lots;
}




