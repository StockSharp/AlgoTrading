template<typename T>
class Generator
{
  public:
    virtual T operator++() = 0;
};

template<typename T>
class ItemGenerator
{
  public:
    virtual bool addItemTo(T *object) = 0;
};

template<typename T>
class SequenceGenerator: public Generator<T>
{
  protected:
    T current;
    int max;
    int count;

  public:
    SequenceGenerator(const T start = NULL, const int _max = 0): current(start), max(_max), count(0) {}
    virtual void reset(const T start, const int _max = 0)
    {
      current = start;
      count = 0;
      max = _max;
    }

    virtual T operator++() = 0;
};

template<typename T>
class SimpleSequenceGenerator: public SequenceGenerator<T>
{
  public:
    SimpleSequenceGenerator(const T start = NULL, const int _max = 0): SequenceGenerator(start, _max) {}

    virtual T operator++() override
    {
      ulong ul = (ulong)current;
      ul++;
      count++;
      if(count > max) return NULL;
      current = (T)ul;
      return current;
    }
};
