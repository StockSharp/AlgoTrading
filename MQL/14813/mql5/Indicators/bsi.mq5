//+------------------------------------------------------------------+
//|                                                          BSI.mq5 |
//|                                          Copyright 2015, fxborg. |
//|                                  http://blog.livedoor.jp/fxborg/ |
//+------------------------------------------------------------------+ 
#property copyright   "Copyright 2015, fxborg"
#property link        "http://blog.livedoor.jp/fxborg/"
#property description "Bounce Strength Indicator" 
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ ������� 4
#property indicator_buffers 4 
//---- ������������ ����� ��� ����������� ����������
#property indicator_plots   3
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����������
#property indicator_type1 DRAW_HISTOGRAM
//---- � �������� �����  ����������� �����������
#property indicator_color1 clrTeal
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ����������
#property indicator_label1 "Floor Bounce Strength"
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����������
#property indicator_type2 DRAW_HISTOGRAM
//---- � �������� �����  ����������� �����������
#property indicator_color2 clrRed
//---- ����� ���������� - ��������
#property indicator_style2 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width2 2
//---- ����������� ����� ����������
#property indicator_label2 "Ceiling Bounce Strength"
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� ����������� �����
#property indicator_type3 DRAW_COLOR_LINE
//---- � �������� ������ ����������� ����� ������������
#property indicator_color3 clrMagenta,clrGray,clrDodgerBlue
//---- ����� ���������� - ��������
#property indicator_style3 STYLE_SOLID
//---- ������� ����� ���������� ����� 3
#property indicator_width3 3
//---- ����������� ����� ���������� �����
#property indicator_label3  "Bounce Strength Index"
//+-----------------------------------+
//| ��������� ��������� �������       |
//+-----------------------------------+
#property indicator_level1     10.0
#property indicator_level2     0.0
#property indicator_level3     -10.0
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
enum Volume_Mode      //��� ���������
  {
   ENUM_WITHOUT_VOLUME = 1,     //Using without Volume
   ENUM_VOLUME,                 //Using Volume
   ENUM_TICKVOLUME              //Using TickVolume
  };
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint InpRangePeriod=20; // Range Period
input uint InpSlowing=3;      // Slowing
input uint InpAvgPeriod=3;    // Avg Period
input Volume_Mode InpUsingVolumeWeight=ENUM_TICKVOLUME;   // Using Volume
//+-----------------------------------+
//---- ���������� ���������� ����������
int Count[];
double ExtHighest[],ExtLowest[],ExtVolume[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,size,min_rates_1;
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double ExtPosBuffer[],ExtNegBuffer[],BSIBuffer[],ColorBSIBuffer[];
//+------------------------------------------------------------------+
//| �������� ������� ������ ������ �������� � �������                |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
                          int Size)
  {
//----
   int numb,Max1,Max2;
   static int count=1;
//----
   Max2=Size;
   Max1=Max2-1;
//----
   count--;
   if(count<0) count=Max1;
//----
   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+    
//| BSI indicator initialization function                            | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(InpRangePeriod+InpSlowing+InpAvgPeriod);
   size=int(MathMax(InpRangePeriod,InpSlowing));
   min_rates_1=size;
//---- ������������� ������ ��� ������� ����������  
   ArrayResize(Count,size);
   ArrayResize(ExtHighest,size);
   ArrayResize(ExtLowest,size);
   ArrayResize(ExtVolume,size);
//---- ����������� ������������� ������� BSIBuffer � ������������ �����
   SetIndexBuffer(0,ExtPosBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� BSIBuffer � ������������ �����
   SetIndexBuffer(1,ExtNegBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� SignBuffer � ������������ �����
   SetIndexBuffer(2,BSIBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(3,ColorBSIBuffer,INDICATOR_COLOR_INDEX);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"BSI( ",InpRangePeriod,", ",InpSlowing,", ",InpAvgPeriod,", ",EnumToString(InpUsingVolumeWeight)," )");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| BSI iteration function                                           | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
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
   if(rates_total<min_rates_total) return(0);
//---- ���������� ������������� ����������
   int first1,first2,bar;
//---- ������������� ���������� � ����� OnCalculate()
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      first1=min_rates_1; // ��������� ����� ��� ������� ���� ����� ������� �����
      first2=min_rates_total; // ��������� ����� ��� ������� ���� ����� ������� �����
      ArrayInitialize(Count,NULL);
      ArrayInitialize(ExtHighest,NULL);
      ArrayInitialize(ExtLowest,NULL);
      ArrayInitialize(ExtVolume,NULL);
     }
   else // ��������� ����� ��� ������� ����� �����
     {
      first1=prev_calculated-1;
      first2=first1;
     }
//---- �������� ���� ������� ����������
   for(bar=first1; bar<rates_total && !IsStopped(); bar++)
     {
      double dmin=1000000.0;
      double dmax=-1000000.0;
      double volmax=NULL;
      for(int kkk=0; kkk<int(InpRangePeriod); kkk++)
        {
         dmin=MathMin(dmin,low[bar-kkk]);
         dmax=MathMax(dmax,high[bar-kkk]);
        }
      ExtLowest[Count[0]]=dmin;
      ExtHighest[Count[0]]=dmax;
      //----
      switch(InpUsingVolumeWeight)
        {
         case ENUM_WITHOUT_VOLUME :
           {
            ExtVolume[Count[0]]=1.0;
            break;
           }
         case ENUM_VOLUME :
           {
            for(int kkk=0; kkk<int(InpRangePeriod); kkk++) volmax=MathMax(volmax,volume[bar-kkk]);
            ExtVolume[Count[0]]=volmax;
            break;
           }
         case ENUM_TICKVOLUME :
           {
            for(int kkk=0; kkk<int(InpRangePeriod); kkk++) volmax=MathMax(volmax,tick_volume[bar-kkk]);
            ExtVolume[Count[0]]=volmax;
           }
        }
      //----
      double sumpos=NULL;
      double sumneg=NULL;
      double sumhigh=NULL;
      double sumpvol=NULL;
      double sumnvol=NULL;
      for(int kkk=0; kkk<int(InpSlowing); kkk++)
        {
         //---
         int barkkk=bar-kkk;
         double vol=1.0;
         switch(InpUsingVolumeWeight)
           {
            case ENUM_WITHOUT_VOLUME :
              {
               break;
              }
            case ENUM_VOLUME :
              {
               if(ExtVolume[Count[kkk]]) vol=volume[barkkk]/ExtVolume[Count[kkk]];
               break;
              }
            case ENUM_TICKVOLUME :
              {
               if(ExtVolume[Count[kkk]]) vol=tick_volume[barkkk]/ExtVolume[Count[kkk]];
              }
           }
         //--- Range position ratio
         double ratio=0;
         //--- Range spread
         double range=ExtHighest[Count[kkk]]-ExtLowest[Count[kkk]];
         range=MathMax(range,_Point);
         //--- Bar Spread
         double sp=(high[barkkk]-low[barkkk]);
         //--- Not DownBar
         if(!(close[barkkk-1]-sp*0.2>close[barkkk]))
           {
            //--- low equal range low
            if(low[barkkk]==ExtLowest[Count[kkk]]) ratio=1;
            else // upper - low / range spread
            ratio=(ExtHighest[Count[kkk]]-low[barkkk])/range;
            sumpos+=(close[barkkk]-low[barkkk])*ratio *vol;
           }
         //--- Not UpBar
         if(!(close[barkkk-1]+sp*0.2<close[barkkk]))
           {
            //--- high equal range high 
            if(high[barkkk]==ExtHighest[Count[kkk]]) ratio=1;
            else // high - lower / range spread
            ratio=(high[barkkk]-ExtLowest[Count[kkk]])/range;
            sumneg+=(high[barkkk]-close[barkkk])*ratio*vol*-1;
           }
         //---
         sumhigh+=range;
        }
      //---
      if(!sumhigh)
        {
         ExtPosBuffer[bar]=NULL;
         ExtNegBuffer[bar]=NULL;
        }
      else
        {
         ExtPosBuffer[bar]=sumpos/sumhigh*100;
         ExtNegBuffer[bar]=sumneg/sumhigh*100;
        }
      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,size);
     }
//---- �������� ���� ������� ����������
   for(bar=first2; bar<rates_total && !IsStopped(); bar++)
     {
      double sumPos=NULL;
      double sumNeg=NULL;
      double sum=NULL;
      for(int kkk=0; kkk<int(InpAvgPeriod); kkk++) sum+=ExtPosBuffer[bar-kkk]+ExtNegBuffer[bar-kkk];
      BSIBuffer[bar]=sum/InpAvgPeriod;
     }
//---- �������� ���� ��������� ���������� BSI
   for(bar=first2; bar<rates_total; bar++)
     {
      int clr=1;
      if(BSIBuffer[bar-1]>BSIBuffer[bar]) clr=0;
      if(BSIBuffer[bar-1]<BSIBuffer[bar]) clr=2;
      ColorBSIBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
