//+------------------------------------------------------------------+
//|                                                      LibRand.mqh |
//|                             Copyright 2021-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

enum STRING_PATTERN
{
   STRING_PATTERN_LOWERCASE = 1,
   STRING_PATTERN_UPPERCASE = 2,
   STRING_PATTERN_MIXEDCASE = 3
};

#import "MQL5Book/LibRand.ex5"
string StringPatternAlpha(const STRING_PATTERN _case = STRING_PATTERN_MIXEDCASE);
string StringPatternDigit();
string RandomString(const int length, string pattern = NULL);
void RandomStrings(string &array[], const int n, const int minlength, const int maxlength, string pattern = NULL);
void PseudoNormalDefaultMean(const double mean = 0.0);
void PseudoNormalDefaultSigma(const double sigma = 1.0);
double PseudoNormalDefaultValue();
double PseudoNormalValue(const double mean = 0.0, const double sigma = 1.0, const bool rooted = false);
bool PseudoNormalArray(double &array[], const int n, const double mean = 0.0, const double sigma = 1.0);
#import
//+------------------------------------------------------------------+
