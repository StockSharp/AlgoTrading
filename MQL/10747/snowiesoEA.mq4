
//+------------------------------------------------------------------+
//|                                                        12425.mq4 |
//|                                          Copyright Shaun Overton |
//|                                           www.onestepremoved.com |
//+------------------------------------------------------------------+

#define DECIMAL_CONVERSION 10
#define COMMENT_DIGITS 2

// defines for evaluating entry conditions
#define LAST_BAR 1
#define THIS_BAR 0
#define NEGATIVE_VALUE -1

// defines for managing trade orders
#define RETRYCOUNT    10
#define RETRYDELAY    500

#define LONG          1
#define SHORT         -1
#define ALL           0

#define ORDER_COMMENT "EA Order"



#property copyright "Snowden, Programmed by OneStepRemoved.com"
#property link      "www.onestepremoved.com"

extern   double   Risk                                =  0.5;
extern   double   Lots                                =  1.0;
extern   int      Stop                                =  50;
extern   int      TakeProfit                          =  100;
extern   bool     Reverse                             =  false;
extern   string   MondayStartTime                     =  "00:00";
extern   string   MondayEndTime                       =  "23:59";
extern   string   TuesdayStartTime                    =  "00:00";
extern   string   TuesdayEndTime                      =  "23:59";
extern   string   WednesdayStartTime                  =  "00:00";
extern   string   WednesdayEndTime                    =  "23:59";
extern   string   ThursdayStartTime                   =  "00:00";
extern   string   ThursdayEndTime                     =  "23:59";
extern   string   FridayStartTime                     =  "00:00";
extern   string   FridayEndTime                       =  "23:59";

extern   string   MondayNoBreakStartTime              =  "00:00";
extern   string   FridayNoBreakEndTime                =  "17:00";
extern   bool     MonFriNoBreak                       =  true;
extern   bool     IntraBar                            =  false;
extern   int      MagicNumber                         =  0;
extern   bool     WriteScreenshots                    =  true;
extern   int      TrailStart                          =  50;
extern   int      TrailAmount                         =  10;
//AMA param
extern int       periodAMA=9;
extern int       nfast=2;
extern int       nslow=30;
extern double    G=2.0;
extern double    dK=2.0; 
//LWMA param
extern int FasterLWMA = 5;
extern int SlowerLWMA = 6;
extern bool soundAlert = true;
extern string soundFile = "lwma_crossover.wav";
extern bool textAlert = true;
//MACD param
extern int FastEMA=12;
extern int SlowEMA=26;
extern int SignalEMA=9;
//indicator checks
double AMABlue;
double AMAGold;
double MACDhisto;
double MACD;
double LWMA0up;
double LWMA1up;
double LWMA2up;
double LWMA0down;
double LWMA1down;
double LWMA2down;
bool bullish;
bool bearish;
bool IsLong=false;
bool IsShort=false;
bool OneTrade=false;
bool InTime=false;
datetime lastTradeTime;
datetime lastAlertTime;
datetime currentTime;
datetime once=NULL;
datetime CheckTime1=NULL;

extern int SlippageExit = 6;
extern int SlippageEnter =2;
string display = "";

double trailStart, trailAmount;

int decimalConv = 1;
int minSLTPdstnc;


int init()
  {
      if (Digits == 3 || Digits == 5)   {
         Stop        *=    DECIMAL_CONVERSION;
         TakeProfit  *=    DECIMAL_CONVERSION;
         TrailStart  *=    DECIMAL_CONVERSION;
         TrailAmount *=    DECIMAL_CONVERSION;
         
         SlippageExit    *=    DECIMAL_CONVERSION;
         SlippageEnter    *=    DECIMAL_CONVERSION;
         decimalConv =     DECIMAL_CONVERSION;
      }
      
      trailStart     =     TrailStart  *  Point;
      trailAmount    =     TrailAmount *  Point;

      // get miniumum stop/take profit distance
      minSLTPdstnc = MarketInfo(Symbol(), MODE_STOPLEVEL);
      Print("Broker: " + AccountCompany());
      Comment("Loading... awaiting new tick");

   return(0);
  }

int deinit()  {  ExitAll(SHORT); ExitAll(LONG); return(0);  }

