//+------------------------------------------------------------------+
//|                                                  MathInvalid.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A)  Print(#A, "=", (A))

#include <MQL5Book/ConverterT.mqh>

static Converter<ulong,double> NaNs;

// general NaNs
#define NAN_INF_PLUS  0x7FF0000000000000
#define NAN_INF_MINUS 0xFFF0000000000000
#define NAN_QUIET     0x7FF8000000000000
#define NAN_IND_MINUS 0xFFF8000000000000

// examples of custom NaNs
#define NAN_QUIET_1   0x7FF8000000000001
#define NAN_QUIET_2   0x7FF8000000000002

static double pinf = NaNs[NAN_INF_PLUS];  // +infinity
static double ninf = NaNs[NAN_INF_MINUS]; // -infinity
static double qnan = NaNs[NAN_QUIET];     // quiet NaN
static double nind = NaNs[NAN_IND_MINUS]; // -nan(ind)

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // checkup
   PRT(MathIsValidNumber(pinf));                   // false
   PRT(EnumToString(MathClassify(pinf)));          // FP_INFINITE
   PRT(MathIsValidNumber(nind));                   // false
   PRT(EnumToString(MathClassify(nind)));          // FP_NAN
   
   // maths with doubles
   PRT(MathIsValidNumber(0));                      // true
   PRT(EnumToString(MathClassify(0)));             // FP_ZERO
   PRT(MathIsValidNumber(M_PI));                   // true
   PRT(EnumToString(MathClassify(M_PI)));          // FP_NORMAL
   PRT(DBL_MIN / 10);                              // 2.225073858507203e-309
   PRT(MathIsValidNumber(DBL_MIN / 10));           // true
   PRT(EnumToString(MathClassify(DBL_MIN / 10)));  // FP_SUBNORMAL
   PRT(MathSqrt(-1.0));                            // -nan(ind)
   PRT(MathIsValidNumber(MathSqrt(-1.0)));         // false
   PRT(EnumToString(MathClassify(MathSqrt(-1.0))));// FP_NAN
   PRT(MathLog(0));                                // -inf
   PRT(MathIsValidNumber(MathLog(0)));             // false
   PRT(EnumToString(MathClassify(MathLog(0))));    // FP_INFINITE
   
   // maths with float
   PRT(1.0f / FLT_MIN / FLT_MIN);                             // inf
   PRT(MathIsValidNumber(1.0f / FLT_MIN / FLT_MIN));          // false
   PRT(EnumToString(MathClassify(1.0f / FLT_MIN / FLT_MIN))); // FP_INFINITE

   // we can use Converter to detect specific NaNs
   PrintFormat("%I64X", NaNs[MathSqrt(-1.0)]);      // FFF8000000000000
   PRT(NaNs[MathSqrt(-1.0)] == NAN_IND_MINUS);      // true

   // NaN != NaN is always true
   PRT(MathSqrt(-1.0) != MathSqrt(-1.0)); // true
   PRT(MathSqrt(-1.0) == MathSqrt(-1.0)); // false
}
//+------------------------------------------------------------------+
