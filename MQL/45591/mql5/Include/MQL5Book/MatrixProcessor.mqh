//+------------------------------------------------------------------+
//|                                              MatrixProcessor.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

template<typename T>
class Transformer
{
protected:
   virtual void act(T &m) = 0;

public:
   static void transform(T &m, Transformer &actor)
   {
      actor.act(m);
   }
};

template<typename T>
class MatrixTransformer: public Transformer<matrix<T>>
{
public:
   virtual void process(matrix<T> &m)
   {
      m.Init(m.Rows(), m.Cols(), transform, this);
   }
};

template<typename T>
class VectorTransformer: public Transformer<vector<T>>
{
public:
   virtual void process(vector<T> &v)
   {
      v.Init(v.Size(), transform, this);
   }
};

template<typename T>
class MatrixNormalizer: public MatrixTransformer<T>
{
public:
   const int d;
   MatrixNormalizer(const int _d = -1): d(_d == -1 ? _Digits : _d) { }
   
   void act(matrix<T> &m) override
   {
      for(ulong i = 0; i < m.Rows() * m.Cols(); ++i)
      {
         m.Flat(i, (T)NormalizeDouble(m.Flat(i), d));
      }
   }
};

template<typename T>
void Normalize(matrix<T> &m, const int d = -1)
{
  MatrixNormalizer<T> normalizer(d);
  normalizer.process(m);
}

template<typename T>
class VectorNormalizer: public VectorTransformer<T>
{
public:
   const int d;
   VectorNormalizer(const int _d = -1): d(_d == -1 ? _Digits : _d) { }
   
   void act(vector<T> &v) override
   {
      for(ulong i = 0; i < v.Size(); ++i)
      {
         v[i] = (T)NormalizeDouble(v[i], d);
      }
   }
};

template<typename T>
void Normalize(vector<T> &v, const int d = -1)
{
  VectorNormalizer<T> normalizer(d);
  normalizer.process(v);
}

template<typename T,typename S>
class VectorExporter: public VectorTransformer<T>
{
public:
   S array[];
   VectorExporter(S &source[]) { ArraySwap(source, array); }
   
   void act(vector<T> &v) override
   {
      ArrayResize(array, (int)v.Size());
      for(ulong i = 0; i < v.Size(); ++i)
      {
         array[i] = (S)v[i];
      }
   }
};

// If a vector is not needed anymore, better use vector.Swap(array);
// but Swap requires the same types of vector and array,
// whereas Export can do convertion:
// for example GraphPlot requires doubles and can't use floats
template<typename T,typename S>
void Export(vector<T> &v, S &target[])
{
  VectorExporter<T,S> exporter(target);
  exporter.process(v);
  ArraySwap(target, exporter.array);
}

//+------------------------------------------------------------------+
/*
   Usage:
   
   matrix rates;
   rates.CopyRates(_Symbol, _Period, COPY_RATES_OPEN | COPY_RATES_CLOSE, 0, 100);
   Normalize(rates);
   
*/
//+------------------------------------------------------------------+
