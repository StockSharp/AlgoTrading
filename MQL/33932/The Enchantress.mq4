
//+------------------------------------------------------------------+
//|                                                      ProjectName |
//|                                      Copyright 2018, CompanyName |
//|                                       http://www.companyname.net |
//+------------------------------------------------------------------+
#define  VERSION "1.0"
#property copyright   "Copyright В© 2021, Rodolphe Ahmad"
#property link        "https://mql5.com/en/users/rodoboss"
#property version     VERSION
#property description "The Enchantress (v"+VERSION+")"
#property strict


#define EMPTYORDER 0
#define EMPTYPRICE 0
#define UNDEFINEDTYPE -100
#define UNDEFINEDSTATUS -100
#define STATUS_OPENED -102
#define STATUS_CLOSED 103
#define ARRAYLENGTH 1000000
#define ZERO 0
#define UNREAL -1000000


#import "stdlib.ex4"
string ErrorDescription(int e);
#import












int order_id=0;
//////int cnt_hours = 0;
int thetext = 0;
string abc = "-----------"; // ---------------------
double Lots=0.01;  //  --------------------- Lot Size
bool   RiskMM=true; // ---------------------Risk Management
double RiskPercent=15;// ------------------   Risk Percentage
string lastprofcomment = "";
double  StopLoss=60;  // Stop Loss
double  VirtualStopLoss=55;  // Stop Loss
double  TakeProfit=19;  // Take Profit
double  VirtualTakeProfit=25;  // Take Profit
int exSlippage;
datetime candletime;
double PipValue                           = 1;
double                        PipPoints = 0.0;

string marketpattern="";
string candletype [ARRAYLENGTH];
string virualpattern[ARRAYLENGTH] = {""};
int virtualorderbuy[ARRAYLENGTH] = {EMPTYORDER};
double virtualorderbuyatprice[ARRAYLENGTH] = {EMPTYPRICE};
double virtualorderbuysl[ARRAYLENGTH] = {ZERO};
double virtualorderbuytp[ARRAYLENGTH] = {ZERO};
int virtualorderbuystatus[ARRAYLENGTH] = {UNDEFINEDSTATUS};
int virtualordersell[ARRAYLENGTH] = {EMPTYORDER};
double virtualordersellatprice[ARRAYLENGTH] = {EMPTYPRICE};
double virtualordersellsl[ARRAYLENGTH] = {ZERO};
double virtualorderselltp[ARRAYLENGTH] = {ZERO};
int virtualordersellstatus[ARRAYLENGTH] = {UNDEFINEDSTATUS};

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/////////extern int minhourtostarttheea = 24; // Collect (X) Candles to Start
int candletyperesultsbullish[ARRAYLENGTH] = {ZERO};
int candletyperesultsbearish[ARRAYLENGTH] = {ZERO};





