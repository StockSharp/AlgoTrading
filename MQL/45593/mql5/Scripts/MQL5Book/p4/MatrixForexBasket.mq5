//+------------------------------------------------------------------+
//|                                            MatrixForexBasket.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <Graphics/Graphic.mqh>
#include <MQL5Book/MatrixProcessor.mqh>

#property script_show_inputs

input int BarCount = 20;  // BarCount (in-sample "history" and out-of-sample "future")
input int BarOffset = 10; // BarOffset (where "future" begins)
input ENUM_CURVE_TYPE CurveType = CURVE_LINES;

//+------------------------------------------------------------------+
//| Model of ideal constantly growing balance curve                  |
//+------------------------------------------------------------------+
void ConstantGrow(vector &v)
{
   for(ulong i = 0; i < v.Size(); ++i)
   {
      v[i] = (double)(i + 1);
   }
}

//+------------------------------------------------------------------+
//| Convert vector of one primitive type (S) to another (T)          |
//+------------------------------------------------------------------+
template<typename T, typename S>
void ConvertV(vector<T> &t, const S &v)
{
   for(ulong i = 0; i < v.Size(); ++i)
   {
      t[i] = (T)v[i];
   }
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // work symbols are hardcoded (adjust according to your needs and environment)
   const string symbols[] = {"EURUSD", "GBPUSD", "USDJPY", "USDCAD", "USDCHF", "AUDUSD", "NZDUSD"};
   const int size = ArraySize(symbols);
   
   // check if the linear system is well-defined
   if(size > BarCount - BarOffset)
   {
      Print("Symbol count must be larger than number of historic bars, given: ",
         size, " and ", BarCount - BarOffset, " respectively");
      return;
   }
   
   // create a matrix for given symbols and bar count
   matrix rates(BarCount, size);
   // build a model of best balance - stable profit on every bar
   vector model(BarCount - BarOffset, ConstantGrow);
   // aux vector to hold intermediate row of i-th symbol quotes
   vector close;
  
   for(int i = 0; i < size; i++) // process all symbols
   {
      // get rates
      if(close.CopyRates(symbols[i], _Period, COPY_RATES_CLOSE, 0, BarCount))
      {
         // get price increment (profit)
         close -= close[0];
         // adjust profit by point value
         close *= SymbolInfoDouble(symbols[i], SYMBOL_TRADE_TICK_VALUE) /
            SymbolInfoDouble(symbols[i], SYMBOL_TRADE_TICK_SIZE);
         // place vector to specific column in the matrix
         rates.Col(close, i);
      }
      else
      {
         Print("vector.CopyRates(%d, COPY_RATES_CLOSE) failed. Error ", symbols[i], _LastError);
         return;
      }
   }
  
   // split the matrix for starting part to get solution
   // (which emulates optimization on history)
   // and second part for forward test
   matrix split[];
   if(BarOffset > 0)
   {
      // training = backtest on BarCount - BarOffset bars
      // out of sample future = forward test on BarOffset bars
      ulong parts[] = {BarCount - BarOffset, BarOffset};
      rates.Split(parts, 0, split);
   }
  
   // solve linear system equation against the model
   vector x = (BarOffset > 0) ? split[0].LstSq(model) : rates.LstSq(model);
   
   Print("Solution (lots per symbol): ");
   {
      // use float vector just for shorter printing of the solution
      vectorf xf(size, ConvertV, x);
      Print(xf);
   }
   
   // use vector for simulated balance curve
   vector balance = vector::Zeros(BarCount);
   for(int i = 1; i < BarCount; ++i)
   {
      balance[i] = 0;
      for(int j = 0; j < size; ++j)
      {
         balance[i] += (float)(rates[i][j] * x[j]);
      }
   }
  
   // now estimate the quality of solution
   if(BarOffset > 0)
   {
      // NB: MQL5 doesn't have Split for vectors!
      // NB: MQL5 can't assign vector to matrix or matrix to vector!
      // make a copy of balance
      vector backtest = balance;
      // only historic in-sample bars are used for backtest estimation
      backtest.Resize(BarCount - BarOffset);
      // prepare forward out-of-sample part of the bars manually
      vector forward(BarOffset);
      for(int i = 0; i < BarOffset; ++i)
      {
         forward[i] = balance[BarCount - BarOffset + i];
      }
      // calculate regression metrics for backtest and forward
      Print("Backtest R2 = ", backtest.RegressionMetric(model, REGRESSION_R2));
      model.Resize(BarOffset);
      model += BarCount - BarOffset;
      Print("Forward R2 = ", forward.RegressionMetric(model, REGRESSION_R2));
   }
   else
   {
      Print("R2 = ", balance.RegressionMetric(model, REGRESSION_R2));
   }

   // copy the 'balance' vector into array of doubles
   double array[];
   Export(balance, array); // TODO: balance.Swap(array);

   // pretty-printing with 2 digits
   Print("Balance: ");
   ArrayPrint(array, 2);
  
   // let's draw the graph of the balance (both "backtest" and "forward")
   GraphPlot(array, CurveType);
   
   if(MQLInfoInteger(MQL_DEBUG))
   {
      Sleep(5000);
   }
}
//+------------------------------------------------------------------+
/*

(EURUSD,H1)	Solution (lots per symbol): 
(EURUSD,H1)	[-0.0057809334,-0.0079846876,0.0088985749,-0.0041461736,-0.010710154,-0.0025694175,0.01493552]
(EURUSD,H1)	Backtest R2 = 0.9896645616246145
(EURUSD,H1)	Forward R2 = 0.8667852183780984
(EURUSD,H1)	Balance: 
(EURUSD,H1)	 0.00  1.68  3.38  3.90  5.04  5.92  7.09  7.86  9.17  9.88  9.55 10.77 12.06 13.67 15.35 15.89 16.28 15.91 16.85 16.58

*/
//+------------------------------------------------------------------+
