//+------------------------------------------------------------------+
//|                                           JFatl_Digit_System.mq5 |
//|                               Copyright � 2016, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2016, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "��������� ������� � �������������� ���������� JFatl_Digit"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window
//---- ��� ������� � ��������� ���������� ������������ ���� �������
#property indicator_buffers 7
//---- ������������ ������ ����������� ����������
#property indicator_plots   4
//+----------------------------------------------+
//|  ��������� ��������� ���������� 1            |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������������ ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ����� ���������� ����������� WhiteSmoke ����
#property indicator_color1  clrWhiteSmoke
//---- ����������� ����� ����������
#property indicator_label1  "JFatl_Digit"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 2            |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ����� ����� ���������� ����������� MediumSeaGreen ����
#property indicator_color2  clrMediumSeaGreen
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 2
#property indicator_width2  2
//---- ����������� ����� ����� ����������
#property indicator_label2  "Upper JFatl_Digit"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 3            |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� �����
#property indicator_type3   DRAW_LINE
//---- � �������� ����� ��������� ����� ���������� ����������� Magenta ����
#property indicator_color3  clrMagenta
//---- ����� ���������� 3 - ����������� ������
#property indicator_style3  STYLE_SOLID
//---- ������� ����� ���������� 3 ����� 2
#property indicator_width3  2
//---- ����������� ��������� ����� ����������
#property indicator_label3  "Lower JFatl_Digit"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 4            |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������������� �����������
#property indicator_type4 DRAW_COLOR_HISTOGRAM2
//---- � �������� ������ ������������� ����������� ������������
#property indicator_color4 clrDeepPink,clrPurple,clrGray,clrMediumBlue,clrDodgerBlue
//---- ����� ���������� - ��������
#property indicator_style4 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width4 2
//---- ����������� ����� ����������
#property indicator_label4 "JFatl_Digit_BARS"
//+-----------------------------------+
//|  �������� ������ CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CJJMA �� ����� JJMASeries_Cls.mqh
CJJMA HJMA,LJMA;
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input string  SirName="ColorJFatl_Digit";     // ������ ����� ����� ����������� ��������
input int JLength=5;                          // ������� JMA �����������                   
input int JPhase=-100;                        // �������� JMA �����������,
//---- ������������ � �������� -100 ... +100,
//---- ������ �� �������� ����������� ��������;
input int PriceShift=0;                       // c���� ������ �� ��������� � �������
input uint   Shift=2;                         // ����� ������ �� ����������� � ����� 
input uint Digit=2;                           // ���������� �������� ����������
input bool ShowPrice=true;                    // ���������� ������� �����
//---- ����� ������� �����
input color  Up_Price_color=clrTeal;
input color  Dn_Price_color=clrMagenta;
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double Up1Buffer[],Dn1Buffer[];
double Up2Buffer[],Dn2Buffer[];
double UpIndBuffer[],DnIndBuffer[],ColorIndBuffer[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� � ������������� ���������� ��� �������� ���������� ��������� �����
int FATLPeriod=39;
int FATLSize;
double dPriceShift;
double PointPow10;
//---- ���������� �������� ��� ��������� �����
string Dn_Price_name,Up_Price_name;
//+----------------------------------------------+
//| ������������� ������������� ��������� �������|
//+----------------------------------------------+
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
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� 
   FATLSize=ArraySize(dFATLTable);
   min_rates_total=FATLSize+30+int(Shift);
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
   PointPow10=_Point*MathPow(10,Digit);
//---- ������������� ��������
   Up_Price_name=SirName+"Up_Price";
   Dn_Price_name=SirName+"Dn_Price";

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,Up1Buffer,INDICATOR_DATA);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,Dn1Buffer,INDICATOR_DATA);
   
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,Up2Buffer,INDICATOR_DATA);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,Dn2Buffer,INDICATOR_DATA);
   
//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(4,UpIndBuffer,INDICATOR_DATA);

//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(5,DnIndBuffer,INDICATOR_DATA);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(6,ColorIndBuffer,INDICATOR_COLOR_INDEX);

   
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
   
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2 �� min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
   
//---- ������������� ������ ���������� 3 �� ����������� �� Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 3 �� min_rates_total
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
   
