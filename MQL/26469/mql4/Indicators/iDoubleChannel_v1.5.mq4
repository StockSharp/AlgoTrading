//====================================================================================================================================================//
#property copyright "Copyright 2017-2019, Nikolaos Pantzos"
#property link      "https://www.mql5.com/en/users/pannik"
#property version   "1.5"
#property strict
//====================================================================================================================================================//
#property indicator_chart_window
#property indicator_buffers 5
//====================================================================================================================================================//
extern int   ChannelPeriod  = 14;
extern int   ChannelWidth   = 2;
extern int   IndicatorShift = 0;
extern bool  ShowChannels   = true;
extern bool  ShowArrows     = true;
extern color LineColor1     = clrTurquoise;
extern color LineColor2     = clrOrangeRed;
extern color LineColor3     = clrGold;
extern color ArrowColor1    = clrBlue;
extern color ArrowColor2    = clrCrimson;
//====================================================================================================================================================//
double ExtMapBuffer0[];
double ExtMapBuffer1[];
double ExtMapBuffer2[];
double ExtMapBuffer3[];
double ExtMapBuffer4[];
int ExtCountedBars=0;
int DrawBigin;
//====================================================================================================================================================//
int OnInit(void)
  {
//----
   Comment(WindowExpertName()+"\n Period: "+IntegerToString(ChannelPeriod));
//----
   IndicatorBuffers(5);
   DrawBigin=ChannelPeriod-1;
   IndicatorDigits(Digits);
   IndicatorShortName("iDoubleChannel("+IntegerToString(ChannelPeriod)+")");
//---- drawing settings
   SetIndexShift(0,IndicatorShift);
   SetIndexShift(1,IndicatorShift);
   SetIndexShift(2,IndicatorShift);
   SetIndexShift(3,IndicatorShift);
   SetIndexShift(4,IndicatorShift);
//---
   SetIndexDrawBegin(0,DrawBigin);
   SetIndexDrawBegin(1,DrawBigin);
   SetIndexDrawBegin(2,DrawBigin);
   SetIndexDrawBegin(3,DrawBigin);
   SetIndexDrawBegin(4,DrawBigin);
//---Show lines
   if(ShowChannels==true)
     {
      SetIndexStyle(0,DRAW_LINE,STYLE_SOLID,ChannelWidth,LineColor1);
      SetIndexStyle(1,DRAW_LINE,STYLE_SOLID,ChannelWidth,LineColor2);
      SetIndexStyle(2,DRAW_LINE,STYLE_DASH,EMPTY,LineColor3);
     }
//---Hide lines
   if(ShowChannels==false)
     {
      SetIndexStyle(0,EMPTY_VALUE);
      SetIndexStyle(1,EMPTY_VALUE);
      SetIndexStyle(2,EMPTY_VALUE);
     }
//---
   SetIndexStyle(3,DRAW_ARROW,STYLE_SOLID,ChannelWidth,ArrowColor1);
   SetIndexArrow(3,SYMBOL_ARROWUP);
   SetIndexArrow(3,233);
//---
   SetIndexStyle(4,DRAW_ARROW,STYLE_SOLID,ChannelWidth,ArrowColor2);
   SetIndexArrow(4,SYMBOL_ARROWDOWN);
   SetIndexArrow(4,234);
//---
   SetIndexBuffer(0,ExtMapBuffer0);
   SetIndexBuffer(1,ExtMapBuffer1);
   SetIndexBuffer(2,ExtMapBuffer2);
   SetIndexBuffer(3,ExtMapBuffer3);
   SetIndexBuffer(4,ExtMapBuffer4);
//---
   SetIndexLabel(0,"DC: Upper Level");
   SetIndexLabel(1,"DC: Lower Level");
   SetIndexLabel(2,"DC: Moving Average");
   SetIndexLabel(3,"DC: Buy Arrow");
   SetIndexLabel(4,"DC: Sell Arrow");
//---- initialization done
   return(INIT_SUCCEEDED);
  }
//====================================================================================================================================================//
void OnDeinit(const int reason)
  {
//--------------------------------------------------------------------------------
   Comment("");
//--------------------------------------------------------------------------------
  }
//====================================================================================================================================================//
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
   if(Bars<=ChannelPeriod) return(0);
   ExtCountedBars=IndicatorCounted();
//---- check for possible errors
   if(ExtCountedBars<0) return(-1);
//---- last counted bar will be recounted
   if(ExtCountedBars>0) ExtCountedBars--;
//----
   MainFunction();
//-----------------------------------------------------------------------------------
   return(rates_total);
  }
