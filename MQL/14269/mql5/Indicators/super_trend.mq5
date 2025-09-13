//+------------------------------------------------------------------+
//|                                                  Super_Trend.mq5 |
//|                   Copyright � 2005, Jason Robinson (jnrtrading). | 
//|                                      http://www.jnrtrading.co.uk | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2005, Jason Robinson (jnrtrading)." 
#property link      "http://www.jnrtrading.co.uk" 
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � �������� ����
#property indicator_chart_window
//---- ���������� ������������ ������� 4
#property indicator_buffers 4 
//---- ������������ ����� ������ ����������� ����������
#property indicator_plots   4
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1 DRAW_LINE
//---- � �������� ������� ���������� ����������� ���� MediumSeaGreen
#property indicator_color1 clrMediumSeaGreen
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ���������� �����
#property indicator_label1  "Super_Trend Up"
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type2 DRAW_LINE
//---- � �������� ������� ���������� ����������� ���� Red
#property indicator_color2 clrRed
//---- ����� ���������� - ��������
#property indicator_style2 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width2 2
//---- ����������� ����� ���������� �����
#property indicator_label2  "Super_Trend Down"
//+----------------------------------------------+
//| ��������� ��������� ������� ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� ������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ������ ����� ���������� ����������� ���� MediumTurquoise
#property indicator_color3  clrMediumTurquoise
//---- ����� ���������� 3 - ����������� ������
#property indicator_style3  STYLE_SOLID
//---- ������� ����� ���������� 3 ����� 1
#property indicator_width3  1
//---- ����������� ������ ����� ����������
#property indicator_label3  "Buy Super_Trend signal"
//+----------------------------------------------+
//| ��������� ��������� ���������� ����������    |
//+----------------------------------------------+
//---- ��������� ���������� 4 � ���� ������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ��������� ����� ���������� ����������� ���� DarkOrange
#property indicator_color4  clrDarkOrange
//---- ����� ���������� 2 - ����������� ������
#property indicator_style4  STYLE_SOLID
//---- ������� ����� ���������� 4 ����� 1
#property indicator_width4  1
//---- ����������� ��������� ����� ����������
#property indicator_label4  "Sell Super_Trend signal"
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0                             // ��������� ��� �������� ��������� ������� �� �������� ����������
#define UP_DOWN_SHIFT_CR  0
#define UP_DOWN_SHIFT_M1  3
#define UP_DOWN_SHIFT_M2  3
#define UP_DOWN_SHIFT_M3  4
#define UP_DOWN_SHIFT_M4  5
#define UP_DOWN_SHIFT_M5  5
#define UP_DOWN_SHIFT_M6  5
#define UP_DOWN_SHIFT_M10 6
#define UP_DOWN_SHIFT_M12 6
#define UP_DOWN_SHIFT_M15 7
#define UP_DOWN_SHIFT_M20 8
#define UP_DOWN_SHIFT_M30 9
#define UP_DOWN_SHIFT_H1  20
#define UP_DOWN_SHIFT_H2  27
#define UP_DOWN_SHIFT_H3  30
#define UP_DOWN_SHIFT_H4  35
#define UP_DOWN_SHIFT_H6  33
#define UP_DOWN_SHIFT_H8  35
#define UP_DOWN_SHIFT_H12 37
#define UP_DOWN_SHIFT_D1  40
#define UP_DOWN_SHIFT_W1  100
#define UP_DOWN_SHIFT_MN1 120
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int CCIPeriod=14; // ������ ���������� CCI 
input int Level=0;      // ������� ������������ CCI
input int Shift=0;      // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double TrendUp[],TrendDown[];
double SignUp[];
double SignDown[];
//----
double UpDownShift;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������� ���������� ��� ������� �����������
int CCI_Handle;
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
int GetUpDownShift(ENUM_TIMEFRAMES Timeframe)
  {
//----
   switch(Timeframe)
     {
      case PERIOD_M1:     return(UP_DOWN_SHIFT_M1);
      case PERIOD_M2:     return(UP_DOWN_SHIFT_M2);
      case PERIOD_M3:     return(UP_DOWN_SHIFT_M3);
      case PERIOD_M4:     return(UP_DOWN_SHIFT_M4);
      case PERIOD_M5:     return(UP_DOWN_SHIFT_M5);
      case PERIOD_M6:     return(UP_DOWN_SHIFT_M6);
      case PERIOD_M10:     return(UP_DOWN_SHIFT_M10);
      case PERIOD_M12:     return(UP_DOWN_SHIFT_M12);
      case PERIOD_M15:     return(UP_DOWN_SHIFT_M15);
      case PERIOD_M20:     return(UP_DOWN_SHIFT_M20);
      case PERIOD_M30:     return(UP_DOWN_SHIFT_M30);
      case PERIOD_H1:     return(UP_DOWN_SHIFT_H1);
      case PERIOD_H2:     return(UP_DOWN_SHIFT_H2);
      case PERIOD_H3:     return(UP_DOWN_SHIFT_H3);
      case PERIOD_H4:     return(UP_DOWN_SHIFT_H4);
      case PERIOD_H6:     return(UP_DOWN_SHIFT_H6);
      case PERIOD_H8:     return(UP_DOWN_SHIFT_H8);
      case PERIOD_H12:     return(UP_DOWN_SHIFT_H12);
      case PERIOD_D1:     return(UP_DOWN_SHIFT_D1);
      case PERIOD_W1:     return(UP_DOWN_SHIFT_W1);
      case PERIOD_MN1:     return(UP_DOWN_SHIFT_MN1);
     }
//----
   return(UP_DOWN_SHIFT_CR);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(CCIPeriod)+1;
//---- ��������� ������ ���������� CCI
   CCI_Handle=iCCI(NULL,0,CCIPeriod,PRICE_TYPICAL);
   if(CCI_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� CCI");
      return(INIT_FAILED);
     }
//---- ������������� ���������� ��� ������ ��������     
   UpDownShift=GetUpDownShift(Period())*_Point;
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Super_Trend(",string(CCIPeriod),", ",string(Shift),")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ����������� ������������� ������� ExtBuffer[] � ������������ �����
   SetIndexBuffer(0,TrendUp,INDICATOR_DATA);
//---- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- ���������� ��������� � �������, ��� � ����������   
   ArraySetAsSeries(TrendUp,true);
//---- ����������� ������������� ������� ExtBuffer[] � ������������ �����
   SetIndexBuffer(1,TrendDown,INDICATOR_DATA);
//---- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- ���������� ��������� � �������, ��� � ����������   
   ArraySetAsSeries(TrendDown,true);
//---- ����������� ������������� ������� SignUp [] � ������������ �����
   SetIndexBuffer(2,SignUp,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � �������, ��� � ����������   
   ArraySetAsSeries(SignUp,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
//---- ������ ��� ����������
   PlotIndexSetInteger(2,PLOT_ARROW,108);
//---- ����������� ������������� ������� SignDown[] � ������������ �����
   SetIndexBuffer(3,SignDown,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � �������, ��� � ����������   
   ArraySetAsSeries(SignDown,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0.0);
//---- ������ ��� ����������
   PlotIndexSetInteger(3,PLOT_ARROW,108);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ���� ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(CCI_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//---- ���������� ��������� ���������� 
   double CCI[],cciTrendNow,cciTrendPrevious;
   int limit,to_copy,bar;
//---- ���������� ��������� � ��������, ��� � ����������  
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(CCI,true);
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total;                 // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated;                 // ��������� ����� ��� ������� ����� �����
     }
//----
   to_copy=limit+2;
//----
   to_copy++;
//---- �������� ����� ����������� ������ � ������ CCI[]
   if(CopyBuffer(CCI_Handle,0,0,to_copy,CCI)<=0) return(RESET);
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      SignUp[bar]=0.0;
      SignDown[bar]=0.0;
      SignUp[bar+1]=0.0;
      SignDown[bar+1]=0.0;
      //----
      cciTrendNow=CCI[bar]+70;
      cciTrendPrevious=CCI[bar+1]+70;
      //----
      if(cciTrendNow>=Level && cciTrendPrevious<Level) TrendUp[bar+1]=TrendDown[bar+1];
      if(cciTrendNow<=Level && cciTrendPrevious>Level) TrendDown[bar+1]=TrendUp[bar+1];
      //----
      if(cciTrendNow>Level)
        {
         TrendDown[bar]=0.0;
         TrendUp[bar]=low[bar]-UpDownShift;
         if(close[bar]<open[bar] && TrendDown[bar+1]!=TrendUp[bar+1]) TrendUp[bar]=TrendUp[bar+1];
         if(TrendUp[bar]<TrendUp[bar+1] && TrendDown[bar+1]!=TrendUp[bar+1]) TrendUp[bar]=TrendUp[bar+1];
         if(high[bar]<high[bar+1] && TrendDown[bar+1]!=TrendUp[bar+1]) TrendUp[bar]=TrendUp[bar+1];
        }
      //----
      if(cciTrendNow<Level)
        {
         TrendUp[bar]=0.0;
         TrendDown[bar]=high[bar]+UpDownShift;
         if(close[bar]>open[bar] && TrendUp[bar+1]!=TrendDown[bar+1]) TrendDown[bar]=TrendDown[bar+1];
         if(TrendDown[bar]>TrendDown[bar+1] && TrendDown[bar+1]!=TrendUp[bar+1]) TrendDown[bar]=TrendDown[bar+1];
         if(low[bar]>low[bar+1] && TrendUp[bar+1]!=TrendDown[bar+1]) TrendDown[bar]=TrendDown[bar+1];
        }
      //----
      if(TrendDown[bar+1]!=0.0 && TrendUp[bar]!=0.0) SignUp[bar+1]=TrendDown[bar+1];
      if(TrendUp[bar+1]!=0.0 && TrendDown[bar]!=0.0) SignDown[bar+1]=TrendUp[bar+1];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+