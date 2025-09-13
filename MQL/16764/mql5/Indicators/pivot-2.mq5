//+------------------------------------------------------------------+ 
//|                                                      Pivot-2.mq5 | 
//|                                       Copyright � 2004, Aborigen | 
//|                                          http://forex.kbpauk.ru/ | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2004, Aborigen"
#property link "http://forex.kbpauk.ru/"
//--- ����� ������ ����������
#property version   "1.00"
#property description "����� ������������� � ���������"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window
//--- ���������� ������������ ������� 7
#property indicator_buffers 7 
//--- ������������ ����� ���� ����������� ����������
#property indicator_plots   7
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET 0                      // ��������� ��� �������� ��������� ������� �� �������� ����������
#define INDICATOR_NAME "Pivot-2"     // ��������� ��� ����� ����������
//+----------------------------------------------+
//|  ��������� ��������� ���������� 1            |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �����
#property indicator_type1   DRAW_LINE
//--- � �������� ����� ���������� �����������
#property indicator_color1  clrTeal
//--- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//--- ������� ����� ���������� 1 ����� 2
#property indicator_width1  2
//--- ����������� ����� ����������
#property indicator_label1  "Res 3"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 2            |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//--- � �������� ����� ���������� �����������
#property indicator_color2  clrDodgerBlue
//--- ����� ���������� - ����������� ������
#property indicator_style2  STYLE_SOLID
//--- ������� ����� ���������� 2 ����� 2
#property indicator_width2  2
//--- ����������� ����� ����������
#property indicator_label2  "Res 2"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 3            |
//+----------------------------------------------+
//--- ��������� ���������� 3 � ���� �����
#property indicator_type3   DRAW_LINE
//--- � �������� ����� ���������� �����������
#property indicator_color3  clrLime
//--- ����� ���������� - ����������� ������
#property indicator_style3  STYLE_SOLID
//--- ������� ����� ���������� 3 ����� 2
#property indicator_width3  2
//--- ����������� ����� ����������
#property indicator_label3  "Res 1"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 4            |
//+----------------------------------------------+
//--- ��������� ���������� 4 � ���� �����
#property indicator_type4   DRAW_LINE
//--- � �������� ����� ���������� �����������
#property indicator_color4  clrBlueViolet
//--- ����� ���������� - ����������� ������
#property indicator_style4  STYLE_SOLID
//--- ������� ����� ���������� 4 ����� 4
#property indicator_width4  4
//--- ����������� ����� ����������
#property indicator_label4  "Pivot"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 5            |
//+----------------------------------------------+
//--- ��������� ���������� 5 � ���� �����
#property indicator_type5   DRAW_LINE
//--- � �������� ����� ���������� �����������
#property indicator_color5  clrRed
//--- ����� ���������� - ����������� ������
#property indicator_style5  STYLE_SOLID
//--- ������� ����� ���������� 5 ����� 2
#property indicator_width5  2
//--- ����������� ����� ����������
#property indicator_label5  "Sup 1"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 6            |
//+----------------------------------------------+
//--- ��������� ���������� 6 � ���� �����
#property indicator_type6   DRAW_LINE
//--- � �������� ����� ���������� �����������
#property indicator_color6  clrMagenta
//--- ����� ���������� - ����������� ������
#property indicator_style6  STYLE_SOLID
//--- ������� ����� ���������� 6 ����� 2
#property indicator_width6  2
//--- ����������� ����� ����������
#property indicator_label6  "Sup 2"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 7            |
//+----------------------------------------------+
//--- ��������� ���������� 7 � ���� �����
#property indicator_type7   DRAW_LINE
//--- � �������� ����� ���������� �����������
#property indicator_color7  clrBrown
//--- ����� ���������� - ����������� ������
#property indicator_style7  STYLE_SOLID
//--- ������� ����� ���������� 7 ����� 2
#property indicator_width7  2
//--- ����������� ����� ����������
#property indicator_label7  "Sup 3"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int Shift=0;                 // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double Ind1Buffer[];
double Ind2Buffer[];
double Ind3Buffer[];
double Ind4Buffer[];
double Ind5Buffer[];
double Ind6Buffer[];
double Ind7Buffer[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- �������� ���������� ���������� �� ������������
   if(!TimeFramesCheck(INDICATOR_NAME,Period())) return(INIT_FAILED);
//--- ������������� ���������� 
   min_rates_total=2*PeriodSeconds(PERIOD_D1)/PeriodSeconds(Period());
//--- ������������� ������������ �������
   IndInit(0,Ind1Buffer,0.0,min_rates_total,Shift);
   IndInit(1,Ind2Buffer,0.0,min_rates_total,Shift);
   IndInit(2,Ind3Buffer,0.0,min_rates_total,Shift);
   IndInit(3,Ind4Buffer,0.0,min_rates_total,Shift);
   IndInit(4,Ind5Buffer,0.0,min_rates_total,Shift);
   IndInit(5,Ind6Buffer,0.0,min_rates_total,Shift);
   IndInit(6,Ind7Buffer,0.0,min_rates_total,Shift);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   string shortname;
   StringConcatenate(shortname,INDICATOR_NAME,"(",Shift,")");
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+    
void OnDeinit(const int reason)
  {
//---
   Comment("");
//---
  }
//+------------------------------------------------------------------+  
//| Custom iteration function                                        | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(RESET);
//--- ���������� ������������� ����������
   int limit,bar;
//--- ���������� ���������� � ��������� ������  
   double P,S1,R1,S2,R2,S3,R3;
   static double LastHigh,LastLow;
//---    
   datetime iTime[1];
   static uint LastCountBar;
//--- ������� ������������ ���������� ���������� ������
//--- � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
      LastCountBar=rates_total;
      LastHigh=0;
      LastLow=999999999;
     }
   else limit=int(LastCountBar)+rates_total-prev_calculated; // ��������� ����� ��� ������� ����� ����� 
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(time,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(open,true);
//--- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Ind1Buffer[bar]=0.0;
      Ind2Buffer[bar]=0.0;
      Ind3Buffer[bar]=0.0;
      Ind4Buffer[bar]=0.0;
      Ind5Buffer[bar]=0.0;
      Ind6Buffer[bar]=0.0;
      Ind7Buffer[bar]=0.0;

      if(high[bar+1]>LastHigh) LastHigh=high[bar+1];
      if(low[bar+1]<LastLow) LastLow=low[bar+1];
      //--- �������� ����� ����������� ������ � ������
      if(CopyTime(Symbol(),PERIOD_D1,time[bar],1,iTime)<=0) return(RESET);

      if(time[bar]>=iTime[0] && time[bar+1]<iTime[0])
        {
         LastCountBar=bar;
         Ind1Buffer[bar+1]=0.0;
         Ind2Buffer[bar+1]=0.0;
         Ind3Buffer[bar+1]=0.0;
         Ind4Buffer[bar+1]=0.0;
         Ind5Buffer[bar+1]=0.0;
         Ind6Buffer[bar+1]=0.0;
         Ind7Buffer[bar+1]=0.0;

         P=(LastHigh+LastLow+close[bar+1])/3;
         double P2=2*P;
         R1=P2-LastLow;
         S1=P2-LastHigh;
         double diff=LastHigh-LastLow;
         R2=P+diff;
         S2=P-diff;
         R3=P2+(LastHigh-2*LastLow);
         S3=P2-(2*LastHigh-LastLow);
         LastLow=open[bar];
         LastHigh=open[bar];
         //--- �������� ���������� �������� � ������������ ������
         Ind1Buffer[bar]=R3;
         Ind2Buffer[bar]=R2;
         Ind3Buffer[bar]=R1;
         Ind4Buffer[bar]=P;
         Ind5Buffer[bar]=S1;
         Ind6Buffer[bar]=S2;
         Ind7Buffer[bar]=S3;
         //--- ������ �����������
         Comment("\n",
                 "Res3=",DoubleToString(R3,_Digits),"\n",
                 "Res2=",DoubleToString(R2,_Digits),"\n",
                 "Res1=",DoubleToString(R1,_Digits),"\n",
                 "Pivot=",DoubleToString(P,_Digits),"\n",
                 "Sup1=",DoubleToString(S1,_Digits),"\n",
                 "Sup2=",DoubleToString(S2,_Digits),"\n",
                 "Sup3=",DoubleToString(S3,_Digits));
        }
      if(Ind1Buffer[bar+1] && !Ind1Buffer[bar])
        {
         Ind1Buffer[bar]=Ind1Buffer[bar+1];
         Ind2Buffer[bar]=Ind2Buffer[bar+1];
         Ind3Buffer[bar]=Ind3Buffer[bar+1];
         Ind4Buffer[bar]=Ind4Buffer[bar+1];
         Ind5Buffer[bar]=Ind5Buffer[bar+1];
         Ind6Buffer[bar]=Ind6Buffer[bar+1];
         Ind7Buffer[bar]=Ind7Buffer[bar+1];
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
//| ������������� ������������� ������                               |
//+------------------------------------------------------------------+    
void IndInit(int Number,double &Buffer[],double Empty_Value,int Draw_Begin,int nShift)
  {
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(Number,Buffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(Number,PLOT_DRAW_BEGIN,Draw_Begin);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(Number,PLOT_EMPTY_VALUE,Empty_Value);
//--- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(Number,PLOT_SHIFT,nShift);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(Buffer,true);
//---
  }
//+------------------------------------------------------------------+
//| TimeFramesCheck()                                                |
//+------------------------------------------------------------------+    
bool TimeFramesCheck(string IndName,
                     ENUM_TIMEFRAMES TFrame)//������ ������� ����������
  {
//--- �������� �������� �������� �� ������������
   if(TFrame>=PERIOD_H12)
     {
      Print("������ ������� ��� ���������� "+IndName+" �� ����� ���� ,������ H12");
      return(RESET);
     }
//---
   return(true);
  }
//+------------------------------------------------------------------+
