//+------------------------------------------------------------------+
//|                                                    FineClock.mq5 |
//|                                 Copyright 2009, Vladimir Gomonov |
//|                                            MetaDriver@rambler.ru |
//+------------------------------------------------------------------+
#property copyright "(c) 2009, Vladimir Gomonov;   MetaDriver@rambler.ru"
#property link      "MetaDriver@rambler.ru"
#property version   "1.00"
#property description "����������� ������"
#property description "-----------------------------------------------"
#property description "������������ ����� �� ���� ��������"
#property description "����� ������� ����������, ������ ����� � ������"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum eClockFormats
  {
   Seconds=0,       //  ��:��:��
   Minutes=1        //  ��:�� 
  };
// ���� ��������� ��� ����������� 
enum �MyCorners
  {
   CLU = CORNER_LEFT_UPPER,   // ����� �������
   CLL = CORNER_LEFT_LOWER,   // ����� ������
   CRU = CORNER_RIGHT_UPPER,  // ������ �������
   CRL = CORNER_RIGHT_LOWER,  // ������ ������
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum eTimeType
  {
   TLocal,  // ������� �����
   TServer, // ����� ��������� �������
   TGMT,    // ����� �� ��������
  };
#include <\Enums\eIntNumbers.mqh>
#include <\Enums\eFloatNumbers.mqh>
//--- input parameters
input eTimeType     TimeType=TLocal;            // ������� ����
input eClockFormats Fmt=Seconds;                // ������ ����������� 
input �MyCorners    Corner=CRL;                 // ���� ��������
input ePInt         X= 170;                     // �������� �� �����������
input ePInt         Y = 38;                     // �������� �� ���������
input string        FontName="Magneto";         // �����
input ePInt         FontSize=16;                // ������ ������
input color         FontColor=clrDarkSlateGray; // ���� ������
input color         ShadowColor=clrDarkSeaGreen;// ���� ����
input ePInt         SS=1;                       // �������� ����
input eFloat01      eSA=-12;                    // ������� ����
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
         //ObjectSetInteger(i, Clock+j, OBJPROP_SELECTED, j); // ����� �������� - �� �����
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
      //   �������������� ������ ���� ������. ��������, ���.
      if(ChartGetInteger(i,CHART_WINDOW_IS_VISIBLE))
        {
         for(int j=1;j>=0;j--)
           {
            ObjectSetString(i,Clock+(string)j,OBJPROP_TEXT,T);
           }
         // �������� ���� � ������������... �� ������ ������� ��������� ���������.
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
