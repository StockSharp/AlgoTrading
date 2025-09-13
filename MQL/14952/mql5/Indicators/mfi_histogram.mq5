//+------------------------------------------------------------------+ 
//|                                                MFI_Histogram.mq5 | 
//|                               Copyright � 2016, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2016, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ ������� 3
#property indicator_buffers 3 
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//|  ���������� ��������              |
//+-----------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����������
#property indicator_type1   DRAW_COLOR_HISTOGRAM2
//---- � �������� ������ ���������� ������������
#property indicator_color1  clrMediumTurquoise,clrGray,clrGold
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1  "MFI_Histogram"

//+-----------------------------------+
//|  ������� ��������� ����������     |
//+-----------------------------------+
input uint                 MFIPeriod=14;           // ������ ����������
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  // ����� 
input uint                 HighLevel=70;           // ������� ���������������
input uint                 LowLevel=30;            // ������� ���������������
input int                  Shift=0;                // ����� ���������� �� ����������� � �����
//+-----------------------------------+

//---- ���������� ����� ���������� ������ ������� ������
int  min_rates_total;
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double UpBuffer[],DnBuffer[],ColorBuffer[];
//---- ���������� ����� ���������� ��� ������� �����������
int MFI_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(MFIPeriod);
//---- ��������� ������ ���������� iMFI
   MFI_Handle=iMFI(NULL,0,MFIPeriod,VolumeType);
   if(MFI_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMFI");
      return(INIT_FAILED);
     }   
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ������ ���������� �� ����������� �� InpKijun
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(UpBuffer,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(DnBuffer,true);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(2,ColorBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorBuffer,true);

//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"MFI_Histogram("+string(MFIPeriod)+")");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ����������  �������������� ������� ���������� 3   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- �������� �������������� ������� ����������   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,50);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,LowLevel);
//---- � �������� ������ ����� �������������� ������� ������������ ����� � ������� �����  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrGreen);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrBrown);
//---- � ����� ��������������� ������ ����������� �������� �����-�������  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//---- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &Time[],
                const double &Open[],
                const double &High[],
                const double &Low[],
                const double &Close[],
                const long &Tick_Volume[],
                const long &Volume[],
                const int &Spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(MFI_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- ���������� ��������� ����������
   int to_copy,limit,bar;
   
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����

   to_copy=limit+1;

//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(MFI_Handle,0,0,to_copy,UpBuffer)<=0) return(RESET);

//---- �������� ���� ��������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      DnBuffer[bar]=50.0;
      int clr=1.0;
      if(UpBuffer[bar]>HighLevel) clr=0.0;
      else if(UpBuffer[bar]<LowLevel) clr=2.0;
      ColorBuffer[bar]=clr;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
