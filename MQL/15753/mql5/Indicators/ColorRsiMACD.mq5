//+---------------------------------------------------------------------+ 
//|                                                    ColorRsiMACD.mq5 | 
//|                                           Copyright � 2016, Maury74 | 
//|                                         molinari.maurizio@gmail.com | 
//+---------------------------------------------------------------------+
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2016, Maury74"
#property link "molinari.maurizio@gmail.com" 
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ ������� 4
#property indicator_buffers 4 
//---- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- � �������� ������ ������������� ����������� ������������
#property indicator_color1 clrGray,clrTeal,clrBlueViolet,clrIndianRed,clrMagenta
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1 "ColorRsiMACD"

//---- ��������� ���������� � ���� ���������� �����
#property indicator_type2 DRAW_COLOR_LINE
//---- � �������� ������ ���������� ����� ������������
#property indicator_color2 clrGray,clrDodgerBlue,clrMagenta
//---- ����� ���������� - ��������������� ������
#property indicator_style2 STYLE_SOLID
//---- ������� ����� ���������� ����� 3
#property indicator_width2 3
//---- ����������� ����� ���������� �����
#property indicator_label2  "Signal Line"
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET 0                // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//|  �������� ������� ����������                 |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+

//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3;
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price 
  };
//+----------------------------------------------+
//|  ���������� ������������                     |
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
//|  ������� ��������� ����������                |
//+----------------------------------------------+
input uint    RSIPeriod=14;
input ENUM_APPLIED_PRICE   RSIPrice=PRICE_CLOSE;
input Smooth_Method XMA_Method=MODE_T3; //����� ���������� �����������
input uint Fast_XMA = 12; //������ �������� �������
input uint Slow_XMA = 26; //������ ���������� �������
input int XPhase=100;  //�������� ���������� ��������,
                       //��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
// ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method Signal_Method=MODE_JJMA; //����� ���������� ���������� �����
input int Signal_XMA=9; //������ ���������� ����� 
input int Signal_Phase=100; // �������� ���������� �����,
                            //������������ � �������� -100 ... +100,
//������ �� �������� ����������� ��������;
input Applied_price_ AppliedPrice=PRICE_CLOSE_;//������� ���������
//+----------------------------------------------+
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2,min_rates_3;
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double XMACDBuffer[],SignBuffer[],ColorXMACDBuffer[],ColorSignBuffer[];
//--- ���������� ������������� ���������� ��� ������� �����������
int Ind_Handle;
//+------------------------------------------------------------------+    
//| XMACD indicator initialization function                          | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_1=int(RSIPeriod);
   min_rates_2=min_rates_1+GetStartBars(XMA_Method,Fast_XMA,XPhase);
   min_rates_3=min_rates_2+GetStartBars(XMA_Method,Slow_XMA,XPhase);
   min_rates_total=min_rates_3+GetStartBars(Signal_Method,Signal_XMA,Signal_Phase)+2;

//--- ��������� ������ ���������� iRSI
   Ind_Handle=iRSI(Symbol(),NULL,RSIPeriod,RSIPrice);
   if(Ind_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iRSI");
      return(INIT_FAILED);
     }

//---- ����������� ������������� ������� XMACDBuffer � ������������ �����
   SetIndexBuffer(0,XMACDBuffer,INDICATOR_DATA);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorXMACDBuffer,INDICATOR_COLOR_INDEX);

//---- ����������� ������������� ������� SignBuffer � ������������ �����
   SetIndexBuffer(2,SignBuffer,INDICATOR_DATA);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(3,ColorSignBuffer,INDICATOR_COLOR_INDEX);

//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("Fast_XMA", Fast_XMA);
   XMA1.XMALengthCheck("Slow_XMA", Slow_XMA);
   XMA1.XMALengthCheck("Signal_XMA", Signal_XMA);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMAPhaseCheck("XPhase", XPhase, XMA_Method);
   XMA1.XMAPhaseCheck("Signal_Phase", Signal_Phase, Signal_Method);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname="ColorRsiMACD";
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| XMACD iteration function                                         | 
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
   if(rates_total<min_rates_total) return(RESET);
   if(BarsCalculated(Ind_Handle)<rates_total) return(prev_calculated);

//---- ���������� ����� ����������
   int first1,first2,first3,bar;
//---- ���������� ���������� � ��������� ������  
   double RSI[1],fast_rsi,slow_rsi,rsi_macd,sign_rsi;

//---- ������������� ���������� � ����� OnCalculate()
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      first1=min_rates_1; // ��������� ����� ��� ������� ���� ����� ������� �����
      first2=min_rates_1+1; // ��������� ����� ��� ������� ���� ����� ������� �����
      first3=min_rates_total+1; // ��������� ����� ��� ������� ���� ����� �������� �����
     }
   else // ��������� ����� ��� ������� ����� �����
     {
      first1=prev_calculated-1;
      first2=first1;
      first3=first1;
     }

//---- �������� ���� ������� ����������
   for(bar=first1; bar<rates_total; bar++)
     {
      if(CopyBuffer(Ind_Handle,0,rates_total-1-bar,1,RSI)<=0) return(RESET);
      fast_rsi=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,Fast_XMA,RSI[0],bar,false);
      slow_rsi=XMA2.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,Slow_XMA,fast_rsi,bar,false);
      rsi_macd=fast_rsi-slow_rsi;
      sign_rsi=XMA3.XMASeries(min_rates_3,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,rsi_macd,bar,false);
      //---- �������� ���������� �������� � ������������ ������      
      XMACDBuffer[bar]=rsi_macd;
      SignBuffer[bar]=sign_rsi;
     }

//---- �������� ���� ��������� ���������� XMACD
   for(bar=first2; bar<rates_total; bar++)
     {
      ColorXMACDBuffer[bar]=0;

      if(XMACDBuffer[bar]>0)
        {
         if(XMACDBuffer[bar]>XMACDBuffer[bar-1]) ColorXMACDBuffer[bar]=1;
         if(XMACDBuffer[bar]<XMACDBuffer[bar-1]) ColorXMACDBuffer[bar]=2;
        }

      if(XMACDBuffer[bar]<0)
        {
         if(XMACDBuffer[bar]<XMACDBuffer[bar-1]) ColorXMACDBuffer[bar]=3;
         if(XMACDBuffer[bar]>XMACDBuffer[bar-1]) ColorXMACDBuffer[bar]=4;
        }
     }

//---- �������� ���� ��������� ���������� �����
   for(bar=first3; bar<rates_total; bar++)
     {
      ColorSignBuffer[bar]=0;
      if(XMACDBuffer[bar]>SignBuffer[bar-1]) ColorSignBuffer[bar]=1;
      if(XMACDBuffer[bar]<SignBuffer[bar-1]) ColorSignBuffer[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
