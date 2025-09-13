//+------------------------------------------------------------------+
//|                                                    Functions.mqh |
//|                        Copyright 2018, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2018, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"

#include <Enum.mqh>
#include <Define.mqh>

double iLowMQL4(string symbol,int tf,int index) {
   if(index < 0)
      return(-1);
   
   double Arr[];
   ENUM_TIMEFRAMES timeframe = TFMigrate(tf);
   
   if(CopyLow(symbol,timeframe, index, 1, Arr)>0)
      return(Arr[0]);
   else
      return(-1);
}

double iHighMQL4(string symbol,int tf,int index) {
   if(index < 0)
      return(-1);
   
   double Arr[];
   ENUM_TIMEFRAMES timeframe=TFMigrate(tf);
   
   if(CopyHigh(symbol,timeframe, index, 1, Arr)>0) 
      return(Arr[0]);
   else
      return(-1);
}

int iHighestMQL4(string symbol, int tf, int type, int count=WHOLE_ARRAY, int start=0)
  {
   if(start<0)
      return(-1);
   
   ENUM_TIMEFRAMES timeframe=TFMigrate(tf);
   
   if(count<=0)
      count=Bars(symbol,timeframe);
   
   if(type<=MODE_OPEN) {
      double Open[];
      ArraySetAsSeries(Open,true);
      CopyOpen(symbol,timeframe,start,count,Open);
      return(ArrayMaximum(Open,0,count)+start);
   }
   if(type==MODE_LOW) {
      double Low[];
      ArraySetAsSeries(Low,true);
      CopyLow(symbol,timeframe,start,count,Low);
      return(ArrayMaximum(Low,0,count)+start);
   }
   if(type==MODE_HIGH) {
      double High[];
      ArraySetAsSeries(High,true);
      CopyHigh(symbol,timeframe,start,count,High);
      return(ArrayMaximum(High,0,count)+start);
   }
   if(type==MODE_CLOSE) {
      double Close[];
      ArraySetAsSeries(Close,true);
      CopyClose(symbol,timeframe,start,count,Close);
      return(ArrayMaximum(Close,0,count)+start);
   }
   if(type==MODE_VOLUME) {
      long Volume[];
      ArraySetAsSeries(Volume,true);
      CopyTickVolume(symbol,timeframe,start,count,Volume);
      return(ArrayMaximum(Volume,0,count)+start);
   }
   if(type>=MODE_TIME) {
      datetime Time[];
      ArraySetAsSeries(Time,true);
      CopyTime(symbol,timeframe,start,count,Time);
      return(ArrayMaximum(Time,0,count)+start);
   }
   return(0);
}

int iLowestMQL4(string symbol, int tf, int type, int count=WHOLE_ARRAY, int start=0)
  {
   if(start<0)
      return(-1);
      
   ENUM_TIMEFRAMES timeframe=TFMigrate(tf);
   
   if(count<=0)
      count=Bars(symbol,timeframe);
   
   if(type<=MODE_OPEN) {
      double Open[];
      ArraySetAsSeries(Open,true);
      CopyOpen(symbol,timeframe,start,count,Open);
      return(ArrayMinimum(Open,0,count)+start);
   }
   if(type==MODE_LOW) {
      double Low[];
      ArraySetAsSeries(Low,true);
      CopyLow(symbol,timeframe,start,count,Low);
      return(ArrayMinimum(Low,0,count)+start);
   }
   if(type==MODE_HIGH) {
      double High[];
      ArraySetAsSeries(High,true);
      CopyHigh(symbol,timeframe,start,count,High);
      return(ArrayMinimum(High,0,count)+start);
   }
   if(type==MODE_CLOSE) {
      double Close[];
      ArraySetAsSeries(Close,true);
      CopyClose(symbol,timeframe,start,count,Close);
      return(ArrayMinimum(Close,0,count)+start);
   }
   if(type==MODE_VOLUME) {
      long Volume[];
      ArraySetAsSeries(Volume,true);
      CopyTickVolume(symbol,timeframe,start,count,Volume);
      return(ArrayMinimum(Volume,0,count)+start);
   }
   if(type>=MODE_TIME) {
      datetime Time[];
      ArraySetAsSeries(Time,true);
      CopyTime(symbol,timeframe,start,count,Time);
      return(ArrayMinimum(Time,0,count)+start);
   }

   return(0);
}

