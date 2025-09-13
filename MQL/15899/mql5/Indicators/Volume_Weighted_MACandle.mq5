//+------------------------------------------------------------------+
//|                                     Volume_Weighted_MACandle.mq5 |
//|                               Copyright � 2016, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2016, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Volume_Weighted_MACandle"
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
#property indicator_color1   clrDeepPink,clrGray,clrTeal
//---- ����������� ����� ����������
#property indicator_label1  "Volume_Weighted_MACandle Open;High;Low;Close"
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint Length=12;                              //������� �����������                    
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  //����� 
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
//----
   min_rates_total=int(Length);

//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
   enum Applied_price_      //��� ���������
     {
      PRICE_CLOSE_ = 1,     //Close
      PRICE_OPEN_,          //Open
      PRICE_HIGH_,          //High
      PRICE_LOW_            //Low
     };

//---- ��������� ������ ���������� iVolume_Weighted_MA
   Ind_Handle[0]=iCustom(NULL,0,"Volume_Weighted_MA",Length,PRICE_OPEN_,VolumeType,0,0);
   if(Ind_Handle[0]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� Volume_Weighted_MA["+string(0)+"]!");
      return(INIT_FAILED);
     }

   Ind_Handle[1]=iCustom(NULL,0,"Volume_Weighted_MA",Length,PRICE_HIGH_,VolumeType,0,0);
   if(Ind_Handle[1]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� Volume_Weighted_MA["+string(1)+"]!");
      return(INIT_FAILED);
     }

   Ind_Handle[2]=iCustom(NULL,0,"Volume_Weighted_MA",Length,PRICE_LOW_,VolumeType,0,0);
   if(Ind_Handle[2]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� Volume_Weighted_MA["+string(2)+"]!");
      return(INIT_FAILED);
     }

   Ind_Handle[3]=iCustom(NULL,0,"Volume_Weighted_MA",Length,PRICE_CLOSE_,VolumeType,0,0);
   if(Ind_Handle[3]==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� Volume_Weighted_MA["+string(3)+"]!");
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
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);

//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� �������� 
   string short_name="Volume_Weighted_MACandle";
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
