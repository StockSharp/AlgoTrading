//+------------------------------------------------------------------+
//|                                                MilleniumCode.mq4 |
//|                                        Tomas Hruby | Parbest.com |
//|                                            http://tomashruby.com |
//+------------------------------------------------------------------+
#property copyright "Tomas Hruby | Parbest.com"
#property link      "http://tomashruby.com" 
#property version   "1.1";
#property description "Millenium Code Expert Advisor is basic trading system developed by Tomas Hruby in cooperation with company Parbest - Trading Systems";
#property description "The strategy is based on a positional approach and opens only one trade per day";
#property description "This version is designed for public and does not contain all the features o riginal Millenium system which created a profit of +54% in summer 2014.";
#property description "Follow us on the web https://parbest.com and http://tomashruby.com.";
#property strict

/*MagicNumber  - Everytime Use three numbers / OrderMagicNumber() =  MagicNumber + DayOfYear() 
Lots10K - money management, lot size is adjusted by ratio to 10K deposit; 0 = disabled [ONLY IN Market version]
TrailingSL, TrailingTP - Trailing Stop Loss and Take Profit [ONLY IN Market version]
CloseByCloseTime - close order the same day when CloseHour and CloseMinute is reached
Reopen - false = stop reopening today closed trades (only one trade per day); true = when trade reach TP os SL the EA will open trade again [ONLY IN Market version]
MaxAllowedSpread - max spread size for opening new orders; in points: 50 = 5pips (with 5 digits brokers)
ATR_Bars - using ATR period for SL,TP and Trailing SL and Trailing TP. 0 = disabled.
TradeDuration - Trade will be closed after a specified number of hours (etc 12, 24, 72,...)
EAtimeframe  - Timeframe 0 = M1, 1 = M5,... 7 = W1, You can try all periods with step 1 from 0 to 7 when backtesting 
FastMA, SlowMA - Periods of crossed Simple MA (filter)
HighLowBars  - Finding High and Low X bars in history (filter)
FinishingMode - stop all new orders but still taking care of opened orders to their end
*/
extern string StrategyName="Millenium Code";
extern string CommentIdentification="Mill"; //Order Comment Word

extern int     MagicNumber    = 111; //EA Unique Magic Number
extern double  Lots           = 0.1; //Fixed Lot
extern double  SL=1100;   //Stop Loss
extern double  TP             = 400;   //Take Profit
extern bool    CloseByCloseTime=false; //Close Trade by Closing Time
extern bool    ReverseSignal  = true;  //Switch Indicator Signal
extern int     MaxAllowedSpread=40;    //Maximal Accepted Spread

extern string  TradingDays    = "-- Days to open a new trades --";
extern bool    Sunday         = true;
extern bool    Monday         = true;
extern bool    Tuesday        = true;
extern bool    Wednesday      = true;
extern bool    Thursday       = true;
extern bool    Friday         = true;

extern int     ATR_Bars       = 0; //ATR Period (for SL, TP and Trailings)
extern int     TradeDuration  = 0; //Close Trade after X hours

extern string  Indicator      = "-- Indicators Variables --";
extern int     EAtimeframe    = 1; //EA Timeframe 1 to 7 (1=M5,7=W1) 
extern int     ShiftMA        = 4; //MA Shift
extern int     FastMA         = 15;
extern int     SlowMA         = 14;
extern int     HighLowBars    = 10; //Bars sample for trend detection
extern int     HourOpen       = 16; //Starting hour (-1 = disabled)
extern int     MinuteOpen     = 5;
extern int     HourClose=17; //Ending Hour (-1 = disabled)
extern int     MinuteClose    = 55;
extern int     HourFridayClose= -1;
extern int     MinuteFridayClose=0;

extern string  OtherSettings;
extern bool    PrintMessages  = true;
extern bool    ShowComments   = true;
extern bool    FinishingMode=false; //Finishing Mode - Ignore new trades

                                    //Today handles
int      Today,dirToday;

