//+------------------------------------------------------------------+
//|                                         PermutationGenerator.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/SimpleArray.mqh>

//+------------------------------------------------------------------+
//| Simple class for permutations generation                         |
//+------------------------------------------------------------------+
class PermutationGenerator
{
public:
   struct Result
   {
      int indices[]; // element indices in every position, e.g.
   };                // indices of chars from alphabet for every place in a string
   
private:
   const int len; // number of positions in each set (set size)
   const int n;   // number of different elements to choose for each position
   
   SimpleArray<Result> result; // here all variants are accumulated

   int indices[]; // array of element indices in current combination

   int gen(const int offset)
   {
      int depth = offset;
      
      // loop through positions in the set
      for(int i = offset; i < len; ++i)
      {
         // loop through candidate elements for every position
         for(int j = 0; j < n; ++j)
         {
            // place j-th element at i-th position
            indices[i] = j;
            
            if(i < len - 1)
            {
               // recursively do it for next positions
               depth = gen(i + 1);
            }
            else // i == len - 1, this is the last position
            {
               // got indices of next set of elements, the array is filled
               // ArrayPrint(indices); // debug
               Result r;
               ArrayCopy(r.indices, indices);
               result << r;
            }
            
         }
         // depth controls when job is done
         if(depth == len - 1)
         {
            break;
         }
      }
      return depth;
   }
   
public:
   PermutationGenerator(const int length, const int elements) : len(length), n(elements) { }
   
   // main worker method
   SimpleArray<Result> *run()
   {
      // if a result exists already, return it right away
      if(result.size() > 0) return &result;
      
      ArrayResize(indices, len);
      gen(0);
      return &result;
   }
};
//+------------------------------------------------------------------+