//+------------------------------------------------------------------+
//|                                                Arrows&Curves.mq5 |
//|          Copyright � 2007, ������� ������ ����������� aka lukas1 |
//|                                                    lukas1@ngs.ru |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2007, ������� ������ ����������� aka lukas1"
//---- ������ �� ���� ������
#property link      "lukas1@ngs.ru"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ������ �������
#property indicator_buffers 8
//---- ������������ ����� ������ ����������� ����������
#property indicator_plots   8
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//---- � �������� ����� ��������� ����� ���������� ����������� ������� ����
#property indicator_color1  Magenta
//---- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//---- ����������� ����� ��������� ����� ����������
#property indicator_label1  "Sell"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ����� ����� ���������� ����������� ������� ����
#property indicator_color2  Lime
//---- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//---- ����������� ����� ������ ����� ����������
#property indicator_label2 "Buy"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� �������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ��������� ����� ���������� ����������� ������� ����
#property indicator_color3  Magenta
//---- ������� ����� ���������� 3 ����� 4
#property indicator_width3  4
//---- ����������� ����� ��������� ����� ����������
#property indicator_label3  "SellStop"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 4 � ���� �������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ����� ����� ���������� ����������� ������� ����
#property indicator_color4  Lime
//---- ������� ����� ���������� 4 ����� 4
#property indicator_width4  4
//---- ����������� ����� ������ ����� ����������
#property indicator_label4 "BuyStop"
//+--------------------------------------------+
//|  ��������� ���������  ������� ����������   |
//+--------------------------------------------+
//---- ��������� �������  � ���� �����
#property indicator_type5   DRAW_LINE
#property indicator_type6   DRAW_LINE
#property indicator_type7   DRAW_LINE
#property indicator_type8   DRAW_LINE
//---- � �������� ������ ������� ������ �����
#property indicator_color5  Orange
#property indicator_color6  MediumSeaGreen
#property indicator_color7  MediumSeaGreen
#property indicator_color8  Orange
//---- ������ ����������� - ��������������� ������
#property indicator_style5 STYLE_DASHDOTDOT
#property indicator_style6 STYLE_DASHDOTDOT
#property indicator_style7 STYLE_DASHDOTDOT
#property indicator_style8 STYLE_DASHDOTDOT
//---- ������� ������� ����������� ����� 1
#property indicator_width5  1
#property indicator_width6  1
#property indicator_width7  1
#property indicator_width8  1
//---- ����������� ����� ������� �����������
#property indicator_label5  "BUY from here"
#property indicator_label6  "BuyStop"
#property indicator_label7  "SellStop"
#property indicator_label8  "SELL from here"

//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int SSP     = 20;   //������ ��������� ��������� ����������
input int Channel = 0;    //���������� ��������� ������. �.�. � ��������� 0-50
input int Ch_Stop = 30;   //���������� ��������� ������ (����������� � ��������)
input int relay   = 10;   //�������� ����� ������ ������� �� 4 ���� 
//+----------------------------------------------+

//---- ���������� ������������ ��������, ������� �����
//---- � ���������� ������������ � �������� ������������ �������
double BuyBuffer[];
double SellBuffer[];
double HBuffer[];
double LBuffer[];
double HSBuffer[];
double LSBuffer[];
double BuyStopBuffer[],SellStopBuffer[];
//---
int StartBars;
bool uptrend_,old_,uptrend2_,old2_;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ���������� 
   StartBars=SSP+1+relay;

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"Sell");
//---- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellBuffer,true);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"Buy");
//---- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,108);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,SellStopBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"SellStop");
//---- ������ ��� ����������
   PlotIndexSetInteger(2,PLOT_ARROW,251);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellStopBuffer,true);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,BuyStopBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(3,PLOT_LABEL,"BuyStop");
//---- ������ ��� ����������
   PlotIndexSetInteger(3,PLOT_ARROW,251);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyStopBuffer,true);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0);

//---- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(4,HBuffer,INDICATOR_DATA);
   SetIndexBuffer(5,HSBuffer,INDICATOR_DATA);
   SetIndexBuffer(6,LSBuffer,INDICATOR_DATA);
   SetIndexBuffer(7,LBuffer,INDICATOR_DATA);
//---- ��������� �������, � ������� ���������� ��������� ������� �����������
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,StartBars);
   PlotIndexSetInteger(5,PLOT_DRAW_BEGIN,StartBars);
   PlotIndexSetInteger(6,PLOT_DRAW_BEGIN,StartBars);
   PlotIndexSetInteger(7,PLOT_DRAW_BEGIN,StartBars);
//---- �������� ����� ��� ����������� � ���� ������
   PlotIndexSetString(4,PLOT_LABEL,"BUY from here");
   PlotIndexSetString(5,PLOT_LABEL,"BuyStop");
   PlotIndexSetString(6,PLOT_LABEL,"SellStop");
   PlotIndexSetString(7,PLOT_LABEL,"SELL from here");
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(4,PLOT_EMPTY_VALUE,0);
   PlotIndexSetDouble(5,PLOT_EMPTY_VALUE,0);
   PlotIndexSetDouble(6,PLOT_EMPTY_VALUE,0);
   PlotIndexSetDouble(7,PLOT_EMPTY_VALUE,0);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(HBuffer,true);
   ArraySetAsSeries(HSBuffer,true);
   ArraySetAsSeries(LSBuffer,true);
   ArraySetAsSeries(LBuffer,true);

