//+------------------------------------------------------------------+
//|                                            LibHoughTransform.mq5 |
//|                               Copyright (c) 2015-2022, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+
#property library

#include <MQL5Book/LibHoughTransform.mqh>

//+------------------------------------------------------------------+
//| Aux 2D-array class                                               |
//+------------------------------------------------------------------+
template<typename T>
class Plain2DArray
{
protected:
   T subarray[];
   int sizex, sizey;

public:
   Plain2DArray() {};
   Plain2DArray(int x, int y)
   {
      allocate(x, y);
   }

   void allocate(int x, int y)
   {
      sizex = x;
      sizey = y;
      ArrayResize(subarray, x * y);
      zero();
   }

   T get(int x, int y) const
   {
      return subarray[x + y * sizex];
   }

   void set(int x, int y, T v)
   {
      subarray[x + y * sizex] = v;
   }

   void inc(int x, int y, T a = (T)1)
   {
      subarray[x + y * sizex] += a;
   }

   void getSizes(int &x, int &y) const
   {
      x = sizex;
      y = sizey;
   }

   bool isAllocated() const
   {
      return ArraySize(subarray) > 0;
   }

   void zero()
   {
      ZeroMemory(subarray);
   }
};

//+------------------------------------------------------------------+
//| Main worker class for Linear Hough Transfrom                     |
//+------------------------------------------------------------------+
template<typename T>
class LinearHoughTransform: public HoughTransformConcrete<T>
{
protected:
   int size;
   Plain2DArray<T> data;
   Plain2DArray<double> trigonometric;

   void init()
   {
      data.allocate(size, size);
      trigonometric.allocate(2, size);
      double t, d = M_PI / size;
      int i;
      for(i = 0, t = 0; i < size; i++, t += d)
      {
         trigonometric.set(0, i, MathCos(t));
         trigonometric.set(1, i, MathSin(t));
      }
   }

   virtual bool findMax(int &x, int &y)
   {
      T max = (T)0;
      bool done = false;
      for(int r = 0; r < size; r++)
      {
         for(int i = 0; i < size; i++)
         {
            if(data.get(r, i) > max)
            {
               max = data.get(r, i);
               x = r;
               y = i;
               done = true;
            }
         }
      }

      if(done)
      for(int r = -1; r < 2; r++)
      {
         for(int i = -1; i < 2; i++)
         {
            if(x + r >= 0 && y + i >= 0 && x + r < size && y + i < size)
            {
               data.set(x + r, y + i, 0);
            }
         }
      }
      return done;
   }

public:
   LinearHoughTransform(const int quants): size(quants)
   {
      init();
   }

   // 2 params per line, 8 lines means 16 parameters
   virtual int extract(const HoughImage<T> &image, double &result[], const int lines = 8) override
   {
      ArrayResize(result, lines * 2);
      ArrayInitialize(result, 0);
      data.zero();

      const int w = image.getWidth();
      const int h = image.getHeight();
      const double d = M_PI / size;     // 180 / 36 = 5 degree, for example
      const double rstep = MathSqrt(w * w + h * h) / size;
      double r, t;
      int i;

      // find straight lines
      for(int x = 0; x < w; x++)
      {
         for(int y = 0; y < h; y++)
         {
            T v = image.get(x, y);
            if(v == (T)0) continue;

            for(i = 0, t = 0; i < size; i++, t += d) // t < Math.PI
            {
               r = (x * trigonometric.get(0, i) + y * trigonometric.get(1, i));
               r = MathRound(r / rstep); // range is [-size, +size]
               r += size; // [0, +2size]
               r /= 2;

               if((int)r < 0) r = 0;
               if((int)r >= size) r = size - 1;
               if(i < 0) i = 0;
               if(i >= size) i = size - 1;

               data.inc((int)r, i, v);
            }
         }
      }

      // y = a * x + b
      // y = (-cos(t)/sin(t)) * x + (r/sin(t))

      // save 8 lines as features (2 params per line)
      for(i = 0; i < lines; i++)
      {
         int x, y;
         if(!findMax(x, y))
         {
            return i;
         }

         double a = 0, b = 0;
         if(MathSin(y * d) != 0)
         {
            a = -1.0 * MathCos(y * d) / MathSin(y * d);
            b = (x * 2 - size) * rstep / MathSin(y * d);
         }
         if(fabs(a) < DBL_EPSILON && fabs(b) < DBL_EPSILON)
         {
            i--;
            continue;
         }
         result[i * 2 + 0] = a;
         result[i * 2 + 1] = b;
      }

      return i;
   }
};

//+------------------------------------------------------------------+
//| Fabric function for Hough transform objects                      |
//+------------------------------------------------------------------+
HoughTransform *createHoughTransform(const int quants, const ENUM_DATATYPE type = TYPE_INT) export
{
   switch(type)
   {
   case TYPE_INT:
      return new LinearHoughTransform<int>(quants);
   case TYPE_DOUBLE:
      return new LinearHoughTransform<double>(quants);
   // ... more types can be supported
   }
   return NULL;
}

//+------------------------------------------------------------------+
//| Built-in meta-information about implemented transform            |
//+------------------------------------------------------------------+
HoughInfo getHoughInfo() export
{
#ifdef LIB_HOUGH_IMPL_DEBUG
   Print("inline library (debug)");
#else
   Print("standalone library (production)");
#endif
   return HoughInfo(2, "Line: y = a * x + b; a = p[0]; b = p[1];");
}
//+------------------------------------------------------------------+
