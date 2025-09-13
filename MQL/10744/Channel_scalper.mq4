#property indicator_chart_window
#property indicator_buffers 2
#property indicator_color1 Orange
#property indicator_color2 Yellow

#property indicator_width1 2
#property indicator_width2 4

double TrendU[];
double TrendUA[];
double TrendD[];
double TrendDA[];
double Direction[];
double Up[];
double Dn[];

int init()
{
 IndicatorBuffers(7);
 SetIndexBuffer(0, TrendU);
 SetIndexBuffer(1, TrendD);
 SetIndexBuffer(3, Direction);
 SetIndexBuffer(4, Up);
 SetIndexBuffer(5, Dn);
      
 SetIndexLabel(0,"");
 SetIndexLabel(1,"");
 SetIndexLabel(2,"");
 SetIndexLabel(3,"");
 IndicatorShortName("");
}

int deinit()
{
 return(0);
}

int start()
{
 int counted_bars = IndicatorCounted();
 int limit,i;

 if(counted_bars < 0) return(-1);
 if(counted_bars > 0) counted_bars--;
 limit = Bars-counted_bars;
      
 for(i = limit; i >= 0; i--)
  {
   double atr    = iATR(NULL,0,11,i);
   double cprice = iMA(NULL,0,1,0,MODE_SMA,PRICE_CLOSE,i);
   double mprice = (High[i]+Low[i])/2; 

   Up[i] = mprice+(1.28*atr);
   Dn[i] = mprice-(1.28*atr);
         
   Direction[i] = Direction[i+1]; 

   if(cprice > Up[i+1])
    {
     Direction[i] = 1;
    }
   if(cprice < Dn[i+1])
    {
     Direction[i] = -1;
    }
     
   if(Direction[i] > 0)
    {
     Dn[i] = MathMax(Dn[i],Dn[i+1]);
     TrendU[i] = Dn[i];

     GlobalVariableSet("GSignalBuy",1);
     GlobalVariableSet("GSignalSell",0);
    } 
   if(Direction[i] < 0)
    {
     Up[i] = MathMin(Up[i],Up[i+1]);
     TrendD[i] = Up[i];

     GlobalVariableSet("GSignalBuy",0);
     GlobalVariableSet("GSignalSell",1);
    }
  }
   return(0);
}
               

