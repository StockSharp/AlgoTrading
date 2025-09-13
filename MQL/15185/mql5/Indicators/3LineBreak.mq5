//+------------------------------------------------------------------+
//|                                                   3LineBreak.mq5 |
//|                               Copyright � 2004, Poul_Trade_Forum |
//|                                                         Aborigen |
//|                                          http://forex.kbpauk.ru/ |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright " Copyright � 2004, Poul_Trade_Forum"
//---- ������ �� ���� ������
#property link      " http://forex.kbpauk.ru/"
//---- ����� ������ ����������
#property version   "1.00"
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 3
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//---- � �������� ������ ������������
#property indicator_color1 clrBlue,clrRed
//---- ������� ����� ���������� 1 ����� 2
#property indicator_width1 2
//---- � �������� ���������� ������������ ������������ ����
#property indicator_type1   DRAW_COLOR_HISTOGRAM2
//---- ����������� ����� ����������
#property indicator_label1  "UpTend; DownTrend;"

//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int Lines_Break=3;
//+----------------------------------------------+

//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtColorsBuffer[];
//----
bool Swing_;
int StartBars;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ���������� 
   StartBars=Lines_Break;
//---- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(0,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtLowBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � �������� ��������� �����   
   SetIndexBuffer(2,ExtColorsBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � �������, ��� � ����������   
   ArraySetAsSeries(ExtHighBuffer,true);
   ArraySetAsSeries(ExtLowBuffer,true);
   ArraySetAsSeries(ExtColorsBuffer,true);

//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="3LineBreak";
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
                const int &spread[])
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<StartBars) return(0);

//---- ���������� ��������� ���������� 
   int limit,bar;
   double HH,LL;
   bool Swing;

//---- ������� ������������ ���������� ���������� ������ �
//���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-StartBars; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }

//---- ���������� ��������� � ��������, ��� � ����������  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- ��������������� �������� ����������
   Swing=Swing_;

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0; bar--)
     {
      //---- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==0) Swing_=Swing;

      HH = high[ArrayMaximum(high,bar+1,Lines_Break)];
      LL = low [ArrayMinimum(low, bar+1,Lines_Break)];
      //----
      if( Swing && low [bar]<LL) Swing=false;
      if(!Swing && high[bar]>HH) Swing=true;
      //----
      ExtHighBuffer[bar]=high[bar];
      ExtLowBuffer [bar]=low [bar];

      if(Swing) ExtColorsBuffer[bar]=0;
      else      ExtColorsBuffer[bar]=1;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
