//+------------------------------------------------------------------+
//|                                                          KDJ.mq4 |
//|                                                        senlin ge |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "senlin ge 20080219"
#property link      "http://www.metaquotes.net"
#property indicator_separate_window
#property indicator_minimum 0
#property indicator_maximum 100
#property indicator_buffers 8

#property indicator_color1 LightSlateGray
#property indicator_color2 0x00ff00
#property indicator_color3 Red
#property indicator_color4 Yellow
#property indicator_color5 0Xffccff
#property indicator_color6 MediumSlateBlue

//---- input parameters
extern int       M1=3;
extern int       M2=6;
extern int       KdjPeriod=30;
//---- buffers
double RsvBuffer[],RSV[],K[],D[],J[],KDC[];
double MaxHigh=0,MinLow=0;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
    //---- name for DataWindow and indicator subwindow label
    string short_name;
   short_name="KDJ("+KdjPeriod+","+M1+","+M2+") "+"Author:Senlin Ge";
   IndicatorShortName(short_name);
   SetIndexLabel(0,NULL);
   SetIndexLabel(1,"RSV:");
   SetIndexLabel(2,"K");
   SetIndexLabel(3,"D:");
   SetIndexLabel(4,"J");
   SetIndexLabel(5,"KDC");
//----
//---- indicators
//---- drawing settings
   IndicatorDigits(Digits-2);       //set小数精度两位
   //SetIndexStyle(0,DRAW_NONE);
   //SetIndexLabel(0,NULL);
   SetIndexShift(0,100);
   SetIndexStyle(0,DRAW_LINE,2,0);
   SetIndexBuffer(0,RsvBuffer);
   //----
   SetIndexStyle(1,DRAW_LINE);
   SetIndexBuffer(1,RSV);
   SetIndexStyle(2,DRAW_LINE);
   SetIndexBuffer(2,K);
   SetIndexStyle(3,DRAW_LINE);
   SetIndexBuffer(3,D);
   SetIndexStyle(4,DRAW_LINE);
   SetIndexBuffer(4,J);
   SetIndexStyle(5,DRAW_HISTOGRAM);
   SetIndexBuffer(5,KDC);
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int start()
  {
   int    counted_bars=IndicatorCounted();
//----
   int i;
   if(Bars<=KdjPeriod) return(0);
//----
   i=Bars-counted_bars-1;
   while(i>=0)
     {
      MaxHigh=High[iHighest(NULL,0,MODE_HIGH,KdjPeriod,i)];
      MinLow=Low[iLowest(NULL,0,MODE_LOW,KdjPeriod,i)];
      RSV[i]=(Close[i]-MinLow)/(MaxHigh-MinLow)*100;
      if(i/2)
      RsvBuffer[i]=50.0;
      i--;
     } 
      Ksma(RSV,M1,2);                     //get K value
      Dsma(K,M2,3);                       //get D value
//---- //
  if(counted_bars>0) counted_bars--;
  for(i=Bars-counted_bars-1;i>=0;i--)
   {
     RsvBuffer[i]=50.0;
      J[i]=3*K[i]-2*D[i];
      if(J[i]<0) J[i]=0;
      if(J[i]>100) J[i]=100;
	   KDC[i]=K[i]-D[i];
      }   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Simple Moving Average                                            |
//+------------------------------------------------------------------+
double Dsma(double ArrPara[],int MA_Period,int flag)
  {
   double sum=0;
   int ExtCountedBars=IndicatorCounted();
   int    i,pos=Bars-IndicatorCounted()-1;
//---- initial accumulation
   if(pos<MA_Period) pos=MA_Period;
   for(i=1;i<MA_Period;i++,pos--)
   sum+=K[pos];
//---- main calculation loop
   while(pos>=0)
     {
      sum+=K[pos];                   //加上最新的K值
      switch(flag)
	   {
	   case 3: D[pos]=sum/MA_Period;break;
	   } 
 	   sum-=K[pos+MA_Period-1];
 	   pos--;
     }
   }
//+------------------------------------------------------------------+
//| Simple Moving Average                                            |
//+------------------------------------------------------------------+
double Ksma(double ArrPara[],int MA_Period,int flag)
  {
   double sum=0;
   int ExtCountedBars=IndicatorCounted();
   int    i,pos=Bars-IndicatorCounted()-1;
   //---- initial accumulation
   if(pos<MA_Period) pos=MA_Period;
   for(i=1;i<MA_Period;i++,pos--)
       sum+=RSV[pos];
   //---- main calculation loop
   while(pos>=0)
     {
     sum+=RSV[pos];                    //加上最新的RSV[]的值
	  switch(flag)
	   {
	   case 2: K[pos]=sum/MA_Period;break;
	   } 
      sum-=RSV[pos+MA_Period-1];
 	   pos--;
     }
  }
//+------------------------------------------------------------------+