//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsBarClosed(int timeframe,bool reset)
  {
   static datetime lastbartime;
   if(timeframe==-1)
     {
      if(reset)
         lastbartime=0;
      else
         lastbartime=iTime(NULL,timeframe,0);
      return(true);
     }
   if(iTime(NULL,timeframe,0)==lastbartime) // wait for new bar
      return(false);
   if(reset)
      lastbartime=iTime(NULL,timeframe,0);
   return(true);
  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {


   if(Digits == 3 || Digits == 5)
      PipValue = 10;
   exSlippage=(Digits==5 || Digits==3)?20:2;   //  Maximum Slippage
   if(Digits < 4)
     {
      PipPoints = 0.01;
     }
   else
     {
      PipPoints = 0.0001;
     }

   for(int i=0; i<ARRAYLENGTH; i++)
     {
      candletype[i] = (string)i;

      if(StringLen(candletype[i]) == 1)
        {
         candletype[i]= "000000"+candletype[i];
        }
      else
         if(StringLen(candletype[i]) == 2)
           {
            candletype[i]= "00000"+candletype[i];
           }

         else
            if(StringLen(candletype[i]) == 3)
              {
               candletype[i]= "0000"+candletype[i];
              }
            else
               if(StringLen(candletype[i]) == 4)
                 {
                  candletype[i]= "000"+candletype[i];
                 }
               else
                  if(StringLen(candletype[i]) == 5)
                    {
                     candletype[i]= "00"+candletype[i];
                    }
                  else
                     if(StringLen(candletype[i]) == 6)
                       {
                        candletype[i]= "0"+candletype[i];
                       }


     }









   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double PipsToDecimal(double Pips)
  {
   double ThePoint=SymbolInfoDouble(Symbol(),SYMBOL_POINT);
   if(ThePoint==0.0001 || ThePoint==0.00001)
     {
      return Pips * 0.0001;
     }
   else
      if(ThePoint==0.01 || ThePoint==0.001)
        {
         return Pips * 0.01;
        }
      else
        {
         return 0;
        }
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {


      if(StringLen(marketpattern)>=7)
        {
         tryclosevirutalorders();
        }


   if(IsBarClosed(0,true))
     {


      

      if((DayOfWeek()!=FRIDAY))
        {

         if(StringLen(marketpattern)>=7)
           {
            TakeDecisions();
           }
         ManagePatterns();
        }
      else
        {
        // // // //  marketpattern  = "";
        }
     }
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void tryclosevirutalorders()
  {


   for(int i=0; i<ARRAYLENGTH; i++)
     {


      if(Bid > virtualorderbuytp[i] && virtualorderbuystatus[i]==STATUS_OPENED)
        {

         //  Print("here 3");
         virtualorderbuystatus[i] =STATUS_CLOSED;
         string _searchforpattern = virualpattern[i];

         for(int p=0; p<ARRAYLENGTH; p++)
           {

            if((int)candletype[p] ==(int) _searchforpattern)
              {

               candletyperesultsbullish[p] =    candletyperesultsbullish[p]+1;

               if(IsTesting() == true)
                 {
                  Print("Virtual Buy #"+(string)+i+" Closed with profit");
                 }

              }
           }

        }

      if(Ask < virtualorderbuysl[i]&& virtualorderbuystatus[i]==STATUS_OPENED)
        {
         virtualorderbuystatus[i] =STATUS_CLOSED;
         string _searchforpattern = virualpattern[i];
         for(int p=0; p<ARRAYLENGTH; p++)
           {
            if((int)candletype[p]  ==(int) _searchforpattern)
              {
               candletyperesultsbullish[p] =    candletyperesultsbullish[p]-3;
               if(IsTesting() == true)
                 {
                  Print("Virtual Buy#"+(string)+i+" Closed with non profit");
                 }

              }
           }

        }


     }





   for(int i=0; i<ARRAYLENGTH; i++)
     {



      if(Ask < virtualorderselltp[i]&& virtualordersellstatus[i]==STATUS_OPENED)
        {
         virtualordersellstatus[i] =STATUS_CLOSED;
         string _searchforpattern = virualpattern[i];
         for(int p=0; p<ARRAYLENGTH; p++)
           {
            if((int)candletype[p] == (int) _searchforpattern)
              {
               candletyperesultsbearish[p] =    candletyperesultsbearish[p]+1;
               if(IsTesting() == true)
                 {
                  Print("Virtual Sell #"+(string)+i+" Closed with profit");
                 }

              }
           }

        }

      if(Bid> virtualordersellsl[i]&& virtualordersellstatus[i]==STATUS_OPENED)
        {
         virtualordersellstatus[i] =STATUS_CLOSED;
         string _searchforpattern = virualpattern[i];
         for(int p=0; p<ARRAYLENGTH; p++)
           {
            if((int)candletype[p] == (int) _searchforpattern)
              {
               candletyperesultsbearish[p] =    candletyperesultsbearish[p]-3;
               if(IsTesting() == true)
                 {
                  Print("Virtual sell#"+(string)+i+" Closed with non profit");
                 }
              }
           }

        }


     }



  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ManagePatterns()
  {



   if(Open[1]>=Close[1])
     {
      double _100 = 100;
      double a = 100-((Low[1] *100) / (High[1]));
      a = MathAbs(a);

      if(a>0 && a<=0.04)
        {
         marketpattern = marketpattern + "0";
        }

      // Is Bearish Candle
      if(a >0.04 && a <=0.15)
        {
         marketpattern = marketpattern + "1";
        }

      if(a >0.15 && a <=0.25)
        {
         marketpattern = marketpattern + "2";
        }

      if(a >0.25 && a <=0.40)
        {
         marketpattern = marketpattern + "3";
        }

      if(a>0.40)
        {
         marketpattern = marketpattern + "4";
        }


     }

   if(Open[1]<Close[1])
     {
      double _100 = 100;
      double a = 100-((Low[1] *100) / (High[1]));
      a = MathAbs(a);

      if(a>0 && a<=0.04)
        {
         marketpattern = marketpattern + "5";
        }

      // Is Bullish Candle
      if(a >0.04 && a <=0.15)
        {
         marketpattern = marketpattern + "6";
        }

      if(a >0.15 && a <=0.25)
        {
         marketpattern = marketpattern + "7";
        }

      if(a >0.25 && a <=0.40)
        {
         marketpattern = marketpattern + "8";
        }

      if(a>0.40)
        {
         marketpattern = marketpattern + "9";
        }

     }


   if(StringLen(marketpattern)>=7)
     {

      string last7pattern =StringSubstr(marketpattern,StringLen(marketpattern)-7,7);

      int cpposition = 0;

      for(int i=0; i<ARRAYLENGTH; i++)
        {

         if(virtualorderbuy[i]==EMPTYORDER)
           {

            break;

           }
         cpposition++;

        }

      order_id++;

      virualpattern[cpposition] = last7pattern;

      virtualorderbuy[cpposition] = order_id;
      virtualorderbuyatprice[cpposition] = Bid;
      virtualorderbuysl[cpposition] =(VirtualStopLoss>0)?MarketInfo(Symbol(),MODE_BID)-PipsToDecimal(VirtualStopLoss):0;
      virtualorderbuytp[cpposition] =(VirtualTakeProfit>0)?MarketInfo(Symbol(),MODE_ASK)+PipsToDecimal(VirtualTakeProfit):0;
      virtualorderbuystatus[cpposition] = STATUS_OPENED;
      if(IsTesting() == true)
        {
         Print("Virtual Buy#"+(string)cpposition + " Placed.");
        }

      virtualordersell[cpposition] = order_id;
      virtualordersellatprice[cpposition] = Ask;
      virtualordersellsl[cpposition] =(VirtualStopLoss>0)?MarketInfo(Symbol(),MODE_ASK)+PipsToDecimal(VirtualStopLoss):0;
      virtualorderselltp[cpposition] =(VirtualTakeProfit>0)?MarketInfo(Symbol(),MODE_BID)-PipsToDecimal(VirtualTakeProfit):0;
      virtualordersellstatus[cpposition] = STATUS_OPENED;
      if(IsTesting() == true)
        {
         Print("Virtual Sell#"+(string)cpposition + " Placed.");
        }



     }
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TakeDecisions()
  {


   int bestbuypattern  = UNREAL;
   int cpbulli = 0;
   int bestsellpattern = UNREAL;
   int cpbeari = 0;

   int bestbuypattern2= UNREAL;
   int cpbulli2=0;
   int bestsellpattern2= UNREAL;
   int cpbeari2=0;
   int bestbuypattern3= UNREAL;
   int cpbulli3=0;
   int bestsellpattern3= UNREAL;
   int cpbeari3 = 0;
   int bestbuypattern4=UNREAL;
   int cpbulli4=0;
   int bestsellpattern4= UNREAL;
   int cpbeari4 = 0;
   int bestbuypattern5= UNREAL;
   int cpbulli5=0;
   int bestsellpattern5= UNREAL;
   int cpbeari5 = 0;
   int bestbuypattern6= UNREAL;
   int cpbulli6=0;
   int bestsellpattern6= UNREAL;
   int cpbeari6 = 0;
   int bestbuypattern7= UNREAL;
   int cpbulli7=0;
   int bestsellpattern7= UNREAL;
   int cpbeari7 = 0;
   int bestbuypattern8= UNREAL;
   int cpbulli8=0;
   int bestsellpattern8= UNREAL;
   int cpbeari8 = 0;
   int bestbuypattern9= UNREAL;
   int cpbulli9=0;
   int bestsellpattern9= UNREAL;
   int cpbeari9 = 0;
   int bestbuypattern10= UNREAL;
   int cpbulli10=0;
   int bestsellpattern10= UNREAL;
   int cpbeari10 = 0;

   for(int i=0; i<ARRAYLENGTH; i++)
     {
      if(candletyperesultsbullish[i]> bestbuypattern)
        {
         bestbuypattern = candletyperesultsbullish[i];
         cpbulli =i;
        }

      if(candletyperesultsbearish[i]> bestsellpattern)
        {
         bestsellpattern = candletyperesultsbearish[i];
         cpbeari=i;
        }


     }



   for(int i=0; i<ARRAYLENGTH; i++)
     {
      if(candletyperesultsbullish[i]> bestbuypattern2 && candletyperesultsbullish[i]<bestbuypattern)
        {
         bestbuypattern2 = candletyperesultsbullish[i];
         cpbulli2 =i;
        }

      if(candletyperesultsbearish[i]> bestsellpattern2 && candletyperesultsbearish[i]<bestsellpattern)
        {
         bestsellpattern2 = candletyperesultsbearish[i];
         cpbeari2=i;
        }


     }



   for(int i=0; i<ARRAYLENGTH; i++)
     {
      if(candletyperesultsbullish[i]> bestbuypattern3 && candletyperesultsbullish[i]<bestbuypattern2)
        {
         bestbuypattern3 = candletyperesultsbullish[i];
         cpbulli3 =i;
        }

      if(candletyperesultsbearish[i]> bestsellpattern3 && candletyperesultsbearish[i]<bestsellpattern2)
        {
         bestsellpattern3 = candletyperesultsbearish[i];
         cpbeari3=i;
        }


     }



   for(int i=0; i<ARRAYLENGTH; i++)
     {
      if(candletyperesultsbullish[i]> bestbuypattern4 && candletyperesultsbullish[i]<bestbuypattern3)
        {
         bestbuypattern4 = candletyperesultsbullish[i];
         cpbulli4 =i;
        }

      if(candletyperesultsbearish[i]> bestsellpattern4 && candletyperesultsbearish[i]<bestsellpattern3)
        {
         bestsellpattern4 = candletyperesultsbearish[i];
         cpbeari4=i;
        }


     }


   for(int i=0; i<ARRAYLENGTH; i++)
     {
      if(candletyperesultsbullish[i]> bestbuypattern5 && candletyperesultsbullish[i]<bestbuypattern4)
        {
         bestbuypattern5 = candletyperesultsbullish[i];
         cpbulli5 =i;
        }

      if(candletyperesultsbearish[i]> bestsellpattern5 && candletyperesultsbearish[i]<bestsellpattern4)
        {
         bestsellpattern5 = candletyperesultsbearish[i];
         cpbeari5=i;
        }


     }


   for(int i=0; i<ARRAYLENGTH; i++)
     {
      if(candletyperesultsbullish[i]> bestbuypattern6 && candletyperesultsbullish[i]<bestbuypattern5)
        {
         bestbuypattern6 = candletyperesultsbullish[i];
         cpbulli6 =i;
        }

      if(candletyperesultsbearish[i]> bestsellpattern6 && candletyperesultsbearish[i]<bestsellpattern5)
        {
         bestsellpattern6 = candletyperesultsbearish[i];
         cpbeari6=i;
        }


     }


   for(int i=0; i<ARRAYLENGTH; i++)
     {
      if(candletyperesultsbullish[i]> bestbuypattern7 && candletyperesultsbullish[i]<bestbuypattern6)
        {
         bestbuypattern7 = candletyperesultsbullish[i];
         cpbulli7 =i;
        }

      if(candletyperesultsbearish[i]> bestsellpattern7 && candletyperesultsbearish[i]<bestsellpattern6)
        {
         bestsellpattern7 = candletyperesultsbearish[i];
         cpbeari7=i;
        }


     }


   for(int i=0; i<ARRAYLENGTH; i++)
     {
      if(candletyperesultsbullish[i]> bestbuypattern8 && candletyperesultsbullish[i]<bestbuypattern7)
        {
         bestbuypattern8 = candletyperesultsbullish[i];
         cpbulli8 =i;
        }

      if(candletyperesultsbearish[i]> bestsellpattern8 && candletyperesultsbearish[i]<bestsellpattern7)
        {
         bestsellpattern8 = candletyperesultsbearish[i];
         cpbeari8=i;
        }


     }



   for(int i=0; i<ARRAYLENGTH; i++)
     {
      if(candletyperesultsbullish[i]> bestbuypattern9 && candletyperesultsbullish[i]<bestbuypattern8)
        {
         bestbuypattern9 = candletyperesultsbullish[i];
         cpbulli9 =i;
        }

      if(candletyperesultsbearish[i]> bestsellpattern9 && candletyperesultsbearish[i]<bestsellpattern8)
        {
         bestsellpattern9 = candletyperesultsbearish[i];
         cpbeari9=i;
        }


     }

   for(int i=0; i<ARRAYLENGTH; i++)
     {
      if(candletyperesultsbullish[i]> bestbuypattern10 && candletyperesultsbullish[i]<bestbuypattern9)
        {
         bestbuypattern10 = candletyperesultsbullish[i];
         cpbulli10 =i;
        }

      if(candletyperesultsbearish[i]> bestsellpattern10 && candletyperesultsbearish[i]<bestsellpattern9)
        {
         bestsellpattern10 = candletyperesultsbearish[i];
         cpbeari10=i;
        }


     }




   if(IsTesting() == true)
     {
      // Print((string)(cnt_hours)+ " Hours Have been passed!");
     }
   string last7p =StringSubstr(marketpattern,StringLen(marketpattern)-7,7);
   if(IsTesting() == true)
     {
      //Print((string)last6p + " was the last 6 patterns");
     }
   bool canbuy = false;
   bool cansell = false;

   canbuy  = (candletyperesultsbullish[cpbulli]>=1) &&(candletype[cpbulli] ==last7p);
   cansell = (candletyperesultsbearish[cpbeari]>=1)&&(candletype[cpbeari] ==last7p);


   canbuy  = (canbuy)|| ((candletyperesultsbullish[cpbulli2]>=1) &&(candletype[cpbulli2] ==last7p));
   cansell = (cansell) ||((candletyperesultsbearish[cpbeari2]>=1)&&(candletype[cpbeari2] ==last7p));


   canbuy  = (canbuy)  ||((candletyperesultsbullish[cpbulli3]>=1) &&(candletype[cpbulli3] ==last7p));
   cansell = (cansell) ||((candletyperesultsbearish[cpbeari3]>=1) &&(candletype[cpbeari3] ==last7p));

   canbuy  = (canbuy)  ||((candletyperesultsbullish[cpbulli4]>=1) &&(candletype[cpbulli4] ==last7p));
   cansell = (cansell) ||((candletyperesultsbearish[cpbeari4]>=1) &&(candletype[cpbeari4] ==last7p));

   canbuy  = (canbuy)  ||((candletyperesultsbullish[cpbulli5]>=1) &&(candletype[cpbulli5] ==last7p));
   cansell = (cansell) ||((candletyperesultsbearish[cpbeari5]>=1) &&(candletype[cpbeari5] ==last7p));

   canbuy  = (canbuy)  ||((candletyperesultsbullish[cpbulli6]>=1) &&(candletype[cpbulli6] ==last7p));
   cansell = (cansell) ||((candletyperesultsbearish[cpbeari6]>=1) &&(candletype[cpbeari6] ==last7p));

   canbuy  = (canbuy)  ||((candletyperesultsbullish[cpbulli7]>=1) &&(candletype[cpbulli7] ==last7p));
   cansell = (cansell) ||((candletyperesultsbearish[cpbeari7]>=1) &&(candletype[cpbeari7] ==last7p));

   canbuy  = (canbuy)  ||((candletyperesultsbullish[cpbulli8]>=1) &&(candletype[cpbulli8] ==last7p));
   cansell = (cansell) ||((candletyperesultsbearish[cpbeari8]>=1) &&(candletype[cpbeari8] ==last7p));


   canbuy  = (canbuy)  ||((candletyperesultsbullish[cpbulli9]>=1) &&(candletype[cpbulli9] ==last7p));
   cansell = (cansell) ||((candletyperesultsbearish[cpbeari9]>=1) &&(candletype[cpbeari9] ==last7p));


   canbuy  = (canbuy)  ||((candletyperesultsbullish[cpbulli10]>=1) &&(candletype[cpbulli10] ==last7p));
   cansell = (cansell) ||((candletyperesultsbearish[cpbeari10]>=1) &&(candletype[cpbeari10] ==last7p));







   if(canbuy == true)
     {
    


      double SL = (StopLoss>0)
                  ?MarketInfo(Symbol(),MODE_BID)-PipsToDecimal(StopLoss):0;
      double TP = (TakeProfit>0)?MarketInfo(Symbol(),MODE_ASK)+PipsToDecimal(TakeProfit):0;

      while(IsTradeContextBusy())
         Sleep(1000);
      if(AccountFreeMarginCheck(Symbol(),OP_BUY,CalculateLots())<=0)
        {
         Comment("Not enough money to trade ! ");

        }
      else
        {

         if(Time[1] != candletime)
           {
            candletime = Time[1];

            int ordersend  =OrderSend(
                               Symbol(),
                               OP_BUY,
                               CalculateLots(),
                               MarketInfo(Symbol(),MODE_ASK),
                               exSlippage,
                               SL,
                               TP,
                               NULL,
                               NULL
                            );



           }


        }

      canbuy=false;
     }

   if(cansell==true)
     {

    




      double SL = (StopLoss>0)
                  ?MarketInfo(Symbol(),MODE_ASK)+PipsToDecimal(StopLoss):0;
      double TP = (TakeProfit>0)?MarketInfo(Symbol(),MODE_BID)-PipsToDecimal(TakeProfit):0;

      while(IsTradeContextBusy())
         Sleep(1000);
      if(AccountFreeMarginCheck(Symbol(),OP_SELL,CalculateLots())<=0)
        {
         Comment("Not enough money to trade ! ");

        }
      else
        {

         if(Time[1] != candletime)
           {
            candletime = Time[1];

            int ordersend  =OrderSend(
                               Symbol(),
                               OP_SELL,
                               CalculateLots(),
                               MarketInfo(Symbol(),MODE_BID),
                               exSlippage,
                               SL,
                               TP,
                               NULL,
                               NULL
                            );


           }



        }

      cansell=false;
     }










  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double  CalculateLots()
  {

   if(!RiskMM)
     {
      if(Lots<MarketInfo(Symbol(),MODE_MINLOT))
        { return MarketInfo(Symbol(),MODE_MINLOT); }

      if(Lots>MarketInfo(Symbol(),MODE_MAXLOT))
        { return MarketInfo(Symbol(),MODE_MAXLOT); }
      return Lots;
     }

   else
     {
      double lottoreturn=0;
      double MinLots=MarketInfo(Symbol(),MODE_MINLOT);
      double MaxLots=MarketInfo(Symbol(),MODE_MAXLOT);
      lottoreturn=AccountFreeMargin()/100000*RiskPercent;
      lottoreturn=MathMin(MaxLots,MathMax(MinLots,lottoreturn));
      if(MinLots<0.1)
         lottoreturn=NormalizeDouble(lottoreturn,2);
      else
        {
         if(MinLots<1)
            lottoreturn=NormalizeDouble(lottoreturn,1);
         else
            lottoreturn=NormalizeDouble(lottoreturn,0);
        }
      if(lottoreturn<MinLots)
         Lots=MinLots;
      if(lottoreturn>MaxLots)
         Lots=MaxLots;
      return(lottoreturn);
     }
   return Lots;
  }



//+------------------------------------------------------------------+
double RoundNumber(int digits,double number)
  {

   number = MathRound(number * MathPow(10, digits));
   return (number * MathPow(10, -digits));
  }


  