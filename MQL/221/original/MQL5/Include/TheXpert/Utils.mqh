//+------------------------------------------------------------------+
//|                                                        Utils.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

void PreparePrice(string symbol, double price, string& pre, string& large, string& post)
{
   int digits = int(SymbolInfoInteger(symbol, SYMBOL_DIGITS));
   double norm = NormalizeDouble(price, digits);
   
   int shift = 2;

   double first = norm;
   while(first > 10)
   {
      first /= 10;
   }
   
   if (first < 5) shift += 1;
   
   pre = DoubleToString(norm, digits);
   
   int dotPos = StringFind(pre, ".");
   if (dotPos != -1 && dotPos < 4) shift += 1;
   
   post = StringSubstr(pre, shift);
   pre = StringSubstr(pre, 0, shift);
   large = "";
   
   int count = 0;
   int pos = 0;
   while (count < 2)
   {
      ushort c = StringGetCharacter(post, pos);
      if (c != '.') count++;
      pos++;
      
      large = large + ShortToString(c);
   }
   
   post = StringSubstr(post, pos);
}

int DoubleDigits(double value)
{
   int res = 0;
   
   double iValue = NormalizeDouble(value, 0);
   double dValue = NormalizeDouble(value, 10);

   while (dValue - iValue != 0)
   {
      dValue = NormalizeDouble(dValue*10, 10);
      iValue = NormalizeDouble(dValue, 0);
      res++;
   }
   
   return res;
}

double PriceOnDropped(int y)
{
   int height = int(ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS, 0));
   int pos = y;
   if (pos >= height) pos = height - 1;
   double max = ChartGetDouble(0, CHART_PRICE_MAX, 0);
   double min = ChartGetDouble(0, CHART_PRICE_MIN, 0);
   
   if (max <= min) return max;
   if (height <= 1) return max;
   
   return min + (max - min)*(height - 1 - pos)/(height - 1);
}


