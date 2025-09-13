//+------------------------------------------------------------------+
//|                                                   Karacatica.mq5 |
//|                                       Copyright � 2005,  ������� |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2006, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net/"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//---- � �������� ����� ���������� ������ ���������� ����������� ������� ����
#property indicator_color1  clrMagenta
//---- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//---- ����������� ����� ��������� ����� ����������
#property indicator_label1  "Karacatica Sell"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ������ ������ ���������� ����������� ������� ����
#property indicator_color2  clrLime
//---- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//---- ����������� ����� ������ ����� ����������
#property indicator_label2 "Karacatica Buy"

//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint iPeriod=70; //������ ����������
//+----------------------------------------------+

//---- ���������� ������������ ��������, ������� �����
//---- � ���������� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//---
double s;
int StartBars;
int ATR_Handle,ADX_Handle,ltr,ltr_;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ���������� 
   s=1.5/2.0;
   StartBars=int(iPeriod)+1;
//---- ��������� ������ ���������� ATR
   ATR_Handle=iATR(NULL,0,iPeriod);
   if(ATR_Handle==INVALID_HANDLE)Print(" �� ������� �������� ����� ���������� ATR");
//---- ��������� ������ ���������� ADX
   ADX_Handle=iADX(NULL,0,iPeriod);
   if(ADX_Handle==INVALID_HANDLE)Print(" �� ������� �������� ����� ���������� ADX");

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"Karacatica Sell");
//---- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellBuffer,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"Karacatica Buy");
//---- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);

//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="Karacatica";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//----   
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
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
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(ADX_Handle)<rates_total
      || rates_total<StartBars)
      return(0);

//---- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double ADXP[],ADXM[],ATR[];

//---- ������� ������������ ���������� ���������� ������ �
//---- ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      to_copy=rates_total;         // ��������� ���������� ���� �����
      limit=rates_total-StartBars; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      to_copy=rates_total-prev_calculated+1; // ��������� ���������� ������ ����� �����
      limit=rates_total-prev_calculated;     // ��������� ����� ��� ������� ����� �����
     }

//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(0);
   if(CopyBuffer(ADX_Handle,1,0,to_copy,ADXP)<=0) return(0);
   if(CopyBuffer(ADX_Handle,2,0,to_copy,ADXM)<=0) return(0);

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(ADXP,true);
   ArraySetAsSeries(ADXM,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);

//---- ��������������� �������� ����������
   ltr=ltr_;

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0; bar--)
     {
      //---- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==0)
         ltr_=ltr;

      SellBuffer[bar]=0.0;
      BuyBuffer[bar]=0.0;

      if(BuyBuffer[bar+1]!=0 && BuyBuffer[bar+1]!=EMPTY_VALUE)ltr=1;
      if(SellBuffer[bar+1]!=0 && SellBuffer[bar+1]!=EMPTY_VALUE)ltr=2;

      if(close[bar]>close[bar+iPeriod] && ADXP[bar]>ADXM[bar] && ltr!=1)BuyBuffer[bar]=low[bar]-ATR[bar]*s;
      if(close[bar]<close[bar+iPeriod] && ADXP[bar]<ADXM[bar] && ltr!=2)SellBuffer[bar]=high[bar]+ATR[bar]*s;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+