int ObjectsDeleteAllMQL4(int window=EMPTY, int type=EMPTY) {
   return(ObjectsDeleteAll(0,window,type));
}

double CopyBufferMQL4(int handle,int index,int shift)
  {
   double buf[];
   switch(index)
     {
      case 0: if(CopyBuffer(handle,0,shift,1,buf)>0)
         return(buf[0]); break;
      case 1: if(CopyBuffer(handle,1,shift,1,buf)>0)
         return(buf[0]); break;
      case 2: if(CopyBuffer(handle,2,shift,1,buf)>0)
         return(buf[0]); break;
      case 3: if(CopyBuffer(handle,3,shift,1,buf)>0)
         return(buf[0]); break;
      case 4: if(CopyBuffer(handle,4,shift,1,buf)>0)
         return(buf[0]); break;
      default: break;
     }
   return(EMPTY_VALUE);
  }

double iATRMQL4(string symbol,
                int tf,
                int period,
                int shift)
  {
   ENUM_TIMEFRAMES timeframe=TFMigrate(tf);
   int handle=iATR(symbol,timeframe,period);
   if(handle<0)
     {
      Print("The iATR object is not created: Error",GetLastError());
      return(-1);
     }
   else
      return(CopyBufferMQL4(handle,0,shift));
  }
  
datetime iTimeMQL4(string symbol,int tf,int index)
{
   if(index < 0) return(-1);
   ENUM_TIMEFRAMES timeframe=TFMigrate(tf);
   datetime Arr[];
   if(CopyTime(symbol, timeframe, index, 1, Arr)>0)
        return(Arr[0]);
   else return(-1);
}

int HourMQL4()
  {
   MqlDateTime tm;
   TimeCurrent(tm);
   return(tm.hour);
  }
  
int TimeHourMQL4(datetime date)
  {
   MqlDateTime tm;
   TimeToStruct(date,tm);
   return(tm.hour);
  }

int TimeMinuteMQL4(datetime date)
  {
   MqlDateTime tm;
   TimeToStruct(date,tm);
   return(tm.min);
  }

int TimeSecondsMQL4(datetime date)
  {
   MqlDateTime tm;
   TimeToStruct(date,tm);
   return(tm.sec);
  }

// DayOfWeekMQL4() returns the current zero-based day of the week (0-Sunday,1,2,3,4,5,6) of the last known server time.
int DayOfWeekMQL4()
  {
   MqlDateTime tm;
   TimeCurrent(tm);
   return(tm.day_of_week);
  }

  
