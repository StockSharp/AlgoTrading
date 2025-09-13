//+---------------------------------------------------------------------+
//|                                                         TSI_CCI.mq4 |
//|                         Copyright � 2006, MetaQuotes Software Corp. |
//|                                           http://www.metaquotes.net |
//+---------------------------------------------------------------------+ 
//| ��� ������ ���������� ������� �������� ���� SmoothAlgorithms.mqh    |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Copyright � 2006, MetaQuotes Software Corp."
//--- ������ �� ���� ������
#property link "http://www.metaquotes.net" 
#property description "TSI_CCI"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ���������� 1            |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//--- � �������� ������ ���������� ������������
#property indicator_color1  clrBlue,clrIndianRed
//--- ����������� ����� ����������
#property indicator_label1  "TSI_CCI"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 +50
#property indicator_level2   0
#property indicator_level3 -50
#property indicator_levelcolor clrMagenta
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| �������� ������ CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- ���������� ���������� ������� CXMA � CMomentum �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4,XMA5;
CMomentum Mom;
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
/*enum Smooth_Method - ������������ ��������� � ����� SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_EMA;          // ����� ����������
input uint CCIPeriod=15;                          // ������ ���������� CCI
input ENUM_APPLIED_PRICE   CCIPrice=PRICE_CLOSE;  // ���� ���������� CCI
input uint MomPeriod=1;                           // ������ ���������
input uint XLength1=5;                            // ������� ������� ����������
input uint XLength2=8;                            // ������� ������� ����������
input uint XLength3=10;                           // ������� ���������� ���������� �����
input int XPhase=15;                              // �������� �����������
//--- XPhase: ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//--- XPhase: ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double UpBuffer[],DnBuffer[];
//--- ���������� ������������� ���������� ��� �������� ������� �����������
int Ind_Handle;
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2,min_rates_3,min_rates_4;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_1=int(CCIPeriod);
   min_rates_2=min_rates_1+int(MomPeriod);
   min_rates_3=min_rates_2+XMA1.GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_4=min_rates_3+XMA1.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_total=min_rates_4+XMA1.GetStartBars(XMA_Method,XLength3,XPhase);
//--- ��������� ������ ���������� TSI_MACD
   Ind_Handle=iCCI(Symbol(),PERIOD_CURRENT,CCIPeriod,CCIPrice);
   if(Ind_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� TSI_MACD");
      return(INIT_FAILED);
     }
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(UpBuffer,true);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(DnBuffer,true);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"TSI_CCI");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
                const double& low[],      // ������� ������ ��������� ����  ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total || BarsCalculated(Ind_Handle)<rates_total) return(RESET);
//--- ���������� ��������� ���������� 
   double CCI[],mtm,xmtm,xxmtm,absmtm,xabsmtm,xxabsmtm,tsi,xtsi;
   int to_copy,limit,bar,maxbar1,maxbar2,maxbar3,maxbar4;
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(CCI,true);
//--- ������� ������������ ���������� ���������� ������
//--- � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_1-1; // ��������� ����� ��� ������� ���� �����
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
//---   
   to_copy=limit+1;
//--- �������� ����� ����������� ������ � �������
   if(CopyBuffer(Ind_Handle,0,0,to_copy,CCI)<=0) return(RESET);
//---  
   maxbar1=rates_total-min_rates_1-1;
   maxbar2=rates_total-min_rates_2-1;
   maxbar3=rates_total-min_rates_3-1;
   maxbar4=rates_total-min_rates_4-1;
//--- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      mtm=Mom.MomentumSeries(maxbar1,prev_calculated,rates_total,MomPeriod,CCI[bar],bar,true);
      absmtm=MathAbs(mtm);
      xmtm=XMA1.XMASeries(maxbar2,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,mtm,bar,true);
      xabsmtm=XMA2.XMASeries(maxbar2,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,absmtm,bar,true);
      xxmtm=XMA3.XMASeries(maxbar3,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xmtm,bar,true);
      xxabsmtm=XMA4.XMASeries(maxbar3,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xabsmtm,bar,true);
      if(xxabsmtm) tsi=100*xxmtm/xxabsmtm;
      else tsi=0;
      if(!tsi) tsi=0.000000001;
      xtsi=XMA5.XMASeries(maxbar4,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,tsi,bar,true);
      if(!xtsi) xtsi=0.000000001;
      UpBuffer[bar]=tsi;
      DnBuffer[bar]=xtsi;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+