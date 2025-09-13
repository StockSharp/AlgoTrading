//+------------------------------------------------------------------+
//|                                           UniversalMACrossEA.mq4 |
//|                                       Copyright © 2006, firedave | 
//|                    Partial Function Copyright © 2006, codersguru | 
//|                        Partial Function Copyright © 2006, pengie |
//|                                        http://www.fx-review.com/ | 
//|                                        http://www.forex-tsd.com/ | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, firedave"
#property link      "http://www.fx-review.com"
//----------------------- INCLUDES
#include <stdlib.mqh>
//----------------------- EA PARAMETER
extern string
         Expert_Name      ="---------- Universal MA Cross EA v4";
extern double
         StopLoss         =100,
         TakeProfit       =200,
         TrailingStop     =40;
extern string
         Indicator_Setting="---------- Indicator Setting";
extern int
         FastMAPeriod     =10,
         FastMAType       =1,    //0:SMA 1:EMA 2:SMMA 3:LWMA
         FastMAPrice      =0,    //0:Close 1:Open 2:High 3:Low 4:Median 5:Typical 6:Weighted
         SlowMAPeriod     =80,
         SlowMAType       =1,    //0:SMA 1:EMA 2:SMMA 3:LWMA
         SlowMAPrice      =0,    //0:Close 1:Open 2:High 3:Low 4:Median 5:Typical 6:Weighted
         MinCrossDistance =0;    //Always positive, 0:disable

extern string
         Exit_Setting     ="---------- Exit Setting";
extern bool
         ReverseCondition =false, // TRUE:buy-sell , sell-buy
         ConfirmedOnEntry =true,  // TRUE:entry on the next signal bar
         OneEntryPerBar   =true,
         StopAndReverse   =true,  // TURE:if signal change, exit and reverse order
         PureSAR          =false;  // TRUE:no SL, no TP, no TS
extern string
         Order_Setting    ="---------- Order Setting";
extern int
         NumberOfTries    =5,
         Slippage         =5,
         MagicNumber      =1234;
extern string
         Time_Parameters  ="---------- EA Active Time";
extern bool
         UseHourTrade     =false;
extern int
         StartHour        =10,
         EndHour          =11;
extern string
         MM_Parameters    ="---------- Money Management";
extern double
         Lots             =1;
extern bool
         MM               =false, //Use Money Management or not
         AccountIsMicro   =false; //Use Micro-Account or not
extern int
         Risk             =10; //10%
extern string
         Testing_Parameters= "---------- Back Test Parameter";
extern bool
         PrintControl     =true,
         Show_Settings    =true;
//----------------------- GLOBAL VARIABLE
static int
         TimeFrame        =0;
string
         TicketComment    ="UniversalMACrossEA v4",
         LastTrade;
datetime
         CheckTime,
         CheckEntryTime;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----------------------- GENERATE MAGIC NUMBER AND TICKET COMMENT
//----------------------- SOURCE : PENGIE
   MagicNumber   =subGenerateMagicNumber(MagicNumber, Symbol(), Period());
   TicketComment =StringConcatenate(TicketComment, "-", Symbol(), "-", Period());
//----------------------- SET MinCrossDistance ALWAYS POSITIVE
   MinCrossDistance=MathAbs(MinCrossDistance);
//----------------------- SHOW EA SETTING ON THE CHART
//----------------------- SOURCE : CODERSGURU
   if(Show_Settings) subPrintDetails();
   else Comment("");
