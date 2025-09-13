//+------------------------------------------------------------------+
//|                                                    FineClock.mq5 |
//|                                 Copyright 2009, Vladimir Gomonov |
//|                                            MetaDriver@rambler.ru |
//+------------------------------------------------------------------+
#property copyright "(c) 2009, Vladimir Gomonov;   MetaDriver@rambler.ru"
#property link      "MetaDriver@rambler.ru"
#property version   "1.00"
#property description "Симпатичные часики"
#property description "-----------------------------------------------"
#property description "Отображаются сразу на всех графиках"
#property description "Можно вручную перемещать, менять шрифт и размер"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum eClockFormats
  {
   Seconds=0,       //  чч:мм:сс
   Minutes=1        //  чч:мм 
  };
// Углы переписал для русификации 
enum еMyCorners
  {
   CLU = CORNER_LEFT_UPPER,   // Левый верхний
   CLL = CORNER_LEFT_LOWER,   // Левый нижний
   CRU = CORNER_RIGHT_UPPER,  // Правый верхний
   CRL = CORNER_RIGHT_LOWER,  // Правый нижний
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum eTimeType
  {
   TLocal,  // Местное время
   TServer, // Время торгового сервера
   TGMT,    // Время по Гринвичу
  };
#include <\Enums\eIntNumbers.mqh>
#include <\Enums\eFloatNumbers.mqh>
//--- input parameters
input eTimeType     TimeType=TLocal;            // Часовой пояс
input eClockFormats Fmt=Seconds;                // Формат отображения 
input еMyCorners    Corner=CRL;                 // Угол привязки
input ePInt         X= 170;                     // Смещение по горизонтали
input ePInt         Y = 38;                     // Смещение по вертикали
input string        FontName="Magneto";         // Шрифт
input ePInt         FontSize=16;                // Размер шрифта
input color         FontColor=clrDarkSlateGray; // Цвет шрифта
input color         ShadowColor=clrDarkSeaGreen;// Цвет тени
input ePInt         SS=1;                       // Смещение тени
input eFloat01      eSA=-12;                    // Поворот тени
//--- vars
string Clock="Clock";
double SA;
bool First=true;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   SA=eFloatToDouble(eSA);
//---
   for(long i=ChartNext(0);i>0;i=ChartNext(i))
     {
      for(int j=0;j<2;j++)
        {
         if(bool(ObjectFind(i,Clock+(string)j)+1)) // ;-)
            ObjectDelete(i,Clock+(string)j);
         ObjectCreate(i,Clock+(string)j,OBJ_LABEL,0,0,0);
         ObjectSetInteger(i,Clock+(string)j,OBJPROP_CORNER,Corner);
         ObjectSetInteger(i,Clock+(string)j,OBJPROP_XDISTANCE,X-Fmt*X/3+j*SS);
         ObjectSetInteger(i,Clock+(string)j,OBJPROP_YDISTANCE,Y+j*SS);
         ObjectSetInteger(i,Clock+(string)j,OBJPROP_COLOR,FontColor);
         ObjectSetInteger(i,Clock+(string)j,OBJPROP_FONTSIZE,FontSize);
         ObjectSetInteger(i,Clock+(string)j,OBJPROP_SELECTABLE,j);
         ObjectSetInteger(i,Clock+(string)j,OBJPROP_ANCHOR,ANCHOR_LEFT);
         ObjectSetInteger(i,Clock+(string)j,OBJPROP_BACK,true);
         ObjectSetString(i,Clock+(string)j,OBJPROP_FONT,FontName);
         ObjectSetString(i,Clock+(string)j,OBJPROP_TEXT,
                         " "+TimeToString(Time(),Fmt ? TIME_MINUTES : TIME_SECONDS)+" ");
         //ObjectSetInteger(i, Clock+j, OBJPROP_SELECTED, j); // можно добавить - по вкусу
        }
      ObjectSetInteger(i,Clock+"0",OBJPROP_COLOR,ShadowColor);
      ObjectSetDouble(i,Clock+"0",OBJPROP_ANGLE,SA);
      ChartRedraw(i);
     }
   if(Fmt==Seconds) { EventSetTimer(1); First=false; }
   else EventSetTimer((int)60-int(TimeLocal()%60));
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   EventKillTimer();
   for(long i=ChartNext(0);i>0;i=ChartNext(i))
      for(int j=0;j<2;j++)
         ObjectDelete(i,Clock+(string)j);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTimer()
  {
   if(First) { EventSetTimer(60); First=false; }
   string T=" "+TimeToString(Time(),Fmt ? TIME_MINUTES : TIME_SECONDS)+" ";
   for(long i=ChartNext(0);i>0;i=ChartNext(i))
      //   Перерисовываем только если видимо. Экономия, мда.
      if(ChartGetInteger(i,CHART_WINDOW_IS_VISIBLE))
        {
         for(int j=1;j>=0;j--)
           {
            ObjectSetString(i,Clock+(string)j,OBJPROP_TEXT,T);
           }
         // Приводим тень в соответствие... На случай ручного изменения атрибутов.
         ObjectSetInteger(i,Clock+"0",OBJPROP_XDISTANCE,
                          ObjectGetInteger(i,Clock+"1",OBJPROP_XDISTANCE)-SS);
         ObjectSetInteger(i,Clock+"0",OBJPROP_YDISTANCE,
                          ObjectGetInteger(i,Clock+"1",OBJPROP_YDISTANCE)-SS);
         ObjectSetString(i,Clock+"0",OBJPROP_FONT,
                         ObjectGetString(i,Clock+"1",OBJPROP_FONT));
         ObjectSetInteger(i,Clock+"0",OBJPROP_FONTSIZE,
                          ObjectGetInteger(i,Clock+"1",OBJPROP_FONTSIZE));

         ChartRedraw(i);
        }
  }
//+------------------------------------------------------------------+

datetime Time()
  {
   switch(TimeType)
     {
      case TLocal:  return TimeLocal();
      case TServer: return TimeTradeServer();
      case TGMT:    return TimeGMT();
      default:  return TimeLocal();
     }
  }
//+------------------------------------------------------------------+