//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� �������� 
   string short_name="Arrows&Curves";
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
   double High,Low,smin,smax,smin2,smax2,Close;
   static bool uptrend,old,uptrend2,old2;

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

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);

//---- ��������������� �������� ����������
   uptrend=uptrend_;
   uptrend2=uptrend2_;
   old=old_;
   old2=old2_;

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0; bar--)
     {
      //---- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==0)
        {
         uptrend_=uptrend;
         uptrend2_=uptrend2;
         old_=old;
         old2_=old2;
        }

      Close= close[bar];
      High = high[iHighest(high,SSP,bar+relay)];
      Low  = low [iLowest (low, SSP,bar+relay)];
      smax = High -(Low-High)*Channel/ 100;           // smax ���� High � ������ �����.Channel
      smin = Low+(High-Low)*Channel / 100;            // smin ���� Low � ������ �����.Channel
      smax2= High -(High-Low)*(Channel+Ch_Stop)/ 100; // smax ���� High � ������ �����.Chan+Ch_Stop
      smin2= Low+(High-Low)*(Channel+Ch_Stop) / 100;  // smin ���� Low � ������ �����.Channel
      BuyBuffer[bar]=0;
      SellBuffer[bar]=0;
      BuyStopBuffer[bar]=0;
      SellStopBuffer[bar]=0;
      //----
      if(Close<smin && Close<smax && uptrend2==true && bar!=0) uptrend=false;
      if( Close > smax  && Close > smin   && uptrend2 == false && bar!=0 ) uptrend  = true;
      if((Close > smax2 || Close > smin2) && uptrend  == false && bar!=0 ) uptrend2 = false;
      if((Close<smin2 || Close<smax2) && uptrend==true && bar!=0) uptrend2=true;
      //---- ��������� ������ �� ����������� ������ "uptrend"
      //---- �� ������������ ������ �� ���������
      if(close[bar]<smin && close[bar]<smax && uptrend2==false && bar!=0)
        {
         SellBuffer[bar]=Low;
         uptrend2=true;
        }
      //---- ��������� ������ �� ����������� ������ "uptrend"
      //---- �� ������������ ������ �� ���������
      if(Close>smax && Close>smin && uptrend2==true && bar!=0)
        {
         BuyBuffer[bar]=High;
         uptrend2=false;
        }
      //----
      if(uptrend != old && uptrend == false) SellBuffer[bar] = Low;
      if(uptrend != old && uptrend == true ) BuyBuffer[bar] = High;
      //----
      if(uptrend2 != old2 && uptrend2 == true ) BuyStopBuffer[bar] = smax2;
      if(uptrend2 != old2 && uptrend2 == false) SellStopBuffer[bar] = smin2;
      //----
      old=uptrend;
      old2=uptrend2;
      //----
      HBuffer[bar]=smax;
      LBuffer[bar]=smin;
      HSBuffer[bar]=smax2;
      LSBuffer[bar]=smin2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
//|  searching index of the highest bar                              |
//+------------------------------------------------------------------+
int iHighest(const double &array[],// ������ ��� ������ ������� ������������� ��������
             int count,            // ����� ��������� ������� (� ����������� �� �������� ���� � ������� �������� �������), 
                                   // ����� ������� ������ ���� ���������� �����.
             int startPos          // ������ (�������� ������������ �������� ����) ���������� ����, 
                                   // � �������� ���������� ����� ����������� ��������
             )
  {
//----
   int index=startPos;

//---- �������� ���������� ������� �� ������������
   if(startPos<0)
     {
      Print("�������� �������� � ������� iHighest, startPos = ",startPos);
      return(0);
     }

   double max=array[startPos];

//---- ����� �������
   for(int i=startPos; i<startPos+count; i++)
     {
      if(array[i]>max)
        {
         index=i;
         max=array[i];
        }
     }
//---- ������� ������� ����������� ����
   return(index);
  }
//+------------------------------------------------------------------+
//|  searching index of the lowest bar                               |
//+------------------------------------------------------------------+
int iLowest(const double &array[],  // ������ ��� ������ ������� ������������ ��������
            int count,              // ����� ��������� ������� (� ����������� �� �������� ���� � ������� �������� �������), 
                                    // ����� ������� ������ ���� ���������� �����.
            int startPos            // ������ (�������� ������������ �������� ����) ���������� ����, 
                                    // � �������� ���������� ����� ����������� ��������
            )
  {
//----
   int index=startPos;

//---- �������� ���������� ������� �� ������������
   if(startPos<0)
     {
      Print("�������� �������� � ������� iLowest, startPos = ",startPos);
      return(0);
     }

   double min=array[startPos];

//---- ����� �������
   for(int i=startPos; i<startPos+count; i++)
     {
      if(array[i]<min)
        {
         index=i;
         min=array[i];
        }
     }
//---- ������� ������� ����������� ����
   return(index);
  }
//+------------------------------------------------------------------+