//----------------------- INITIALIZE PURE Stop And Reverse
//----------------------- NO STOP LOSS, NO TAKE PROFIT, NO TRAILING STOP
   if(PureSAR)
     {
      StopLoss      =0;
      TakeProfit    =0;
      TrailingStop  =0;
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----------------------- PREVENT RE-COUNTING WHILE USER CHANGING TIME FRAME
//----------------------- SOURCE : CODERSGURU
   TimeFrame=Period();
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   double
   FastMAPrevious,
   FastMACurrent,
   SlowMAPrevious,
   SlowMACurrent;
//----
   int
   cnt,
   ticket,
   total;
//----
   bool
   BuyCondition,
   SellCondition;
//----------------------- TIME FILTER
   if (UseHourTrade)
     {
      if(!(Hour()>=StartHour && Hour()<=EndHour))
        {
         Comment("Non-Trading Hours!");
         return(0);
        }
     }
//----------------------- CHECK CHART NEED MORE THAN 100 BARS
   if(Bars<100)
     {
      Print("bars less than 100");
      return(0);
     }
//----------------------- TRAILING STOP SECTION
   if(TrailingStop>0 && subTotalTrade()>0)
     {
      total=OrdersTotal();
      for(cnt=0;cnt<total;cnt++)
        {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
//----
         if(OrderType()<=OP_SELL &&
            OrderSymbol()==Symbol() &&
            OrderMagicNumber()==MagicNumber)
           {
            subTrailingStop(OrderType());
           }
        }
     }
//----------------------- ADJUST LOTS IF USING MONEY MANAGEMENT
   if(MM) Lots=subLotSize();
//----------------------- SET VALUE FOR VARIABLE
//----------------------- MACD
   if(ConfirmedOnEntry)
     {
      if(CheckTime==iTime(NULL,TimeFrame,0)) return(0); else CheckTime=iTime(NULL,TimeFrame,0);
//----
      FastMAPrevious=iMA(NULL,TimeFrame,FastMAPeriod,0,FastMAType,FastMAPrice,2);
      FastMACurrent =iMA(NULL,TimeFrame,FastMAPeriod,0,FastMAType,FastMAPrice,1);
      SlowMAPrevious=iMA(NULL,TimeFrame,SlowMAPeriod,0,SlowMAType,SlowMAPrice,2);
      SlowMACurrent =iMA(NULL,TimeFrame,SlowMAPeriod,0,SlowMAType,SlowMAPrice,1);
     }
   else
     {
      FastMAPrevious=iMA(NULL,TimeFrame,FastMAPeriod,0,FastMAType,FastMAPrice,1);
      FastMACurrent =iMA(NULL,TimeFrame,FastMAPeriod,0,FastMAType,FastMAPrice,0);
      SlowMAPrevious=iMA(NULL,TimeFrame,SlowMAPeriod,0,SlowMAType,SlowMAPrice,1);
      SlowMACurrent =iMA(NULL,TimeFrame,SlowMAPeriod,0,SlowMAType,SlowMAPrice,0);
     }
//----------------------- CONDITION CHECK
   if(!ReverseCondition)
     {
      //----------------------- BUY CONDITION   
      BuyCondition  =(FastMAPrevious<SlowMAPrevious &&
                        FastMACurrent>SlowMACurrent &&
                        (FastMACurrent-SlowMACurrent)>=MinCrossDistance*Point);
      //----------------------- SELL CONDITION   
      SellCondition =(FastMAPrevious>SlowMAPrevious &&
                        FastMACurrent<SlowMACurrent &&
                        (SlowMACurrent-FastMACurrent)>=MinCrossDistance*Point);
     }
   else
     {
      //----------------------- BUY CONDITION   
      SellCondition =(FastMAPrevious<SlowMAPrevious &&
                        FastMACurrent>SlowMACurrent &&
                        (FastMACurrent-SlowMACurrent)>=MinCrossDistance*Point);
      //----------------------- SELL CONDITION   
      BuyCondition  =(FastMAPrevious>SlowMAPrevious &&
                        FastMACurrent<SlowMACurrent &&
                        (SlowMACurrent-FastMACurrent)>=MinCrossDistance*Point);
     }
//----------------------- EXIT CONDITION
//----------------------- not available
//----------------------- STOP AND REVERSE
   if(StopAndReverse && subTotalTrade()>0)
     {
      if((LastTrade=="BUY" && SellCondition) || (LastTrade=="SELL" && BuyCondition))
        {
         subCloseOrder();
         if(IsTesting() && PrintControl) Print("STOP AND REVERSE !");
        }
     }
//----------------------- ENTRY
//----------------------- TOTAL ORDER BASE ON MAGICNUMBER AND SYMBOL
   total=subTotalTrade();
//----------------------- IF NO TRADE
   if(total<1)
     {
      //----------------------- ONE ENTRY PER BAR
      if(OneEntryPerBar)
        {
         if(CheckEntryTime==iTime(NULL,TimeFrame,0)) return(0); else CheckEntryTime=iTime(NULL,TimeFrame,0);
        }
      //----------------------- BUY CONDITION   
      if(BuyCondition)
        {
         ticket     =subOpenOrder(OP_BUY); // open BUY order
         subCheckError(ticket,"BUY");
         LastTrade  ="BUY";
         return(0);
        }
      //----------------------- SELL CONDITION   
      if(SellCondition)
        {
         ticket     =subOpenOrder(OP_SELL); // open SELL order
         subCheckError(ticket,"SELL");
         LastTrade  ="SELL";
         return(0);
        }
      return(0);
     }
   return(0);
  }
