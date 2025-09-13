class SORT
{
  private:
    template<typename T>
    static void Swap(T &Array[], const int i, const int j)
    {
      const T Temp = Array[i];
  
      Array[i] = Array[j];
      Array[j] = Temp;
    }
  
    template<typename T1, typename T2>
    static int Partition(T1 &Array[], const T2 &Compare, const int Start, const int End)
    {
      int Marker = Start;
  
      for(int i = Start; i <= End; i++)
      {
        if(Compare.Compare(Array[i], Array[End]) <= 0)
        {
          Swap(Array, i, Marker);
  
          Marker++;
        }
      }
  
      return(Marker - 1);
    }
  
    template<typename T1, typename T2>
    static void QuickSort(T1 &Array[], const T2 &Compare, const int Start, const int End)
    {
      if(Start < End)
      {
        const int Pivot = Partition(Array, Compare, Start, End);
  
        QuickSort(Array, Compare, Start, Pivot - 1);
        QuickSort(Array, Compare, Pivot + 1, End);
      }
    }
  
  public:
    /* MQL-like ArraySort:
         void&       array[]                   // array to sort
         void&       Compare                   // comparator
         int         count = WHOLE_ARRAY,      // number of elements
         int         start = 0,                // starting index
    */
    template<typename T1, typename T2>
    static void Sort(T1 &Array[], const T2 &Compare, int Count = WHOLE_ARRAY, const int Start = 0)
    {
      if(Count == WHOLE_ARRAY)
        Count = ArraySize(Array);
  
        QuickSort(Array, Compare, Start, Start + Count - 1);
    }
};

template<typename T>
class COMPARE
{
  protected:
    int Direction;
  
  public:
    COMPARE(const int iMode = +1)
    {
      Direction = iMode;
    }
  
    virtual int Compare(const T &First, const T &Second) const
    {
      return 0;
    }
};

template<typename T>
class DefaultCompare: public COMPARE<T>
{
  public:
    DefaultCompare(const int iMode = +1): COMPARE(iMode) {}
  
    virtual int Compare(const T &First, const T &Second) const override
    {
      return (First > Second) ? Direction : -Direction;
    }
};
