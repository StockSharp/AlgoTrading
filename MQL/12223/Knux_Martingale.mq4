//+------------------------------------------------------------------+
//|                                                                  |
//|                                             KNUX Martingale V1.0 |
//|                                              by R. Broszeit 2014 |
//|                                                                  |
//+------------------------------------------------------------------+
//
#property copyright "KNUX Martingale by R. Broszeit  *** BroTrader2014@Gmail.com ***"
#property version "1.0"
#property description" The Martingale- Base [Pure Martingale] was coded by Matus German, www.MTexperts.net"
//
//
extern string    MainSet               ="------ Main settings ------";
extern double    StopLoss              = 150;
extern double    TakeProfit            = 50;

extern string    MartingaleSet="------ Martingale settings ------";

extern double    lotsMultiplier        = 1.5;
extern double    distanceMultiplier    = 1.5;

extern string Capital="------ Trade Lots Size ------";

extern double ManualLots               = 0.1;
extern bool   AutoLot                  = false;
extern int    Risk                     = 3;
extern double MaxLot                   = 100;
extern double MinLot                   = 0.01;

extern string MainSignal="------ Main Signal Logic ------";

extern int    ADX_FilterPeriod         = 14;
extern int    RVI_FilterPeriod         = 20;
extern int    TimeShift                = 0;

extern string ADXSignal="------ ADX Signal Logic ------";

extern int    ADX_CrossPeriod          = 5;
extern int    CCI_FilterPeriod         = 40;
extern int    CCI_Level                = 150;

extern string WPRSignal="------ WPR Signal Logic ------";

extern int    WPR_Period               = 70;
extern int    WPR_BuyRange             = 15;
extern int    WPR_SellRange            = 15;
extern int    WPR_ADXmaxLevel          = 25;
extern int    WPR_ADXminLevel          = 5;

extern string Strategy_Mode="------ Strategy ------";

extern bool   ADX_Strategy             = true;
extern bool   WPR_Strategy             = true;

extern string Times="------ Time Filters ------";

extern bool   TimeControl              = false;
extern string TimeZone                 = "Adjust ServerTimeZone if Required";
extern int    ServerTimeZone           = 1;
extern string TradingTimes             = "HourStopGMT > HourStartGMT";
extern int    HourStartGMT             = 7;
extern int    HourStopGMT              = 22;
extern string DontTradeFriday          = "Dont Trade After FridayFinalHourGMT";
extern bool   UseFridayFinalTradeTime  = true;
extern int    FridayFinalHourGMT       = 6;

extern string   MenuSetting            ="------ Wiew settings ------";
extern bool     showMenu               = true;
extern color    menuColor              = Yellow;
extern color    variablesColor         = Red;
extern int      font                   = 10;

extern string    Extras                ="------ Extra Settings ------";
extern double    MaxSlippage           = 3;
extern int       Identification        = 1212123;
extern bool      Auto_Ident            = true;

double stopLoss,takeProfit,
minAllowedLot,lotStep,maxAllowedLot,Lots,
pips2dbl,pips2point,pipValue,minGapStop,maxSlippage,
menulots,profit,lots,ma1;

double Min_Lot=MinLot;
double Max_Lot=MaxLot;

int magicNumber;

double RF=((100/StopLoss)*(100/AccountLeverage()));
double stepLot=MarketInfo(Symbol(),MODE_LOTSTEP);
double LotSize=(MarketInfo(Symbol(),MODE_LOTSIZE)/10);
double MiL=MarketInfo(Symbol(),MODE_MINLOT);
double MaL=MarketInfo(Symbol(),MODE_MAXLOT);

datetime barTime=0;

bool  noHistory=false,
buy=false,
sell=false,
stopsChecked;

int   medzera=8;
int   TS=TimeShift;
int   trades,Error;

