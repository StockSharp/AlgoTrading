class Printable
{
  public:
    virtual string toString(){return "?";};
};

class CFormatOut: public Printable
{
  private:
    int digits;
    char delimiter;
    int timemode;
    string format;
    string line;
    
    void createFormat()
    {
      format = "%." + (string)digits + "f";
    }
    
    string trimDelimiter(/*const string line*/)
    {
      if(delimiter != 0)
      {
        int n = StringLen(line);
        if(n > 0)
        {
          if(StringGetCharacter(line, n - 1) == delimiter)
          {
            return StringSubstr(line, 0, n - 1);
          }
        }
      }
      return line;
    }
   
  public:
    CFormatOut(): digits(_Digits), timemode(TIME_DATE|TIME_MINUTES)
    {
      createFormat();
    }
    CFormatOut(int p): digits(p), timemode(TIME_DATE|TIME_MINUTES)
    {
      createFormat();
    }
    CFormatOut(int p, char d): digits(p), delimiter(d), timemode(TIME_DATE|TIME_MINUTES)
    {
      createFormat();
    }
    CFormatOut(int p, char d, int t): digits(p), delimiter(d), timemode(t)
    {
      createFormat();
    }

    CFormatOut *operator<<(const string v)
    {
      line += v;
      if(delimiter != 0)
      {
        line += CharToString(delimiter);
      }
      return(GetPointer(this));
    }

    template<typename T>
    CFormatOut *operator<<(const T v)
    {
      if(typename(v) == "double" || typename(v) == "float")
      {
        if(v == EMPTY_VALUE) line += "<EMPTY_VALUE>";
        else line += StringFormat(format, v);
      }
      else
      if(typename(v) == "datetime")
      {
        line += TimeToString((datetime)v, timemode);
      }
      else
      if(typename(v) == "color")
      {
        line += StringFormat("%02X%02X%02X", ((int)v & 0xFF), (((int)v >> 8) & 0xFF), (((int)v >> 16) & 0xFF));
      }
      else
      if(typename(v) == "char" || typename(v) == "uchar")
      {
        if(v == '\n')
        {
          Print(trimDelimiter(/*line*/));
          line = NULL;
          return(GetPointer(this));
        }
        else
        {
          line += CharToString((char)v);
        }
      }
      else
      if(typename(v) == "ushort" && v == '\n')
      {
        Print(trimDelimiter(/*line*/));
        line = NULL;
        return(GetPointer(this));
      }
      else
      {
        line += (string)v;
      }
      if(delimiter != 0)
      {
        line += CharToString(delimiter);
      }
      return(GetPointer(this));
    }
    
    template<typename T>
    CFormatOut *operator<<(T &a[])
    {
      int n = ArraySize(a);
      for(int i = 0; i < n; i++)
      {
        this << a[i];
      }
      return(GetPointer(this));
    }
    
    template<typename T>
    CFormatOut *operator<<(const T *v)
    {
      line += typename(v) + StringFormat("%X", v);
      if(delimiter != 0)
      {
        line += CharToString(delimiter);
      }
      return GetPointer(this);
    }
    
    virtual string toString()
    {
      return (string)digits + " " + (string)delimiter + " " + (string)timemode;
    }
    
    string operator>>(bool flash = false) const
    {
      return line;
    }
    
};

class ObjectToString
{
  public:
    template<typename T>
    static string toString(T &v)
    {
      string s = StringFormat("%X", GetPointer(v));
      return typename(v) + s;
    }
};
  
template<typename T> string EN(T enum_value)    { return(EnumToString(enum_value)); }

#define O2S(V) ObjectToString::toString(V)
#define EOL (char)'\n'

// USAGE
/*
CFormatOut     OUT(5, ',', TIME_DATE|TIME_MINUTES|TIME_SECONDS);
void OnStart()
{
  bool x = true;
  enum days
  {
    one,
    two,
    three
  };
  
  datetime dt = TimeCurrent();
  color clr = Red;

  days day = two;
  OUT<<M_PI<<"test"<<x<<EN(day)<<dt<<clr<<EOL;//<<" Test"<<x<<EOL;

}
*/