int start()
{
   Comment("");
   TrailStop(MagicNumber,trailStart,trailAmount);
   checkopentrades();
   //risk %
   if(Risk!=0.0)
      Lots = (Risk * AccountEquity()) / MarketInfo(Symbol(), MODE_MARGINREQUIRED);  
   //check enter
   InTime=false;
   if(TimeDayOfWeek(Time[0])>=1&&TimeDayOfWeek(Time[0])<=5)
   {
      InTime=true;
      CheckTimes();
      if(InTime)
      {
        if(IntraBar)
        {
            AssignIndicators(THIS_BAR);
             if(((LWMA0up != EMPTY_VALUE && LWMA0up != 0.0)||(LWMA1up != EMPTY_VALUE && LWMA1up != 0.0)||(LWMA2up != EMPTY_VALUE && LWMA2up != 0.0)) &&
                 MACDhisto>0 &&
                (AMABlue != EMPTY_VALUE && AMABlue != 0.0)&&
                !OneTrade)
            {
              if (currentTime != Time[0])
                  {
                  currentTime = Time[0];
                  if(Reverse)
                     {
                     bearish = DoTrade(SHORT, Lots, Stop,TakeProfit ,"");
                     bullish = false;
                     IsShort=true;
                     OneTrade=true;
                     }
                  else
                     {
                     bullish = DoTrade(LONG, Lots, Stop,TakeProfit ,"");
                     bearish = false;
                     OneTrade=true;
                     IsLong=true;
                     }
                  }
            }
            if(((LWMA0down != EMPTY_VALUE && LWMA0down != 0.0)||(LWMA1down != EMPTY_VALUE && LWMA1down != 0.0)||(LWMA2down != EMPTY_VALUE && LWMA2down != 0.0)) &&
                 MACDhisto<0 &&
                (AMAGold != EMPTY_VALUE && AMAGold != 0.0)&&
                !OneTrade)
            {
               if (currentTime != Time[0])
                  {
                  currentTime = Time[0];
                  if(Reverse)
                     {
                     bullish = DoTrade(LONG, Lots, Stop,TakeProfit ,"");
                     bearish = false;
                     OneTrade=true;
                     IsLong=true;
                     }
                  else
                     {
                     bearish = DoTrade(SHORT, Lots, Stop,TakeProfit ,"");
                     bullish = false;
                     IsShort=true;
                     OneTrade=true;
                     }
                  }    
             }
        }
        else
        {
            AssignIndicators(LAST_BAR);
             if(((LWMA0up != EMPTY_VALUE )||(LWMA1up != EMPTY_VALUE )) &&
                 MACDhisto>0 &&
                (AMABlue != EMPTY_VALUE && AMABlue != 0.0)&&
                !OneTrade)
            {
                  if(currentTime!=Time[0])
                  {
                     currentTime=Time[0];
                     if(Reverse)
                        {
                        bearish = DoTrade(SHORT, Lots, Stop,TakeProfit ,"");
                        bullish = false;
                        IsShort=true;
                        OneTrade=true;
                        }
                     else
                        {
                        bullish =  DoTrade(LONG, Lots, Stop,TakeProfit ,"");
                        bearish = false;
                        OneTrade= true;
                        IsLong=   true;
                        }
                  }
            }
            if(((LWMA0down != EMPTY_VALUE && LWMA0down != 0.0)||(LWMA1down != EMPTY_VALUE && LWMA1down != 0.0)) &&
                 MACDhisto<0 &&
                (AMAGold != EMPTY_VALUE && AMAGold != 0.0)&&
                !OneTrade)
            {
               if(currentTime!=Time[0])
               {
                  currentTime=Time[0];
                  if(Reverse)
                     {
                     bullish =  DoTrade(LONG, Lots, Stop,TakeProfit ,"");
                     bearish = false;
                     OneTrade= true;
                     IsLong=   true;
                     }
                  else
                     {
                     bearish = DoTrade(SHORT, Lots, Stop,TakeProfit ,"");
                     bullish = false;
                     IsShort=true;
                     OneTrade=true;
                     }
               }
            }
        }
      }
      else
      {
       ExitAll(SHORT); 
       ExitAll(LONG);
       OneTrade=false;
       IsLong=false;
       IsShort=false;
      }
          
     }//end check time of week
       //check exits
      if(IntraBar)
      {
         AssignIndicators(THIS_BAR);
      }
      else  
      {
         AssignIndicators(LAST_BAR); 
         LWMA1down=LWMA0down;
         LWMA1up=LWMA0up;
      }
      if( (MACDhisto<=0) && currentTime!=Time[0])
       {
         if(Reverse&&IsShort)
         {
            if(IntraBar)
            {
               ExitAll(SHORT);
               OneTrade=false;
               IsShort=false;
            }
            else 
            {
               ExitAll(SHORT);
               OneTrade=false;
               IsShort=false;
            }
         }
         else if(!Reverse&&IsLong)
         {
           if(IntraBar)
           {
              ExitAll(LONG);
              OneTrade=false;
              IsLong=false;
           }
           else 
           {
              ExitAll(LONG);
              OneTrade=false;
              IsLong=false;
           }
         }
       }
       if( (MACDhisto>=0) && currentTime!=Time[0])
       {
         if(Reverse&&IsLong)
         {
           if(IntraBar)
           {
              ExitAll(LONG);
              OneTrade=false;
              IsLong=false;
           }
           else 
           {
              ExitAll(LONG);
              OneTrade=false;
              IsLong=false;
           }
         }
         else if(!Reverse&&IsShort)
         {
            if(IntraBar)
            {
               ExitAll(SHORT);
               OneTrade=false;
               IsShort=false;
            }
            else 
            {
               ExitAll(SHORT);
               OneTrade=false;
               IsShort=false;
            }
         }
       }
   return(0);
}
void checkopentrades()
{
   bool notrades=true;
   for(int i = OrdersTotal() - 1; i >= 0; i-- ) 
      {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if( OrderMagicNumber() == MagicNumber || OrderSymbol() == Symbol() ) {notrades=false;}
      }
      if(notrades)
      {
         OneTrade=false;
         IsShort=false;
         IsLong=false;
      }
}
void AssignIndicators(int bar)
{
   AMABlue  =iCustom( Symbol(), Period(), "AMA", periodAMA, nfast, nslow, G, dK,1, bar);  
   AMAGold  =iCustom( Symbol(), Period(), "AMA", periodAMA, nfast, nslow, G, dK,2, bar); 
   MACDhisto=iCustom( Symbol(), Period(), "FX-Bay_MACD",FastEMA, SlowEMA, SignalEMA,2,bar);
   MACD     =iCustom( Symbol(), Period(), "FX-Bay_MACD",FastEMA, SlowEMA, SignalEMA,0,bar);
   LWMA0up  =iCustom( Symbol(), Period(), "LWMA", FasterLWMA, SlowerLWMA, soundAlert, soundFile, textAlert,0,bar);
   LWMA1up  =iCustom( Symbol(), Period(), "LWMA", FasterLWMA, SlowerLWMA, soundAlert, soundFile, textAlert,0,bar+1);
   LWMA2up  =iCustom( Symbol(), Period(), "LWMA", FasterLWMA, SlowerLWMA, soundAlert, soundFile, textAlert,0,bar+2);
   LWMA0down=iCustom( Symbol(), Period(), "LWMA", FasterLWMA, SlowerLWMA, soundAlert, soundFile, textAlert,1,bar);
   LWMA1down=iCustom( Symbol(), Period(), "LWMA", FasterLWMA, SlowerLWMA, soundAlert, soundFile, textAlert,1,bar+1);
   LWMA2down=iCustom( Symbol(), Period(), "LWMA", FasterLWMA, SlowerLWMA, soundAlert, soundFile, textAlert,1,bar+2);
}