double ADX_Dplus,ADX_Dminus,ADX_Dplus0,ADX_Dminus0,ADX_Dplus1,ADX_Dminus1;
double adx_Dplus,adx_Dminus,adx_Dplus1,adx_Dminus1;
double ADX,ADX0,ADX1,adx,adx1;
double RVI,RVIs;
double CCI,CCI1;
double WPR,WPR1,WPR2,WPR3;
///
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
///
int init()
  {
//----

   if(Digits==5 || Digits==3) // Adjust for five (5) digit brokers.
     {
      pips2dbl=Point*10; pips2point=10; pipValue=(MarketInfo(Symbol(),MODE_TICKVALUE))*10;
     }
   else
     {
      pips2dbl=Point;   pips2point=1; pipValue=(MarketInfo(Symbol(),MODE_TICKVALUE))*1;
     }

   CalcLot();
   maxSlippage=MaxSlippage*pips2dbl;
   stopLoss=pips2dbl*StopLoss;
   takeProfit=pips2dbl*TakeProfit;
   minGapStop=MarketInfo(Symbol(),MODE_STOPLEVEL)*Point;


   lots=Lots;
   minAllowedLot  =  MarketInfo(Symbol(), MODE_MINLOT);    //IBFX= 0.10
   lotStep        =  MarketInfo(Symbol(), MODE_LOTSTEP);   //IBFX= 0.01
   maxAllowedLot  =  MarketInfo(Symbol(), MODE_MAXLOT );   //IBFX=50.00

   if(lots<minAllowedLot)
      lots=minAllowedLot;
   if(lots>maxAllowedLot)
      lots=maxAllowedLot;
   if(showMenu)
     {
      DrawMenu();
     }

   if(Auto_Ident)
     {

      int temp2[7],Mcar;
      int temp3=1,temp4=0,temp5=0;
      double temp=0;
      double temp1;

      for(Mcar=1; Mcar<6; Mcar++)
        {
         temp2[Mcar]=StringGetChar(Symbol(),Mcar)*temp3;
         temp3=temp3*10;
        }

      temp1=NormalizeDouble(temp2[1]+temp2[2]+temp2[3]+temp2[4]+temp2[5]+temp2[6]+RVI_FilterPeriod+ADX_FilterPeriod+CCI_FilterPeriod+CCI_Level+ADX_CrossPeriod+Period(),0);

      if(temp1>9999999999) {temp1=temp1/100;}
      if(temp1>999999999) {temp1=temp1/10;}            //maximum of integer = 2?147?483?647 is bigger then 999?999?999 make / 10 

      for(Mcar=0;Mcar<OrdersHistoryTotal();Mcar++) //this loop checks if the number is free
        {
         temp=OrderSelect(Mcar,SELECT_BY_POS,MODE_HISTORY);
         if(OrderMagicNumber()==temp1) // check the Magic Number
           {temp4++; }
        }
      if(temp4!=0) {MessageBox("This Identification Number ( "+temp1+" ) is found on History!! Please check it!!!"); }

      for(Mcar=0;Mcar<OrdersTotal();Mcar++) //this loop checks if the number is free
        {
         temp=OrderSelect(Mcar,SELECT_BY_POS,MODE_TRADES);
         if(OrderMagicNumber()==temp1) // check the Magic Number
           {temp5++;}
        }
      if(temp5!=0) {MessageBox("This Identification Number ( "+temp1+" ) is found on active Trades!! Please check it!!!"); }

      magicNumber=temp1;

      Print("Identification Magic- Number calculates to "+magicNumber+" !!!");

     }

   else

     {

      magicNumber=Identification;

     }

//----
   return(0);
  }
///  
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
///
int deinit()
  {
//----
   if(showMenu)
     {
      ObjectDelete("name");
      ObjectDelete("Openl");
      ObjectDelete("Open");
      ObjectDelete("Lotsl");
      ObjectDelete("Lots");
      ObjectDelete("Profitl");
      ObjectDelete("Profit");
     }
//----
   return(0);
  }
