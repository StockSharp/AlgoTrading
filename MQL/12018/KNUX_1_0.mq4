//+------------------------------------------------------------------+
//|                                                                  |
//|                                                        KNUX V1.0 |
//|                                              by R. Broszeit 2014 |
//|                                                                  |
//| Do you have a reformation, a good set-file or an idea            |
//| please mail it to: BroTrader2014@Gmail.com                       |
//|                                                                  |
//| If you have a good set-file found, mail me.                      |
//| I'll send then to another set-files.                             |
//+------------------------------------------------------------------+

#property copyright "KNUX *** Copyright by R. Broszeit  *** BroTrader2014@Gmail.com"

#property version "1.0"

#property description"Do you have a reformation, a good set-file or an idea"
#property description"please mail it to: BroTrader2014@Gmail.com"
#property description"If you have a good set-file found, mail me."
#property description"I'll send then to another set-files."

extern string Basic = "=== Basic Settings ===";

extern double StopLoss = 150;
extern double TakeProfit = 50;
extern int    TrailingStop = 5;
extern int    TrailingStep = 1;
extern bool   Trailing = true;
extern bool   MoreTrades = false;
extern bool   Trigger_Enable = true;

extern string MainSignal = "=== Main Signal Logic ===";

extern int    ADX_FilterPeriod = 14;
extern int    ADX_FilterLevel = 15;
extern int    RVI_FilterPeriod = 20;
extern int    CCI_FilterPeriod = 40;
extern int    MA_TriggerFast = 5;
extern int    MA_TriggerSlow = 20;
extern int    TimeShift = 0;

extern string ADXSignal = "=== ADX Signal Logic ===";

extern int    ADX_CrossPeriod = 4;
extern int    CCI_Level = 150;

extern string WPRSignal = "=== WPR Signal Logic ===";

extern int    WPR_Period = 60;
extern int    WPR_BuyRange = 15;
extern int    WPR_SellRange = 15;
extern int    WPR_ADXmaxLevel = 25;
extern int    WPR_ADXminLevel = 15;

extern string Strategy_Mode = "=== Strategy ===";

extern bool   ADX_Strategy = true;
extern bool   WPR_Strategy = true;

extern string Capital = "=== Trade Lots Size ===";

extern double ManualLots = 0.1;
extern bool   AutoLot = true;
extern double MaxRisk = 25;
extern double MaxLot = 100;
extern double MinLot = 0.1;

extern string Losses = "After Losing Management ( 0 = Off, 1 = Reduce, >1 = BoostFactor )";

extern double LossManager = 0;

extern string Times = "=== Time Filters === ";

extern string UseTradingHours = "Time Control ( 1 = True, 0 = False )";
extern int    TimeControl = 0;
extern string TimeZone = "Adjust ServerTimeZone if Required";
extern int    ServerTimeZone = 1;
extern string TradingTimes = "HourStopGMT > HourStartGMT";
extern int    HourStartGMT = 7;
extern int    HourStopGMT = 22;
extern string DontTradeFriday = "Dont Trade After FridayFinalHourGMT";
extern bool   UseFridayFinalTradeTime = true;
extern int    FridayFinalHourGMT = 6;

extern string Extras = "=== Extra Settings ===";

extern int    MaxTrades = 2;
extern double Slippage = 3;
extern double MaxSpread = 4.0;
extern int    Identification = 78578;
extern bool   Auto_Ident = true;
extern string TradeComment = "KNUX V1.0 *** BroTrader2014@Gmail.com ***";


int    SlipPage;
int    TPM=0;
int    MyOrders=0;
double Points;
double LastCapital = 0;
double lots;
double Lots;
double Spread;
double Max_Lot=MaxLot;
double Min_Lot=MinLot;
int    WPRs,WPRb,ADXs,ADXb,LR,OMChk;
bool   sell, buy, MT, MTb, MTs;
double sActive, bActive;

  

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