//----------------------- END PROGRAM
//+------------------------------------------------------------------+
//| FUNCTION DEFINITIONS
//+------------------------------------------------------------------+
//----------------------- MONEY MANAGEMENT FUNCTION  
//----------------------- SOURCE : CODERSGURU
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double subLotSize()
  {
   double lotMM=MathCeil(AccountFreeMargin() *  Risk/1000)/100;
   if(AccountIsMicro==false) //normal account
     {
      if(lotMM < 0.1)                  lotMM=Lots;
      if((lotMM > 0.5) && (lotMM < 1)) lotMM=0.5;
      if(lotMM > 1.0)                  lotMM=MathCeil(lotMM);
      if(lotMM > 100)                  lotMM=100;
     }
   else //micro account
     {
      if(lotMM < 0.01)                 lotMM=Lots;
      if(lotMM > 1.0)                  lotMM=MathCeil(lotMM);
      if(lotMM > 100)                  lotMM=100;
     }
   return(lotMM);
  }
//----------------------- NUMBER OF ORDER BASE ON SYMBOL AND MAGICNUMBER FUNCTION
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int subTotalTrade()
  {
   int
   cnt,
   total=0;
   //
   for(cnt=0;cnt<OrdersTotal();cnt++)
     {
      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderType()<=OP_SELL &&
         OrderSymbol()==Symbol() &&
         OrderMagicNumber()==MagicNumber) total++;
     }
   return(total);
  }
//----------------------- OPEN ORDER FUNCTION
//----------------------- SOURCE   : CODERSGURU
//----------------------- SOURCE   : PENGIE
//----------------------- MODIFIED : FIREDAVE
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int subOpenOrder(int type)
  {
   int
   ticket     =0,
   err        =0,
   c          =0;
   double
   aStopLoss  =0,
   aTakeProfit=0,
   bStopLoss  =0,
   bTakeProfit=0;
   if(StopLoss!=0)
     {
      aStopLoss  =Ask-StopLoss*Point;
      bStopLoss  =Bid+StopLoss*Point;
     }
   if(TakeProfit!=0)
     {
      aTakeProfit=Ask+TakeProfit*Point;
      bTakeProfit=Bid-TakeProfit*Point;
     }
   if(type==OP_BUY)
     {
      for(c=0;c<NumberOfTries;c++)
        {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,aStopLoss,aTakeProfit,TicketComment,MagicNumber,0,Green);
         err=GetLastError();
         if(err==0)
           {
            break;
           }
         else
           {
            if(err==4 || err==137 ||err==146 || err==136) //Busy errors
              {
               Sleep(5000);
               continue;
              }
            else //normal error
              {
               break;
              }
           }
        }
     }
   if(type==OP_SELL)
     {
      for(c=0;c<NumberOfTries;c++)
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,bStopLoss,bTakeProfit,TicketComment,MagicNumber,0,Red);
         err=GetLastError();
         if(err==0)
           {
            break;
           }
         else
           {
            if(err==4 || err==137 ||err==146 || err==136) //Busy errors
              {
               Sleep(5000);
               continue;
              }
            else //normal error
              {
               break;
              }
           }
        }
     }
   return(ticket);
  }
//----------------------- CLOSE ORDER FUNCTION
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void subCloseOrder()
  {
   int
   cnt,
   total=0;
   //
   total=OrdersTotal();
   for(cnt=total-1;cnt>=0;cnt--)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol()==Symbol() &&
         OrderMagicNumber()==MagicNumber)
        {
         switch(OrderType())
           {
            case OP_BUY      :
               OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,Violet);
               break;
            case OP_SELL     :
               OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,Violet);
               break;
//----
            case OP_BUYLIMIT :
            case OP_BUYSTOP  :
            case OP_SELLLIMIT:
            case OP_SELLSTOP :
               OrderDelete(OrderTicket());
           }
        }
     }
  }