bool DoTrade(int dir, double volume, int stop, int take, string comment)  {

   double sl, tp;

   bool retVal = false;

   switch(dir)  {
      case LONG:
         if (stop != 0) { sl = (stop*Point); }
	 else { sl = 0; }
	 if (take != 0) { tp = (take*Point); }
	 else { tp = 0; }

	 retVal = OpenTrade(LONG, volume, sl, tp, comment);
	 break;

      case SHORT:
         if (stop != 0) { sl = (stop*Point); }
	 else { sl = 0; }
	 if (take != 0) { tp = (take*Point); }
	 else { tp = 0; }

	 retVal = OpenTrade(SHORT, volume, sl, tp, comment);
	 break;

   }

return(retVal);

}

void CheckTimes()
{
   if(MonFriNoBreak)
      {
         if(TimeDayOfWeek(Time[0])==1) 
         {
            if(TimeToStr(TimeCurrent(),TIME_MINUTES)=="23:59")
               InTime=true;
            else
               InTime= CheckTime(MondayNoBreakStartTime,"23:59"); 
         }
         else if(TimeDayOfWeek(Time[0])==5)
         {
            if(TimeToStr(TimeCurrent(),TIME_MINUTES)=="00:00")
               InTime=true;
            else
                InTime=CheckTime("00:00",FridayNoBreakEndTime);
         }
      }
      else 
      {
         switch(TimeDayOfWeek(Time[0]))
         {
         case 1:InTime=CheckTime(MondayStartTime,MondayEndTime);
                if(TimeToStr(TimeCurrent(),TIME_MINUTES)=="23:59" && MondayEndTime=="23:59")
                     InTime=true;
                if(MondayStartTime==MondayEndTime)
                     InTime=false;
                break;
         case 2:InTime=CheckTime(TuesdayStartTime,TuesdayEndTime);
                if( (TimeToStr(TimeCurrent(),TIME_MINUTES)=="23:59" && TuesdayEndTime=="23:59") || (TimeToStr(TimeCurrent(),TIME_MINUTES)=="00:00" && TuesdayStartTime == "00:00") )
                     InTime=true;
                if(MondayStartTime==TuesdayEndTime)
                     InTime=false;
                break;
         case 3:InTime=CheckTime(WednesdayStartTime,WednesdayEndTime);
                if(TimeToStr(TimeCurrent(),TIME_MINUTES)=="23:59" && WednesdayEndTime=="23:59" || TimeToStr(TimeCurrent(),TIME_MINUTES)=="00:00" && WednesdayStartTime == "00:00")
                     InTime=true;
                if(WednesdayStartTime==WednesdayEndTime)
                     InTime=false;
                break;
         case 4:InTime=CheckTime(ThursdayStartTime,ThursdayEndTime);
                if(TimeToStr(TimeCurrent(),TIME_MINUTES)=="23:59" && ThursdayEndTime=="23:59" || TimeToStr(TimeCurrent(),TIME_MINUTES)=="00:00" && ThursdayStartTime == "00:00")
                     InTime=true;
                if(ThursdayStartTime==ThursdayEndTime)
                     InTime=false;
                break;
         case 5:InTime=CheckTime(FridayStartTime,FridayEndTime);
                if(TimeToStr(TimeCurrent(),TIME_MINUTES)=="00:00" && FridayStartTime=="00:00")
                     InTime=true;
                if(FridayStartTime==FridayEndTime)
                     InTime=false;
                break;
         default:InTime=false;
         }
         if(!InTime)
         Alert("false "+ TimeToStr(TimeCurrent(),TIME_MINUTES) );
        
      }
}