// 4 or 5 Digit Broker Account Recognition
void HandleDigits()
 {
    // Automatically Adjusts to Full-Pip and Sub-Pip Accounts
   if (Digits == 4 || Digits == 2)
     {
       SlipPage = Slippage;
       Points = Point;
     }
  
   if (Digits == 5 || Digits == 3)
     {
       SlipPage = Slippage*10;
       Points = Point*10;
     }
 }

//----------------------- PRINT COMMENT FUNCTION
void subPrintDetails()
{
   string sComment   = "";
   string SP         = " ---------------------------------------------------------\n";
   string NL         = "\n";
   string SessionActive,CurrentSpread,spread;
   
   TradeSession();
   MaxSpreadFilter();
   if(Digits == 4 || Digits == 5){Spread=NormalizeDouble(10000.0 * (Ask - Bid),1);spread = DoubleToStr(Spread,1);}
   if(Digits == 2 || Digits == 3){Spread=NormalizeDouble(100.0 * (Ask - Bid),1);spread = DoubleToStr(Spread,1);}
   
   if (TradeSession())SessionActive = "Trading...";
     else SessionActive = "Non-Trading Time";
   
   if (SessionActive == "Trading..." && bActive>0) {SessionActive = "Wait for Buy-Trigger...";}
   else if (SessionActive == "Trading..." && sActive>0) {SessionActive = "Wait for Sell-Trigger...";}
   
   if (MaxSpreadFilter())CurrentSpread = " ...to High!!!";

   sComment = TradeComment + NL;
   sComment = sComment + NL;
   sComment = sComment + "Balance " +  DoubleToStr(AccountBalance(),2) +" | Margin " + DoubleToStr(AccountFreeMargin(),2) + NL;
   sComment = sComment + "StopLoss " + DoubleToStr(StopLoss,0) + " | ";
   sComment = sComment + "TakeProfit " + DoubleToStr(TakeProfit,0) + " | ";
   sComment = sComment + "TrailingStop " + DoubleToStr(TrailingStop,0) + NL;
   sComment = sComment + "ADX-Cross Period " + ADX_CrossPeriod + " | ADX-Filterperiod " + ADX_FilterPeriod  + " | RVI-Filterperiod " + RVI_FilterPeriod  + NL
                       + "CCI-Filterperiod " + CCI_FilterPeriod + " | CCI-Level " + CCI_Level + NL;
   sComment = sComment + "Server Time " + TimeToStr(TimeCurrent(), TIME_SECONDS);
   sComment = sComment + " | GMT Time " + TimeToStr((TimeCurrent()+ (( 0 - ServerTimeZone) * 3600)), TIME_SECONDS) + NL;
   sComment = sComment + SP;
   
   
      if(AutoLot && Trigger_Enable == True)
         if ((Lots*2)<Max_Lot)
         
         {sComment = sComment + "Auto-Lot " + DoubleToStr(Lots*2,2) + " --> " + DoubleToStr(MaxRisk,0) + "% | Spread " + spread + CurrentSpread + NL;}
            else 
         {sComment = sComment + "Maximum-Lot " + DoubleToStr(Max_Lot,2) +  " | Spread " + spread + CurrentSpread + NL;}
      
      else if (AutoLot && !Trigger_Enable)
         
         if (Lots<Max_Lot)
         {sComment = sComment + "Auto-Lot " + DoubleToStr(Lots,2) + " --> " + DoubleToStr(MaxRisk,0) + "% | Spread " + spread + CurrentSpread + NL;}
            else 
         {sComment = sComment + "Maximum-Lot " + DoubleToStr(Max_Lot,2) +  " | Spread " + spread + CurrentSpread + NL;}
         
      else   
         
         sComment = sComment + "Lot Size " + DoubleToStr(Lots,2) + " | Spread " + spread + CurrentSpread + NL;
            
   sComment = sComment + "Active Main- Trades " + MyOrders + " | " + SessionActive + NL;
   Comment(sComment);
}


