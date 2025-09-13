//+------------------------------------------------------------------+
//|                                                     RSquared.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Coefficient of determination (R²) implementation                 |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| R² and its LR-line angle                                         |
//+------------------------------------------------------------------+
struct R2A
{
   double r2;
   double angle; // tangent
   R2A(): r2(0), angle(0) { }
};

//+------------------------------------------------------------------+
//| Calculate R² and its LR-line angle                               |
//+------------------------------------------------------------------+
R2A RSquared(const double &data[])
{
   int size = ArraySize(data);
   if(size <= 2) return R2A();
   double x, y, div;
   int k = 0;
   double Sx = 0, Sy = 0, Sxy = 0, Sx2 = 0, Sy2 = 0;
   for(int i = 0; i < size; ++i)
   {
      if(data[i] == EMPTY_VALUE
      || !MathIsValidNumber(data[i])) continue;
      x = i + 1;
      y = data[i];
      Sx  += x;
      Sy  += y;
      Sxy += x * y;
      Sx2 += x * x;
      Sy2 += y * y;
      ++k;
   }
   size = k;
   const double Sx22 = Sx * Sx / size;
   const double Sy22 = Sy * Sy / size;
   const double SxSy = Sx * Sy / size;
   div = (Sx2 - Sx22) * (Sy2 - Sy22);
   if(fabs(div) < DBL_EPSILON) return R2A();
   R2A result;
   result.r2 = (Sxy - SxSy) * (Sxy - SxSy) / div;
   result.angle = (Sxy - SxSy) / (Sx2 - Sx22);
   return result;
}

//+------------------------------------------------------------------+
//| Special criterion for tester, using R² and its LR-line angle     |
//+------------------------------------------------------------------+
double RSquaredTest(const double &data[])
{
   const R2A result = RSquared(data);
   const double weight = 1.0 - 1.0 / sqrt(ArraySize(data) + 1);
   if(result.angle < 0) return -fabs(result.r2) * weight;
   return result.r2 * weight;
}
//+------------------------------------------------------------------+
