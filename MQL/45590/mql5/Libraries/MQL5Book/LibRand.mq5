//+------------------------------------------------------------------+
//|                                                      LibRand.mq5 |
//|                             Copyright 2021-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property library

#include <MQL5Book/LibRand.mqh>

string StringPatternAlpha(const STRING_PATTERN _case = STRING_PATTERN_MIXEDCASE) export
{
   string result = "";
   static const short delta = 'A' - 'a';
   for(short i = 'a'; i <= 'z'; ++i)
   {
      if((bool)(_case & STRING_PATTERN_LOWERCASE))
         result += ShortToString((ushort)(i));
      if((bool)(_case & STRING_PATTERN_UPPERCASE))
         result += ShortToString((ushort)(i + delta));
   }
   return result;
}

string StringPatternDigit() export
{
   string result = "";
   for(short i = '0'; i <= '9'; ++i)
   {
      result += ShortToString(i);
   }
   return result;
}

string RandomString(const int length, string pattern = NULL) export
{
   if(StringLen(pattern) == 0)
   {
      pattern = StringPatternAlpha() + StringPatternDigit();
   }
   const int size = StringLen(pattern);
   string result = "";
   for(int i = 0; i < length; ++i)
   {
      result += ShortToString(pattern[rand() % size]);
   }
   return result;
}

void RandomStrings(string &array[], const int n, const int minlength, const int maxlength, string pattern = NULL) export
{
   if(StringLen(pattern) == 0)
   {
      pattern = StringPatternAlpha() + StringPatternDigit();
   }
   ArrayResize(array, n);
   for(int j = 0; j < n; ++j)
   {
      array[j] = RandomString(rand() * (maxlength - minlength) / 32768 + minlength, pattern);
   }
}

double DefaultMean = 0.0;
double DefaultSigma = 1.0;

void PseudoNormalDefaultMean(const double mean = 0.0) export
{
   DefaultMean = mean;
}

void PseudoNormalDefaultSigma(const double sigma = 1.0) export
{
   DefaultSigma = sigma;
}

double PseudoNormalDefaultValue() export
{
   return PseudoNormalValue(DefaultMean, DefaultSigma);
}

double PseudoNormalValue(const double mean = 0.0, const double sigma = 1.0, const bool rooted = false) export
{
   const double s = !rooted ? sqrt(sigma) : sigma; // allow to get ready-made sqrt in massive calculations
   const double r = (rand() - 16383.5) / 16384.0; // [-1,+1] excluding boundaries, cause of infinity
   const double x = -(log(1 / ((r + 1) / 2) - 1) * s) / M_PI * M_E + mean;
   return x;
}

bool PseudoNormalArray(double &array[], const int n,
   const double mean = 0.0, const double sigma = 1.0) export
{
   bool success = true;
   const double s = sqrt(fabs(sigma)); // calculate ready-made sqrt value once
   ArrayResize(array, n);
   for(int i = 0; i < n; ++i)
   {
      array[i] = PseudoNormalValue(mean, s, true);
      success = success && MathIsValidNumber(array[i]);
   }
   return success;
}
//+------------------------------------------------------------------+