bool OpenTrade(int dir, double volume, double stop, double take, string comment, int t = 0)  
{
    int i, j, ticket, cmd;
    double prc, sl, tp, lots;
    string cmt;
    color clr;

    Print("OpenTrade("+dir+","+DoubleToStr(volume,3)+","+DoubleToStr(stop,Digits)+","+DoubleToStr(take,Digits)+","+t+")");

    lots = CheckLots(volume);
    

    for (i=0; i<RETRYCOUNT; i++) 
    {
        for (j=0; (j<50) && IsTradeContextBusy(); j++)
            Sleep(100);
        RefreshRates();

        if (dir == LONG) 
        {
            cmd = OP_BUY;
            prc = Ask;
            sl = stop;
            tp = take;
         clr = Blue;
        }
        if (dir == SHORT) 
        {
            cmd = OP_SELL;
            prc = Bid;
            sl = stop;
            tp = take;
         clr = Red;
        }
        Print("OpenTrade: prc="+DoubleToStr(prc,Digits)+" sl="+DoubleToStr(sl,Digits)+" tp="+DoubleToStr(tp,Digits));

        cmt = comment;
        if (t > 0)
            cmt = comment + "|" + t;

        ticket = OrderSend(Symbol(), cmd, lots, prc, SlippageEnter, 0, 0, cmt, MagicNumber,0,clr);
        if (ticket != -1)
        {
         if( sl > 0 || tp > 0)
         {
            Print("OpenTrade: opened ticket " + ticket);
            Screenshot("OpenTrade");

            // Assign Stop Loss and Take Profit to order            
            AssignSLTP(ticket, dir, stop, take);

            return (true);
        }
        else
        { 
            if( ticket > 0 ) 
            {
               Screenshot("OpenTrade_SLTP");
               return( true );
            }
         }   // end if( sl > 0 || tp > 0)
        }   // end if (ticket != -1)
        else if (ticket == -1)
        {// look for any open trades that are missing a stop loss and take profit
         if (FindTradesMissingSLTP(dir, stop, take, comment))
         {
             Screenshot("OpenTrade_SLTP");
             return( true );
         }
        }
        
        Print("OpenTrade: error \'"+ErrorDescription(GetLastError())+"\' when entering with "+DoubleToStr(lots,3)+" @"+DoubleToStr(prc,Digits));
        Sleep(RETRYDELAY);
        
    } // end for (i=0; i<RETRYCOUNT; i++) 

    Print("OpenTrade: can\'t enter after "+RETRYCOUNT+" retries");
    return (false);
}

//-----------------------------------------------------------------------------------+
// This routine calculates a Take Profit price and checks it against the minimum stop 
//  loss take profit distance returned by MarketInfo() for the currency pair.
//-----------------------------------------------------------------------------------+
double GetTakeProfit(int dir, double openPrc, double take)
{
   double tp = 0;


   // Calculate Take Profit Price.
   //-----------------------------
   
   if (dir == LONG)
   {
      tp = NormalizeDouble(openPrc + take, Digits);
      
      RefreshRates();
      
      if ((tp - Bid) < minSLTPdstnc*Point)
      {
         tp = NormalizeDouble(Bid + minSLTPdstnc*Point, Digits);
         if (lastAlertTime != Time[0])
         {
            Print("Min. Stop Loss Take Profit distance = ", minSLTPdstnc);
            Alert("GetTakeProfit() - Specified TP is TOO CLOSE to current price!\nMinimum allowed TP distance for this currency of (",tp,") used instead");
            lastAlertTime = Time[0];
         }
      }
   }
   else if (dir == SHORT)
   {
      tp = NormalizeDouble(openPrc - take, Digits);
      
      RefreshRates();
      
      if ((Ask - tp) < minSLTPdstnc*Point)
      {
         tp = NormalizeDouble(Ask - minSLTPdstnc*Point, Digits);
         if (lastAlertTime != Time[0])
         {
            Print("Min. Stop Loss Take Profit distance = ", minSLTPdstnc);
            Alert("GetTakeProfit() - Specified TP is TOO CLOSE to current price!\nMinimum allowed TP distance for this currency of (",tp,") used instead");
            lastAlertTime = Time[0];
         }
      }
   }
   
   return(tp);
}