//+------------------------------------------------------------------+
bool TradeSession()
{
   int HourStartTrade;
   int HourStopTrade;

   HourStartTrade = HourStartGMT + ServerTimeZone;
   HourStopTrade = HourStopGMT + ServerTimeZone;
   if (HourStartTrade < 0)HourStartTrade = HourStartTrade + 24;
   if (HourStartTrade >= 24)HourStartTrade = HourStartTrade - 24;
   if (HourStopTrade > 24)HourStopTrade = HourStopTrade - 24;
   if (HourStopTrade <= 0)HourStopTrade = HourStopTrade + 24;
   if ((UseFridayFinalTradeTime && (Hour()>=FridayFinalHourGMT + ServerTimeZone) && DayOfWeek()==5)||DayOfWeek()==0)return (FALSE); // Friday Control
   if((TimeControl(HourStartTrade,HourStopTrade)!=1 && TimeControl==1 && HourStartTrade<HourStopTrade)
        || (TimeControl(HourStopTrade,HourStartTrade)!=0 && TimeControl==1 && HourStartTrade>HourStopTrade)
          ||TimeControl==0)return (TRUE); // "Trading Time";
    return (FALSE); // "Non-Trading Time";
}


//+------------------------------------------------------------------+

int TimeControl(int StartHour, int EndHour)
{
   if (Hour()>=StartHour && Hour()< EndHour)
      {
      return(0);
      }
return(1);
}

//+------------------------------------------------------------------+