///  
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
///
int start()
  {
//----

   Comment("KNUX Martingale by. BroTrader2014@Gmail.com  ( "+magicNumber+" ) \n"+"Balance: "+
           NormalizeDouble(AccountBalance(),2)+" | Margin: "+NormalizeDouble(AccountFreeMargin(),2));

//---- 

   adx=iADX(NULL,0,ADX_FilterPeriod,PRICE_CLOSE,MODE_MAIN,0+TS);
   adx1=iADX(NULL,0,ADX_FilterPeriod,PRICE_CLOSE,MODE_MAIN,2+TS);

   if(adx>adx1 && TradingTime() && OrdersTotal()==0)
     {
      if(ADX_Strategy)
        {
         ADX_Dplus=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_PLUSDI,0+TS);
         ADX_Dminus=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_MINUSDI,0+TS);
         ADX_Dplus0=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_PLUSDI,1+TS);
         ADX_Dminus0=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_MINUSDI,1+TS);
         ADX_Dplus1=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_PLUSDI,2+TS);
         ADX_Dminus1=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_MINUSDI,2+TS);

         ADX=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_MAIN,0+TS);
         ADX0=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_MAIN,1+TS);
         ADX1=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_MAIN,2+TS);
        }

      adx_Dplus=iADX(NULL,0,ADX_FilterPeriod,PRICE_CLOSE,MODE_PLUSDI,0+TS);
      adx_Dminus=iADX(NULL,0,ADX_FilterPeriod,PRICE_CLOSE,MODE_MINUSDI,0+TS);
      adx_Dplus1=iADX(NULL,0,ADX_FilterPeriod,PRICE_CLOSE,MODE_PLUSDI,2+TS);
      adx_Dminus1=iADX(NULL,0,ADX_FilterPeriod,PRICE_CLOSE,MODE_MINUSDI,2+TS);

      RVI=iRVI(NULL,0,RVI_FilterPeriod,MODE_MAIN,0+TS);
      RVIs=iRVI(NULL,0,RVI_FilterPeriod,MODE_SIGNAL,0+TS);

      CCI=iCCI(NULL,0,CCI_FilterPeriod,PRICE_CLOSE,0+TS);
      CCI1=iCCI(NULL,0,CCI_FilterPeriod,PRICE_CLOSE,1+TS);

      if(WPR_Strategy)
        {
         WPR=iWPR(NULL,0,WPR_Period,0+TS);
         WPR1=iWPR(NULL,0,WPR_Period,1+TS);
         WPR2=iWPR(NULL,0,WPR_Period,2+TS);
         WPR3=iWPR(NULL,0,WPR_Period,3+TS);
        }
     }

   else
     {
      buy=false; sell=false;
      //RVI=0; RVIs=0;
     }

   if(HistoryForMNandPT(magicNumber,Symbol())<=0)
      noHistory=true;
   else
      noHistory=false;

   if(showMenu)
     {
      profit=ProfitCheck();
      ReDrawMenu();
     }

   if(TimeControl)
     {
      if(TradingTime())
         OpenOrderCheck();
     }
   else
     {
      OpenOrderCheck();
     }

   if(!stopsChecked)
      if(CheckStops())
         stopsChecked=true;
   else return;

   checkSlTp();
//----
   return(0);
  }
//////////////////////////////////////////////////////////////////////////////////////////////////  
bool EnterBuyCondition()
  {
   if(!buy && ADX_Strategy && RVI>RVIs && CCI<(-1*CCI_Level) && RVI<0 && CCI<CCI1)
     {
      if((ADX_Dplus>ADX_Dminus && ADX_Dplus1<ADX_Dminus1 && ADX_Dplus>ADX_Dplus1 && ADX_Dminus1>ADX_Dminus) || 

         (ADX_Dplus>ADX && ADX_Dplus1<ADX1 && ADX_Dplus>ADX_Dplus1 && ADX_Dminus1>ADX_Dminus) || 

         (ADX_Dplus>ADX_Dminus && ADX_Dplus0<ADX_Dminus0 && ADX_Dplus>ADX_Dplus0 && ADX_Dminus0>ADX_Dminus)// || 

         //(ADX_Dplus>ADX && ADX_Dplus1<ADX0 && ADX_Dplus>ADX_Dplus0 && ADX_Dminus0>ADX_Dminus)
         )

        {buy=true; CalcLot(); return(true);}

     }

   if(!buy && WPR_Strategy && adx<WPR_ADXmaxLevel && adx>WPR_ADXminLevel && RVI>RVIs && WPR<((-100)+WPR_BuyRange) && WPR>WPR1 && WPR1>WPR2 && WPR2>WPR3 && 
      adx_Dplus>adx_Dplus1 && adx_Dminus1>adx_Dminus)

     {buy=true; CalcLot(); return(true);}

   return (false);
  }
