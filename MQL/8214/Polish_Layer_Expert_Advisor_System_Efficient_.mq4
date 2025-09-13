#property copyright "Copyright © 2008, PUNCHER from POLAND"
#property link      "bemowo@tlen.pl"
// 5 or 15 MIN timeframe only
extern int RSIPeriod=14;
extern int Short_price_MA_periods=9;
extern int Long_price_MA_periods=45;
extern int Short_RSI_MA_periods=9;
extern int Long_RSI_MA_periods=45;
extern int Slippage=3;
extern int pisto=0; //Stochastic Indicator period
extern int pistok=5; // Stochastic Period(amount of bars) for the calculation of %K line
extern int pistod=3; //Stochastic Averaging period for the calculation of %D line
extern int istslow=3; //Stochastic Value of slowdown
extern int pimacd=0; //Indicator period
extern int fastpimacd=12; //Averaging period for calculation of a quick MA
extern int slowpimacd=26; //Averaging period for calculation of a slow MA
extern int signalpimacd=9; //Averaging period for calculation of a signal line
extern int piwpr=0; //WPR Indicator period
extern int piwprbar=14; //WPR Period (amount of bars) for indicator calculation
extern int pidem=0; //Indicator period
extern int pidemu=14; //Period of averaging for indicator calculation
double Lots=1; // that mean 0.1 value if you want 1 lot type 10
double  tp=17; // take profit
double  sl=7777; // stop loss
int start()
{
   int arraysize=200;
   double RSI[];
   double RSI_SMA[];
   ArrayResize(RSI,arraysize);
   ArrayResize(RSI_SMA,arraysize);
   ArraySetAsSeries(RSI,true);

   for(int i3=arraysize-1;i3>=0;i3--)
      RSI[i3]=iRSI(NULL,0,RSIPeriod,PRICE_CLOSE,i3);
   for(i3=arraysize-1;i3>=0;i3--)
      RSI_SMA[i3]=iMAOnArray(RSI,0,Short_RSI_MA_periods,0,MODE_SMA,i3);
   double RSI9 =RSI_SMA[1];

   for(i3=arraysize-1;i3>=0;i3--)
      RSI[i3]=iRSI(NULL,0,RSIPeriod,PRICE_CLOSE,i3);
   for(i3=arraysize-1;i3>=0;i3--)
      RSI_SMA[i3]=iMAOnArray(RSI,0,Long_RSI_MA_periods,0,MODE_SMA,i3);
   double RSI45 =RSI_SMA[1];
   double Price45=iMA(NULL,0, Long_price_MA_periods ,0,MODE_LWMA,PRICE_CLOSE,1);
   double Price9 =iMA(NULL,0, Short_price_MA_periods,0,MODE_SMA,PRICE_CLOSE,1);

   bool Long=false;
   bool Short=false;
   bool Sideways=false;
   if(Price9>Price45 && RSI9>RSI45) Long=true;
   if(Price9<Price45 && RSI9<RSI45) Short=true;
   if(Price9>Price45 && RSI9<RSI45) Sideways=true;
   if(Price9<Price45 && RSI9>RSI45) Sideways=true;
   if(Long==true  && OrdersTotal()==0)
     {
     if(iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_MAIN,1)<19&&iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_MAIN,0)>=19)
         {
         if(iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_MAIN,1)<iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_SIGNAL,1)&&iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_MAIN,0)>=iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_SIGNAL,0))
            {
            if(iDeMarker(NULL,pidem,pidemu,1)<0.35&&iDeMarker(NULL,pidem,pidemu,0)>=0.35)                
                  {
                  if (iWPR(NULL,piwpr,piwprbar,1)<-81&&iWPR(NULL,piwpr,piwprbar,0)>=-81)
                     {OrderSend(Symbol(),OP_BUY,Lots/10,Ask,Slippage,Ask-sl*Point,Ask+tp*Point,"Piotrek Buy");}
                  }
               
            }
         }
     }
   if(Short==true && OrdersTotal()==0)
     {
     if(iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_MAIN,1)>81&&iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_MAIN,0)<=81)
         {
         if(iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_MAIN,1)>iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_SIGNAL,1)&&iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_MAIN,0)<=iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_SIGNAL,0))
             {
             if(iDeMarker(NULL,pidem,pidemu,1)>0.63&&iDeMarker(NULL,pidem,pidemu,0)<=0.63)   
                   {
                   if (iWPR(NULL,piwpr,piwprbar,1)>-19&&iWPR(NULL,piwpr,piwprbar,0)<=-19)
                     {OrderSend(Symbol(),OP_SELL,Lots/10,Bid,Slippage,Bid+sl*Point,Bid-tp*Point,"Piotrek Sell");}
                   }
                
             }
         }
     }
}

