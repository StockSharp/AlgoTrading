//+------------------------------------------------------------------+
//|                                                  FiboCandles.mq5 |
//|                                  Copyright � 2010, Ivan Kornilov |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2010, Ivan Kornilov"
#property link "excelf@gmail.com"
#property description "Fibo Candles 2"
//---- ����� ������ ����������
#property version   "1.00"
//+------------------------------------------------+
//|  ��������� ��������� ����������                |
//+------------------------------------------------+
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ���� �������
#property indicator_buffers 5
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//---- � �������� ���������� ������������ ������� �����
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1  Teal, Magenta
//---- ����������� ����� ����������
#property indicator_label1  "FiboCandles Open; FiboCandles High; FiboCandles Low; FiboCandles Close"
//+------------------------------------------------+
//|  ���������� ��������                           |
//+------------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//---- ��������� ����-�������
#define LEVEL_1 0.236
#define LEVEL_2 0.382
#define LEVEL_3 0.500
#define LEVEL_4 0.618
#define LEVEL_5 0.762
//+------------------------------------------------+
//|  ������������ ��� ����-�������                 |
//+------------------------------------------------+
enum ENUM_FIBORATIO //��� ���������
  {
   LEVEL_1_ = 1,   //0.236
   LEVEL_2_,       //0.382
   LEVEL_3_,       //0.500
   LEVEL_4_,       //0.618
   LEVEL_5_        //0.762
  };
//+------------------------------------------------+ 
//| ������������ ��� ��������� ������������ ������ |
//+------------------------------------------------+ 
enum ENUM_ALERT_MODE //��� ���������
  {
   OnlySound,   //������ ����
   OnlyAlert    //������ �����
  };
//+------------------------------------------------+
//| ������� ��������� ����������                   |
//+------------------------------------------------+
input int period=10;                        // ������ ����������
input ENUM_FIBORATIO fiboLevel=LEVEL_1_;    // �������� ����������
//---- ��������� ��� ���������� �������
input uint SignalBar=0;                     // ����� ���� ��� ��������� ������� (0 - ������� ���)
input ENUM_ALERT_MODE alert_mode=OnlySound; // ������� ��������� ������������
input uint AlertCount=0;                    // ���������� ���������� �������
//+------------------------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double ExtOpenBuffer[];
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtCloseBuffer[];
double ExtColorBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int  min_rates_total;
//---- ���������� ���������� ��� �������� ����������
double level;
//+------------------------------------------------------------------+
//|  ��������� ���������� � ���� ������                              |
//+------------------------------------------------------------------+
string GetStringTimeframe(ENUM_TIMEFRAMES timeframe)
  {
//----
   return(StringSubstr(EnumToString(timeframe),7,-1));
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ���������� 
   min_rates_total=period;

   switch(fiboLevel)
     {
      case 1: level = LEVEL_1; break;
      case 2: level = LEVEL_2; break;
      case 3: level = LEVEL_3; break;
      case 4: level = LEVEL_4; break;
      case 5: level = LEVEL_5; break;
     }

//---- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� ExtColorBuffer[] � �������� ��������� �����   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);

//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtOpenBuffer,true);
   ArraySetAsSeries(ExtHighBuffer,true);
   ArraySetAsSeries(ExtLowBuffer,true);
   ArraySetAsSeries(ExtCloseBuffer,true);
   ArraySetAsSeries(ExtColorBuffer,true);

//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="Fibo Candles 2";
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
   if(rates_total<min_rates_total) return(RESET);

//---- ���������� ��������� ���������� 
   int limit,bar,trend;
   double maxHigh,minLow,range;
   static int trend_;
   static uint buycount=0,sellcount=0;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      trend_=0;
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);

//---- ������ �������� ������� � �������� ���������   
   if(rates_total!=prev_calculated && AlertCount)
     {
      buycount=AlertCount;
      sellcount=AlertCount;
     }

//---- ��������������� �������� ����������
   trend=trend_;

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==0) trend_=trend;

      maxHigh=high[ArrayMaximum(high,bar,period)];
      minLow=low[ArrayMinimum(low,bar,period)];
      range=maxHigh-minLow;

      if(open[bar]>close[bar])
        {
         if(!(trend<0 && range*level<close[bar]-minLow)) trend=+1;
         else trend=-1;
        }
      else
        {
         if(!(trend>0 && range*level<maxHigh-close[bar])) trend=-1;
         else trend=+1;
        }

      if(trend==+1)
        {
         ExtOpenBuffer [bar]=MathMax(open[bar], close[bar]);
         ExtCloseBuffer[bar]=MathMin(open[bar], close[bar]);
        }

      if(trend==-1)
        {
         ExtOpenBuffer [bar]=MathMin(open[bar], close[bar]);
         ExtCloseBuffer[bar]=MathMax(open[bar], close[bar]);
        }

      ExtHighBuffer [bar]=high[bar];
      ExtLowBuffer  [bar]=low[bar];

      //--- ������������� ������
      if(ExtOpenBuffer[bar]>ExtCloseBuffer[bar]) ExtColorBuffer[bar]=1.0;
      else                                       ExtColorBuffer[bar]=0.0;
     }

   if(ExtColorBuffer[SignalBar+1]==0 && ExtColorBuffer[SignalBar]==1 && buycount)
     {
      if(alert_mode==OnlyAlert) Alert("FiboCandles: ������ �� ������� �� ",Symbol(),GetStringTimeframe(_Period));
      if(alert_mode==OnlySound) PlaySound("alert.wav");
      buycount--;
     }

   if(ExtColorBuffer[SignalBar+1]==1 && ExtColorBuffer[SignalBar]==0 && sellcount)
     {
      if(alert_mode==OnlyAlert) Alert("FiboCandles: ������ �� ������� �� ",Symbol(),GetStringTimeframe(_Period));
      if(alert_mode==OnlySound) PlaySound("alert.wav");
      sellcount--;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