//////////////////////////////////////////////////////////////////////////////////////////////////
bool EnterSellCondition()
  {
   if(!sell && ADX_Strategy && RVI<RVIs && CCI>CCI_Level && RVI>0 && CCI>CCI1)
     {
      if((ADX_Dminus>ADX_Dplus && ADX_Dminus1<ADX_Dplus1 && ADX_Dminus>ADX_Dminus1 && ADX_Dplus1>ADX_Dplus) || 

         (ADX_Dminus>ADX && ADX_Dminus1<ADX1 && ADX_Dminus>ADX_Dminus1 && ADX_Dplus1>ADX_Dplus) || 

         (ADX_Dminus>ADX_Dplus && ADX_Dminus0<ADX_Dplus0 && ADX_Dminus>ADX_Dminus0 && ADX_Dplus0>ADX_Dplus)// || 

         //(ADX_Dminus>ADX && ADX_Dminus1<ADX0 && ADX_Dminus>ADX_Dminus0 && ADX_Dplus0>ADX_Dplus)
         )

        {sell=true; CalcLot(); return(true);}

     }

   if(!sell && WPR_Strategy && adx<WPR_ADXmaxLevel && adx>WPR_ADXminLevel && RVI<RVIs && WPR>(-1*WPR_SellRange) && WPR<WPR1 && WPR1<WPR2 && WPR2<WPR3 && 
      adx_Dplus<adx_Dplus1 && adx_Dminus1<adx_Dminus)

     {sell=true; CalcLot(); return(true);}

   return (false);
  }