//Control variables      
double   LastSLUpdate,StopLossDistance,TakeProfitDistance;
int      HistTradesCheck=10,//we need check last 10 trades for getting information about today realized trade
_OrdersTotalHandle=0,
first=0,//init handler
Timeframe;
bool     Opennew;
//Orders
double   _Ask,_Bid,StopLoss,TakeProfit,Price,Spread,_Lots;

int      i,r,u,e;

//Messages
string   Message,Comment1,Comment2,CommSL;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
   Print("Start. Server time hour:"+string(Hour())+" min:"+string(Minute())+" day:"+string(DayOfWeek()));

   if(IsOptimization())
     {
      ShowComments=false;   //always false if we optimizing
     }
   if(IsTesting() && !IsVisualMode())
     {
      ShowComments=false;   //always false if testing without visual mode
     }

   if(EAtimeframe==0)
     {
      Timeframe=PERIOD_M1;
        }else if(EAtimeframe == 1){ Timeframe = PERIOD_M5;
        }else if(EAtimeframe == 2){ Timeframe = PERIOD_M15;
        }else if(EAtimeframe == 3){ Timeframe = PERIOD_M30;
        }else if(EAtimeframe == 4){ Timeframe = PERIOD_H1;
        }else if(EAtimeframe == 5){ Timeframe = PERIOD_H4;
        }else if(EAtimeframe == 6){ Timeframe = PERIOD_D1;
        }else{                      Timeframe=PERIOD_W1;
     }
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   if(IsTesting() || IsOptimization())
     {
      //continue
        }else{
      if(!IsExpertEnabled())
        {
         Print("Expert disabled by user.");
         return(0);
        }
     }

   int ticket,total;
   Message= ""; Comment1 = ""; Comment2 = "";
   Spread = MathAbs(Ask-Bid);

   if(FinishingMode==true)
     {
      if(first==0) Print("--- TRADER "+StrategyName+" STARTED IN FINISHING MODE ("+CommentIdentification+") ---");
      if(ShowComments) Message=StringConcatenate("TRADER ",StrategyName," v",CommentIdentification," Magic ",MagicNumber,", FINISHING MODE\n");
        }else{
      if(first==0) Print("--- TRADER "+StrategyName+" ("+CommentIdentification+") STARTED ---");
      if(ShowComments) Message=StringConcatenate("TRADER ",StrategyName," ",CommentIdentification," Magic ",MagicNumber,"\n");
     }

   if(FinishingMode==true)
     {
      Opennew=false;    //stop opening new trades when FinishingMode enabled
        }else{
      Opennew=true;
     }

   total=OrdersTotal();
   if(total>0)
     {
      for(r=0;r<OrdersTotal();r++) //check actual opened orders for actions: Modifying SL, Closing actual or continue to open new trade
        {
         if(OrderSelect(r,SELECT_BY_POS,MODE_TRADES)==true)
           {
            if(MagicNumber==OrderMagicNumber())
              { //is it order of this EA?
               if(TradeDuration>0 || (HourClose>-1 && CloseByCloseTime)){  CloseByTime(); } //...or we close this order if it duration is reached (only if Lenght of trade is set) 

               if(OrderOpenTime()>=StrToTime(StringConcatenate(Year(),".",Month(),".",Day()," 00:00")))
                 {
                  Opennew=false;     //...we stop opening today trade
                 }
              }
           }
        }
     }

   if((AccountFreeMargin()-AccountCredit()>100))
     {
      if(AllowedDay(DayOfWeek())==false)
        {
         Comment("Today is not allowed for trading");
         return(0);
           }else{
         double Atr=0;
         dirToday=-1;
         if(Opennew==true)
           {
            //check indicators values only if is open time for buy or sell and get weights (processing time is smarter)
            if(OpenByTime()==1)
              {
               //double SlowMAprice = iMA(Symb, Timeframe, SMA, 0, 0, PRICE_MEDIAN, 0);
               double price=Ask -((Ask-Bid)/2);

               int    iH1 = iHighest(Symbol(),Timeframe,MODE_HIGH,HighLowBars,0);
               int    iL1 = iLowest(Symbol(),Timeframe,MODE_LOW,HighLowBars,0);
               double pH1 = iHigh(Symbol(),Timeframe,iH1);
               double pL1 = iLow(Symbol(),Timeframe,iL1);

               double  FastMAprice = iMA(Symbol(), Timeframe, FastMA, 0, 0, PRICE_CLOSE, 0);
               double yFastMAprice = iMA(Symbol(), Timeframe, FastMA, 0, 0, PRICE_CLOSE, ShiftMA);
               double  SlowMAprice = iMA(Symbol(), Timeframe, SlowMA, 0, 0, PRICE_CLOSE, 0);
               double ySlowMAprice = iMA(Symbol(), Timeframe, SlowMA, 0, 0, PRICE_CLOSE, ShiftMA);

               if(yFastMAprice<ySlowMAprice && FastMAprice>SlowMAprice
                  && Ask>SlowMAprice && Ask>FastMAprice
                  && pL1<SlowMAprice && pL1<FastMAprice)
                 {
                  if(ReverseSignal)
                    {
                     dirToday=OP_SELL;
                       }else{
                     dirToday=OP_BUY;
                    }

                 }
               //opposite
               if(yFastMAprice>ySlowMAprice && FastMAprice<SlowMAprice
                  && Bid<SlowMAprice && Bid<FastMAprice
                  && pH1>SlowMAprice && pH1>FastMAprice)
                 {
                  if(ReverseSignal)
                    {
                     dirToday=OP_BUY;
                       }else{
                     dirToday=OP_SELL;
                    }
                 }

               if(dirToday==-1)
                 {
                  Opennew=false;
                 }

                 }else{ //OpenByTime = 0
               Opennew=false;
              }
           }

         if(Spread>MaxAllowedSpread)
           {
            Opennew=false;
            Comment2=StringConcatenate("Spread is bigger than ",DBS(Spread/Point,0));
           }
         if(ShowComments)
           {
            Comment1 = StringConcatenate("Today: ",Symbol()," ",CmdPrint(dirToday),", ",HourOpen,":",MinuteOpen," to ",HourClose,":",MinuteClose,", Duration: ",TradeDuration);
            Comment2 = "";
            Message=StringConcatenate(Message,Comment1,"\n",Comment2,"\n",CommSL,"\n");
           }
         //right time for openning new Allowed trade?
         if(OpenByTime()==1 && Opennew==true)
           {
            //Check history if open rule is still valid and Reopen is off (because if reopen is true EA will jump over this control)
            if(OrdersHistoryTotal()>0)
              {
               if(OrdersHistoryTotal()<HistTradesCheck){ HistTradesCheck=OrdersHistoryTotal(); }//else HistTradesCheck = default 100
               for(u=(OrdersHistoryTotal()-HistTradesCheck);u<OrdersHistoryTotal();u++)
                 {
                  if(OrderSelect(u,SELECT_BY_POS,MODE_HISTORY))
                    {
                     if(MagicNumber==OrderMagicNumber())
                       {
                        if(OrderOpenTime()>StrToTime(StringConcatenate(Year(),".",Month(),".",Day()," 00:01")))
                          {
                           Opennew=false;
                          } //Dont open new trade if magic of this day was found in history yet. 
                       }
                    }//if select
                 }//eof for
              }//eof trades in history

            //All controls passed, lets go to open trade
            if(Opennew==true)
              {
               RefreshRates();
               string Comm="";

               if(ATR_Bars>0)
                 {
                  Atr=iATR(Symbol(),Timeframe,ATR_Bars,0);
                  StopLossDistance     = NormalizeDouble(SL*Atr,Digits);
                  TakeProfitDistance   = NormalizeDouble(TP*Atr,Digits);
                    }else{
                  StopLossDistance     = NormalizeDouble(SL*Point,Digits);
                  TakeProfitDistance   = NormalizeDouble(TP*Point,Digits);
                 }

               //resizing lots to actual size and check lots step       
               _Lots=LotsResize(Lots);

               //Buy
               if(dirToday==OP_BUY)
                 {
                  if(SL>0)
                    {
                     StopLoss=Bid-StopLossDistance;
                       }else{
                     StopLoss=0;
                    }

                  if(TP>0)
                    {
                     TakeProfit=Bid+TakeProfitDistance;
                       }else{
                     TakeProfit=0;
                    }

                  Comm=StringConcatenate(DBS(Atr/Point,0),";",DBS(Spread/Point,0),";",CommentIdentification,";",TradeDuration);

                  ticket=OrderSend(Symbol(),OP_BUY,_Lots,Ask,3,StopLoss,TakeProfit,Comm,MagicNumber,0,clrGreenYellow);
                  if(ticket>0)
                    {
                       {
                        if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                          {
                           PrintM("Opened BUY. Direction: "+CmdPrint(dirToday)+", SL distance: "+DBS(StopLossDistance,2)+"="+DBS(SL,2)+" of Atr "+DBS(Atr,Digits)+". Day: "+string(Today)+", DayOfWeek: "+string(DayOfWeek()));
                          }
                       }
                       }else{
                     PrintM("Error opening BUY order : "+string(GetLastError())+" "+Symbol()+" "+DBS(_Lots,2));
                     return(0);
                    }

                  //Sell
                    }else if(dirToday==OP_SELL){
                  if(SL>0)
                    {
                     StopLoss=Ask+StopLossDistance;
                       }else{
                     StopLoss=0;
                    }
                  if(TP>0)
                    {
                     TakeProfit=Ask-TakeProfitDistance;
                       }else{
                     TakeProfit=0;
                    }

                  Comm=StringConcatenate(DBS(Atr/Point,0),";",DBS(Spread/Point,0),";",CommentIdentification,";",TradeDuration);

                  ticket=OrderSend(Symbol(),OP_SELL,_Lots,Bid,3,StopLoss,TakeProfit,Comm,MagicNumber,0,clrHotPink);
                  if(ticket>0)
                    {
                       {
                        if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                          {
                           PrintM("Opened SELL. Direction: "+CmdPrint(dirToday)+", SL distance: "+DBS(StopLossDistance,2)+"="+DBS(SL,2)+" of Atr "+DBS(Atr,Digits)+". Day: "+string(Today)+", DayOfWeek: "+string(DayOfWeek()));
                          }
                       }
                       }else{
                     PrintM("Error opening SELL order : "+string(GetLastError())+" "+Symbol()+" "+DBS(_Lots,2));
                     return(0);
                    }
                 }//eof direction
               PrintM("No Direction. No trade today!");
              }//eof open new 
           }//eof open allowed direction and by allowed time

         if(ShowComments)
           {
            Comment(Message);
           }

         first=1;
         return(0);
        }//eof Allowed day
        }else{
      Print("Low margin "+DBS(AccountFreeMargin(),2));
     } //eof free margin
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit()
  {
   if(IsOptimization() || IsTesting())
     {
      Print("--END OF TESTING --");
        }else{
      Print("-- END --");
     }

   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OpenByTime()
  {
   bool HolidayStop=false;

   if((HourOpen)==-1)
     {
      return(1); //open everytime
        }else{
      //Christmas & New Year
      if((Month()==12 && Day()==24) || (Month()==12 && Day()==31))
        {
         HolidayStop=true;
        }

      //Open is possible only if actual hour is the same as from Timezone
      if(Hour()>=HourOpen && Minute()>=MinuteOpen && HolidayStop==false)
        {
         if((Hour())==HourOpen)
           {
            return(1);
              }else if(Hour()==HourClose && Minute()>=MinuteClose){
            return(0);
              }else if(Hour()>HourClose){
            return(0);
              }else{
            return(1);
           }
           }else{
         return(0);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseByTime()
  {
   bool   closeIt    = false;
   string closedBy   = "";

//Close by Duration in hours
   if(TradeDuration>0)
     {
      int durationMax=TradeDuration*3600; //minutes to the seconds
      datetime durationOrd=TimeCurrent()-OrderOpenTime();
      if(durationOrd>=durationMax)
        {
         closeIt=true;
         closedBy=StringConcatenate("TradeDuration ",DBS((durationMax/3600),0)," hours. ");
        }
     }

//Close by Close Time   
//Friday can be closed earlier so we can use special Close Hour
   if(HourClose>-1 && CloseByCloseTime)
     {
      if(Hour()==HourClose)
        {
         if(Minute()>=MinuteClose)
           {
            closeIt=true;
            closedBy=StringConcatenate("CloseByCloseTime: ",Hour(),":",Minute()," > ",HourClose,":",MinuteClose," ");
           }
           }else if(Hour()>HourClose){
         closeIt=true;
         closedBy=StringConcatenate("CloseByCloseTime: ",Hour()," > ",HourClose," ");
        }
      if(DayOfWeek()==5 && HourFridayClose>-1)
        {
         if(HourFridayClose>0 && CloseByCloseTime)
           {
            if(Hour()>=HourFridayClose && Minute()>=MinuteFridayClose)
              {
               closeIt = true;
               closedBy= StringConcatenate("CloseByCloseTime: Friday");
              }
           }
        }
     }

   if(closeIt==true)
     {
      if(OrderType()==OP_BUY)
        {
         if(OrderClose(OrderTicket(),OrderLots(),MarketInfo(Symbol(),MODE_BID),3,Red))
           {
            PrintM("Closed by "+closedBy+", Comment: "+OrderComment()+", magic: "+string(OrderMagicNumber())+", lots "+DBS(OrderLots(),2));
            LastSLUpdate=0;
           }
           }else if(OrderType()==OP_SELL){
         if(OrderClose(OrderTicket(),OrderLots(),MarketInfo(Symbol(),MODE_ASK),3,Red))
           {
            PrintM("Closed by "+closedBy+", Comment: "+OrderComment()+", magic: "+string(OrderMagicNumber())+", lots "+DBS(OrderLots(),2));
            LastSLUpdate=0;
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double LotsResize(double fLots)
  {
   double rLots=0;
   if(MarketInfo(Symbol(),MODE_LOTSTEP)==0.10)
     {
      rLots = MathRound( rLots/MarketInfo(Symbol(),MODE_LOTSTEP)  )  *  MarketInfo(Symbol(),MODE_LOTSTEP);
      rLots = NormalizeDouble(rLots,1);
     }

   if(rLots<MarketInfo(Symbol(),MODE_MINLOT))
     {
      rLots=MarketInfo(Symbol(),MODE_MINLOT);
        }else if(rLots>MarketInfo(Symbol(),MODE_MAXLOT)){
      rLots=MarketInfo(Symbol(),MODE_MAXLOT);
     }

   return (NormalizeDouble(rLots,2));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void PrintM(string fMessage)
  {
   if(PrintMessages==true)
     {
      Print(fMessage);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string DBS(double Num,int Nen)
  {
   return (DoubleToStr(Num,Nen));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string CmdPrint(int Cmd)
  {
   if(Cmd==0)
     {
      return ("BUY");
        }else if(Cmd==1){
      return ("SELL");
        }else{
      return("x");
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool AllowedDay(int fDayOfWeek)
  {
   bool Allowed=true;

   if(fDayOfWeek == 0 && Sunday == false) Allowed = false; //Sunday
   if(fDayOfWeek == 1 && Monday == false) Allowed = false; //Monday
   if(fDayOfWeek == 2 && Tuesday == false) Allowed = false;
   if(fDayOfWeek == 3 && Wednesday == false) Allowed = false;
   if(fDayOfWeek == 4 && Thursday == false) Allowed = false;
   if(fDayOfWeek == 5 && Friday == false) Allowed = false;
//if(fDayOfWeek == 6 && Monday == false) Allowed = false; //Saturday

   return(Allowed);
  }

/*     
int MakeMagic(int fToday){ //Magic = EEETDDD, E = EANumber, T = Day Of week, D = DayOf Year
   string EANumber = "";
   if(MagicNumber==0){
      EANumber = 999;
   }else{
      EANumber = MagicNumber;
   }
   return(  StrToInteger(  StringConcatenate(EANumber,fToday,DayOfYear())));
}*/
//+------------------------------------------------------------------+
