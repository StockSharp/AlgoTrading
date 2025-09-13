//+------------------------------------------------------------------+
//|                                             AnchoredMomentum.mq5 | 
//|                              Copyright � 2010, Umnyashkin Victor | 
//|                                       http://www.metaquotes.net/ | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2010, Umnyashkin Victor"
#property link "http://www.metaquotes.net/"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ ������� 4
#property indicator_buffers 4 
//---- ������������ ����� ������ ����������� ����������
#property indicator_plots   4
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ����� ����
#property indicator_color1 clrGray
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "Momentum"
//+----------------------------------------------+
//| ��������� ��������� ������� ����������        |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ��������� ����
#property indicator_color2 clrSpringGreen
//---- ������� ����� ���������� ����� 3
#property indicator_width2 3
//---- ����������� ������ ����� ����������
#property indicator_label2 "Up_Signal"
//+----------------------------------------------+
//| ��������� ��������� ���������� ����������    |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� �����-������� ����
#property indicator_color3  clrDeepPink
//---- ������� ����� ���������� ����� 3
#property indicator_width3 3
//---- ����������� ��������� ����� ����������
#property indicator_label3 "Dn_Signal"
//+----------------------------------------------+
//| ��������� ��������� ������������� ���������� |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ������������� ���������� ����������� �����
#property indicator_color4  clrGray
//---- ������� ����� ���������� ����� 3
#property indicator_width4 3
//---- ����������� ������������ ����� ����������
#property indicator_label4 "No_Signal"
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint MomPeriod=8;    // ������ SMA 
input uint SmoothPeriod=6; // ������ EMA
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE; // ������� ���������, �� ������� ������������ ������ ����������
input double UpLevel=+0.025; // ������� ��������� �������
input double DnLevel=-0.025; // ������ ��������� �������
input int Shift=0; // ����� ���������� �� ����������� � �����
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//----  ���������� ������������ � �������� ������������ �������
double MomBuffer[];
double UpBuffer[];
double DnBuffer[];
double FlBuffer[];
//---- ���������� ������������� ���������� ��� ������� �����������
int SMA_Handle,EMA_Handle;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+   
//| Momentum indicator initialization function                       | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ��������� ������ ���������� SMA
   SMA_Handle=iMA(NULL,0,MomPeriod,0,MODE_SMA,IPC);
   if(SMA_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� SMA");
//---- ��������� ������ ���������� SMA
   EMA_Handle=iMA(NULL,0,MomPeriod,0,MODE_EMA,IPC);
   if(EMA_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� EMA");
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(MomPeriod);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,MomBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(MomBuffer,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,UpBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ����� ������� ��� ���������
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(UpBuffer,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,DnBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ����� ������� ��� ���������
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(DnBuffer,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,FlBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 3
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(3,PLOT_LABEL,"No Signal");
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ����� ������� ��� ���������
   PlotIndexSetInteger(3,PLOT_ARROW,159);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(FlBuffer,true);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Momentum(",MomPeriod,", ",SmoothPeriod,", ",Shift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,3);
//---- ���������� �������������� ������� ���������� 2   
   IndicatorSetInteger(INDICATOR_LEVELS,2);
//---- �������� �������������� ������� ����������   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,UpLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,DnLevel);
//---- � �������� ������ ����� �������������� ������� ������������ ����� � ������� �����  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrBlue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrMagenta);
//---- � ����� ��������������� ������ ����������� �������� �����-�������  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//|  Momentum iteration function                                     | 
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
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(SMA_Handle)<rates_total
      || BarsCalculated(EMA_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);
//---- ���������� ���������� � ��������� ������  
   double res,Momentum,SMA[],EMA[];
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int to_copy,limit,bar;
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-1-min_rates_total; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }
//---
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(SMA_Handle,0,0,to_copy,SMA)<=0) return(RESET);
   if(CopyBuffer(EMA_Handle,0,0,to_copy,EMA)<=0) return(RESET);
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(SMA,true);
   ArraySetAsSeries(EMA,true);
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      res=SMA[bar];
      if(res) Momentum=100*(EMA[bar]/SMA[bar]-1);
      else Momentum=EMPTY_VALUE;
      //---
      MomBuffer[bar]=Momentum;
      //---- ������������� ����� ������������ ������� ������
      UpBuffer[bar]=EMPTY_VALUE;
      DnBuffer[bar]=EMPTY_VALUE;
      FlBuffer[bar]=EMPTY_VALUE;
      //---
      if(Momentum==EMPTY_VALUE) continue;
      //---- ������������� ����� ������������ ������� ����������� ���������� 
      if(Momentum>UpLevel) UpBuffer[bar]=Momentum; //���� ���������� �����
      else if(Momentum<DnLevel) DnBuffer[bar]=Momentum; //���� ���������� �����
      else FlBuffer[bar]=Momentum; //��� ������
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
