//+------------------------------------------------------------------+
//|                                                      HLRSign.mq5 |
//|                                      Copyright � 2007, Alexandre |
//|                      http://www.kroufr.ru/content/view/1184/124/ |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2006, Alexandre"
#property link      "http://www.kroufr.ru/content/view/1184/124/"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//--- � �������� ����� ��������� ����� ���������� ����������� Salmon ����
#property indicator_color1  clrSalmon
//--- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//--- ����������� ����� ����� ����������
#property indicator_label1  "HLRSign Sell"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//--- � �������� ����� ����� ����� ���������� ����������� MediumSeaGreen ����
#property indicator_color2  clrMediumSeaGreen
//--- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//--- ����������� ��������� ����� ����������
#property indicator_label2 "HLRSign Buy"
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
enum Alg_Method
  {
   MODE_IN,  //�������� �� ����� � ���� �� � ��
   MODE_OUT  //�������� �� ������ � ���� �� � ��
  };
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input Alg_Method Mode=MODE_IN;    // ��������� ��������
input uint HLR_Range=40;          // ������ ���������� ����������
input uint HLR_UpLevel=80;        // ������� ���������������
input uint HLR_DnLevel=20;        // ������� ���������������
input int  Shift=0;               // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
int ATR_Handle;
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//--- ������������� ���������� ���������� 
   int ATR_Period=100;
   min_rates_total=int(MathMax(HLR_Range+1,ATR_Period));
//--- ��������� ������ ���������� ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ATR");
      return(INIT_FAILED);
     }
   if(HLR_UpLevel<=HLR_DnLevel)
     {
      Print("������� ��������������� ������ ������ ���� ������ ������ ���������������!!!");
      return(INIT_FAILED);
     }
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,171);
//--- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellBuffer,true);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,171);
//--- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);
//--- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"HLRSign(",HLR_Range,", ",Shift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(ATR_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//--- ���������� ����������
   int to_copy,limit;
//--- ���������� ���������� � ��������� ������  
   double m_pr,HH,LL,HL,HLR0,ATR[];
   static double HLR1;
//--- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
      HLR1=0;
     }
   else limit=rates_total-prev_calculated;  // ��������� ����� ��� ������� ������ ����� �����
   to_copy=limit+1;
//--- �������� ����� ����������� ������ � �������
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(ATR,true);
//---
   HLR0=HLR1;
//--- �������� ���� ������� ����������
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      //---
      HH=high[ArrayMaximum(high,bar,HLR_Range)];
      LL=low[ArrayMinimum(low,bar,HLR_Range)];
      m_pr=(high[bar]+low[bar])/2.0;
      HL=HH-LL;
      if(HL) HLR0=100.0*(m_pr-LL)/(HL);
      else HLR0=0.0;
      //---
      if(Mode==MODE_IN)
        {
         if(HLR0>HLR_UpLevel && HLR1<=HLR_UpLevel) BuyBuffer[bar]=low[bar]-ATR[0]*3/8;
         if(HLR0<HLR_DnLevel && HLR1>=HLR_DnLevel) SellBuffer[bar]=high[bar]+ATR[0]*3/8;
        }
      else
        {
         if(HLR0<HLR_UpLevel && HLR1>=HLR_UpLevel) SellBuffer[bar]=high[bar]+ATR[0]*3/8;
         if(HLR0>HLR_DnLevel && HLR1<=HLR_DnLevel) BuyBuffer[bar]=low[bar]-ATR[0]*3/8;
        }

      if(bar) HLR1=HLR0;
     }
//---
   return(rates_total);
  }
//+------------------------------------------------------------------+
