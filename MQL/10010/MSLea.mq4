//+---------------------------------------------------------------------------------------------------+
//|                      MSL EA                                                                      |
//|                      Copyright © 2010, Thomas Quester.                                            |
//|                                        tquester@gmx.de                                            |
//|                                                                                                   |
//|I do custom mql4/mql5 programming at low prices.                                                   |
//|                                                                                                   |
//|                                                                                                   |
//|You may support the author by sending money to one of the following addresses, so I can continue   |
//|publishing my code for free                                                                        |
//|                                                                                                   |
//|  - Send money to PayPal:        tquester@gmx.de                                                   |
//|  - Send money to Moneybrokers:  tquester@gmx.de                                                   |
//|                                                                                                   |
//| If you do not already have a broker, you may consider opening one of the following                |
//|  - Open an account at avafx and drop me a line with your user name. (you and I will receive a     |
//|    free bonus)                                                                                    |
//|                                                                                                   |
//+---------------------------------------------------------------------------------------------------+
#property copyright "Copyright © 2010, Thomas Quester"
#property link      "tquester@gmx.de"

extern int MaxTrades=2;
extern int Level=1;
extern int Distance=4;
#include <ea.mqh>
extern string _____________________________i7="Bildschirmaufteilung";
extern int  StartY=50;
extern int  StartX=00;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----

   SetComment("mslea");
   ObjectCreate("msl",OBJ_HLINE,0,0,0);
   ObjectCreate("msh",OBJ_HLINE,0,0,0);
   ObjectSet("msl",OBJPROP_WIDTH,2);
   ObjectSet("msh",OBJPROP_WIDTH,2);
   ObjectSet("msl",OBJPROP_COLOR,Red);
   ObjectSet("msh",OBJPROP_COLOR,LawnGreen);
   
   makelabel("data", StartX,StartY,"...",White);   
   makelabel("param",StartX+400,StartY,"...",White);
   makelabel("trade",StartX+400,StartY+15,"...",White);
   CorrectParameters();
   SetText("param","Lots="+DoubleToStr(GetLots(),3)+" StopLoss="+GetStopLoss()+" MaxTrades="+MaxTrades);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
     double msl = -1;
     double msh = -1;
     int imsl=0;
     int imsh=0;
     int i;
     for (i=0;i<=Bars;i++)       // backward search
     {
         if (High[i+2] < High[i+1] && High[i+1] > High[i])    // h2 is bigger than h1 and h3 -> it is a local High
         {
             if (msh == -1 || (High[i+1] > msh && imsh < Level))
             {
                  msh = High[i+1];
                  Print("msh = "+High[i+1]+" level="+imsh);
                  imsh++;
             }
         }
         if (Low[i+2] > Low[i+1] && Low[i+1] < Low[i])    // l2 is lower than l1 and l3  -> it is a local Low
         {
             if (msl == -1 || (Low[i+1] < msl && imsl < Level))
             {
                  Print("msl = "+Low[i+1]+" level="+imsl);
                  msl = Low[i+1];
                  imsl++;
             }
         }
         if (imsh >= Level && imsl >= Level) // we found local high and local low
             break;
      
     }
     if (msh != -1 && msl != -1)
     {
        // if there is local high and local Low
        // set the lines
       msl -= Point*Distance;
       msh += Point*Distance;
        ObjectSet("msl",OBJPROP_PRICE1,msl);
        ObjectSet("msh",OBJPROP_PRICE1,msh);
     }
//----
    SetText("data","msl="+msl+" msh="+msh);
    FindOrders(true);
    SetText("trade","Trade="+GetNumTickets()+" Secure="+GetNumSecureTickets()+" profit="+DoubleToStr(GetTotalProfit(),2));
    if (GetNumTickets() == 0 ||                                                        // we trade if either no ticket is there
        (GetNumTickets() < MaxTrades && GetNumTickets() == GetNumSecureTickets()))     // or if number of tickets < allowed and
                                                                                       // all of them are secure
    {
       if (Ask > msh) {Print (Ask +" < "+msh + " sell"); Buy(GetLots());  }
       if (Bid < msl) {Print (Bid +" < "+msl + " sell"); Sell(GetLots()); }
    }
   
//----
   return(0);
  }
//+------------------------------------------------------------------+