//////////////////////////////////////////////////////////////////////////////////////////////////
// chceck trades if they do not have set sl and tp than modify trade
bool CheckStops()
  {
   double sl=0,tp=0,loss;
   double total=OrdersTotal();

   int ticket=-1;

   for(int cnt=total-1;cnt>=0;cnt--)
     {
      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(   OrderType()<=OP_SELL                      // check for opened position 
         && OrderSymbol()==Symbol()                   // check for symbol
         && OrderMagicNumber() == magicNumber)        // my magic number
        {
         if(OrderType()==OP_BUY)
           {
            if(OrderStopLoss()==0 || OrderTakeProfit()==0)
              {
               ticket=OrderTicket();
               while(!IsTradeAllowed()) Sleep(500);
               RefreshRates();

               SelectLastHistoryOrder(Symbol(),magicNumber);

               if(!noHistory)
                 {

                  if(OrderProfit()<0)
                    {
                     loss=MathAbs(OrderClosePrice()-OrderOpenPrice());
                     sl=Ask-(loss)*distanceMultiplier;
                     tp=Ask+(loss)*distanceMultiplier;
                    }
                  else
                    {
                     sl = Ask-stopLoss;
                     tp = Ask+takeProfit;
                    }
                 }
               else
                 {
                  sl = Ask-stopLoss;
                  tp = Ask+takeProfit;
                 }

               if(Bid-sl<=MarketInfo(Symbol(),MODE_STOPLEVEL)*Point)
                  sl=Bid-MarketInfo(Symbol(),MODE_STOPLEVEL)*Point;

               if(tp-Bid<=MarketInfo(Symbol(),MODE_STOPLEVEL)*Point)
                  tp=Bid+MarketInfo(Symbol(),MODE_STOPLEVEL)*Point;

               // last selected is from history so we have to select trade again   
               if(!OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                  return(false);

               if(OrderModify(OrderTicket(),OrderOpenPrice(),sl,tp,0,Green)) // modify position
                 {
                 }
               else
                  return (false);
              }
           }
         if(OrderType()==OP_SELL)
           {
            if(OrderStopLoss()==0 && OrderTakeProfit()==0)
              {
               ticket=OrderTicket();
               while(!IsTradeAllowed()) Sleep(500);
               RefreshRates();

               SelectLastHistoryOrder(Symbol(),magicNumber);

               if(!noHistory)
                 {

                  if(OrderProfit()<0)
                    {
                     loss=MathAbs(OrderClosePrice()-OrderOpenPrice());
                     sl=Bid+(loss)*distanceMultiplier;
                     tp=Bid-(loss)*distanceMultiplier;
                    }
                  else
                    {
                     sl = Bid+stopLoss;
                     tp = Bid-takeProfit;
                    }
                 }
               else
                 {
                  sl = Bid+stopLoss;
                  tp = Bid-takeProfit;
                 }

               if(sl-Ask<=MarketInfo(Symbol(),MODE_STOPLEVEL)*Point)
                  sl=Ask+MarketInfo(Symbol(),MODE_STOPLEVEL)*Point;

               if(Ask-tp<=MarketInfo(Symbol(),MODE_STOPLEVEL)*Point)
                  tp=Ask-MarketInfo(Symbol(),MODE_STOPLEVEL)*Point;

               // last selected is from history so we have to select trade again    
               if(!OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                  return(false);

               if(OrderModify(OrderTicket(),OrderOpenPrice(),sl,tp,0,Green)) // modify position
                 {
                 }
               else
                  return (false);
              }
           }
        }
     }
   return (true);
  }
//////////////////////////////////////////////////////////////////////////////////////////////////
bool OpenOrderCheck()
  {
   double olots=lots;
   int ticket;
   int total=OpenTradesForMNandPairType(magicNumber,Symbol());

   if(total==0)
     {
      // check for long position (BUY) possibility     
      if(EnterBuyCondition())
        {
         if(!noHistory)
           {
            SelectLastHistoryOrder(Symbol(),magicNumber);
            if(OrderProfit()<0 && Error!=134)
              {
               olots=OrderLots()*lotsMultiplier;
              }
            else if(OrderProfit()<0 && Error==134) {olots=lots; Error=0; Print("Not enought Money!! Reset Martingale!!");}
           }

         while(!IsTradeAllowed()) Sleep(500);
         RefreshRates();

         ticket=OrderSend(Symbol(),OP_BUY,olots,Ask,maxSlippage,0,0,"",magicNumber,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
              {
               stopsChecked=false;
               Print("BUY order opened : ",OrderOpenPrice());
               return (true);
              }
           }
         else
           {
            Error=GetLastError();
            Print("Error opening BUY order : ",Error);
            return(false);
           }

        }

      // check for short position (SELL) possibility
      if(EnterSellCondition()) // OrderSelect() was in function EnterBuyCondition
        {
         if(!noHistory)
           {
            SelectLastHistoryOrder(Symbol(),magicNumber);
            if(OrderProfit()<0 && Error!=134)
              {
               olots=OrderLots()*lotsMultiplier;
              }
            else if(OrderProfit()<0 && Error==134) {olots=lots; Error=0;Print("Not enought Money!! Reset Martingale!!");}
           }

         while(!IsTradeAllowed()) Sleep(500);
         RefreshRates();

         ticket=OrderSend(Symbol(),OP_SELL,olots,Bid,maxSlippage,0,0,"",magicNumber,0,Red);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
              {
               stopsChecked=false;
               Print("SELL order opened : ",OrderOpenPrice());
               return (true);
              }
           }
         else
           {
            Error=GetLastError();
            Print("Error opening SELL order : ",Error);
            return (false);
           }
        }
     }
   return (true);
  }
//////////////////////////////////////////////////////////////////////////////////////////////////
int OpenTradesForMNandPairType(int iMN,string sOrderSymbol)
  {
   int icnt,itotal,retval;

   retval=0;
   itotal=OrdersTotal();

   for(icnt=itotal-1;icnt>=0;icnt--) // for loop
     {
      OrderSelect(icnt,SELECT_BY_POS,MODE_TRADES);
      // check for opened position, symbol & MagicNumber
      if(OrderSymbol()==sOrderSymbol)
        {
         if(OrderMagicNumber()==iMN)
            retval++;
        } // sOrderSymbol
     } // for loop

   return(retval);
  }
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
double ProfitCheck()
  {
   double profit_=0;
   int total=OrdersTotal();
   for(int cnt=total-1; cnt>=0; cnt--)
     {
      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()==Symbol() && (OrderMagicNumber()==magicNumber))
         profit_+=OrderProfit();
     }
   return(profit_);
  }
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
int CalcLot()
  {
   if(!AutoLot)

     {
      Lots=ManualLots;
     }

   else if(AutoLot)

     {
      double RCC=AccountFreeMargin();

      if(stepLot==0.01) {Lots=(RF*NormalizeDouble(RCC*Risk/LotSize,1)/10);}
      else if(stepLot==0.1) {Lots=(RF*NormalizeDouble(RCC*Risk/LotSize,0)/10);}

      if(MaL==0) {MaL=100;}
      if(MiL==0) {MiL=0.1;}
      if(Min_Lot<MiL) {Min_Lot=MiL;}
      if(Max_Lot>MaL) {Max_Lot=MaL;}
      if(Lots<Min_Lot) {Lots=Min_Lot;}
      if(Lots>Max_Lot) {Lots=Max_Lot;}
     }

   lots=Lots;
   return(0);
  }