void SetIndexStyleMQL4(int index,
                       int type,
                       int style=EMPTY,
                       int width=EMPTY,
                       color clr=CLR_NONE)
  {
   if(width>-1)
      PlotIndexSetInteger(index,PLOT_LINE_WIDTH,width);
   if(clr!=CLR_NONE)
      PlotIndexSetInteger(index,PLOT_LINE_COLOR,clr);
   switch(type)
     {
      case 0:
         PlotIndexSetInteger(index,PLOT_DRAW_TYPE,DRAW_LINE);
      case 1:
         PlotIndexSetInteger(index,PLOT_DRAW_TYPE,DRAW_SECTION);
      case 2:
         PlotIndexSetInteger(index,PLOT_DRAW_TYPE,DRAW_HISTOGRAM);
      case 3:
         PlotIndexSetInteger(index,PLOT_DRAW_TYPE,DRAW_ARROW);
      case 4:
         PlotIndexSetInteger(index,PLOT_DRAW_TYPE,DRAW_ZIGZAG);
      case 12:
         PlotIndexSetInteger(index,PLOT_DRAW_TYPE,DRAW_NONE);

      default:
         PlotIndexSetInteger(index,PLOT_DRAW_TYPE,DRAW_LINE);
     }
   switch(style)
     {
      case 0:
         PlotIndexSetInteger(index,PLOT_LINE_STYLE,STYLE_SOLID);
      case 1:
         PlotIndexSetInteger(index,PLOT_LINE_STYLE,STYLE_DASH);
      case 2:
         PlotIndexSetInteger(index,PLOT_LINE_STYLE,STYLE_DOT);
      case 3:
         PlotIndexSetInteger(index,PLOT_LINE_STYLE,STYLE_DASHDOT);
      case 4:
         PlotIndexSetInteger(index,PLOT_LINE_STYLE,STYLE_DASHDOTDOT);

      default: return;
     }
  }
  
int ObjectFindMQL4(string name)
  {
   return(ObjectFind(0,name));
  }
  
int ObjectsTotalMQL4(int type=EMPTY,
                     int window=-1)
  {
   return(ObjectsTotal(0,window,type));
  }

string ObjectNameMQL4(int index)
  {
   return(ObjectName(0,index));
  }

bool ObjectDeleteMQL4(string name)
  {
   return(ObjectDelete(0,name));
  }


int TimeDayOfWeekMQL4(datetime date)
  {
   MqlDateTime tm;
   TimeToStruct(date,tm);
   return(tm.day_of_week);
  }
  
int TimeDayMQL4(datetime date)
  {
   MqlDateTime tm;
   TimeToStruct(date,tm);
   return(tm.day);
  }
  

int MonthMQL4()
  {
   MqlDateTime tm;
   TimeCurrent(tm);
   return(tm.mon);
  }
  
double MarketInfoMQL4(string symbol,
                      int type)
  {
   switch(type)
     {
      case MODE_LOW:
         return(SymbolInfoDouble(symbol,SYMBOL_LASTLOW));
      case MODE_HIGH:
         return(SymbolInfoDouble(symbol,SYMBOL_LASTHIGH));
      case MODE_TIME:
         return((double)SymbolInfoInteger(symbol,SYMBOL_TIME));
      case MODE_BID:
         return(SymbolInfoDouble(symbol, SYMBOL_BID));
      case MODE_ASK:
         return(SymbolInfoDouble(symbol, SYMBOL_ASK));
      case MODE_POINT:
         return(SymbolInfoDouble(symbol,SYMBOL_POINT));
      case MODE_DIGITS:
         return((double)SymbolInfoInteger(symbol,SYMBOL_DIGITS));
      case MODE_SPREAD:
         return((double)SymbolInfoInteger(symbol,SYMBOL_SPREAD));
      case MODE_STOPLEVEL:
         return((double)SymbolInfoInteger(symbol,SYMBOL_TRADE_STOPS_LEVEL));
      case MODE_LOTSIZE:
         return(SymbolInfoDouble(symbol,SYMBOL_TRADE_CONTRACT_SIZE));
      case MODE_TICKVALUE:
         return(SymbolInfoDouble(symbol,SYMBOL_TRADE_TICK_VALUE));
      case MODE_TICKSIZE:
         return(SymbolInfoDouble(symbol,SYMBOL_TRADE_TICK_SIZE));
      case MODE_SWAPLONG:
         return(SymbolInfoDouble(symbol,SYMBOL_SWAP_LONG));
      case MODE_SWAPSHORT:
         return(SymbolInfoDouble(symbol,SYMBOL_SWAP_SHORT));
      case MODE_STARTING:
         return(0);
      case MODE_EXPIRATION:
         return(0);
      case MODE_TRADEALLOWED:
         return(0);
      case MODE_MINLOT:
         return(SymbolInfoDouble(symbol,SYMBOL_VOLUME_MIN));
      case MODE_LOTSTEP:
         return(SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP));
      case MODE_MAXLOT:
         return(SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX));
      case MODE_SWAPTYPE:
         return((double)SymbolInfoInteger(symbol,SYMBOL_SWAP_MODE));
      case MODE_PROFITCALCMODE:
         return((double)SymbolInfoInteger(symbol,SYMBOL_TRADE_CALC_MODE));
      case MODE_MARGINCALCMODE:
         return(0);
      case MODE_MARGININIT:
         return(0);
      case MODE_MARGINMAINTENANCE:
         return(0);
      case MODE_MARGINHEDGED:
         return(0);
      case MODE_MARGINREQUIRED:
         return(0);
      case MODE_FREEZELEVEL:
         return((double)SymbolInfoInteger(symbol,SYMBOL_TRADE_FREEZE_LEVEL));

      default: return(0);
     }
   return(0);
  }
  
