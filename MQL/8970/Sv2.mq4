//+------------------------------------------------------------------+
//|                                                  SilverTrend.mq4 |
//|                              SilverTrend rewritten by CrazyChart |
//|                                                  http://viac.ru/ |
//|                  modified by Kalenzo bartlomiej.gorski@gmail.com |
//+------------------------------------------------------------------+
#property copyright "SilverTrend  rewritten by CrazyChart"
#property link      "http://viac.ru/ "

#property indicator_separate_window
#property indicator_buffers 2
#property indicator_color1 RoyalBlue //Aqua
#property indicator_color2 DeepPink  //Red 

extern int       SSP=7;
extern double    Kmax=50.6; //24 21.6 21.6 
extern bool      Alerts=False;
extern int       CountBars=300;

//---- buffers
bool     SAO=false;           // Used to stop constant alerts
bool     BAO=false;            // Used to stop constant alerts
double ExtMapBuffer1[];
double ExtMapBuffer2[];
double ExtHBuffer1[];
double ExtHBuffer2[];
double SubeBaja[];





//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
//---- indicators
   IndicatorBuffers(4);
   SetIndexStyle(0,DRAW_HISTOGRAM,0,4);
   SetIndexBuffer(0,ExtHBuffer1);
   SetIndexStyle(1,DRAW_HISTOGRAM,0,4);
   SetIndexBuffer(1,ExtHBuffer2);
   
   SetIndexBuffer(2,ExtMapBuffer1);
   SetIndexBuffer(3,ExtMapBuffer2);
   
   
   SetIndexBuffer(4,SubeBaja);

   IndicatorShortName("SilverTrend");
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custor indicator deinitialization function                       |
//+------------------------------------------------------------------+
int deinit()
  {

   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int start()
  {
 
  if (CountBars>=Bars) CountBars=Bars;
  
   SetIndexDrawBegin(0,Bars-CountBars+SSP);
   SetIndexDrawBegin(1,Bars-CountBars+SSP);
   
  int i, counted_bars=IndicatorCounted();
  double SsMax, SsMin, smin, smax, val1, val2; 
  
  if(Bars<=SSP+1) return(0);

if(counted_bars<SSP+1)
   {
      for(i=1;i<=SSP;i++) ExtMapBuffer1[CountBars-i]=0.0;
      for(i=1;i<=SSP;i++) ExtMapBuffer2[CountBars-i]=0.0;
   }

for(i=CountBars-SSP;i>=0;i--) { 


  SsMax = High[iHighest(NULL,0,MODE_HIGH,SSP,i-SSP+1)]; 
  SsMin = Low[iLowest(NULL,0,MODE_LOW,SSP,i-SSP+1)]; 
  
   smax = SsMax-(SsMax-SsMin)*Kmax/100;
       
   ExtMapBuffer1[i-SSP+6]=smax; 
   ExtMapBuffer2[i-SSP-1]=smax; 
   val1 = ExtMapBuffer1[0]; 
   val2 = ExtMapBuffer2[0];

}
   for(int b=CountBars-SSP;b>=0;b--)
   {
      if(ExtMapBuffer1[b]>ExtMapBuffer2[b])
      {
         ExtHBuffer1[b]=1;
         ExtHBuffer2[b]=0;
      }
      else
      {
         ExtHBuffer1[b]=0;
         ExtHBuffer2[b]=1;
      }
      
      if (val1 > val2){
      if(Alerts==true && BAO==false){
            PlaySound("alert.wav");
            Alert(Symbol() + " " + "(ST) UP signal " + "M" + Period());
            BAO=true;
            SAO=false;
         }     
      }

      if (val1 < val2){
         if(Alerts==true && SAO==false){
            PlaySound("alert.wav");
            Alert(Symbol() + " " + "(ST) DN signal " + "M" + Period());
            BAO=false;
            SAO=true;
          }
      }
  }  
  return(0);
}