//----

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool DrawMenu()
  {
   ObjectCreate("name",OBJ_LABEL,0,0,0,0,0);
   ObjectCreate("Openl",OBJ_LABEL,0,0,0,0,0);
   ObjectCreate("Open",OBJ_LABEL,0,0,0,0,0);
   ObjectCreate("Lotsl",OBJ_LABEL,0,0,0,0,0);
   ObjectCreate("Lots",OBJ_LABEL,0,0,0,0,0);
   ObjectCreate("Profitl",OBJ_LABEL,0,0,0,0,0);
   ObjectCreate("Profit",OBJ_LABEL,0,0,0,0,0);

   medzera=8;

   trades=Opened();
   menulots=Lots();

   ObjectSetText("name","KNUX MARTINGALE",font+1,"Arial",menuColor);
   ObjectSet("name",OBJPROP_XDISTANCE,medzera*font);
   ObjectSet("name",OBJPROP_YDISTANCE,10+font);
   ObjectSet("name",OBJPROP_CORNER,1);

   ObjectSetText("Openl","Opened trades: ",font,"Arial",menuColor);
   ObjectSet("Openl",OBJPROP_XDISTANCE,medzera*font);
   ObjectSet("Openl",OBJPROP_YDISTANCE,10+2*(font+2));
   ObjectSet("Openl",OBJPROP_CORNER,1);

   ObjectSetText("Open",""+trades,font,"Arial",variablesColor);
   ObjectSet("Open",OBJPROP_XDISTANCE,3*font);
   ObjectSet("Open",OBJPROP_YDISTANCE,10+2*(font+2));
   ObjectSet("Open",OBJPROP_CORNER,1);

   ObjectSetText("Lotsl","Lots of opened positions: ",font,"Arial",menuColor);
   ObjectSet("Lotsl",OBJPROP_XDISTANCE,medzera*font);
   ObjectSet("Lotsl",OBJPROP_YDISTANCE,10+3*(font+2));
   ObjectSet("Lotsl",OBJPROP_CORNER,1);

   ObjectSetText("Lots",DoubleToStr(menulots,2),font,"Arial",variablesColor);
   ObjectSet("Lots",OBJPROP_XDISTANCE,3*font);
   ObjectSet("Lots",OBJPROP_YDISTANCE,10+3*(font+2));
   ObjectSet("Lots",OBJPROP_CORNER,1);

   ObjectSetText("Profitl","Profit of opened positions: ",font,"Arial",menuColor);
   ObjectSet("Profitl",OBJPROP_XDISTANCE,medzera*font);
   ObjectSet("Profitl",OBJPROP_YDISTANCE,10+4*(font+2));
   ObjectSet("Profitl",OBJPROP_CORNER,1);

   ObjectSetText("Profit",DoubleToStr(profit,2),font,"Arial",variablesColor);
   ObjectSet("Profit",OBJPROP_XDISTANCE,3*font);
   ObjectSet("Profit",OBJPROP_YDISTANCE,10+4*(font+2));
   ObjectSet("Profit",OBJPROP_CORNER,1);

  }
///////////////////////////////////////////////////////////////////////////////////////////////////////////
bool ReDrawMenu()
  {
   medzera=8;

   trades=Opened();
   menulots=Lots();

   ObjectSetText("Open",""+trades,font,"Arial",variablesColor);
   ObjectSetText("Lots",DoubleToStr(menulots,2),font,"Arial",variablesColor);
   ObjectSetText("Profit",DoubleToStr(profit,2),font,"Arial",variablesColor);
  }
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
int Opened()
  {
   int total= OrdersTotal();
   int count= 0;
   for(int cnt=total-1; cnt>=0; cnt--)
     {
      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()==Symbol() && (OrderMagicNumber()==magicNumber))
         if(OrderType()==OP_BUY || OrderType()==OP_SELL)
            count++;
     }
   return (count);
  }
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
double Lots()
  {
   int total=OrdersTotal();
   double lots_=0;
   for(int cnt=total-1; cnt>=0; cnt--)
     {
      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()==Symbol() && (OrderMagicNumber()==magicNumber))
         if(OrderType()==OP_BUY || OrderType()==OP_SELL)
            lots_+=OrderLots();
     }
   return (lots_);
  }
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool SelectLastHistoryOrder(string symbol,int _magicNumber)
  {
   int lastOrder=NULL;
   for(int i=OrdersHistoryTotal()-1;i>=0;i--)
     {
      OrderSelect(i,SELECT_BY_POS,MODE_HISTORY);
      if(OrderSymbol()==symbol && OrderMagicNumber()==_magicNumber)
        {
         lastOrder=i;
         break;
        }
     }
   if(lastOrder==NULL)
      return(false);
   else
      return(true);
  }