double iCloseMQL4(string symbol,int tf,int index)
{
   if(index < 0) return(-1);
   double Arr[];
   ENUM_TIMEFRAMES timeframe=TFMigrate(tf);
   if(CopyClose(symbol,timeframe, index, 1, Arr)>0) 
        return(Arr[0]);
   else return(-1);
}
double iOpenMQL4(string symbol,int tf,int index)

{   
   if(index < 0) return(-1);
   double Arr[];
   ENUM_TIMEFRAMES timeframe=TFMigrate(tf);
   if(CopyOpen(symbol,timeframe, index, 1, Arr)>0) 
        return(Arr[0]);
   else return(-1);
}

bool ObjectCreateMQL4(string name,
                      ENUM_OBJECT type,
                      int window,
                      datetime time1,
                      double price1,
                      datetime time2=0,
                      double price2=0,
                      datetime time3=0,
                      double price3=0)
  {
   return(ObjectCreate(0,name,type,window,
          time1,price1,time2,price2,time3,price3));
  }

bool ObjectSetTextMQL4(string name,
                       string text,
                       int font_size,
                       string font="",
                       color text_color=CLR_NONE)
  {
   int tmpObjType=(int)ObjectGetInteger(0,name,OBJPROP_TYPE);
   if(tmpObjType!=OBJ_LABEL && tmpObjType!=OBJ_TEXT) return(false);
   if(StringLen(text)>0 && font_size>0)
     {
      if(ObjectSetString(0,name,OBJPROP_TEXT,text)==true
         && ObjectSetInteger(0,name,OBJPROP_FONTSIZE,font_size)==true)
        {
         if((StringLen(font)>0)
            && ObjectSetString(0,name,OBJPROP_FONT,font)==false)
            return(false);
         if (text_color>=1 && ObjectSetInteger(0,name,OBJPROP_COLOR,text_color)==false)
            return(false);
         return(true);
        }
      return(false);
     }
   return(false);
  }

