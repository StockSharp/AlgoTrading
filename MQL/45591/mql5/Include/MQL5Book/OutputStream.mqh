//+------------------------------------------------------------------+
//|                                                 OutputStream.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Print a sequence of data via overloaded operator<<               |
//+------------------------------------------------------------------+
class OutputStream
{
protected:
   int     digits;
   ushort  delimiter;
   int     timemode;
   string  line;

   // remove trailing delimiter
   string trimDelimiter() const
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
   
   // add new component into the string using delimiter as a glue
   void appendWithDelimiter(const string v, const bool skipDelimiter = false)
   {
      line += v;
      if(delimiter != 0 && !skipDelimiter)
      {
         line += ShortToString(delimiter);
      }
   }
   
   // detach the string from this and hand over it to caller
   string move()
   {
      string result = trimDelimiter();
      line = NULL;
      return result;
   }

public:
   OutputStream(int p = 0, ushort d = 0, int t = TIME_DATE|TIME_MINUTES):
      digits(p ? p : _Digits), delimiter(d), timemode(t) { }
   OutputStream(string d, int p = 0, int t = TIME_DATE|TIME_MINUTES):
      digits(p ? p : _Digits),
      delimiter(d[0]),
      timemode(t) { }

   // flush content into Print or add more from other stream
   OutputStream *operator<<(OutputStream &self)
   {
      // passing itself into << means flush
      if(&this == &self)
      {
         Print(trimDelimiter());
         line = NULL;
      }
      else // append other stream data to this
      {
         this << self.move();
      }
      return &this;
   }

   OutputStream *operator<<(const double v)
   {
      appendWithDelimiter(v == EMPTY_VALUE ? "<EMPTY_VALUE>" : StringFormat("%.*f", digits, v));
      return &this;
   }

   OutputStream *operator<<(const datetime v)
   {
      appendWithDelimiter(TimeToString((datetime)v, timemode));
      return &this;
   }

   OutputStream *operator<<(const ushort v)
   {
      appendWithDelimiter(ShortToString(v));
      return &this;
   }

   OutputStream *operator<<(const color v)
   {
      string result = (string)v;
      // non-embedded colors print in hex
      if(StringFind(result, "clr") != 0)
      {
         result = "clr" + StringFormat("%02X%02X%02X",
            ((int)v & 0xFF), (((int)v >> 8) & 0xFF), (((int)v >> 16) & 0xFF));
      }
      appendWithDelimiter(result);
      return &this;
   }

   template<typename T>
   OutputStream *operator<<(const T v)
   {
      appendWithDelimiter((string)v);
      return &this;
   }

   template<typename T>
   OutputStream *operator<<(T &a[])
   {
      int n = ArraySize(a);
      this << this;
      appendWithDelimiter("[", true);
      
      // we could use ArrayPrint (need to respect datetime settings in flags)
      // ArrayPrint(a, digits, ShortToString(delimiter), 0, WHOLE_ARRAY, flags);
      
      for(int i = 0; i < n; i++)
      {
         this << a[i];
      }
      line = trimDelimiter();
      appendWithDelimiter("]", true);
      this << this;
      
      return &this;
   }

   template<typename T>
   OutputStream *operator<<(const T *v)
   {
      appendWithDelimiter(typename(v) + StringFormat("%lld", v));
      return &this;
   }
};
//+------------------------------------------------------------------+
/*
// USAGE
OutputStream out(5, ',', TIME_DATE|TIME_MINUTES|TIME_SECONDS);
void OnStart()
{
   bool b = true;
   datetime dt = TimeCurrent();
   color clr = Red;

   out << M_PI << "text" << b << dt << clr << out;
}
*/
//+------------------------------------------------------------------+
