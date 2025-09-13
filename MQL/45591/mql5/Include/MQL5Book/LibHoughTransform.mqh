//+------------------------------------------------------------------+
//|                                            LibHoughTransform.mqh |
//|                               Copyright (c) 2015-2022, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Source image for processing                                      |
//+------------------------------------------------------------------+
template<typename T>
interface HoughImage
{
   virtual int getWidth() const;
   virtual int getHeight() const;
   virtual T get(int x, int y) const;
};

//+------------------------------------------------------------------+
//| Meta-information about specific Hough algorithm (i.e. linear)    |
//+------------------------------------------------------------------+
struct HoughInfo
{
   const int dimension; // number of parameters in formula
   const string about;  // description
   HoughInfo(const int n, const string s): dimension(n), about(s) { }
   HoughInfo(const HoughInfo &other): dimension(other.dimension), about(other.about) { }
};

//+------------------------------------------------------------------+
//| Main service provider - class compatible with export by pointer  |
//+------------------------------------------------------------------+
class HoughTransform
{
public:
   template<typename T>
   int transform(const HoughImage<T> &image, double &result[], const int elements = 8)
   {
      HoughTransformConcrete<T> *ptr = dynamic_cast<HoughTransformConcrete<T> *>(&this);
      if(ptr) return ptr.extract(image, result, elements);
      return 0;
   }
};

//+------------------------------------------------------------------+
//| Main service worker - template class with actual implementation  |
//+------------------------------------------------------------------+
template<typename T>
class HoughTransformConcrete: public HoughTransform
{
public:
   virtual int extract(const HoughImage<T> &image, double &result[], const int elements = 8) = 0;
};

//+------------------------------------------------------------------+
//| Include (debug) vs import (production)                           |
//+------------------------------------------------------------------+
#ifdef LIB_HOUGH_IMPL_DEBUG // use this in the main program to embed library source inline
#include "../../Libraries/MQL5Book/LibHoughTransform.mq5"
#else
#import "MQL5Book/LibHoughTransform.ex5"
HoughTransform *createHoughTransform(const int quants, const ENUM_DATATYPE type = TYPE_INT);
HoughInfo getHoughInfo();
#import
#endif
//+------------------------------------------------------------------+