//-----------------------------------------------------------------------------------+
// This routine calculates a Stop Loss price and checks it against the minimum stop 
//  loss take profit distance returned by MarketInfo() for the currency pair.
//-----------------------------------------------------------------------------------+
double GetStopLoss(int dir, double openPrc, double stop)
{
   double sl = 0;


   // Calculate Take Profit Price.
   //-----------------------------
   
   if (dir == LONG)
   {
      sl = NormalizeDouble(openPrc - stop, Digits);
      
      RefreshRates();
      
      if ((Bid - sl) < minSLTPdstnc*Point)
      {
         sl = NormalizeDouble(Bid - minSLTPdstnc*Point, Digits);
         if (lastAlertTime != Time[0])
         {
            Print("Min. Stop Loss Take Profit distance = ", minSLTPdstnc);
            Alert("GetStopLoss() - Specified SL is TOO CLOSE to current price!\nMinimum allowed SL/TP distance for this currency of (",sl,") used instead");
            lastAlertTime = Time[0];
         }
      }
   }
   else if (dir == SHORT)
   {
      sl = NormalizeDouble(openPrc + stop, Digits);
      
      RefreshRates();
      
      if ((sl - Ask) < minSLTPdstnc*Point)
      {
         sl = NormalizeDouble(Ask + minSLTPdstnc*Point, Digits);
         if (lastAlertTime != Time[0])
         {
            Print("Min. Stop Loss Take Profit distance = ", minSLTPdstnc);
            Alert("GetStopLoss() - Specified SL is TOO CLOSE to current price!\nMinimum allowed SL/TP distance for this currency of (",sl,") used instead");
            lastAlertTime = Time[0];
         }
      }
   }
   
   return(sl);
}

//----------------------------------------------------------------+
// routine makes RETRYCOUNT attempts to "modify" the Stop Loss and 
//  Take Profit for the specified ticket/order
//----------------------------------------------------------------+

bool AssignSLTP(int tkt, int dir, double stop, double take)
{
   int ix, ix2;
   double tp = take;
   double sl = stop;
   bool modified = false;
   

   // Select specified ticket.
   OrderSelect(tkt, SELECT_BY_TICKET, MODE_TRADES);

   // Make several attempts   

   ix = 0;
   while(!modified && ix < RETRYCOUNT)   
   {
      for (ix2 = 0; (ix2 < 50) && IsTradeContextBusy(); ix2++)
         Sleep(100);
      RefreshRates();
      
      if(dir == LONG)   // LONG order
      {
         // set take profit
         if( tp != 0 ) 
            tp = GetTakeProfit(dir, OrderOpenPrice(), take);
	      else 
	         tp = 0;
	      
	      // set stop loss	     
         if( sl != 0 ) 
            sl = GetStopLoss(dir, OrderOpenPrice(), stop);
	      else 
	         sl = 0;

         // attempt to set stop loss and take profit
         if ( OrderModify(tkt, 0, sl, tp, 0) ) 
         {
            Print("OpenTrade: SL/TP are set LONG");
            Screenshot("OpenTrade_SLTP");
            modified = true;
         }
         else
         {
            Print("AssignSLTP: error \'"+ErrorDescription(GetLastError())+"\' when setting SL/TP");
            Sleep(RETRYDELAY);
         }
      }
      
      else if( dir == SHORT )    // SHORT order
      {
         // set take profit
         if( tp != 0 ) 
            tp = GetTakeProfit(dir, OrderOpenPrice(), take);
	      else 
	         tp = 0;
	      
	      // set stop loss	     
         if( sl != 0 ) 
            sl = GetStopLoss(dir, OrderOpenPrice(), stop);
	      else 
	         sl = 0;

         // attempt to set stop loss and take profit
         if ( OrderModify(tkt, 0, sl,tp, 0) ) 
         {
            Print("OpenTrade: SL/TP are set SHORT");
            Screenshot("OpenTrade_SLTP");
            modified = true;
         }                
         else
         {
            Print("AssignSLTP: error \'"+ErrorDescription(GetLastError())+"\' when setting SL/TP");
            Sleep(RETRYDELAY);
         }
      }
      
      ix++;    // increment loop counter

   }  // end while(!modified && ix < RETRYCOUNT)   
   
   return(modified);
}

