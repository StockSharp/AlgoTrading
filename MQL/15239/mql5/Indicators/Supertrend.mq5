//+------------------------------------------------------------------+
//|                                                   Supertrend.mq5 |
//|                   Copyright � 2005, Jason Robinson (jnrtrading). | 
//|                                      http://www.jnrtrading.co.uk | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2005, Jason Robinson (jnrtrading)." 
#property link      "http://www.jnrtrading.co.uk" 
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � �������� ����
#property indicator_chart_window
//---- ���������� ������������ ������� 4
#property indicator_buffers 4 
//---- ������������ ����� ������ ����������� ����������
#property indicator_plots   4
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1 DRAW_LINE
//---- � �������� ������� ���������� ����������� ���� Lime
#property indicator_color1 clrLime
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ���������� �����
#property indicator_label1  "Supertrend Up"
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type2 DRAW_LINE
//---- � �������� ������� ���������� ������������ ��� �����
#property indicator_color2 clrRed
//---- ����� ���������� - ��������
#property indicator_style2 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width2 2
//---- ����������� ����� ���������� �����
#property indicator_label2  "Supertrend Down"
//+----------------------------------------------+
//|  ��������� ��������� ������� ����������      |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� ������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ����� ����� ���������� ����������� ���� MediumTurquoise
#property indicator_color3  clrMediumTurquoise
//---- ����� ���������� 3 - ����������� ������
#property indicator_style3  STYLE_SOLID
//---- ������� ����� ���������� 3 ����� 4
#property indicator_width3  4
//---- ����������� ������ ����� ����������
#property indicator_label3  "Buy Supertrend signal"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 4 � ���� ������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ��������� ����� ���������� ����������� ���� DarkOrange
#property indicator_color4  clrDarkOrange
//---- ����� ���������� 2 - ����������� ������
#property indicator_style4  STYLE_SOLID
//---- ������� ����� ���������� 4 ����� 4
#property indicator_width4  4
//---- ����������� ��������� ����� ����������
#property indicator_label4  "Sell Supertrend signal"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int CCIPeriod=50; // ������ ���������� CCI 
input int ATRPeriod=5;  // ������ ���������� ATR
input int Level=0;      // ������� ������������ CCI
input int Shift=0;      // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double TrendUp[],TrendDown[];
double SignUp[];
double SignDown[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������� ���������� ��� ������� �����������
int ATR_Handle,CCI_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=MathMax(CCIPeriod,ATRPeriod);
//---- ��������� ������ ���������� CCI
   CCI_Handle=iCCI(NULL,0,CCIPeriod,PRICE_TYPICAL);
   if(CCI_Handle==INVALID_HANDLE)Print(" �� ������� �������� ����� ���������� CCI");
//---- ��������� ������ ���������� ATR
   ATR_Handle=iATR(NULL,0,ATRPeriod);
   if(ATR_Handle==INVALID_HANDLE)Print(" �� ������� �������� ����� ���������� ATR");
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Supertrend(",string(CCIPeriod),", ",string(ATRPeriod),", ",string(Shift),")");
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
//----
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
   if
   (BarsCalculated(CCI_Handle)<rates_total
    || BarsCalculated(ATR_Handle)<rates_total
    || rates_total<min_rates_total) return(0);

//---- ���������� ��������� ���������� 
   double ATR[],CCI[];
   int limit,to_copy,bar;

//---- ���������� ��������� � ��������, ��� � ����������  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(ATR,true);
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

   to_copy=limit+1;

//---- �������� ����� ����������� ������ � ������ ATR[]
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(0);

   to_copy++;
//---- �������� ����� ����������� ������ � ������ CCI[]
   if(CopyBuffer(CCI_Handle,0,0,to_copy,CCI)<=0) return(0);

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      TrendUp[bar]=0.0;
      TrendDown[bar]=0.0;
      SignUp[bar]=0.0;
      SignDown[bar]=0.0;

      if(CCI[bar]>=Level && CCI[bar+1]<Level) TrendUp[bar]=TrendDown[bar+1];

      if(CCI[bar]<=Level && CCI[bar+1]>Level) TrendDown[bar]=TrendUp[bar+1];

      if(CCI[bar]>Level)
        {
         TrendUp[bar]=low[bar]-ATR[bar];
         if(TrendUp[bar]<TrendUp[bar+1] && CCI[bar+1]>=Level) TrendUp[bar]=TrendUp[bar+1];
        }

      if(CCI[bar]<Level)
        {
         TrendDown[bar]=high[bar]+ATR[bar];
         if(TrendDown[bar]>TrendDown[bar+1] && CCI[bar+1]<=Level) TrendDown[bar]=TrendDown[bar+1];
        }

      if(TrendDown[bar+1]!=0.0 && TrendUp[bar]!=0.0) SignUp[bar]=TrendUp[bar];

      if(TrendUp[bar+1]!=0.0 && TrendDown[bar]!=0.0) SignDown[bar]=TrendDown[bar];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
