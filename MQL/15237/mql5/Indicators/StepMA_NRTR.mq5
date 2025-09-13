//+------------------------------------------------------------------+
//|                                                  StepMA_NRTR.mq5 |
//|                                Copyright � 2006, TrendLaboratory |
//|            http://finance.groups.yahoo.com/group/TrendLaboratory |
//|                                   E-mail: igorad2003@yahoo.co.uk |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2006, TrendLaboratory"
//---- ������ �� ���� ������
#property link "http://www.forex-instruments.info"
#property link "http://finance.groups.yahoo.com/group/TrendLaboratory"
//---- ����� ������ ����������
#property version   "8.00"
//---- ��������� ���������� � �������� ����
#property indicator_chart_window
//---- ��� ������� � ��������� ���������� ������������ 4 ������
#property indicator_buffers 4
//---- ������������ 4 ����������� ����������
#property indicator_plots   4
//+----------------------------------------------+
//|  ��������� ��������� ����� ����������        |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ����� ���������� ����������� ���� BlueViolet
#property indicator_color1  clrBlueViolet
//---- ����� ���������� 1 - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� 1 ����� 2
#property indicator_width1  2
//---- ����������� ����� ������ ����� ����������
#property indicator_label1  "Upper StepMA"
//---- ��������� ���������� 2 � ���� �����
//+----------------------------------------------+
//|  ��������� ��������� ����� ����������        |
//+----------------------------------------------+
#property indicator_type2   DRAW_LINE
//---- � �������� ����� �������� ����� ���������� ����������� ���� Gold
#property indicator_color2  clrGold
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 2
#property indicator_width2  2
//---- ����������� ����� ��������� ����� ����������
#property indicator_label2  "Lower StepMA"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� ������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ����� ����� ���������� ����������� ���� SpringGreen
#property indicator_color3  clrSpringGreen
//---- ����� ���������� 3 - ����������� ������
#property indicator_style3  STYLE_SOLID
//---- ������� ����� ���������� 3 ����� 4
#property indicator_width3  4
//---- ����������� ����� ����������
#property indicator_label3  "StepMA Buy"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 4 � ���� ������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� �������� ����� ���������� ����������� ���� Red
#property indicator_color4  clrRed
//---- ����� ���������� 2 - ����������� ������
#property indicator_style4  STYLE_SOLID
//---- ������� ����� ���������� 4 ����� 4
#property indicator_width4  4
//---- ����������� ����� ����������
#property indicator_label4  "StepMA Sell"
//+-----------------------------------+
//|  ���������� ������������          |
//+-----------------------------------+
enum MA_MODE // ��� ���������
  {
   SMA,     // SMA
   LWMA     // LWMA
  };
//+-----------------------------------+
//|  ���������� ������������          |
//+-----------------------------------+
enum PRICE_MODE // ��� ���������
  {
   HighLow,     // High/Low
   CloseClose   // Close/Close
  };
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int        Length      = 10;      // Volty Length
input double     Kv          = 1.0;     // Sensivity Factor
input int        StepSize    = 0;       // Constant Step Size (if need)
input double     Percentage  = 0;       // Percentage of Up/Down Moving   
input PRICE_MODE Switch      = HighLow; // High/Low Mode Switch (more sensitive)    
input int        Shift=0; // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double UpBuffer[];
double DnBuffer[];
double SellBuffer[];
double BuyBuffer[];

double ratio;
int trend1,trend1_,trend0;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+ 
//| StepSize Calculation                                             |
//+------------------------------------------------------------------+ 
double StepSizeCalc(const double &High[],const double &Low[],int Len,double Km,int Size,int bar)
  {
//----
   double result;

   if(!Size)
     {
      double Range=0.0;
      double ATRmax=-1000000;
      double ATRmin=+1000000;

      for(int iii=Len-1; iii>=0; iii--)
        {
         Range=High[bar+iii]-Low[bar+iii];
         if(Range>ATRmax) ATRmax=Range;
         if(Range<ATRmin) ATRmin=Range;
        }
      result=MathRound(0.5*Km*(ATRmax+ATRmin)/_Point);
     }
   else result=Km*Size;
//----
   return(result);
  }