//--------------------------------------------------------------+
// routine checks for open trades placed by the EA this time bar 
//  that have Stop Loss and Take Profit values of zero.  It then
//  reattempts to assign the stop loss or take profit.  If 
//  successful it returns true, otherwise false.
//--------------------------------------------------------------+

bool FindTradesMissingSLTP(int dir, double stop, double take, string comment)
{
   bool result = true;
   
   
   for (int ix = OrdersTotal()-1; ix >= 0; ix--) 
   {
      OrderSelect(ix, SELECT_BY_POS, MODE_TRADES);
      
      // look for one of the EAs trades
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber && OrderComment() == comment) 
      {
         // if the trade has neither stop loss or take profit attempt to assign one
         if (OrderStopLoss() <= 0 && OrderTakeProfit() <= 0)
            if (!AssignSLTP(OrderTicket(), dir, stop, take))
               result = false;   // record whether AssignSLTP ever fails
      }
      
   }  // end for (int ix = OrdersTotal()-1; ix >= 0; ix--)
   
   // return result of our attempt
   return (result);
}

string ConvertBoolToStr(bool value)
{
   if (value) { return("True"); }
   else { return("False"); }
}  

double CheckLots(double lots)
{
    double lot, lotmin, lotmax, lotstep, margin;
    
    lotmin = MarketInfo(Symbol(), MODE_MINLOT);
    lotmax = MarketInfo(Symbol(), MODE_MAXLOT);
    lotstep = MarketInfo(Symbol(), MODE_LOTSTEP);
    margin = MarketInfo(Symbol(), MODE_MARGINREQUIRED);
    
    if (lots*margin > AccountFreeMargin())
        lots = AccountFreeMargin() / margin;

    lot = MathFloor(lots/lotstep + 0.5) * lotstep;

    if (lot < lotmin)
        lot = lotmin;
    if (lot > lotmax)
        lot = lotmax;

    return (lot);
}


void Screenshot(string moment_name)
{
    if ( WriteScreenshots )
        WindowScreenShot(WindowExpertName()+"_"+Symbol()+"_M"+Period()+"_"+
                         Year()+"-"+two_digits(Month())+"-"+two_digits(Day())+"_"+
                         two_digits(Hour())+"-"+two_digits(Minute())+"-"+two_digits(Seconds())+"_"+
                         moment_name+".gif", 1024, 768);
}

string two_digits(int i)
{
    if (i < 10)
        return ("0"+i);
    else
        return (""+i);
}



bool Exit(int ticket, int dir, double volume, color clr, int t = 0)  {
    int i, j, cmd;
    double prc, sl, tp, lots;
    string cmt;

    bool closed;

    Print("Exit("+dir+","+DoubleToStr(volume,3)+","+t+")");

    for (i=0; i<RETRYCOUNT; i++) {
        for (j=0; (j<50) && IsTradeContextBusy(); j++)
            Sleep(100);
        RefreshRates();

        if (dir == LONG) {
            prc = Bid;
        }
        if (dir == SHORT) {
            prc = Ask;
       }
        Print("Exit: prc="+DoubleToStr(prc,Digits));
        closed = OrderClose(ticket,volume,prc,SlippageExit,clr);
        if (closed) {
            Print("Trade closed");
            Screenshot("Exit");
            return (true);
        }
        OrderSelect(ticket,SELECT_BY_TICKET ,MODE_TRADES);
        Print("OrderType()= "+OrderType()+"Dir= "+dir+"OrderLots()= "+OrderLots()+"Slippage= "+SlippageExit);
        Print("Exit: error \'"+ErrorDescription(GetLastError())+"\' when exiting with "+DoubleToStr(volume,3)+" @"+DoubleToStr(prc,Digits));
        Sleep(RETRYDELAY);
    }

    Print("Exit: can\'t exit after "+RETRYCOUNT+" retries");
    return (false);
}


