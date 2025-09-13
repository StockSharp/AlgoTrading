//+------------------------------------------------------------------+
//|                                        MatrixMachineLearning.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

input int TicksToLoad = 100;
input int TicksToTest = 50;
input int PredictorSize = 20;
input int ForecastSize = 10;
input bool DebugLog = false;

#include <MQL5Book/MqlError.mqh>
#define PRTX(A) ObjectPrint(#A, (A))
#define LIMIT        100             // max steps to converge
#define ACCURACY 0.00001             // neuron state change to converge

//+------------------------------------------------------------------+
//| Helper printer returning object reference and checking errors    |
//+------------------------------------------------------------------+
template<typename T>
T ObjectPrint(const string s, const T &retval)
{
   const int snapshot = _LastError; // required because _LastError is volatile
   const string err = E2S(snapshot) + "(" + (string)snapshot + ")";
   Print(s, "=", retval, " / ", (snapshot == 0 ? "ok" : err));
   ResetLastError(); // cleanup for next execution
   return retval;
}

template<typename T>
vector<T> Binary(const vector<T> &v)
{
   vector<T> signs = vector<T>::Ones(v.Size());
   for(ulong i = 0; i < v.Size(); ++i)
   {
      if(v[i] < 0)
      {
         signs[i] = -1;
      }
   }
   return signs;
}

template<typename T>
void Binarize(vector<T> &v)
{
   for(ulong i = 0; i < v.Size(); ++i)
   {
      if(v[i] < 0)
      {
         v[i] = -1;
      }
      else
      {
         v[i] = +1;
      }
   }
}

template<typename T>
matrix<T> TrainWeights(const vector<T> &data, const uint predictor, const uint responce,
   const uint start = 0, const uint _stop = 0, const uint step = 1)
{
   const uint sample = predictor + responce;
   const uint stop = _stop <= start ? (uint)data.Size() : _stop;
   const uint n = (stop - sample + 1 - start) / step;
   matrix<T> A(n, predictor), B(n, responce);
   matrix<T> W = matrix<T>::Zeros(predictor, responce);
   
   ulong k = 0;
   for(ulong i = start; i < stop - sample + 1; i += step, ++k)
   {
      for(ulong j = 0; j < predictor; ++j)
      {
         A[k][j] = data[start + i * step + j];
      }
      for(ulong j = 0; j < responce; ++j)
      {
         B[k][j] = data[start + i * step + j + predictor];
      }
   }
   
   if(DebugLog)
   {
      PRTX(A);
      PRTX(B);
   }
   
   for(ulong i = 0; i < k; ++i)
   {
      W += A.Row(i).Outer(B.Row(i));
   }
   
   return W;
}

template<typename T>
vector<T> RunWeights(const matrix<T> &W,
   const vector<T> &data)
{
   const uint predictor = (uint)W.Rows();
   const uint responce = (uint)W.Cols();

   vector a = data;
   vector b = vector::Zeros(responce);
   
   if(data.Size() != predictor)
   {
      Print("Predictor size mismatch: W=[%dx%d], data=%d",
         predictor, responce, data.Size());
      return b;
   }
   
   vector x, y;
   uint j = 0;
   const uint limit = LIMIT;
   const matrix<T> w = W.Transpose();
   
   for( ; j < limit; ++j)
   {
      x = a;
      y = b;
      a.MatMul(W).Activation(b, AF_TANH);
      b.MatMul(w).Activation(a, AF_TANH);
      if(!a.Compare(x, ACCURACY) && !b.Compare(y, ACCURACY)) break;
   }
   
   if(DebugLog)
   {
      if(j < limit)
      {
         PrintFormat("Converged in %d cycles", j);
      }
      else
      {
         PrintFormat("Non-converged");
      }
   }
   
   Binarize(a);
   Binarize(b);

   if(DebugLog)
   {
      PRTX(a);        // final state
      PRTX(b);        // estimate
   }
   
   return b;
}

