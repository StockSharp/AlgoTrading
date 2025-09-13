//+---------------------------------------------------------------------+
//|                                                ColorJFatl_Digit.mq5 | 
//|                                 �Copyright � 2016, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "2016,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"

//---- ��������� ���������� � �������� ����
#property indicator_chart_window
//---- ��� ������� � ��������� ���������� ����������� ���� �����
#property indicator_buffers 2
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_COLOR_LINE
//---- � �������� ������ ���������� ����� ������������
#property indicator_color1  clrMagenta,clrGray,clrGold
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "ColorJFatl_Digit"
//+-----------------------------------+
//|  �������� ������ CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CJJMA �� ����� JJMASeries_Cls.mqh
CJJMA JMA;
//+-----------------------------------+
//|  ���������� ������������          |
//+-----------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //PRICE_CLOSE
   PRICE_OPEN_,          //PRICE_OPEN
   PRICE_HIGH_,          //PRICE_HIGH
   PRICE_LOW_,           //PRICE_LOW
   PRICE_MEDIAN_,        //PRICE_MEDIAN
   PRICE_TYPICAL_,       //PRICE_TYPICAL
   PRICE_WEIGHTED_,      //PRICE_WEIGHTED
   PRICE_SIMPL_,         //PRICE_SIMPL_
   PRICE_QUARTER_,       //PRICE_QUARTER_
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//|  ������� ��������� ����������     |
//+-----------------------------------+
input string  SirName="ColorJFatl_Digit";     //������ ����� ����� ����������� ��������
input int JLength=5; // ������� JMA �����������                   
input int JPhase=-100; // �������� JMA �����������,
                      //������������ � �������� -100 ... +100,
//������ �� �������� ����������� ��������;
input Applied_price_ IPC=PRICE_CLOSE_;//������� ���������
input int FATLShift=0; // ����� ����� �� ����������� � �����
input int PriceShift=0; // c���� ����� �� ��������� � �������
input uint Digit=2;                       //���������� �������� ����������
input bool ShowPrice=true; //���������� ������� �����
//---- ����� ������� �����
input color  Price_color=clrGray;
//+-----------------------------------+

//---- ���������� � ������������� ���������� ��� �������� ���������� ��������� �����
int FATLPeriod=39;

//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double ExtLineBuffer[];
double ColorExtLineBuffer[];

int start,fstart,FATLSize;
double dPriceShift;
double PointPow10;
//---- ���������� �������� ��� ��������� �����
string Price_name;
//+X----------------------------------------------X+ 
//| ������������� ������������� ��������� �������  |
//+X----------------------------------------------X+ 
double dFATLTable[]=
  {
   +0.4360409450, +0.3658689069, +0.2460452079, +0.1104506886,
   -0.0054034585, -0.0760367731, -0.0933058722, -0.0670110374,
   -0.0190795053, +0.0259609206, +0.0502044896, +0.0477818607,
   +0.0249252327, -0.0047706151, -0.0272432537, -0.0338917071,
   -0.0244141482, -0.0055774838, +0.0128149838, +0.0226522218,
   +0.0208778257, +0.0100299086, -0.0036771622, -0.0136744850,
   -0.0160483392, -0.0108597376, -0.0016060704, +0.0069480557,
   +0.0110573605, +0.0095711419, +0.0040444064, -0.0023824623,
   -0.0067093714, -0.0072003400, -0.0047717710, +0.0005541115,
   +0.0007860160, +0.0130129076, +0.0040364019
  };
//+------------------------------------------------------------------+
// �������� ������� iPriceSeries()                                   |
// �������� ������� iPriceSeriesAlert()                              |
// �������� ������ CJJMA                                             |
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh>  
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ����������� ������������� ������� ExtLineBuffer � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� �� ����������� �� FATLShift
   PlotIndexSetInteger(0,PLOT_SHIFT,FATLShift);
//---- ������������� ���������� 
   FATLSize=ArraySize(dFATLTable);
   start=FATLSize+30;
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
   PointPow10=_Point*MathPow(10,Digit);
//---- ������������� ��������
   Price_name=SirName+"Price";
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,start);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"ColorJFatl_Digit(",JLength," ,",JPhase,")");
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//--- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorExtLineBuffer,INDICATOR_COLOR_INDEX);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<start) return(0);

//---- ���������� ��������� ���������� 
   int first,bar;
   double jfatl,FATL,trend;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=FATLPeriod-1; // ��������� ����� ��� ������� ���� �����
      fstart=first;
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ������� ��� ������� FATL
      FATL=0.0;
      for(int iii=0; iii<FATLSize; iii++) FATL+=dFATLTable[iii]*PriceSeries(IPC,bar-iii,open,low,high,close);
      jfatl=JMA.JJMASeries(fstart,prev_calculated,rates_total,0,JPhase,JLength,FATL,bar,false);
      jfatl+=dPriceShift;
      ExtLineBuffer[bar]=PointPow10*MathRound(jfatl/PointPow10);
     }
//---- ������������� �������� ���������� first
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first++; // ��������� ����� ��� ������� ���� �����

//---- �������� ���� ��������� ���������� �����
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      double clr=1;
      trend=ExtLineBuffer[bar]-ExtLineBuffer[bar-1];
      if(!trend) clr=ColorExtLineBuffer[bar-1];
      else
        {
         if(trend>0) clr=2;
         if(trend<0) clr=0;
        }
      ColorExtLineBuffer[bar]=clr;
     }
//----
   ChartRedraw(0);
   return(rates_total);
  }
//+------------------------------------------------------------------+
