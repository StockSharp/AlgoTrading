//+------------------------------------------------------------------+
//|                                                          ZZP.mq4 |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, TheXpert"
#property link      "theforexpert@gmail.com"

#property indicator_chart_window

extern double ChannelPercent = 0.08;

// attention! when used, this option can lead to incorrect visualization of last ZZ ray at 0 bar
// incorrect visualization will be redrawn when new bar appears
// use it carefully and don't expect complete correctness
// never(!) use this indicator values from 0 bar in your automated strategies.


// never(!) use this option if this indicator is used in automated strategy.
// if used, this option can lead to incorrect extremum parsing in your EA

/* extern */ bool DrawZeroBar = false;

#property indicator_buffers 5

#property indicator_color1 LightGray
#property indicator_color2 LightGray

double Maxs[];
double Mins[];

double Direction[];

double MaxTime[];
double MinTime[];

string symbol;

#define UP 0.0001
#define DN -0.0001
#define NONE 0

bool Inited;

datetime LastTime;

int init()
{
   SetIndexBuffer(0, Maxs);
   SetIndexBuffer(1, Mins);
   
   SetIndexBuffer(2, MaxTime);
   SetIndexBuffer(3, MinTime);
   
   SetIndexBuffer(4, Direction);

   SetIndexStyle(0, DRAW_ZIGZAG);
   SetIndexStyle(1, DRAW_ZIGZAG);

   SetIndexStyle(2, DRAW_NONE);
   SetIndexStyle(3, DRAW_NONE);
   SetIndexStyle(4, DRAW_NONE);

   symbol = Symbol();
   
   Inited = false;
   LastTime = 0;
   
   return(0);
}

int start()
{
   if (!Inited)
   {
      ArrayInitialize(Mins, EMPTY_VALUE);
      ArrayInitialize(Maxs, EMPTY_VALUE);
      ArrayInitialize(MinTime, EMPTY_VALUE);
      ArrayInitialize(MaxTime, EMPTY_VALUE);
      ArrayInitialize(Direction, EMPTY_VALUE);
   }
   
   int ToCount = Bars - IndicatorCounted();
   
   // DON'T fix to >= 0. For the 0 bar the logic is in DrawZeroBar block
   for (int i = ToCount - 1; i > 0; i--)
   {
      if (i < Bars - 1)
      {
         MinTime[i] = MinTime[i + 1];
         MaxTime[i] = MaxTime[i + 1];
         Direction[i] = Direction[i + 1];
         Maxs[i] = EMPTY_VALUE;
         Mins[i] = EMPTY_VALUE;
      }
      
      Check(i);
   }
   
   if (DrawZeroBar)
   {
      if (LastTime != Time[0])
      {
         LastTime = Time[0];

         MinTime[0] = MinTime[1];
         MaxTime[0] = MaxTime[1];
         Direction[0] = Direction[1];
         Maxs[0] = EMPTY_VALUE;
         Mins[0] = EMPTY_VALUE;
      }
      
      CheckPrice(0, Close[0]);
   }
   else
   {
      Maxs[0] = EMPTY_VALUE;
      Mins[0] = EMPTY_VALUE;
   }
   
   return(0);
}

void Check(int offset)
{
   double o = Open[offset];
   double h = High[offset];
   double l = Low[offset];
   double c = Close[offset];

   if (c < o)
   {
      CheckPrice(offset, h);
      CheckPrice(offset, l);
   }
   else
   {
      CheckPrice(offset, l);
      CheckPrice(offset, h);
   }
}

void CheckPrice(int offset, double p)
{
   if (!Inited)
   {
      CheckInit(offset, p);
      Inited = true;
   }
   else
   {
      if (Direction[offset] == UP) CheckUp(offset, p);
      else if (Direction[offset] == DN) CheckDn(offset, p);
   }
}

void CheckInit(int offset, double c)
{
   MaxTime[offset] = Time[offset];
   MinTime[offset] = Time[offset];
   
   Maxs[offset] = c;
   Mins[offset] = c;
      
   Direction[offset] = UP;
}

void CheckUp(int offset, double c)
{
   int lastOffset = iBarShift(symbol, 0, MaxTime[offset]);
   if (Maxs[lastOffset] == EMPTY_VALUE)
   {
      Maxs[lastOffset] = High[lastOffset];
   }
   
   if (c > Maxs[lastOffset])
   {
      Maxs[lastOffset] = EMPTY_VALUE;
      Maxs[offset] = c;
      MaxTime[offset] = Time[offset];
      lastOffset = offset;
   }
   
   if (c*(1 + ChannelPercent/100.0) < Maxs[lastOffset])
   {
      Direction[offset] = DN;
      
      Mins[offset] = c;
      MinTime[offset] = Time[offset];
   }
}

void CheckDn(int offset, double c)
{
   int lastOffset = iBarShift(symbol, 0, MinTime[offset]);
   if (Mins[lastOffset] == EMPTY_VALUE)
   {
      Mins[lastOffset] = Low[lastOffset];
   }
   
   if (c < Mins[lastOffset])
   {
      Mins[lastOffset] = EMPTY_VALUE;
      Mins[offset] = c;
      MinTime[offset] = Time[offset];
      lastOffset = offset;
   }
   
   if (c*(1 - ChannelPercent/100.0) > Mins[lastOffset])
   {
      Direction[offset] = UP;

      Maxs[offset] = c;
      MaxTime[offset] = Time[offset];
   }
}