template<typename T>
void CheckWeights(const matrix<T> &W,
   const vector<T> &data,
   const uint start = 0, const uint _stop = 0, const uint step = 1)
{
   const uint predictor = (uint)W.Rows();
   const uint responce = (uint)W.Cols();
   const uint sample = predictor + responce;
   const uint stop = _stop <= start ? (uint)data.Size() : _stop;
   const uint n = (stop - sample + 1 - start) / step;
   matrix<T> A(n, predictor), B(n, responce);
   
   ulong k = 0;
   for(ulong i = start; i < stop - sample + 1; i += step, ++k)
   {
      for(ulong j = 0; j < predictor; ++j)
      {
         A[k][j] = data[start + i * step + j];
      }
      for(ulong j = 0; j < responce; ++j)
      {
         B[k][j] = data[start + i * step + j + predictor];
      }
   }
   
   const matrix<T> w = W.Transpose();

   const uint limit = LIMIT;
   
   int positive = 0;
   int negative = 0;
   int average = 0;
   
   for(ulong i = 0; i < k; ++i)
   {
      vector a = A.Row(i);
      vector b = vector::Zeros(responce);

      vector x, y;
      uint j = 0;
      
      for( ; j < limit; ++j)
      {
         x = a;
         y = b;
         a.MatMul(W).Activation(b, AF_TANH);
         b.MatMul(w).Activation(a, AF_TANH);
         if(!a.Compare(x, ACCURACY) && !b.Compare(y, ACCURACY)) break;
      }
      
      if(DebugLog)
      {
         if(j < limit)
         {
            PrintFormat("%d: Converged in %d cycles", i, j);
         }
         else
         {
            PrintFormat("%d: Non-converged", i);
         }
      }
      
      Binarize(a);
      Binarize(b);
      const int match = (int)(b.Dot(B.Row(i)));
      if(match > 0) positive++;
      else if(match < 0) negative++;

      if(DebugLog)
      {
         PRTX(a);        // final state
         PRTX(b);        // estimate
         PRTX(B.Row(i)); // target
         PRTX(match);
      }
      
      average += match;  // 0's match means 50/50 prediction (naive guessing)
   }
   float skew = (float)average / k;
   
   PrintFormat("Count=%d Positive=%d Negative=%d Accuracy=%.2f%%",
      k, positive, negative, ((skew + responce) / 2 / responce) * 100);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // get latest ticks
   MqlTick ticks[];
   const int n = CopyTicks(_Symbol, ticks, COPY_TICKS_ALL, 0, TicksToLoad);
   if(n != TicksToLoad)
   {
      PrintFormat("Insufficient ticks: %d, error: %d", n, _LastError);
      return;
   }

   // could use new feature - 'vector|matrix::CopyTicks' method
   // vector ticks;
   // ticks.CopyTicks(_Symbol, COPY_TICKS_ALL | COPY_TICKS_ASK, 0, TicksToLoad);
   // if(ticks.Size() != TicksToLoad) return; // error
   
   // extract ticks for training on backtest
   vector ask1(n - TicksToTest);
   for(int i = 0; i < n - TicksToTest; ++i)
   {
      ask1[i] = ticks[i].ask;
   }
   
   // extract more ticks for testing on forward
   vector ask2(TicksToTest);
   for(int i = 0; i < TicksToTest; ++i)
   {
      ask2[i] = ticks[i + TicksToLoad - TicksToTest].ask;
   }
   
   // calculate price changes
   vector differentiator = {+1, -1};
   vector deltas = ask1.Convolve(differentiator, VECTOR_CONVOLVE_VALID);
   
   vector inputs = Binary(deltas);
   if(DebugLog)
   {
      PRTX(deltas);
      PRTX(inputs);
   }
   
   matrix W = TrainWeights(inputs, PredictorSize, ForecastSize);

   Print("Check training on backtest: ");   
   CheckWeights(W, inputs);

   vector test = Binary(ask2.Convolve(differentiator, VECTOR_CONVOLVE_VALID));
   Print("Check training on forwardtest: ");   
   CheckWeights(W, test);
   
   Sleep(1000); // wait a bit, so some ticks are probably added
   
   // could use new feature - 'vector|matrix::CopyTicks' method
   // ticks.CopyTicks(_Symbol, COPY_TICKS_ALL | COPY_TICKS_ASK, 0, PredictorSize + 1);
   // if(ticks.Size() == PredictorSize + 1) ...
   const int m = CopyTicks(_Symbol, ticks, COPY_TICKS_ALL, 0, PredictorSize + 1);
   if(m == PredictorSize + 1)
   {
      vector ask3(PredictorSize + 1);
      for(int i = 0; i < PredictorSize + 1; ++i)
      {
         ask3[i] = ticks[i].ask;
      }
      vector online = Binary(ask3.Convolve(differentiator, VECTOR_CONVOLVE_VALID));
      Print("Online: ", online);
      vector forecast = RunWeights(W, online);
      Print("Forecast: ", forecast);
   }
}
//+------------------------------------------------------------------+
/*

   Check training on backtest: 
   Count=20 Positive=20 Negative=0 Accuracy=85.50%
   Check training on forwardtest: 
   Count=20 Positive=12 Negative=2 Accuracy=58.50%
   Online: [1,1,1,1,-1,-1,-1,1,-1,1,1,-1,1,1,-1,-1,1,1,-1,-1]
   Forecast: [-1,1,-1,1,-1,-1,1,1,-1,1]

*/
//+------------------------------------------------------------------+