//---- ������������� ������ ���������� 3 �� ����������� �� Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,0);
//---- ������������� ������ ������ ������� ��������� ���������� 4 �� min_rates_total
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"JFatl_Digit_System(",Shift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+    
void OnDeinit(const int reason)
  {
//----
   ObjectDelete(0,Up_Price_name);
   ObjectDelete(0,Dn_Price_name);
//----
   ChartRedraw(0);
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
   if(rates_total<min_rates_total) return(0);

//---- ���������� ��������� ���������� 
   int first,bar;
   double jfatl,FATL;
   static int fstart;

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
      //---- ������ ������� ������� ������
      FATL=0.0;
      for(int iii=0; iii<FATLSize; iii++) FATL+=dFATLTable[iii]*high[bar-iii];
      jfatl=HJMA.JJMASeries(fstart,prev_calculated,rates_total,0,JPhase,JLength,FATL,bar,false);
      jfatl+=dPriceShift;      
      Up1Buffer[bar]=Up2Buffer[bar]=PointPow10*MathRound(jfatl/PointPow10);
      //---- ������ ������ ������� ������
      FATL=0.0;
      for(int iii=0; iii<FATLSize; iii++) FATL+=dFATLTable[iii]*low[bar-iii];
      jfatl=LJMA.JJMASeries(fstart,prev_calculated,rates_total,0,JPhase,JLength,FATL,bar,false);
      jfatl+=dPriceShift;
      Dn1Buffer[bar]=Dn2Buffer[bar]=PointPow10*MathRound(jfatl/PointPow10);
     }


//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) first=min_rates_total;     
//---- �������� ���� ��������� ����� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=2;
      UpIndBuffer[bar]=0.0;
      DnIndBuffer[bar]=0.0;
      
      if(close[bar]>Up1Buffer[bar-Shift])
        {
         if(open[bar]<close[bar]) clr=4;
         else clr=3;
         UpIndBuffer[bar]=high[bar];
         DnIndBuffer[bar]=low[bar];
        }

      if(close[bar]<Dn1Buffer[bar-Shift])
        {
         if(open[bar]>close[bar]) clr=0;
         else clr=1;
         UpIndBuffer[bar]=high[bar];
         DnIndBuffer[bar]=low[bar];
        }

      ColorIndBuffer[bar]=clr;
     }
   if(ShowPrice)
     {
      int bar0=rates_total-1;
      datetime time0=time[bar0]+Shift*PeriodSeconds();
      SetRightPrice(0,Up_Price_name,0,time0,Up1Buffer[bar0-Shift],Up_Price_color);
      SetRightPrice(0,Dn_Price_name,0,time0,Dn1Buffer[bar0-Shift],Dn_Price_color);
     }
//----     
   ChartRedraw(0);
   return(rates_total);
  }
//+------------------------------------------------------------------+
//|  RightPrice creation                                             |
//+------------------------------------------------------------------+
void CreateRightPrice(long chart_id,// chart ID
                      string   name,              // object name
                      int      nwin,              // window index
                      datetime time,              // price level time
                      double   price,             // price level
                      color    Color              // Text color
                      )
//---- 
  {
//----
   ObjectCreate(chart_id,name,OBJ_ARROW_RIGHT_PRICE,nwin,time,price);
   ObjectSetInteger(chart_id,name,OBJPROP_COLOR,Color);
   ObjectSetInteger(chart_id,name,OBJPROP_BACK,true);
   ObjectSetInteger(chart_id,name,OBJPROP_WIDTH,2);
//----
  }
//+------------------------------------------------------------------+
//|  RightPrice reinstallation                                       |
//+------------------------------------------------------------------+
void SetRightPrice(long chart_id,// chart ID
                   string   name,              // object name
                   int      nwin,              // window index
                   datetime time,              // price level time
                   double   price,             // price level
                   color    Color              // Text color
                   )
//---- 
  {
//----
   if(ObjectFind(chart_id,name)==-1) CreateRightPrice(chart_id,name,nwin,time,price,Color);
   else ObjectMove(chart_id,name,0,time,price);
//----
  }
//+------------------------------------------------------------------+