//----------------------- TRAILING STOP FUNCTION
//----------------------- SOURCE   : CODERSGURU
//----------------------- MODIFIED : FIREDAVE
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void subTrailingStop(int Type)
  {
   if(Type==OP_BUY)   // buy position is opened   
     {
      if(Bid-OrderOpenPrice()>Point*TrailingStop &&
         OrderStopLoss()<Bid-Point*TrailingStop)
        {
         OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
         return(0);
        }
     }
   if(Type==OP_SELL)   // sell position is opened   
     {
      if(OrderOpenPrice()-Ask>Point*TrailingStop)
        {
         if(OrderStopLoss()>Ask+Point*TrailingStop || OrderStopLoss()==0)
           {
            OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
            return(0);
           }
        }
     }
  }
//----------------------- CHECK ERROR CODE FUNCTION
//----------------------- SOURCE : CODERSGURU
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void subCheckError(int ticket, string Type)
  {
   if(ticket>0)
     {
      if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print(Type + " order opened : ",OrderOpenPrice());
     }
   else Print("Error opening " + Type + " order : ", ErrorDescription(GetLastError()));
  }
//----------------------- GENERATE MAGIC NUMBER BASE ON SYMBOL AND TIME FRAME FUNCTION
//----------------------- SOURCE   : PENGIE
//----------------------- MODIFIED : FIREDAVE
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int subGenerateMagicNumber(int MagicNumber, string symbol, int timeFrame)
  {
   int isymbol=0;
   if (symbol=="EURUSD")       isymbol=1;
   else if (symbol=="GBPUSD")  isymbol=2;
      else if (symbol=="USDJPY")  isymbol=3;
         else if (symbol=="USDCHF")  isymbol=4;
            else if (symbol=="AUDUSD")  isymbol=5;
               else if (symbol=="USDCAD")  isymbol=6;
                  else if (symbol=="EURGBP")  isymbol=7;
                     else if (symbol=="EURJPY")  isymbol=8;
                        else if (symbol=="EURCHF")  isymbol=9;
                           else if (symbol=="EURAUD")  isymbol=10;
                              else if (symbol=="EURCAD")  isymbol=11;
                                 else if (symbol=="GBPUSD")  isymbol=12;
                                    else if (symbol=="GBPJPY")  isymbol=13;
                                       else if (symbol=="GBPCHF")  isymbol=14;
                                          else if (symbol=="GBPAUD")  isymbol=15;
                                             else if (symbol=="GBPCAD")  isymbol=16;
                                                else                          isymbol=17;
   if(isymbol<10) MagicNumber=MagicNumber * 10;
   return(StrToInteger(StringConcatenate(MagicNumber, isymbol, timeFrame)));
  }
//----------------------- PRINT COMMENT FUNCTION
//----------------------- SOURCE : CODERSGURU
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void subPrintDetails()
  {
   string sComment  ="";
   string sp        ="----------------------------------------\n";
   string NL        ="\n";
   //
   sComment=sp;
   sComment=sComment + "TakeProfit=" + DoubleToStr(TakeProfit,0) + " | ";
   sComment=sComment + "TrailingStop=" + DoubleToStr(TrailingStop,0) + " | ";
   sComment=sComment + "StopLoss=" + DoubleToStr(StopLoss,0) + NL;
   sComment=sComment + sp;
   sComment=sComment + "Reverse Entry Condition=" + subBoolToStr(ReverseCondition) + NL;
   sComment=sComment + "Confirmed On Entry=" + subBoolToStr(ConfirmedOnEntry) + NL;
   sComment=sComment + "Stop And Reverse=" + subBoolToStr(StopAndReverse) + NL;
   sComment=sComment + "Pure SAR=" + subBoolToStr(PureSAR) + NL;
   sComment=sComment + sp;
   sComment=sComment + "Lots=" + DoubleToStr(Lots,2) + " | ";
   sComment=sComment + "MM=" + subBoolToStr(MM) + " | ";
   sComment=sComment + "Risk=" + DoubleToStr(Risk,0) + "%" + NL;
   sComment=sComment + sp;
   //
   Comment(sComment);
  }
//----------------------- BOOLEN VARIABLE TO STRING FUNCTION
//----------------------- SOURCE : CODERSGURU
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string subBoolToStr(bool value)
  {
   if(value) return("True");
   else return("False");
  }
//----------------------- END FUNCTION
//+------------------------------------------------------------------+