//+------------------------------------------------------------------+
//| StepMA Calculation                                               |
//+------------------------------------------------------------------+ 
double StepMACalc(const double &High[],const double &Low[],const double &Close[],bool HL,double Size,int bar)
  {
//----
   double result,smax0,smin0,SizeP,Size2P;
   static double smax1,smin1;
   static bool FirstStart=true;
   SizeP=Size*_Point;
   Size2P=2.0*SizeP;

//---- ��������� ������������� ����������
   if(FirstStart)
     {
      trend1=0;
      smax1=Low[bar]+Size2P;
      smin1=High[bar]-Size2P;
      FirstStart=false;
     }

   if(HL)
     {
      smax0=Low[bar]+Size2P;
      smin0=High[bar]-Size2P;
     }
   else
     {
      smax0=Close[bar]+Size2P;
      smin0=Close[bar]-Size2P;
     }

   trend0=trend1;

   if(Close[bar]>smax1) trend0=+1;
   if(Close[bar]<smin1) trend0=-1;

   if(trend0>0)
     {
      if(smin0<smin1) smin0=smin1;
      result=smin0+SizeP;
     }
   else
     {
      if(smax0>smax1) smax0=smax1;
      result=smax0-SizeP;
     }
   trend1_=trend1;

   if(bar)
     {
      smax1=smax0;
      smin1=smin0;
      trend1=trend0;
     }
//----
   return(result);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=Length+3;

//---- ������������� ����������  
   ratio=Percentage/100.0*_Point;

//---- ����������� ������������� ������� BufferUp � ������������ �����
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������� ��� � ����������   
   ArraySetAsSeries(UpBuffer,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� BufferDown � ������������ �����
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������� ��� � ����������   
   ArraySetAsSeries(DnBuffer,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� BufferUp1 � ������������ �����
   SetIndexBuffer(2,BuyBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������� ��� � ����������   
   ArraySetAsSeries(BuyBuffer,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������ ��� ����������
   PlotIndexSetInteger(2,PLOT_ARROW,108);

//---- ����������� ������������� ������� BufferDown1 � ������������ �����
   SetIndexBuffer(3,SellBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������� ��� � ����������   
   ArraySetAsSeries(SellBuffer,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������ ��� ����������
   PlotIndexSetInteger(3,PLOT_ARROW,108);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"StepMA NRTR (",Length,", ",Kv,", ",StepSize,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
                const double& low[],      // ������� ������ ��������� ����  ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);

//---- ���������� ��������� ���������� 
   int limit,bar;
   double StepMA,Step;

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total;   // ��������� ����� ��� ������� ���� �����
      trend1_=0;
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0; bar--)
     {
      UpBuffer[bar]=EMPTY_VALUE;
      DnBuffer[bar]=EMPTY_VALUE;
      SellBuffer[bar]=EMPTY_VALUE;
      BuyBuffer[bar]=EMPTY_VALUE;

      Step=StepSizeCalc(high,low,Length,Kv,StepSize,bar);
      if(!Step) Step=1;

      StepMA=StepMACalc(high,low,close,Switch,Step,bar)+ratio/Step;

      if(trend0>0)
        {
         UpBuffer[bar]=StepMA-Step*_Point;
         if(trend1_<0) BuyBuffer[bar]=UpBuffer[bar];
         DnBuffer[bar]=EMPTY_VALUE;
        }

      if(trend0<0)
        {
         DnBuffer[bar]=StepMA+Step*_Point;
         if(trend1_>0) SellBuffer[bar]=DnBuffer[bar];
         UpBuffer[bar]=EMPTY_VALUE;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