bool ObjectSetMQL4(string name,
                   int index,
                   double value)
  {
   switch(index)
     {
     /* case OBJPROP_TIME1:
         ObjectSetInteger(0,name,OBJPROP_TIME,(int)value);return(true);
      case OBJPROP_PRICE1:
         ObjectSetDouble(0,name,OBJPROP_PRICE,value);return(true);
      case OBJPROP_TIME2:
         ObjectSetInteger(0,name,OBJPROP_TIME,1,(int)value);return(true);
      case OBJPROP_PRICE2:
         ObjectSetDouble(0,name,OBJPROP_PRICE,1,value);return(true);
      case OBJPROP_TIME3:
         ObjectSetInteger(0,name,OBJPROP_TIME,2,(int)value);return(true);
      case OBJPROP_PRICE3:
         ObjectSetDouble(0,name,OBJPROP_PRICE,2,value);return(true);*/
      case OBJPROP_COLOR:
         ObjectSetInteger(0,name,OBJPROP_COLOR,(int)value);return(true);
      case OBJPROP_STYLE:
         ObjectSetInteger(0,name,OBJPROP_STYLE,(int)value);return(true);
      case OBJPROP_WIDTH:
         ObjectSetInteger(0,name,OBJPROP_WIDTH,(int)value);return(true);
      case OBJPROP_BACK:
         ObjectSetInteger(0,name,OBJPROP_BACK,(int)value);return(true);
      case OBJPROP_RAY:
         ObjectSetInteger(0,name,OBJPROP_RAY_RIGHT,(int)value);return(true);
      case OBJPROP_ELLIPSE:
         ObjectSetInteger(0,name,OBJPROP_ELLIPSE,(int)value);return(true);
      case OBJPROP_SCALE:
         ObjectSetDouble(0,name,OBJPROP_SCALE,value);return(true);
      case OBJPROP_ANGLE:
         ObjectSetDouble(0,name,OBJPROP_ANGLE,value);return(true);
      case OBJPROP_ARROWCODE:
         ObjectSetInteger(0,name,OBJPROP_ARROWCODE,(int)value);return(true);
      case OBJPROP_TIMEFRAMES:
         ObjectSetInteger(0,name,OBJPROP_TIMEFRAMES,(int)value);return(true);
      case OBJPROP_DEVIATION:
         ObjectSetDouble(0,name,OBJPROP_DEVIATION,value);return(true);
      case OBJPROP_FONTSIZE:
         ObjectSetInteger(0,name,OBJPROP_FONTSIZE,(int)value);return(true);
      case OBJPROP_CORNER:
         ObjectSetInteger(0,name,OBJPROP_CORNER,(int)value);return(true);
      case OBJPROP_XDISTANCE:
         ObjectSetInteger(0,name,OBJPROP_XDISTANCE,(int)value);return(true);
      case OBJPROP_YDISTANCE:
         ObjectSetInteger(0,name,OBJPROP_YDISTANCE,(int)value);return(true);
     // case OBJPROP_FIBOLEVELS:
     //    ObjectSetInteger(0,name,OBJPROP_LEVELS,(int)value);return(true);
      case OBJPROP_LEVELCOLOR:
         ObjectSetInteger(0,name,OBJPROP_LEVELCOLOR,(int)value);return(true);
      case OBJPROP_LEVELSTYLE:
         ObjectSetInteger(0,name,OBJPROP_LEVELSTYLE,(int)value);return(true);
      case OBJPROP_LEVELWIDTH:
         ObjectSetInteger(0,name,OBJPROP_LEVELWIDTH,(int)value);return(true);

      default: return(false);
     }
   return(false);
  }

ENUM_MA_METHOD MethodMigrate(int method)
  {
   switch(method)
     {
      case 0: return(MODE_SMA);
      case 1: return(MODE_EMA);
      case 2: return(MODE_SMMA);
      case 3: return(MODE_LWMA);
      default: return(MODE_SMA);
     }
  }
ENUM_APPLIED_PRICE PriceMigrate(int price)
  {
   switch(price)
     {
      case 1: return(PRICE_CLOSE);
      case 2: return(PRICE_OPEN);
      case 3: return(PRICE_HIGH);
      case 4: return(PRICE_LOW);
      case 5: return(PRICE_MEDIAN);
      case 6: return(PRICE_TYPICAL);
      case 7: return(PRICE_WEIGHTED);
      default: return(PRICE_CLOSE);
     }
  }
ENUM_STO_PRICE StoFieldMigrate(int field)
  {
   switch(field)
     {
      case 0: return(STO_LOWHIGH);
      case 1: return(STO_CLOSECLOSE);
      default: return(STO_LOWHIGH);
     }
  }
