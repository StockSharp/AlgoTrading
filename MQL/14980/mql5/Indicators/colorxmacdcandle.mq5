//+---------------------------------------------------------------------+
//|                                                ColorXMACDCandle.mq5 |
//|                                  Copyright � 2016, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2016, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ��� ������� � ��������� ���������� ������������ ���� �������
#property indicator_buffers 7
//---- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- � �������� ���������� ������������ ������� �����
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1   clrMagenta,clrGray,clrBlue
//---- ����������� ����� ����������
#property indicator_label1  "MACDCandle Open;MACDCandle High;MACDCandle Low;MACDCandle Close"
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� ���������� �����
#property indicator_type2 DRAW_COLOR_LINE
//---- � �������� ������ ���������� ����� ������������
#property indicator_color2 clrGray,clrLime,clrRed
//---- ����� ���������� - �������� �����
#property indicator_style2 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width2 2
//---- ����������� ����� ���������� �����
#property indicator_label2  "Signal Line"
//+-----------------------------------+
//|  �������� ������� ����������      |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3,XMA4,XMA5,XMA6,XMA7,XMA8,XMA9;
//+-----------------------------------+
//|  ���������� ������������          |
//+-----------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_            //Low
  };
//+-----------------------------------+
//|  ���������� ������������          |
//+-----------------------------------+
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
//+-----------------------------------+
//|  ������� ��������� ����������     |
//+-----------------------------------+
input Smooth_Method XMA_Method=MODE_T3; //����� ���������� �����������
input int Fast_XMA = 12; //������ �������� �������
input int Slow_XMA = 26; //������ ���������� �������
input int XPhase = 100;  //�������� ���������� ��������,
                       //��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
// ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method Signal_Method=MODE_JJMA; //����� ���������� ���������� �����
input int Signal_XMA=9; //������ ���������� ����� 
input int Signal_Phase=100; // �������� ���������� �����,
                            //������������ � �������� -100 ... +100,
//������ �� �������� ����������� ��������;
input Applied_price_ AppliedPrice=PRICE_CLOSE_;//������� ��������� ���������� �����
//+-----------------------------------+
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total,min_rates_1;
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double ExtOpenBuffer[],ExtHighBuffer[],ExtLowBuffer[],ExtCloseBuffer[],ExtColorBuffer[];
double SignBuffer[],ColorSignBuffer[];
//+------------------------------------------------------------------+    
//| XMACD indicator initialization function                          | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_1=MathMax(GetStartBars(XMA_Method,Fast_XMA,XPhase),GetStartBars(XMA_Method,Slow_XMA,XPhase));
   min_rates_total=min_rates_1+GetStartBars(Signal_Method,Signal_XMA,Signal_Phase)+2;

//---- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
   
//---- ����������� ������������� ������� SignBuffer � ������������ �����
   SetIndexBuffer(5,SignBuffer,INDICATOR_DATA);
   
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(6,ColorSignBuffer,INDICATOR_COLOR_INDEX);   
   
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
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(XMA_Method);
   string Smooth2=XMA1.GetString_MA_Method(Signal_Method);
   StringConcatenate(shortname,
                     "XMACD( ",Fast_XMA,", ",Slow_XMA,", ",Signal_XMA,", ",Smooth1,", ",Smooth2," )");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
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
   if(rates_total<min_rates_total) return(0);

//---- ���������� ����� ����������
   int first1,first2,bar;
//---- ���������� ���������� � ��������� ������  
   double fast_xma,slow_xma,sign_xma=0.0,oxmacd,cxmacd,hxmacd,lxmacd,Max,Min;

//---- ������������� ���������� � ����� OnCalculate()
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      first1=0; // ��������� ����� ��� ������� ���� ����� ������� �����
      first2=min_rates_total+1; // ��������� ����� ��� ������� ���� ����� ������� �����
     }
   else // ��������� ����� ��� ������� ����� �����
     {
      first1=prev_calculated-1;
      first2=first1;
     }

//---- �������� ���� ������� ����������
   for(bar=first1; bar<rates_total; bar++)
     {
      fast_xma=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Fast_XMA,open[bar],bar,false);
      slow_xma=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Slow_XMA,open[bar],bar,false);
      oxmacd=(fast_xma-slow_xma)/_Point;
      //----
      fast_xma=XMA3.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Fast_XMA,close[bar],bar,false);
      slow_xma=XMA4.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Slow_XMA,close[bar],bar,false);
      cxmacd=(fast_xma-slow_xma)/_Point;
      //----
      fast_xma=XMA5.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Fast_XMA,high[bar],bar,false);
      slow_xma=XMA6.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Slow_XMA,high[bar],bar,false);
      hxmacd=(fast_xma-slow_xma)/_Point;
      //----
      fast_xma=XMA7.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Fast_XMA,low[bar],bar,false);
      slow_xma=XMA8.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Slow_XMA,low[bar],bar,false);
      lxmacd=(fast_xma-slow_xma)/_Point;
      //---- ����������� � ����������� ������
      Max=MathMax(oxmacd,cxmacd);
      Min=MathMin(oxmacd,cxmacd);     
      ExtCloseBuffer[bar]=cxmacd;
      ExtOpenBuffer[bar]=oxmacd;
      ExtHighBuffer[bar]=MathMax(Max,hxmacd);
      ExtLowBuffer[bar]=MathMin(Min,lxmacd);
      if(ExtOpenBuffer[bar]<ExtCloseBuffer[bar]) ExtColorBuffer[bar]=2.0;
      else if(ExtOpenBuffer[bar]>ExtCloseBuffer[bar]) ExtColorBuffer[bar]=0.0;
      else ExtColorBuffer[bar]=1.0;
      //---- 
      switch(AppliedPrice)
        {
         case PRICE_OPEN_ :
           {
            sign_xma=XMA9.XMASeries(min_rates_1,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,oxmacd,bar,false);
            break;
           }
         case PRICE_CLOSE_ :
           {
            sign_xma=XMA9.XMASeries(min_rates_1,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,cxmacd,bar,false);
            break;
           }
         case PRICE_HIGH_ :
           {
            sign_xma=XMA9.XMASeries(min_rates_1,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,hxmacd,bar,false);
            break;
           }
         case PRICE_LOW_ :
           {
            sign_xma=XMA9.XMASeries(min_rates_1,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,lxmacd,bar,false);
           }
        }
      SignBuffer[bar] = sign_xma;
     }

//---- �������� ���� ��������� ���������� �����
   for(bar=first2; bar<rates_total; bar++)
     {
      ColorSignBuffer[bar]=0;
      if(SignBuffer[bar]>SignBuffer[bar-1]) ColorSignBuffer[bar]=1;
      if(SignBuffer[bar]<SignBuffer[bar-1]) ColorSignBuffer[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