string ErrorDescription(int error_code)
{
    string error_string;

    switch( error_code ) {
        case 0:
        case 1:   error_string="no error";                                                  break;
        case 2:   error_string="common error";                                              break;
        case 3:   error_string="invalid trade parameters";                                  break;
        case 4:   error_string="trade server is busy";                                      break;
        case 5:   error_string="old version of the client terminal";                        break;
        case 6:   error_string="no connection with trade server";                           break;
        case 7:   error_string="not enough rights";                                         break;
        case 8:   error_string="too frequent requests";                                     break;
        case 9:   error_string="malfunctional trade operation (never returned error)";      break;
        case 64:  error_string="account disabled";                                          break;
        case 65:  error_string="invalid account";                                           break;
        case 128: error_string="trade timeout";                                             break;
        case 129: error_string="invalid price";                                             break;
        case 130: error_string="invalid stops";                                             break;
        case 131: error_string="invalid trade volume";                                      break;
        case 132: error_string="market is closed";                                          break;
        case 133: error_string="trade is disabled";                                         break;
        case 134: error_string="not enough money";                                          break;
        case 135: error_string="price changed";                                             break;
        case 136: error_string="off quotes";                                                break;
        case 137: error_string="broker is busy (never returned error)";                     break;
        case 138: error_string="requote";                                                   break;
        case 139: error_string="order is locked";                                           break;
        case 140: error_string="long positions only allowed";                               break;
        case 141: error_string="too many requests";                                         break;
        case 145: error_string="modification denied because order too close to market";     break;
        case 146: error_string="trade context is busy";                                     break;
        case 147: error_string="expirations are denied by broker";                          break;
        case 148: error_string="amount of open and pending orders has reached the limit";   break;
        case 149: error_string="hedging is prohibited";                                     break;
        case 150: error_string="prohibited by FIFO rules";                                  break;
        case 4000: error_string="no error (never generated code)";                          break;
        case 4001: error_string="wrong function pointer";                                   break;
        case 4002: error_string="array index is out of range";                              break;
        case 4003: error_string="no memory for function call stack";                        break;
        case 4004: error_string="recursive stack overflow";                                 break;
        case 4005: error_string="not enough stack for parameter";                           break;
        case 4006: error_string="no memory for parameter string";                           break;
        case 4007: error_string="no memory for temp string";                                break;
        case 4008: error_string="not initialized string";                                   break;
        case 4009: error_string="not initialized string in array";                          break;
        case 4010: error_string="no memory for array\' string";                             break;
        case 4011: error_string="too long string";                                          break;
        case 4012: error_string="remainder from zero divide";                               break;
        case 4013: error_string="zero divide";                                              break;
        case 4014: error_string="unknown command";                                          break;
        case 4015: error_string="wrong jump (never generated error)";                       break;
        case 4016: error_string="not initialized array";                                    break;
        case 4017: error_string="dll calls are not allowed";                                break;
        case 4018: error_string="cannot load library";                                      break;
        case 4019: error_string="cannot call function";                                     break;
        case 4020: error_string="expert function calls are not allowed";                    break;
        case 4021: error_string="not enough memory for temp string returned from function"; break;
        case 4022: error_string="system is busy (never generated error)";                   break;
        case 4050: error_string="invalid function parameters count";                        break;
        case 4051: error_string="invalid function parameter value";                         break;
        case 4052: error_string="string function internal error";                           break;
        case 4053: error_string="some array error";                                         break;
        case 4054: error_string="incorrect series array using";                             break;
        case 4055: error_string="custom indicator error";                                   break;
        case 4056: error_string="arrays are incompatible";                                  break;
        case 4057: error_string="global variables processing error";                        break;
        case 4058: error_string="global variable not found";                                break;
        case 4059: error_string="function is not allowed in testing mode";                  break;
        case 4060: error_string="function is not confirmed";                                break;
        case 4061: error_string="send mail error";                                          break;
        case 4062: error_string="string parameter expected";                                break;
        case 4063: error_string="integer parameter expected";                               break;
        case 4064: error_string="double parameter expected";                                break;
        case 4065: error_string="array as parameter expected";                              break;
        case 4066: error_string="requested history data in update state";                   break;
        case 4099: error_string="end of file";                                              break;
        case 4100: error_string="some file error";                                          break;
        case 4101: error_string="wrong file name";                                          break;
        case 4102: error_string="too many opened files";                                    break;
        case 4103: error_string="cannot open file";                                         break;
        case 4104: error_string="incompatible access to a file";                            break;
        case 4105: error_string="no order selected";                                        break;
        case 4106: error_string="unknown symbol";                                           break;
        case 4107: error_string="invalid price parameter for trade function";               break;
        case 4108: error_string="invalid ticket";                                           break;
        case 4109: error_string="trade is not allowed in the expert properties";            break;
        case 4110: error_string="longs are not allowed in the expert properties";           break;
        case 4111: error_string="shorts are not allowed in the expert properties";          break;
        case 4200: error_string="object is already exist";                                  break;
        case 4201: error_string="unknown object property";                                  break;
        case 4202: error_string="object is not exist";                                      break;
        case 4203: error_string="unknown object type";                                      break;
        case 4204: error_string="no object name";                                           break;
        case 4205: error_string="object coordinates error";                                 break;
        case 4206: error_string="no specified subwindow";                                   break;
        default:   error_string="unknown error";
    }

    return(error_string);
}

