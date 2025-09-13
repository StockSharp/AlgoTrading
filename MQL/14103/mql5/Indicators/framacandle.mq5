//+------------------------------------------------------------------+
//|                                                  FrAMACandle.mq5 |
//|                               Copyright � 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2015, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "FrAMACandle Smoothed"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ���� �������
#property indicator_buffers 5
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- � �������� ���������� ������������ ������� �����
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1   clrDeepPink,clrGray,clrDodgerBlue
//---- ����������� ����� ����������
#property indicator_label1  "FrAMACandle Open;FrAMACandle High;FrAMACandle Low;FrAMACandle Close"
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint FrAMA_Period=14;
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double ExtOpenBuffer[];
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtCloseBuffer[];
double ExtColorBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������� ���������� ��� ������� �����������
int FrAMA_Handle[4];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ���������� 
   min_rates_total=int(FrAMA_Period);
//---- ��������� ������ ���������� iFrAMA
   FrAMA_Handle[0]=iFrAMA(NULL,0,FrAMA_Period,0,PRICE_OPEN);
   if(FrAMA_Handle[0]==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iFrAMA["+string(0)+"]!");
//----
   FrAMA_Handle[1]=iFrAMA(NULL,0,FrAMA_Period,0,PRICE_HIGH);
   if(FrAMA_Handle[1]==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iFrAMA["+string(1)+"]!");
//----
   FrAMA_Handle[2]=iFrAMA(NULL,0,FrAMA_Period,0,PRICE_LOW);
   if(FrAMA_Handle[2]==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iFrAMA["+string(2)+"]!");
//----
   FrAMA_Handle[3]=iFrAMA(NULL,0,FrAMA_Period,0,PRICE_CLOSE);
   if(FrAMA_Handle[3]==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iFrAMA["+string(3)+"]!");
//---- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������� ��� � ����������
   ArraySetAsSeries(ExtOpenBuffer,true);
   ArraySetAsSeries(ExtHighBuffer,true);
   ArraySetAsSeries(ExtLowBuffer,true);
   ArraySetAsSeries(ExtCloseBuffer,true);
   ArraySetAsSeries(ExtColorBuffer,true);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="FrAMACandl";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
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
                const int &spread[])
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(FrAMA_Handle[0])<rates_total
      || BarsCalculated(FrAMA_Handle[1])<rates_total
      || BarsCalculated(FrAMA_Handle[2])<rates_total
      || BarsCalculated(FrAMA_Handle[3])<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//---- ���������� ��������� ���������� 
   int to_copy,limit,bar,clr;
//---- ������� ������������ ���������� ���������� ������ � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }
//----
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(FrAMA_Handle[0],0,0,to_copy,ExtOpenBuffer)<=0) return(RESET);
   if(CopyBuffer(FrAMA_Handle[1],0,0,to_copy,ExtHighBuffer)<=0) return(RESET);
   if(CopyBuffer(FrAMA_Handle[2],0,0,to_copy,ExtLowBuffer)<=0) return(RESET);
   if(CopyBuffer(FrAMA_Handle[3],0,0,to_copy,ExtCloseBuffer)<=0) return(RESET);
//---- �������� ���� ����������� � ����������� ������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      double Max=MathMax(ExtOpenBuffer[bar],ExtCloseBuffer[bar]);
      double Min=MathMin(ExtOpenBuffer[bar],ExtCloseBuffer[bar]);
      //----
      ExtHighBuffer[bar]=MathMax(Max,ExtHighBuffer[bar]);
      ExtLowBuffer[bar]=MathMin(Min,ExtLowBuffer[bar]);
      //----
      if(ExtOpenBuffer[bar]<ExtCloseBuffer[bar]) clr=2;
      else if(ExtOpenBuffer[bar]>ExtCloseBuffer[bar]) clr=0;
      else clr=1;
      ExtColorBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