//====================================================================================================================================================//
void MainFunction()
  {
   double sum0=0;
   double sum1=0;
   double sum2=0;
   double sum3=0;
   double sum4=0;
   int i;
   int pos=Bars-ExtCountedBars-1;
   double DistanceArrow=(iMA(NULL,0,ChannelPeriod,0,MODE_SMA,PRICE_HIGH,1)-iMA(NULL,0,ChannelPeriod,0,MODE_SMA,PRICE_LOW,1))*2;
//---- initial accumulation
   if(pos<ChannelPeriod) pos=ChannelPeriod;
//---
   for(i=1; i<ChannelPeriod; i++,pos--)
     {
      //---
      sum4+=Close[pos];
      //---
      sum0+=High[pos]+(High[pos]-Close[pos]);
      sum1+=Low[pos]-(Open[pos]-Low[pos]);
      sum2+=High[pos]+(Close[pos]-Low[pos]);
      sum3+=Low[pos]-(High[pos]-Open[pos]);
     }
//---- main calculation loop
   while(pos>=0)
     {
      //---
      sum4+=Close[pos];
      //---
      sum0+=High[pos]+(High[pos]-Close[pos]);
      sum1+=Low[pos]-(Open[pos]-Low[pos]);
      sum2+=High[pos]+(Close[pos]-Low[pos]);
      sum3+=Low[pos]-(High[pos]-Open[pos]);
      //---Show lines
      ExtMapBuffer2[pos]=sum4/ChannelPeriod;
      ExtMapBuffer0[pos]= ExtMapBuffer2[pos]+(((sum0/ChannelPeriod)-(sum2/ChannelPeriod))/1);
      ExtMapBuffer1[pos]= ExtMapBuffer2[pos]+(((sum1/ChannelPeriod)-(sum3/ChannelPeriod))/1);
      //---Show arrows
      if(ShowArrows==true)
        {
         //---Buy arrows
         if(
            (ExtMapBuffer1[pos+1]>ExtMapBuffer0[pos+1]) && (ExtMapBuffer1[pos+2]<ExtMapBuffer0[pos+2])
            && (ExtMapBuffer0[pos+1]>ExtMapBuffer2[pos+1])
            && (ExtMapBuffer0[pos+1]>ExtMapBuffer0[pos+2])
            && (ExtMapBuffer1[pos+1]>ExtMapBuffer1[pos+2])
            && (ExtMapBuffer1[pos+1]-ExtMapBuffer0[pos+1]>ExtMapBuffer0[pos+1]-ExtMapBuffer2[pos+1])
            && (Close[pos+1]>ExtMapBuffer1[pos+1])
            && (Open[pos+1]<Close[pos+1])
            )
           {
            ExtMapBuffer3[pos]=Low[pos]-DistanceArrow;
            ExtMapBuffer4[pos]=EMPTY_VALUE;
           }
         //---Sell arrows
         if(
            (ExtMapBuffer1[pos+1]<ExtMapBuffer0[pos+1]) && (ExtMapBuffer1[pos+2]>ExtMapBuffer0[pos+2])
            && (ExtMapBuffer0[pos+1]<ExtMapBuffer2[pos+1])
            && (ExtMapBuffer0[pos+1]<ExtMapBuffer0[pos+2])
            && (ExtMapBuffer1[pos+1]<ExtMapBuffer1[pos+2])
            && (ExtMapBuffer0[pos+1]-ExtMapBuffer1[pos+1]>ExtMapBuffer2[pos+1]-ExtMapBuffer0[pos+1])
            && (Close[pos+1]<ExtMapBuffer1[pos+1])
            && (Open[pos+1]>Close[pos+1])
            )
           {
            ExtMapBuffer4[pos]=High[pos]+DistanceArrow;
            ExtMapBuffer3[pos]=EMPTY_VALUE;
           }
        }
      //---
      sum0-=High[pos+ChannelPeriod-1]+(High[pos+ChannelPeriod-1]-Close[pos+ChannelPeriod-1]);
      sum1-=Low[pos+ChannelPeriod-1]-(Open[pos+ChannelPeriod-1]-Low[pos+ChannelPeriod-1]);
      sum2-=High[pos+ChannelPeriod-1]+(Close[pos+ChannelPeriod-1]-Low[pos+ChannelPeriod-1]);
      sum3-=Low[pos+ChannelPeriod-1]-(High[pos+ChannelPeriod-1]-Open[pos+ChannelPeriod-1]);
      //---
      sum4-=Close[pos+ChannelPeriod-1];
      //---
      pos--;
     }
//---
//---- zero initial bars
   if(ExtCountedBars<1)
     {
      for(i=1;i<ChannelPeriod;i++)
        {
         ExtMapBuffer0[Bars-i]=0;
         ExtMapBuffer1[Bars-i]=0;
         ExtMapBuffer2[Bars-i]=0;
         ExtMapBuffer3[Bars-i]=0;
         ExtMapBuffer4[Bars-i]=0;
        }
     }
//---- redraw chart
   WindowRedraw();
  }
//====================================================================================================================================================//