void ExitAll(int direction) {

   for (int i = OrdersTotal()-1; i >= 0; i--) {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber) {
         if (OrderType() == OP_BUY && ( direction == LONG || direction == ALL) ) { Exit(OrderTicket(), LONG, OrderLots(), Blue); }
         if (OrderType() == OP_SELL && ( direction == SHORT || direction == ALL) ) { Exit( OrderTicket(), SHORT, OrderLots(), Red); }
      }
   }
}

bool CheckTime(string start, string end) {

   string today = TimeToStr( iTime(Symbol(),PERIOD_D1, 0 ), TIME_DATE ) + " ";
   
   datetime startTime = StrToTime( today + start);
   datetime endTime = StrToTime( today + end);   
  
   if(startTime <= endTime) {
      if(TimeCurrent() > startTime && TimeCurrent() < endTime) { return(true); }
   }
  
   if( startTime >= endTime ) {
      if( TimeCurrent() > startTime ) { return(true); }
      if( TimeCurrent() < endTime ) { return(true); }
   }
  
   if( startTime == endTime) {
      Comment("***** The Start Time cannot equal the End Time ******* ");
   }
  
   return(false);
}

bool TrailStop(int magic, double trailStart, double trailAmount) {

   double profitPips, increments, sl;
   double chgFromOpen;


   for(int i = 0; i < OrdersTotal(); i++)
   {
      OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if (OrderMagicNumber() == magic && OrderSymbol() == Symbol())
      { 
         RefreshRates();   
            
         if( OrderType() == OP_BUY ) {
            chgFromOpen = NormalizeDouble(Bid - OrderOpenPrice(), Digits);
   
            // move to break even
            if(chgFromOpen >= trailStart && chgFromOpen > (minSLTPdstnc * Point) &&
                NormalizeDouble(OrderStopLoss(),Digits) < NormalizeDouble(OrderOpenPrice(), Digits) && trailStart != 0 ) 
            {
               Print("Moving stop to breakeven on order " + OrderTicket() + ". Bid is " + DoubleToStr( Bid, Digits ) + ", Trail start is " + DoubleToStr( trailStart, Digits ) );
               return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderOpenPrice(), OrderTakeProfit(), 0, Blue) );
            }
         
            profitPips = Bid - ( trailStart + OrderOpenPrice() ) ;
            if( trailAmount != 0) 
               increments = MathFloor( profitPips / trailAmount ); 
            else 
               increments = 0;
         
         
            if ( increments >= 1 && Bid >= OrderOpenPrice() + trailStart + ( increments * trailAmount ) ) {
               sl = NormalizeDouble( OrderOpenPrice() + ( (increments-1)  * trailAmount ), Digits);
            
               display = "\n\nRequested stop loss: " + DoubleToStr( sl, Digits);
            
               if( sl > NormalizeDouble(OrderStopLoss(), Digits) ) {
            
                  if( OrderModify( OrderTicket(), OrderOpenPrice(), sl, OrderTakeProfit(), 0, Blue ) ){
                     Print("Trailng stop updated. Total increments: " + DoubleToStr(increments, Digits) );
                     return(true);
                  }
               }
            }
         }
      
         if( OrderType() == OP_SELL ) {
            chgFromOpen = NormalizeDouble(OrderOpenPrice() - Ask, Digits);
            
            // move to break even
            if( chgFromOpen >= trailStart && chgFromOpen > (minSLTPdstnc * Point) &&
               NormalizeDouble(OrderStopLoss(),Digits) > NormalizeDouble(OrderOpenPrice(), Digits) && trailStart != 0 ) {
               Print("Moving stop to breakeven on order " + OrderTicket() + ". Ask is " + DoubleToStr( Ask, Digits ) + ", Trail start is " + DoubleToStr( trailStart, Digits ) );
               return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderOpenPrice(), OrderTakeProfit(), 0, Red) );
            }
         
            profitPips = ( OrderOpenPrice()- trailStart ) - Ask ;
            if( trailAmount != 0) { increments = MathFloor( profitPips / trailAmount ); }
            else { increments = 0; }
         
            if ( increments >= 1 && Ask <= OrderOpenPrice() - trailStart - ( increments * trailAmount ) ) {
               sl = NormalizeDouble(OrderOpenPrice() - ( (increments-1) * trailAmount ), Digits);
            
               if( sl < NormalizeDouble(OrderStopLoss(), Digits) ) {
                  if( OrderModify( OrderTicket(), OrderOpenPrice(), sl, OrderTakeProfit(), 0, Red) ) {
                     Print("Trailng stop updated. Total increments: " + DoubleToStr(increments, Digits) );
                     return(true);
                  }
               }
            }
         }
      
         if( IsStopped() ) { break; } 
      }          
   }
   
   return( false );
}