//+------------------------------------------------------------------+
enum ALLIGATOR_MODE  { MODE_GATORJAW=1,   MODE_GATORTEETH, MODE_GATORLIPS };
enum ADX_MODE        { MODE_MAIN,         MODE_PLUSDI, MODE_MINUSDI };
enum UP_LOW_MODE     { MODE_BASE,         MODE_UPPER,      MODE_LOWER };
enum ICHIMOKU_MODE   { MODE_TENKANSEN=1,  MODE_KIJUNSEN, MODE_SENKOUSPANA, MODE_SENKOUSPANB, MODE_CHINKOUSPAN };
//enum MAIN_SIGNAL_MODE{ MODE_MAIN,         MODE_SIGNAL };

double iMAMQL4(string symbol,
               int tf,
               int period,
               int ma_shift,
               int method,
               int price,
               int shift)
  {
   ENUM_TIMEFRAMES timeframe=TFMigrate(tf);
   ENUM_MA_METHOD ma_method=MethodMigrate(method);
   ENUM_APPLIED_PRICE applied_price=PriceMigrate(price);
   int handle=iMA(symbol,timeframe,period,ma_shift,
                  ma_method,applied_price);
   if(handle<0)
     {
      Print("The iMA object is not created: Error",GetLastError());
      return(-1);
     }
   else
      return(CopyBufferMQL4(handle,0,shift));
  }
  
double iMAOnArrayMQL4(double &array[],
                      int total,
                      int period,
                      int ma_shift,
                      int ma_method,
                      int shift)
  {
   double buf[],arr[];
   if(total==0) total=ArraySize(array);
   if(total>0 && total<=period) return(0);
   if(shift>total-period-ma_shift) return(0);
   switch(ma_method)
     {
      case MODE_SMA :
        {
         total=ArrayCopy(arr,array,0,shift+ma_shift,period);
         if(ArrayResize(buf,total)<0) return(0);
         double sum=0;
         int    i,pos=total-1;
         for(i=1;i<period;i++,pos--)
            sum+=arr[pos];
         while(pos>=0)
           {
            sum+=arr[pos];
            buf[pos]=sum/period;
            sum-=arr[pos+period-1];
            pos--;
           }
         return(buf[0]);
        }
      case MODE_EMA :
        {
         if(ArrayResize(buf,total)<0) return(0);
         double pr=2.0/(period+1);
         int    pos=total-2;
         while(pos>=0)
           {
            if(pos==total-2) buf[pos+1]=array[pos+1];
            buf[pos]=array[pos]*pr+buf[pos+1]*(1-pr);
            pos--;
           }
         return(buf[shift+ma_shift]);
        }
      case MODE_SMMA :
        {
         if(ArrayResize(buf,total)<0) return(0);
         double sum=0;
         int    i,k,pos;
         pos=total-period;
         while(pos>=0)
           {
            if(pos==total-period)
              {
               for(i=0,k=pos;i<period;i++,k++)
                 {
                  sum+=array[k];
                  buf[k]=0;
                 }
              }
            else sum=buf[pos+1]*(period-1)+array[pos];
            buf[pos]=sum/period;
            pos--;
           }
         return(buf[shift+ma_shift]);
        }
      case MODE_LWMA :
        {
         if(ArrayResize(buf,total)<0) return(0);
         double sum=0.0,lsum=0.0;
         double price;
         int    i,weight=0,pos=total-1;
         for(i=1;i<=period;i++,pos--)
           {
            price=array[pos];
            sum+=price*i;
            lsum+=price;
            weight+=i;
           }
         pos++;
         i=pos+period;
         while(pos>=0)
           {
            buf[pos]=sum/weight;
            if(pos==0) break;
            pos--;
            i--;
            price=array[pos];
            sum=sum-lsum+price*period;
            lsum-=array[i];
            lsum+=price;
           }
         return(buf[shift+ma_shift]);
        }
      default: return(0);
     }
   return(0);
  }

int iBarsMQL4(string symbol,int tf)
  {
   ENUM_TIMEFRAMES timeframe=TFMigrate(tf);
   return(Bars(symbol,timeframe));
  }