//////////////////////////////////////////////////////////////////////////////////////////////////
int HistoryForMNandPT(int iMN,string sOrderSymbol)
  {
   int icnt,itotal,retval;

   retval=0;
   itotal=OrdersHistoryTotal();

   for(icnt=itotal-1;icnt>=0;icnt--) // for loop
     {
      OrderSelect(icnt,SELECT_BY_POS,MODE_HISTORY);
      // check for opened position, symbol & MagicNumber
      if(OrderSymbol()==sOrderSymbol)
        {
         if(OrderMagicNumber()==iMN)
            retval++;
        } // sOrderSymbol
     } // for loop

   return(retval);
  }
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool TradingTime()
  {
   int HourStartTrade;
   int HourStopTrade;

   HourStartTrade= HourStartGMT+ServerTimeZone;
   HourStopTrade = HourStopGMT+ServerTimeZone;
   if(HourStartTrade<0)HourStartTrade=HourStartTrade+24;
   if(HourStartTrade>=24)HourStartTrade=HourStartTrade-24;
   if(HourStopTrade>24)HourStopTrade=HourStopTrade-24;
   if(HourStopTrade<=0)HourStopTrade=HourStopTrade+24;
   if((UseFridayFinalTradeTime && (Hour()>=FridayFinalHourGMT+ServerTimeZone) && DayOfWeek()==5) || DayOfWeek()==6 || DayOfWeek()==0)return(FALSE); // Friday Control
   if((TimeControl(HourStartTrade,HourStopTrade)!=1 && TimeControl && HourStartTrade<HourStopTrade)
      || (TimeControl(HourStopTrade,HourStartTrade)!=0 && TimeControl && HourStartTrade>HourStopTrade)
      || !TimeControl) return(TRUE); // "Trading Time";
   return(FALSE); // "Non-Trading Time";
  }
//+------------------------------------------------------------------+

int TimeControl(int StartHour,int EndHour)
  {
   if(Hour()>=StartHour && Hour()<EndHour)
     {
      return(0);
     }
   return(1);
  }
//+------------------------------------------------------------------+

int checkSlTp()

  {
/*
   int cnt,OMChk;

   if(OrdersTotal()>0)
     {
      for(cnt=0;cnt<OrdersTotal();cnt++)
        {
         RefreshRates();

         OMChk=OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);

         if(OrderSymbol()==Symbol() && // check for symbol
            OrderMagicNumber()==magicNumber) // check the Magic Number
           {

            if(OrderType()==OP_BUY) // go to long position
              {

               // check TakeProfit and StopLoss
               if(Bid<=OrderStopLoss()) {OMChk=OrderClose(OrderTicket(),OrderLots(),Ask,maxSlippage,Green);Print("#### Buy- Trade closed by StopLoss!! #### > "+Ask);}
               if(Bid>=OrderTakeProfit()) {OMChk=OrderClose(OrderTicket(),OrderLots(),Ask,maxSlippage,Green);Print("#### Buy- Trade closed by TakeProfit!! #### > "+Ask);}

              }
           }

         //----------------------------------------------------------

         if(OrderType()==OP_SELL) // go to short position
           {
            if(OrderSymbol()==Symbol() && // check for symbol
               OrderMagicNumber()==magicNumber) // check the Magic Number

              {

               // check TakeProfit and StopLoss
               if(Ask>=OrderStopLoss()) {OMChk=OrderClose(OrderTicket(),OrderLots(),Bid,maxSlippage,Red);Print("#### Sell- Trade closed by StopLoss!! #### > "+Bid); }
               if(Ask<=OrderTakeProfit()) {OMChk=OrderClose(OrderTicket(),OrderLots(),Bid,maxSlippage,Red);Print("#### Sell- Trade closed by TakeProfit!! #### > "+Bid); }

              }
           }
        }
     }
     */
   return(0);
  }
//+------------------------------------------------------------------+