bool MaxSpreadFilter()
  {
   RefreshRates();
     if ((Digits == 4 || Digits == 5)&&(10000.0 * (Ask - Bid) > MaxSpread))
       {return(true);}
     if ((Digits == 2 || Digits == 3)&&(100.0 * (Ask - Bid) > MaxSpread))
       {return(true);}
   else return(false);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

int OnInit()
{
   
   if (ADX_FilterLevel>WPR_ADXminLevel && WPR_Strategy) {ADX_FilterLevel=WPR_ADXminLevel-5; Print("ADX-Filter Level set to " + ADX_FilterLevel);}
   if (ADX_FilterLevel<0) {ADX_FilterLevel=1; Print("ADX-Filter Level set to " + ADX_FilterLevel);}

//---

   if (MA_TriggerSlow<=MA_TriggerFast) {MA_TriggerSlow=MA_TriggerFast+1; Print("MA-Trigger Slow set to " + MA_TriggerSlow);}

//----


   if(TakeProfit<5)
     {
      Print("TakeProfit less than 5!!!");
      return(0);  // check TakeProfit
     }
   if(StopLoss<5)
   {
      Print("StoppLoss less than 5!!!");
      return(0);  // check StoppLoss
     }
   
//----

   if (TrailingStep>=TrailingStop) {TrailingStep=TrailingStop-2;}

//----   

   if (MoreTrades) {Trigger_Enable=true;}

//----

    if (Auto_Ident)
    {
     
     int temp, temp0, temp2[7], Mcar;
      double temp1=0;

      temp  = round((Minute()*100)+(Hour()*10)+(Seconds()*1000)+DayOfWeek());
      temp0 = DayOfWeek()+Hour()+Seconds()+Minute();
            
      for(Mcar = 1; Mcar < 6; Mcar++)
         {
            temp2[Mcar] = StringGetChar(Symbol(), Mcar)*temp1;
            temp1=temp1+100;
         }
      
      temp1=((temp2[1]+temp2[2]+temp2[3]+temp2[4]+temp2[5]+temp2[6])/((temp0+1)/(DayOfWeek()+1))+temp);
      
      if(temp1 > 9999999999) {temp1 = temp1 / 100;} 
      if(temp1 > 999999999)  {temp1 = temp1 / 10;}            //maximum of integer = 2?147?483?647 is bigger then 999?999?999 make / 10 
      
      for(Mcar=0;Mcar<OrdersHistoryTotal();Mcar++)            //this loop checks if the number is free
      {
      temp=OrderSelect(Mcar, SELECT_BY_POS, MODE_HISTORY);    
      if(OrderMagicNumber()==temp1)                           // check the Magic Number
      {temp1=temp1-10;}
      }   
                 
      Identification=round(temp1);
      
      Print("Identification Magic- Number calculates to " + Identification + " !!!");
      
       
     } 
     
     TradeComment=TradeComment + " ( " + Identification + " ) "; 

//---- 
     

return(0);
}

  
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
{

   int cnt, ticket;
   
      
   double TP=TakeProfit; 
   double SL=StopLoss;
   
      
            
   double MiL = MarketInfo(OrderSymbol(),MODE_MINLOT);
   double MaL = MarketInfo(OrderSymbol(),MODE_MAXLOT);   
        
   int TS=TimeShift;
   int Max_Trades=MaxTrades;
   int Error;
   
   


//----
          
   
   if(!AutoLot) 
     
      {
         Lots = ManualLots;
         
      }         
      
   else
         
     { 
         double RF=((100/StopLoss)*(100/AccountLeverage()));
         double stepLot = MarketInfo(OrderSymbol(),MODE_LOTSTEP);
         double RCC;
         double MR;
               
         if(MaxRisk < 5 ) {MaxRisk = 5;}
         
         if (AccountEquity()>AccountBalance()) {RCC=AccountBalance();} else {RCC=AccountEquity();}
         
         if (Trigger_Enable) {MR=MaxRisk/2;}  else  {MR=MaxRisk;}
         
                   
         if    (stepLot==0.01)  {Lots = (RF*NormalizeDouble(RCC * MR / 10000,1) / 10); LR=2;}
         else if(stepLot==0.1)  {Lots = (RF*NormalizeDouble(RCC * MR / 10000,0) / 10); LR=1;}
      }    
   
      
   
	if (LastCapital > AccountBalance() && LossManager > 0)
	  
	   {
	      if (LossManager == 1)  { Lots = Lots / 2; }
	      else if (LossManager > 1)  { Lots = Lots * LossManager; }
	   }
	   
//----

   if (MiL!=0 && Min_Lot<MiL) {Min_Lot=MiL;}
   if (MaL!=0 && Max_Lot>MaL) {Max_Lot=MaL;}
   if (MaL==0) {MaL=100;}
   if (MiL==0) {MiL=0.05;}
   if (Lots<Min_Lot) {Lots = Min_Lot;}
   if (Lots<MiL) {Lots = MiL;}
   if (lots<Min_Lot) {lots = Min_Lot;}
   if (lots<MiL) {lots = MiL;}  
   if (lots>Max_Lot) {lots=Max_Lot;}
//----

   HandleDigits();
   subPrintDetails();

//----         
      
 double adx=iADX(NULL,0,ADX_FilterPeriod,PRICE_CLOSE,MODE_MAIN,0+TS);
 double adx1=iADX(NULL,0,ADX_FilterPeriod,PRICE_CLOSE,MODE_MAIN,2+TS);    

       
 if(MyOrders<Max_Trades && adx>adx1 && !MaxSpreadFilter() && TradeSession())
 {
 
 double ADX_Dplus=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_PLUSDI,0+TS);
 double ADX_Dminus=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_MINUSDI,0+TS);
 double ADX_Dplus0=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_PLUSDI,1+TS);
 double ADX_Dminus0=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_MINUSDI,1+TS);
 double ADX_Dplus1=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_PLUSDI,2+TS);
 double ADX_Dminus1=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_MINUSDI,2+TS);
 
 
 double adx_Dplus=iADX(NULL,0,ADX_FilterPeriod,PRICE_CLOSE,MODE_PLUSDI,0+TS);
 double adx_Dminus=iADX(NULL,0,ADX_FilterPeriod,PRICE_CLOSE,MODE_MINUSDI,0+TS);
 double adx_Dplus1=iADX(NULL,0,ADX_FilterPeriod,PRICE_CLOSE,MODE_PLUSDI,2+TS);
 double adx_Dminus1=iADX(NULL,0,ADX_FilterPeriod,PRICE_CLOSE,MODE_MINUSDI,2+TS);

 
 double ADX=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_MAIN,0+TS);
 double ADX0=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_MAIN,1+TS);
 double ADX1=iADX(NULL,0,ADX_CrossPeriod,PRICE_CLOSE,MODE_MAIN,2+TS);

   
 double RVI=iRVI(NULL,0,RVI_FilterPeriod,MODE_MAIN,0+TS);
 double RVIs=iRVI(NULL,0,RVI_FilterPeriod,MODE_SIGNAL,0+TS);

 
 double CCI=iCCI(NULL,0,CCI_FilterPeriod,PRICE_CLOSE,0+TS);
 double CCI1=iCCI(NULL,0,CCI_FilterPeriod,PRICE_CLOSE,1+TS);

 
 double MAf=iMA(NULL,0,MA_TriggerFast,0,MODE_EMA,PRICE_CLOSE,0+TS);
 double MAs=iMA(NULL,0,MA_TriggerSlow,0,MODE_EMA,PRICE_CLOSE,0+TS);
 double MAs0=iMA(NULL,0,MA_TriggerSlow,0,MODE_EMA,PRICE_CLOSE,2+TS);
 double MAf0=iMA(NULL,0,MA_TriggerFast,0,MODE_EMA,PRICE_CLOSE,2+TS);

 
 double WPR=iWPR(NULL,0,WPR_Period,0+TS);
 double WPR1=iWPR(NULL,0,WPR_Period,1+TS);
 double WPR2=iWPR(NULL,0,WPR_Period,2+TS);
 double WPR3=iWPR(NULL,0,WPR_Period,3+TS);

 
    
      // check for long position (BUY) possibility
      if(!buy && ADX_Strategy && RVI>RVIs && CCI<(-1*CCI_Level) && RVI<0 && CCI<CCI1)
      {   
         if ((ADX_Dplus>ADX_Dminus && ADX_Dplus1<ADX_Dminus1 && ADX_Dplus>ADX_Dplus1 && ADX_Dminus1>ADX_Dminus) || 
      
         (ADX_Dplus>ADX && ADX_Dplus1<ADX1 && ADX_Dplus>ADX_Dplus1 && ADX_Dminus1>ADX_Dminus) || 
         
         (ADX_Dplus>ADX_Dminus && ADX_Dplus0<ADX_Dminus0 && ADX_Dplus>ADX_Dplus0 && ADX_Dminus0>ADX_Dminus) || 
         
         (ADX_Dplus>ADX && ADX_Dplus1<ADX0 && ADX_Dplus>ADX_Dplus0 && ADX_Dminus0>ADX_Dminus)) 
         
         {bActive++; sActive=0; ADXb++; MT=true;}
         
      }
      
      if(!buy && WPR_Strategy && adx<WPR_ADXmaxLevel && adx>WPR_ADXminLevel && RVI>RVIs && WPR<((-100)+WPR_BuyRange) && WPR>WPR1 && WPR1>WPR2 && WPR2>WPR3 &&
         adx_Dplus>adx_Dplus1 && adx_Dminus1>adx_Dminus)
      
      {  bActive++; sActive=0; WPRb++; MT=true;
         if (bActive>4 && ADXb==0) bActive=4;}
      
      
      
      
      
                  
      // check for short position (SELL) possibility
      if (!sell && ADX_Strategy && RVI<RVIs && CCI>CCI_Level && RVI>0 && CCI>CCI1)
      {
         if ((ADX_Dminus>ADX_Dplus && ADX_Dminus1<ADX_Dplus1 && ADX_Dminus>ADX_Dminus1 && ADX_Dplus1>ADX_Dplus) || 
      
         (ADX_Dminus>ADX && ADX_Dminus1<ADX1 && ADX_Dminus>ADX_Dminus1 && ADX_Dplus1>ADX_Dplus) || 
         
         (ADX_Dminus>ADX_Dplus && ADX_Dminus0<ADX_Dplus0 && ADX_Dminus>ADX_Dminus0 && ADX_Dplus0>ADX_Dplus) || 
         
         (ADX_Dminus>ADX && ADX_Dminus1<ADX0 && ADX_Dminus>ADX_Dminus0 && ADX_Dplus0>ADX_Dplus))
      
         {sActive++; bActive=0; ADXs++; MT=true;}
         
       }         
      
       if (!sell && WPR_Strategy && adx<WPR_ADXmaxLevel && adx>WPR_ADXminLevel && RVI<RVIs && WPR>(-1*WPR_SellRange) && WPR<WPR1 && WPR1<WPR2 &&  WPR2<WPR3 &&
           adx_Dplus<adx_Dplus1 && adx_Dminus1<adx_Dminus )
      
       {   sActive++; bActive=0; WPRs++; MT=true;
           if (sActive>4 && ADXs==0) sActive=4;}
     
     
      
      
      // Message Management
      if (WPRb==1 && Trigger_Enable) {Print("Wait for Long- Trigger (WPR)..."); WPRb++;}
      if (WPRs==1 && Trigger_Enable) {Print("Waiting for Sell- Trigger (WPR)..."); WPRs++;}
      if (ADXb==1 && Trigger_Enable) {Print("Wait for Long- Trigger (ADX-Cross)..."); ADXb++;}
      if (ADXs==1 && Trigger_Enable) {Print("Waiting for Sell- Trigger (ADX-Cross)..."); ADXs++;}
      
      
      
      
      
      // check trigger for long position (BUY) possibility
      
      //RefreshRates();
      
      if((!Trigger_Enable && bActive>0 && !buy) || 
        (MT && bActive>0 && MoreTrades && !MTb) ||
        ((!buy && bActive>0 && adx>ADX_FilterLevel && MAf>MAs && MAs>MAs0 && MAf>MAf0 && adx_Dplus>adx_Dminus && CCI<CCI1 && RVI>RVIs && RVI<0) && ((WPRb>0 && ADXb==0 && WPR<-50) || ADXb>0 || (WPRb>0 && ADXb>0))))
      
        {
         if ((AutoLot) && (bActive>1)) {TP=TP+((bActive-1)*5);}
         bActive=bActive/2;
         if (bActive>1.4) {bActive=2;}
         if ((bActive>0.1 && bActive<1) || (!AutoLot) || (MoreTrades && MT)) {bActive=1;}
         if (Lots*bActive>Max_Lot) {Lots=Max_Lot/bActive;}
                  
         if (LR==0) {LR=1;}
         
         ticket=OrderSend(Symbol(),OP_BUY,NormalizeDouble((Lots*bActive),LR),Ask,SlipPage,Ask-(SL*Points),Ask+(TP*Points),TradeComment,Identification,0,Green);
         LastCapital=AccountBalance();  
         
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
               {
                
                Print("BUY order opened : ",OrderOpenPrice());
                                 
                if (!MoreTrades || MTb) {buy=true; sell=false; bActive=0;}
                
                MT=false; MTb=true; MTs=false;
               }
                   
         else {Error=GetLastError(); Print("Error opening BUY order : ",Error); if(Error==4051) {Min_Lot=Min_Lot+MarketInfo(OrderSymbol(),MODE_LOTSTEP);}}
         return(0);
        }
      
                
      // check trigger for short position (SELL) possibility
      
      //RefreshRates();  
      
      if ((!Trigger_Enable && sActive>0 && !sell) || 
         (MT && sActive>0 && MoreTrades && !MTs) ||
         ((!sell && sActive>0 && adx>ADX_FilterLevel && MAs>MAf && MAs<MAs0 && MAf<MAf0 && adx_Dplus<adx_Dminus && CCI>CCI1 && RVI<RVIs && RVI>0) && ((WPRs>0 && ADXs==0 && WPR>-50) || ADXs>0 || (WPRs>0 && ADXs>0))))
            
        {
         if ((AutoLot) && (sActive>1)) {TP=TP+((sActive-1)*5);}
         sActive=sActive/2;
         if (sActive>1.4) {sActive=2;}
         if ((sActive>0.1 && sActive<1) || (!AutoLot) || (MoreTrades && MT)) {sActive=1;}
         if (Lots*sActive>Max_Lot) {Lots=Max_Lot/sActive;}
                  
         if (LR==0) {LR=1;}
         
         ticket=OrderSend(Symbol(),OP_SELL,NormalizeDouble((Lots*sActive),LR),Bid,SlipPage,Bid+(SL*Points),Bid-(TP*Points),TradeComment,Identification,0,Red);
         LastCapital=AccountBalance();  
                          
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
               {
                               
                Print("SELL order opened : ",OrderOpenPrice());
                                 
                if (!MoreTrades || MTs) {buy=false; sell=true; sActive=0;}
                
                MT=false; MTs=true; MTb=false;
               }
            
         else {Error=GetLastError(); Print("Error opening SELL order : ",Error); if(Error==4051) {Min_Lot=Min_Lot+MarketInfo(OrderSymbol(),MODE_LOTSTEP);}}
         return(0);
        }

   return(0);
  }
     
   // it is important to enter the market correctly, 
   // but it is more important to exit it correctly...
  
  else
   {
   buy=false; sell=false; MTb=false; MTs=false;
    
   if (sActive>1 && adx<ADX_FilterLevel && adx<adx1) {sActive=sActive/2; Print("Reduce Risk on Sell- Trigger by ADX-Filter...");}
   if (bActive>1 && adx<ADX_FilterLevel && adx<adx1) {bActive=bActive/2; Print("Reduce Risk on Buy- Trigger by ADX-Filter...");}
   }
   
   
   if (bActive==0) {WPRb=0; ADXb=0;}
   if (sActive==0) {WPRs=0; ADXs=0;} 
   
 MyOrders=0;
 RefreshRates();
  
 for(cnt=0;cnt<OrdersTotal();cnt++)
     {
     
     RefreshRates();
     
                   
      OMChk=OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      
      if(OrderSymbol()==Symbol() &&             // check for symbol
         OrderMagicNumber()==Identification )    // check the Magic Number
        {
        
        
         if(OrderType()==OP_BUY) // go to long position
           {
            if(OrderComment()==TradeComment) {MyOrders++;} //count open orders on this EA                                                  
                                              
                                          
           
            // check for trailing stop
            if(TrailingStop>0 && Trailing)
              {
               if((Bid-OrderOpenPrice()>(Points*TrailingStop)) )
                 {
                  if((OrderStopLoss()<(Bid-Points*(TrailingStop+TrailingStep)))) {OMChk=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Points*TrailingStop,OrderTakeProfit(),0,Green); return(0);}
                  
                 } 
                
              }
                       
           }
         if(OrderType()==OP_SELL) // go to short position
           {
            if(OrderComment()==TradeComment) {MyOrders++;} //count open orders on this EA                                                  
                                              
                                          
           
            // check for trailing stop
            if(TrailingStop>0 && Trailing)
              {
               if((OrderOpenPrice()-Ask>(Points*TrailingStop)) )
                 {
                  if((OrderStopLoss()>(Ask+Points*(TrailingStop+TrailingStep)))) {OMChk=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Points*TrailingStop,OrderTakeProfit(),0,Red); return(0);}
                  
                 }
                
              }
           
           }
        }
    }   
     
   return(0);
  }
    
// the end.