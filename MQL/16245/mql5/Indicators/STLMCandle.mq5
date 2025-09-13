//+------------------------------------------------------------------+
//|                                                   STLMCandle.mq5 |
//|                               Copyright � 2016, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2016, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "��������� ColorSTLM � ������� ����"
//---- ����� ������ ����������
#property version   "1.00"
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ���� �������
#property indicator_buffers 5
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//---- � �������� ���������� ������������ ������� �����
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1   clrDarkOrange,clrGray,clrDarkTurquoise
//---- ����������� ����� ����������
#property indicator_label1  "STLMCandle Open;High;Low;Close"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor clrBlueViolet
#property indicator_levelstyle STYLE_SOLID
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//|  ������� ��������� ����������                |
//+----------------------------------------------+
input int JLength=5; // ������� �����������                   
input int JPhase=100; // �������� �����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double ExtOpenBuffer[];
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtCloseBuffer[];
double ExtColorBuffer[];

//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ����� ���������� ��� ������� �����������
int Ind_Handle[4];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ������������� ���������� ���������� 
   int STLMPeriod=91;
   min_rates_total=STLMPeriod+1+30;
//---- ��������� ������� ���������� ColorSTLM_HISTOGRAM
   Ind_Handle[0]=iCustom(NULL,0,"ColorSTLM_HISTOGRAM",JLength,JPhase,PRICE_OPEN,0);
   if(Ind_Handle[0]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ColorSTLM_HISTOGRAM["+string(0)+"]!");
      return(INIT_FAILED);
     }

   Ind_Handle[1]=iCustom(NULL,0,"ColorSTLM_HISTOGRAM",JLength,JPhase,PRICE_HIGH,0);
   if(Ind_Handle[1]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ColorSTLM_HISTOGRAM["+string(1)+"]!");
      return(INIT_FAILED);
     }

   Ind_Handle[2]=iCustom(NULL,0,"ColorSTLM_HISTOGRAM",JLength,JPhase,PRICE_LOW,0);
   if(Ind_Handle[2]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ColorSTLM_HISTOGRAM["+string(2)+"]!");
      return(INIT_FAILED);
     }

   Ind_Handle[3]=iCustom(NULL,0,"ColorSTLM_HISTOGRAM",JLength,JPhase,PRICE_CLOSE,0);
   if(Ind_Handle[3]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ColorSTLM_HISTOGRAM["+string(3)+"]!");
      return(INIT_FAILED);
     }

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
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� �������� 
   string short_name="ColorSTLMCandle";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
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
   if(BarsCalculated(Ind_Handle[0])<rates_total
      || BarsCalculated(Ind_Handle[1])<rates_total
      || BarsCalculated(Ind_Handle[2])<rates_total
      || BarsCalculated(Ind_Handle[3])<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- ���������� ��������� ���������� 
   int to_copy,limit,bar;

//---- ������� ������������ ���������� ���������� ������ � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }

   to_copy=limit+1;

//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(Ind_Handle[0],0,0,to_copy,ExtOpenBuffer)<=0) return(RESET);
   if(CopyBuffer(Ind_Handle[1],0,0,to_copy,ExtHighBuffer)<=0) return(RESET);
   if(CopyBuffer(Ind_Handle[2],0,0,to_copy,ExtLowBuffer)<=0) return(RESET);
   if(CopyBuffer(Ind_Handle[3],0,0,to_copy,ExtCloseBuffer)<=0) return(RESET);


//---- �������� ���� ����������� � ����������� ������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      double Max=MathMax(ExtOpenBuffer[bar],ExtCloseBuffer[bar]);
      double Min=MathMin(ExtOpenBuffer[bar],ExtCloseBuffer[bar]);

      ExtHighBuffer[bar]=MathMax(Max,ExtHighBuffer[bar]);
      ExtLowBuffer[bar]=MathMin(Min,ExtLowBuffer[bar]);

      if(ExtOpenBuffer[bar]<ExtCloseBuffer[bar]) ExtColorBuffer[bar]=2.0;
      else if(ExtOpenBuffer[bar]>ExtCloseBuffer[bar]) ExtColorBuffer[bar]=0.0;
      else ExtColorBuffer[bar]=1.0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
