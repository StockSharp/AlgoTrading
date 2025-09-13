//+------------------------------------------------------------------+
//|                                         AeronForexAutoTrader.mq4 |
//|                                                            Aeron |
//|                                         http://www.aeroninfo.com |
//+------------------------------------------------------------------+
#property copyright "Aeron"
#property link      "http://www.aeroninfo.com"

extern string _____Important______="Do not modify any parameter";
extern string _____Important_____="all are very optimized";
extern string _____Important____="Please contact support@aeroninfo.com";
extern string _____Important___="before any modification";
extern string ActivationKey="Mandatory for Real Accounts";

double forResetLoss;
string body1;
bool CloseAllTrade;
extern double LossTolerance=20;
double LossTolerance1=0;
extern int RestartAfter_days=7;
extern double ResetAfter_Loss=0.0;
double closeDuration1=0;
double closeDuration2=0;
double LTprofit1=0;double LTprofit2=0;

extern double First_Lots_Size=0.02;
extern double  Lots_Increment_Factor=2.667; 
extern double PositionsGap=150;
extern double PositionsGap_Factor=1;
extern int MaxTrades=3;
extern double TakeProfit=19;
extern double  Stoploss=1000;
extern bool Hedging=true;
extern string OrderComments="AeronRobot_EURUSD-r9.06";
extern int MagicNumber1=82379;
extern int MagicNumber2=72379;
extern int MagicNumber3=62379;

double LotIncre;
bool AutoLots=false;
double FreeMargin_for_AutoLots=1;
double Lots=0.1;
bool CheckFreeMargin=true;
bool TrailSL=false;
int TrailPips=12;
int n1=1; int n2=1; int n3=1;
int lotsdigit5=0;
double iLots1, tLots1;
double prcRequired;
double mrgRequired;
double db1;
bool TradeSafe=true;
bool ExtremeProtection=false;
double PipsNext;
double indMA, indENV1, indENV2, indENV11, indENV22, indIch;
int     MMType=1;
bool    CloseClose=false;
bool    AddAdd=true;
int MagicNumber;
double PriceTarget, StartEquity, BuyTarget, SellTarget;
bool    UseEquityStop=false;
double  TotalEquityRisk=20;
double AveragePrice, SellLimit, BuyLimit;
double LastBuyPrice, LastSellPrice, ClosePrice, Spread;
double  slip=3;
double  LotsDigits;
datetime timeprev=0;
datetime timeprev1=0;
datetime expiration;
int flag;
string EAName="AeronForexAutoTrader";
int NumOfTrades=0;
bool TradeNow=false;
bool LongTrade=false;
bool ShortTrade=false;
bool flagTrade;
int ticket;
double iLots;
int cnt=0, total;
double Stopper=0;
bool FreshOrdP=false;


int init()
{
  Spread=MarketInfo(Symbol(), MODE_SPREAD)*Point;
  return(0);
}
  
int deinit()
{
  return(0);
}
int start()
{
  //////////////
    int int6;
    int timeMinimum=2147483647;
    double ttlProfit=0.0;
    for(int6=0; int6<=OrdersTotal()-1;int6++)
    {
      OrderSelect(int6,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber1 || OrderMagicNumber()==MagicNumber2 || OrderMagicNumber()==MagicNumber3))
      {
        ttlProfit=ttlProfit+OrderProfit();
        if(OrderOpenTime()<timeMinimum)
          timeMinimum=OrderOpenTime();
      }
    }
    //CloseAllTrade=false;
    if(ResetAfter_Loss==0.0)
      ResetAfter_Loss=forResetLoss;
    if(ttlProfit<-1.0*ResetAfter_Loss)
      {CloseAllTrade=true; closeDuration1=TimeCurrent() + (60*60*24*RestartAfter_days); closeDuration2=TimeCurrent() + (60*60*24*RestartAfter_days);}
    if(CountTrades()==0)
      CloseAllTrade=false;
    if(CloseAllTrade==true)
    {
      int int4;
      for(int4=OrdersTotal()-1;int4>=0;int4--)
      {
        OrderSelect(int4,SELECT_BY_POS,MODE_TRADES);
        if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber1 || OrderMagicNumber()==MagicNumber2 || OrderMagicNumber()==MagicNumber3))
        {
          if(OrderType()==OP_SELL)
          {
            body1="Order Type: SELL\nCurrency Symbol: " + Symbol() +
                  "\nOrder Ticket: " + OrderTicket() + 
                  "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                  "\nLots Size: " + OrderLots() +
                  "\nOrder Open Price: " + OrderOpenPrice() + 
                  "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                  "\nOrder close price: " + NormalizeDouble(Ask,Digits) + 
                  "\nOrder Comment: " + OrderComment() +
                  "\nOrder Magic Number: " + OrderMagicNumber();
            if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),2,CLR_NONE)==true)
                SendMail("Aeron: Sell order closed",body1);
          }
          if(OrderType()==OP_BUY )
          {
            body1="Order Type: BUY\nCurrency Symbol: " + Symbol() +
                  "\nOrder Ticket: " + OrderTicket() + 
                  "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                  "\nLots Size: " + OrderLots() +
                  "\nOrder Open Price: " + OrderOpenPrice() + 
                  "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                  "\nOrder close price: " + NormalizeDouble(Bid,Digits) +
                  "\nOrder Comment: " + OrderComment() +
                  "\nOrder Magic Number: " + OrderMagicNumber();
            if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),2,CLR_NONE)==true)
                 SendMail("Aeron: Buy order closed",body1);
          }
        }
      }
    }
  //////////////
    
  if(LossTolerance==0.0)
  {
    LossTolerance1=AccountBalance() * 10 / 100;
  }
  else
  {
    LossTolerance1=AccountBalance() * LossTolerance / 100;
  }
  //if(closeDuration1==0)
  {
  LTprofit1=0;
  LTprofit2=0;
  int LTint1;
  
  for(LTint1=0;LTint1<=OrdersTotal()-1;LTint1++)
  {
    OrderSelect(LTint1,SELECT_BY_POS,MODE_TRADES);
    if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber1||OrderMagicNumber()==MagicNumber3||OrderMagicNumber()==MagicNumber2))
    {
      if(OrderType()==OP_BUY)
      {
        LTprofit1 = LTprofit1 + OrderProfit();
      }
    }
  }
  if(LTprofit1 * (-1) > LossTolerance1)
  {
    for(LTint1=0;LTint1<=OrdersTotal()-1;LTint1++)
    {
      OrderSelect(LTint1,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber1||OrderMagicNumber()==MagicNumber3||OrderMagicNumber()==MagicNumber2))
      {
        if(OrderType()==OP_BUY)
        {
           ///////////////
        }
      }
    }
    closeDuration1=TimeCurrent() + (60*60*24*RestartAfter_days);
  }
  if(LTprofit1 > LossTolerance1)
  {
  
    for(LTint1=OrdersTotal()-1;LTint1>=0;LTint1--)
    {
      OrderSelect(LTint1,SELECT_BY_POS,MODE_TRADES);
      //if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber1||OrderMagicNumber()!=MagicNumber2)continue;
      if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber1||OrderMagicNumber()==MagicNumber3||OrderMagicNumber()==MagicNumber2))
      {
        if(OrderType()==OP_BUY)
        {
          body1="Order Type: BUY\nCurrency Symbol: " + Symbol() +
                  "\nOrder Ticket: " + OrderTicket() + 
                  "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                  "\nLots Size: " + OrderLots() +
                  "\nOrder Open Price: " + OrderOpenPrice() + 
                  "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                  "\nOrder close price: " + NormalizeDouble(Bid,Digits) +
                  "\nOrder Comment: " + OrderComment() +
                  "\nOrder Magic Number: " + OrderMagicNumber();
          if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),2,CLR_NONE)==true)
              SendMail("Aeron: Buy order closed",body1);
        }
      }
    }
    //closeDuration1=TimeCurrent() + (60*60*24*7*2);
  }
  }
  LTprofit1=0;
  //if(closeDuration2==0)
  {
  int LTint2;
  
  for(LTint2=0;LTint2<=OrdersTotal()-1;LTint2++)
  {
    OrderSelect(LTint2,SELECT_BY_POS,MODE_TRADES);
    //if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber1||OrderMagicNumber()!=MagicNumber2)continue;
    if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber1||OrderMagicNumber()==MagicNumber3||OrderMagicNumber()==MagicNumber2))
    {
      if(OrderType()==OP_SELL)
      {
        LTprofit2 = LTprofit2 + OrderProfit();
      }
    }
  }
  //Alert(LTprofit2);
  //Alert(LossTolerance);
  if(LTprofit2 * (-1) > LossTolerance1)
  {
    for(LTint2=0;LTint2<=OrdersTotal()-1;LTint2++)
    {
      OrderSelect(LTint2,SELECT_BY_POS,MODE_TRADES);
      //if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber1||OrderMagicNumber()!=MagicNumber2)continue;
      if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber1||OrderMagicNumber()==MagicNumber3||OrderMagicNumber()==MagicNumber2))
      {
        if(OrderType()==OP_SELL)
        {
          /////////////
        }
      }
    }
    closeDuration2=TimeCurrent() + (60*60*24*RestartAfter_days);
  }
  if(LTprofit2 > LossTolerance1)
  {
    for(LTint2=OrdersTotal()-1;LTint2>=0;LTint2--)
    {
      OrderSelect(LTint2,SELECT_BY_POS,MODE_TRADES);
      //if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber1||OrderMagicNumber()!=MagicNumber2)continue;
      if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber1||OrderMagicNumber()==MagicNumber3||OrderMagicNumber()==MagicNumber2))
      {
        if(OrderType()==OP_SELL)
        {
          body1="Order Type: SELL\nCurrency Symbol: " + Symbol() +
                  "\nOrder Ticket: " + OrderTicket() + 
                  "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                  "\nLots Size: " + OrderLots() +
                  "\nOrder Open Price: " + OrderOpenPrice() + 
                  "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) +
                  "\nOrder close price: " + NormalizeDouble(Ask,Digits) + 
                  "\nOrder Comment: " + OrderComment() +
                  "\nOrder Magic Number: " + OrderMagicNumber();
          if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),2,CLR_NONE)==true)
              SendMail("Aeron: Sell order closed",body1);
        }
      }
    }
    //closeDuration2=TimeCurrent() + (60*60*24*7*2);
  }
  }
  LTprofit2=0;
  
  if (First_Lots_Size > 0 && First_Lots_Size <= MarketInfo(Symbol(),MODE_MINLOT))
  {
    First_Lots_Size=MarketInfo(Symbol(),MODE_MINLOT) * 2;
  }
  
  mrgRequired=MarketInfo(Symbol(),MODE_MARGINREQUIRED) * MarketInfo(Symbol(),MODE_MINLOT);
  if (First_Lots_Size==0)
  {
     db1=(MathFloor(AccountFreeMargin() / (mrgRequired * 100/3))) * MarketInfo(Symbol(),MODE_MINLOT) * 100/AccountLeverage();
     if(db1<MarketInfo(Symbol(),MODE_MINLOT))
     {
       First_Lots_Size=MarketInfo(Symbol(),MODE_MINLOT);
     }
       else
     {
       First_Lots_Size=db1;
     }
     //Alert(First_Lots_Size);
     if(MathRound(First_Lots_Size/(2*MarketInfo(Symbol(),MODE_MINLOT))) > 5)
     {
       First_Lots_Size=5 * 2*MarketInfo(Symbol(),MODE_MINLOT);
     }
     else
     {
       First_Lots_Size=MathRound(First_Lots_Size/(2*MarketInfo(Symbol(),MODE_MINLOT))) * 2*MarketInfo(Symbol(),MODE_MINLOT);
     }
  }
  
  LotIncre=Lots_Increment_Factor;
  Lots=First_Lots_Size;

 if (MarketInfo(Symbol(),MODE_LOTSTEP)==0.10)
  {
    LotsDigits=1;
  }
 if (MarketInfo(Symbol(),MODE_LOTSTEP)==0.01)
  {
    LotsDigits=2;
  }
 if (MarketInfo(Symbol(),MODE_LOTSTEP)==0.05)
  {
    LotsDigits=1;
    lotsdigit5=1;
  }

int dy, mth, yr;
  bool licflag;
  licflag=false;
  dy=30;
  mth=6;
  yr=2020;
  if (Year() < yr)
  {
   licflag=true;
  }
  if (Year() == yr && Month() < mth)
  {
   licflag=true;
  }
  if (Year() == yr && Month() == mth && Day() <= dy)
  {
   licflag=true;
  }
  if (licflag == true)
  {

  
if(Year()>1999)
{

  int bb;
  for(bb=0;bb<=OrdersTotal()-1;bb++)
  {
    OrderSelect(bb,SELECT_BY_POS,MODE_TRADES);
    //if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber1||OrderMagicNumber()!=MagicNumber2)continue;
    if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber1||OrderMagicNumber()==MagicNumber2))
    {
      if(OrderType()==OP_BUY)
      {
        if (OrderOpenPrice()-Bid>Stoploss*0.0001)
        {
          body1="Order Type: BUY\nCurrency Symbol: " + Symbol() +
                  "\nOrder Ticket: " + OrderTicket() + 
                  "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                  "\nLots Size: " + OrderLots() +
                  "\nOrder Open Price: " + OrderOpenPrice() + 
                  "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) +
                  "\nOrder close price: " + NormalizeDouble(Bid,Digits) + 
                  "\nOrder Comment: " + OrderComment() +
                  "\nOrder Magic Number: " + OrderMagicNumber();
          if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),2,CLR_NONE)==true)
             SendMail("Aeron: Buy order closed",body1);
        }
      }
      if(OrderType()==OP_SELL)
      {
        if (Ask-OrderOpenPrice()>Stoploss*0.0001)
        {
          body1="Order Type: SELL\nCurrency Symbol: " + Symbol() +
                  "\nOrder Ticket: " + OrderTicket() + 
                  "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                  "\nLots Size: " + OrderLots() +
                  "\nOrder Open Price: " + OrderOpenPrice() + 
                  "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                  "\nOrder close price: " + NormalizeDouble(Ask,Digits) +
                  "\nOrder Comment: " + OrderComment() +
                  "\nOrder Magic Number: " + OrderMagicNumber();
          if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),2,CLR_NONE)==true)
               SendMail("Aeron: Sell order closed",body1);
        }
      }
    }
  }
 
  if (TrailSL==true)
  {
    int zz;
    if (CountTradesB()==1)
    {
      //int zz;
      for(zz=0;zz<=OrdersTotal()-1;zz++)
      {
        OrderSelect(zz,SELECT_BY_POS,MODE_TRADES);
        //if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=12379)continue;
        if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber1||OrderMagicNumber()==MagicNumber3))
        {
          if(OrderType()==OP_BUY)
          {
            if (Bid-OrderOpenPrice() >= TrailPips*0.0001  && OrderStopLoss() < Bid-TrailPips*0.0001-0.0002)
            {
              if(OrderModify(OrderTicket(),OrderOpenPrice(),Bid-TrailPips*0.0001+0.0002,Bid-TrailPips*0.0001+0.0002+0.0020,0,Yellow)==true)
                if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true)
                  SendMail("Aeron: Buy order modified",
                         "Order Type: BUY\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
            }
          }
        }
     }               
   }
   if (CountTradesS()==1)
   {
     //int zz;
     for(zz=0;zz<=OrdersTotal()-1;zz++)
     {
       OrderSelect(zz,SELECT_BY_POS,MODE_TRADES);
       //if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=12379)continue;
       if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber2||OrderMagicNumber()==MagicNumber3))
       {
         if(OrderType()==OP_SELL)
         {
           if (OrderOpenPrice()-Ask >= TrailPips*0.0001  && OrderStopLoss() > Ask+TrailPips*0.0001-0.0002)
           {
             if(OrderModify(OrderTicket(),OrderOpenPrice(),Ask+TrailPips*0.0001-0.0002,Ask+TrailPips*0.0001-0.0002-0.0020,0,Yellow)==true)
               if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true && OrderType()==OP_SELL)
                  SendMail("Aeron: Sell order modified",
                         "Order Type: SELL\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
           }
         }
       }
     }               
   }
 }   

int ppp=Period();

if(Hedging==true)
{    
  {  
    if (TradeSafe==true)
    {
      MagicNumber=MagicNumber1;
    }
    else
    {
      MagicNumber=12378;
    }
    
    if (ppp==1 && "EURUSD"==StringSubstr(Symbol(),0,6))
    {  
      double ActivKey, calcKey, len, i, j, k;
      string ii;
      if (IsDemo()==true)
      {      
/*code deleted*/
      }
      else
      {
/*code deleted*/
     }
    
     if ((IsDemo()==false && ActivKey==calcKey) || IsDemo()==true)
    {
      string sComm;
      sComm="Aeron Forex Auto Trader by AERON";
      sComm=sComm+"\nBroker: " + AccountCompany();
      if (IsDemo()==true)
      {
         sComm=sComm+"\nDemo Account: " + AccountNumber();
      }
      else
      {
         sComm=sComm+"\nReal Account: " + AccountNumber();   
      }
 
      flagTrade=false;
      double LBLots;
      double LSLots;
      int ccc;
   
      if (TradeSafe==true)
      {
        FreshOrdP=false;
        
    int m1;
    if (CountTradesB()==0)
    {
      n1=1;
    }
    if (CountTradesB()>1)
    {
      for(m1=0;m1<=OrdersTotal()-1;m1++)
      {
        OrderSelect(m1,SELECT_BY_POS,MODE_TRADES);
        if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber1||OrderMagicNumber()==MagicNumber3))
        {
          if(OrderType()==OP_BUY)
          { 
            if (((TimeCurrent()-OrderOpenTime()) > 4*60*60*n1 && (TimeCurrent()-OrderOpenTime()) < 24*60*60) || (TimeCurrent()-OrderOpenTime()) > 72*60*60)
            {
              n1=n1+1;
              if (OrderLots()>=First_Lots_Size)
              {
                body1="Order Type: BUY\nCurrency Symbol: " + Symbol() +
                  "\nOrder Ticket: " + OrderTicket() + 
                  "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                  "\nLots Size: " + First_Lots_Size +
                  "\nOrder Open Price: " + OrderOpenPrice() + 
                  "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                  "\nOrder close price: " + NormalizeDouble(Bid,Digits) +
                  "\nOrder Comment: " + OrderComment() +
                  "\nOrder Magic Number: " + OrderMagicNumber();
                if(OrderClose(OrderTicket(),First_Lots_Size,NormalizeDouble(Bid,Digits),2)==true)
                    SendMail("Aeron: Buy order closed",body1);
              }
              ///////////////////
              break;
            }
            if (TimeCurrent()-OrderOpenTime() > 60)
            {
              int m2;
              for(m2=OrdersTotal()-1;m2>=0;m2--)
              {
                OrderSelect(m2,SELECT_BY_POS,MODE_TRADES);
                if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber1||OrderMagicNumber()==MagicNumber3))
                {
                  if(OrderType()==OP_BUY && Bid-OrderOpenPrice() > TakeProfit*0.0001)
                  {
                    body1="Order Type: BUY\nCurrency Symbol: " + Symbol() +
                    "\nOrder Ticket: " + OrderTicket() + 
                    "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                    "\nLots Size: " + OrderLots() +
                    "\nOrder Open Price: " + OrderOpenPrice() + 
                    "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                    "\nOrder close price: " + NormalizeDouble(Bid,Digits) +
                    "\nOrder Comment: " + OrderComment() +
                    "\nOrder Magic Number: " + OrderMagicNumber();
                    if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),2)==true)
                        SendMail("Aeron: Buy order closed",body1);
                    FreshOrdP=true;
                    break;
                  }
                }
              }
            }
          }
        }
      }
    }
        
        
        if (AccountLeverage() != 999)
        {
          prcRequired=AccountFreeMargin()*(FreeMargin_for_AutoLots/100);
          mrgRequired=MarketInfo(Symbol(),MODE_MARGINREQUIRED) * MarketInfo(Symbol(),MODE_MINLOT);
          if(AutoLots==false)
          {
            Lots=First_Lots_Size;
            if (CountTradesS()>1)
            {
              if (CountTradesB()==0)
              {
                int temp1;
                for(temp1=OrdersTotal()-1;temp1>=0;temp1--)
                {
                  OrderSelect(temp1,SELECT_BY_POS,MODE_TRADES);
                  if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber2||OrderMagicNumber()==MagicNumber3))
                  {
                    if (OrderType()==OP_SELL)
                    {
                      Lots=CountTradesS()*OrderLots();
                      break;
                    }
                  }
                }
              }
            }
          }
       
          int h1;
          if ( mrgRequired <= prcRequired )
          {
            //sComm=sComm+"\nYou are doing Safe Trading";
            flagTrade=true;
            h1=mrgRequired * (100/FreeMargin_for_AutoLots) * AccountLeverage()/100;
            sComm=sComm+"\nFree Margin should be atleast: " + DoubleToStr(h1*0.75,0) + " [recommended]";
            forResetLoss=h1*0.75;
          }
          else
          {
            h1=mrgRequired * (100/FreeMargin_for_AutoLots) * AccountLeverage()/100;
            sComm=sComm+"\nFree Margin should be atleast: " + DoubleToStr(h1*0.75,0) + " [recommended]";
            forResetLoss=h1*0.75;
          }
        }
        else
        {
          Lots=0;
          //Alert("Account Leverage should be 100 or LESS\nYour Account Leverage is: ", AccountLeverage());
       }
     }
     else
     {
       //sComm=sComm+"\nYou are doing Trading at Your Own Risk";
     }
   
     int tp;
     tp=TakeProfit;
     //sComm=sComm+"\nTake Profit: " + tp;
     Comment(sComm);
   
     if(timeprev==Time[0])
     {
       return(0);
     }
     timeprev=Time[0];
     double CurrentPairProfit=CalculateProfit();
     total=CountTrades();
     
     PipsNext=PositionsGap;
     if(total>=1)
     {
       PipsNext=PositionsGap * MathPow(PositionsGap_Factor,(total-1));
     }     
     
     if (total==0)
     {
       flag=0;
     }
     double LastBuyLots;
     double LastSellLots;
     for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
     {
       OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
       if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)continue;
       if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
       if(OrderType()==OP_BUY)
       {
         LongTrade=true;
         ShortTrade=false;
         LastBuyLots=OrderLots();
         break;
       }
       if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
       if(OrderType()==OP_SELL)
       {
         LongTrade=false;
         ShortTrade=true;
         LastSellLots=OrderLots();
         break;
       }
     }
     if(total>0 && total<=MaxTrades)
     {
       RefreshRates();
       LastBuyPrice=LastBuyPrice();
       LastSellPrice=LastSellPrice();
      
       if (ExtremeProtection == false)
       {
         //PipsNext=75;
       }
       else
       {
         PipsNext=75*total*2.5;
       }
      
       if(LongTrade && (LastBuyPrice - Ask)>=(PipsNext*0.0001))
       {
         TradeNow=true;
       }
       if(ShortTrade && (Bid - LastSellPrice)>=(PipsNext*0.0001))
       {
         TradeNow=true;
       }
     }
     if (total < 1)
     {
       ShortTrade=false;
       LongTrade=false;
       TradeNow=true;
       StartEquity=AccountEquity();
     }
     if (TradeNow)
     {
       LastBuyPrice=LastBuyPrice();
       LastSellPrice=LastSellPrice();
       if(ShortTrade)
       {
         if(CloseClose)
         {
           fOrderCloseMarket(false,true);
           iLots=NormalizeDouble(LotIncre*LastSellLots,LotsDigits);
         }
         else
         {
           iLots=fGetLots(OP_SELL);
         }
         if(AddAdd)
         {
           NumOfTrades=total;
           if(iLots>0)
           {
             RefreshRates();
             //ticket=OpenPendingOrder(OP_SELL,iLots,Bid,slip,Ask,0,0,EAName+"-"+NumOfTrades,MagicNumber,0,HotPink);
             if(ticket<0){Print("Error: ",GetLastError()); return(0);}
             LastSellPrice=LastSellPrice();
             TradeNow=false;
             FreshOrdP=true;
           }
         }
       }
       else if (LongTrade)
       {
         if(CloseClose)
         {
           fOrderCloseMarket(true,false);
           iLots=NormalizeDouble(LotIncre*LastBuyLots,LotsDigits);
         }
         else
         {
           iLots=fGetLots(OP_BUY);
         }
         if(AddAdd)
         {
           NumOfTrades=total;
           if(iLots>0)
           {
             ticket=OpenPendingOrder(OP_BUY,iLots,Ask,slip,Bid,0,0,EAName+"-"+NumOfTrades,MagicNumber,0,Lime);
             if(ticket<0)
             {Print("Error: ",GetLastError()); return(0);}
             LastBuyPrice=LastBuyPrice();
             TradeNow=false;
             FreshOrdP=true;
           }
         }
       }
     }
     if (TradeNow && total<1)
     {
       double PrevCl=iClose(Symbol(),0,2);
       double CurrCl=iClose(Symbol(),0,1);
       SellLimit=Bid;
       BuyLimit=Ask;
       if(!ShortTrade && !LongTrade)
       {
         NumOfTrades=total;
         if(PrevCl > CurrCl)
         {
           iLots=fGetLots(OP_SELL);
           if(iLots>0)
           {
             //ticket=OpenPendingOrder(OP_SELL,iLots,SellLimit,slip,SellLimit,0,0,EAName+"-"+NumOfTrades,MagicNumber,0,HotPink);
             if(ticket<0){Print(iLots,"Error: ",GetLastError()); return(0);
           }
           LastBuyPrice=LastBuyPrice();
           FreshOrdP=true;
         }
       }
       else
       {
         iLots=fGetLots(OP_BUY);
         if(iLots>0)
         {     
           ticket=OpenPendingOrder(OP_BUY,iLots,BuyLimit,slip,BuyLimit,0,0,EAName+"-"+NumOfTrades,MagicNumber,0,Lime);
           if(ticket<0){Print(iLots,"Error: ",GetLastError()); return(0);}
           LastSellPrice=LastSellPrice();
           FreshOrdP=true;
         }
       }
     }
     TradeNow=false;
   }
   total=CountTrades();
   AveragePrice=0;
   double Count=0;
   for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
   {
     OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
     if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)
       continue;
     if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
     if(OrderType()==OP_BUY || OrderType()==OP_SELL)
     {
       AveragePrice=AveragePrice+OrderOpenPrice()*OrderLots();
       Count=Count + OrderLots();
     }
   }
   if(total > 0)
   AveragePrice=NormalizeDouble(AveragePrice/Count, Digits);
   if(FreshOrdP)
   for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
   {
     OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
     if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)
     continue;
     if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
     if(OrderType()==OP_BUY)
     {
       PriceTarget=AveragePrice+(TakeProfit*0.0001);
       BuyTarget=PriceTarget;
       Stopper=AveragePrice-(Stoploss*Point);
       flag=1;
     }
     if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
     if(OrderType()==OP_SELL)
     {
       PriceTarget=AveragePrice-(TakeProfit*0.0001);
       SellTarget=PriceTarget;
       Stopper=AveragePrice+(Stoploss*Point);
       flag=1;
     }
   }
   if(FreshOrdP)
   if(flag==1)
   {
     for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)
       continue;
       if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
       {
         if(OrderStopLoss()>0)
         {
           if(OrderModify(OrderTicket(),AveragePrice,OrderStopLoss(),PriceTarget,0,Yellow)==true)
            { 
              if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true && OrderType()==OP_BUY)
                SendMail("Aeron: Buy order modified",
                         "Order Type: BUY\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
              if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true && OrderType()==OP_SELL)
                SendMail("Aeron: Sell order modified",
                         "Order Type: SELL\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
            }
         }
         else
         {
           if(OrderModify(OrderTicket(),AveragePrice,OrderOpenPrice()-Stoploss*0.0001,PriceTarget,0,Yellow)==true)
           {
             if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true && OrderType()==OP_BUY)
                SendMail("Aeron: Buy order modified",
                         "Order Type: BUY\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
             if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true && OrderType()==OP_SELL)
                SendMail("Aeron: Sell order modified",
                         "Order Type: SELL\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
           }
         }
       }
       FreshOrdP=false;
     }
   }
 }
 else
 {
   Alert("Please Activate your copy\nof AeronForexAutoTrader");
 }
}
else
{
  Alert("Works only on EURUSD 1 Minute Chart", "\nbut You are trying to use it on ", Symbol(), " ", Period());
}
}
{  
  if (TradeSafe==true)
   {
      TradeNow=false;
      MagicNumber=MagicNumber2;
   }
   else
   {
      MagicNumber=12378;
   }
  
  
  //int ppp=Period();
  if (ppp==1 && "EURUSD"==StringSubstr(Symbol(),0,6))
  {  
   //double ActivKey, calcKey, len, i, j, k;
   ActivKey=0; calcKey=0; len=0; i=0; j=0; k=0;
   //string ii;
   
   if (IsDemo()==true)
   {      
/*code deleted*/
   }
   else
   {
/*code deleted*/
   }
    
   if ((IsDemo()==false && ActivKey==calcKey) || IsDemo()==true)
   {
   

      //string sComm;
      sComm="Aeron Forex Auto Trader by AERON";
   
      sComm=sComm+"\nBroker: " + AccountCompany();
      if (IsDemo()==true)
      {
         sComm=sComm+"\nDemo Account: " + AccountNumber();
      }
      else
      {
         sComm=sComm+"\nReal Account: " + AccountNumber();   
      }
   
   
   ////////////////////
   flagTrade=false;
   //double LBLots;
   //double LSLots;
   //int ccc;
   LBLots=0;
   LSLots=0;
   ccc=0;
   
   if (TradeSafe==true)
   {
     FreshOrdP=false;
     int m3;
     
    if (CountTradesS()==0)
    {
      n2=1;
    }
    if (CountTradesS()>1)
    {
      for(m3=0;m3<=OrdersTotal()-1;m3++)
      {
        OrderSelect(m3,SELECT_BY_POS,MODE_TRADES);
        if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber2||OrderMagicNumber()==MagicNumber3))
        {
          if(OrderType()==OP_SELL)
          {  
            if (((TimeCurrent()-OrderOpenTime()) > 4*60*60*n2 && (TimeCurrent()-OrderOpenTime()) < 24*60*60) ||  (TimeCurrent()-OrderOpenTime()) > 72*60*60)
            {
             
              n2=n2+1;
              if (OrderLots()>=First_Lots_Size)
              {
                body1="Order Type: SELL\nCurrency Symbol: " + Symbol() +
                      "\nOrder Ticket: " + OrderTicket() + 
                      "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                      "\nLots Size: " + OrderLots() +
                      "\nOrder Open Price: " + First_Lots_Size + 
                      "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                      "\nOrder close price: " + NormalizeDouble(Ask,Digits) +
                      "\nOrder Comment: " + OrderComment() +
                      "\nOrder Magic Number: " + OrderMagicNumber();
                if(OrderClose(OrderTicket(),First_Lots_Size,NormalizeDouble(Ask,Digits),2)==true)
                   SendMail("Aeron: Sell order closed",body1);
              }
              /////////////////
              break;
            }
            if (TimeCurrent()-OrderOpenTime() > 60)
            {
              int m4;
              for(m4=OrdersTotal()-1;m4>=0;m4--)
              {
                OrderSelect(m4,SELECT_BY_POS,MODE_TRADES);
                if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber3||OrderMagicNumber()==MagicNumber3))
                {
                  if(OrderType()==OP_SELL && OrderOpenPrice()-Ask > TakeProfit*0.0001)
                  {
                    body1="Order Type: SELL\nCurrency Symbol: " + Symbol() +
                     "\nOrder Ticket: " + OrderTicket() + 
                     "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                     "\nLots Size: " + OrderLots() +
                     "\nOrder Open Price: " + OrderOpenPrice() + 
                     "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                     "\nOrder close price: " + NormalizeDouble(Ask,Digits) +
                     "\nOrder Comment: " + OrderComment() +
                     "\nOrder Magic Number: " + OrderMagicNumber();
                    if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),2)==true)
                        SendMail("Aeron: Sell order closed",body1);
                    FreshOrdP=true;
                    break;
                  }
                }
              }
            }
          }
        }
      }
    }
     if (AccountLeverage() != 999)
     {
       prcRequired=AccountFreeMargin()*(FreeMargin_for_AutoLots/100);
       mrgRequired=MarketInfo(Symbol(),MODE_MARGINREQUIRED) * MarketInfo(Symbol(),MODE_MINLOT);
       if(AutoLots==false)
       {
         Lots=First_Lots_Size;
         if (CountTradesB()>1)
            {
              if (CountTradesS()==0)
              {
                int temp2;
                for(temp2=OrdersTotal()-1;temp2>=0;temp2--)
                {
                  OrderSelect(temp2,SELECT_BY_POS,MODE_TRADES);
                  if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber1||OrderMagicNumber()==MagicNumber3))
                  {
                    if (OrderType()==OP_BUY)
                    {
                      Lots=CountTradesB()*OrderLots();
                      break;
                    }
                  }
                }
              }
            }
       }
       
       int h2;
       if ( mrgRequired <= prcRequired )
       {
         //sComm=sComm+"\nYou are doing Safe Trading";
         flagTrade=true;
         h2=mrgRequired * (100/FreeMargin_for_AutoLots) * AccountLeverage()/100;
         sComm=sComm+"\nFree Margin should be atleast: " + DoubleToStr(h2*0.75,0) + " [recommended]";
         forResetLoss=h2*0.75;
       }
       else
       {
         h2=mrgRequired * (100/FreeMargin_for_AutoLots) * AccountLeverage()/100;
         sComm=sComm+"\nFree Margin should be atleast: " + DoubleToStr(h2*0.75,0) + " [recommended]";
         forResetLoss=h2*0.75;
       }
     }
     else
     {
       Lots=0;
       //Alert("Account Leverage should be 100 or LESS\nYour Account Leverage is: ", AccountLeverage());
     }
   }
   else
   {
      //sComm=sComm+"\nYou are doing Trading at Your Own Risk";
   }
   
   tp=TakeProfit;
   //sComm=sComm+"\nTake Profit: " + tp;
   Comment(sComm);
   
     if(timeprev1==Time[0])
     {
      return(0);
     }
     
   timeprev1=Time[0];
   CurrentPairProfit=CalculateProfit();
   total=CountTrades();
   
   PipsNext=PositionsGap;
   if(total>=1)
   {
     PipsNext=PositionsGap * MathPow(PositionsGap_Factor,(total-1));
   }
     
     
     if (total==0)
     {
      flag=0;
     }
   //double LastBuyLots;
   //double LastSellLots;
   LastBuyLots=0;
   LastSellLots=0;
   
     for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
     {
      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)continue;
      if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
           if(OrderType()==OP_BUY)
           {
            LongTrade=true;
            ShortTrade=false;
            LastBuyLots=OrderLots();
            break;
           }
      if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
           if(OrderType()==OP_SELL)
           {
            LongTrade=false;
            ShortTrade=true;
            LastSellLots=OrderLots();
            break;
           }
     }

     if(total>0 && total<=MaxTrades)
     {
      RefreshRates();
      LastBuyPrice=LastBuyPrice();
      LastSellPrice=LastSellPrice();
      
        if (ExtremeProtection == false)
        {
         //PipsNext=75;
        }
        else
        {
         PipsNext=75*total*2.5;
        }
      
        if(LongTrade && (LastBuyPrice - Ask)>=(PipsNext*0.0001))
        {
         TradeNow=true;
        }
        if(ShortTrade && (Bid - LastSellPrice)>=(PipsNext*0.0001))
        {
         TradeNow=true;
        }
     }
     if (total < 1)
     {
      ShortTrade=false;
      LongTrade=false;
      TradeNow=true;
      StartEquity=AccountEquity();
     }
     if (TradeNow)
     {
      LastBuyPrice=LastBuyPrice();
      LastSellPrice=LastSellPrice();
        if(ShortTrade)
        {
           if(CloseClose)
           {
            fOrderCloseMarket(false,true);
            iLots=NormalizeDouble(LotIncre*LastSellLots,LotsDigits);
           }
           else
           {
            iLots=fGetLots(OP_SELL);
           }
           if(AddAdd)
           {
            NumOfTrades=total;
              if(iLots>0)
              {
               RefreshRates();
               ticket=OpenPendingOrder(OP_SELL,iLots,Bid,slip,Ask,0,0,EAName+"-"+NumOfTrades,MagicNumber,0,HotPink);
               if(ticket<0){Print("Error: ",GetLastError()); return(0);}
               LastSellPrice=LastSellPrice();
               TradeNow=false;
               FreshOrdP=true;
              }
           }
        }
        else if (LongTrade)
        {
              if(CloseClose)
              {
               fOrderCloseMarket(true,false);
               iLots=NormalizeDouble(LotIncre*LastBuyLots,LotsDigits);
              }
              else
              {
               iLots=fGetLots(OP_BUY);
              }
              if(AddAdd)
              {
               NumOfTrades=total;
                 if(iLots>0)
                 {
                  //ticket=OpenPendingOrder(OP_BUY,iLots,Ask,slip,Bid,0,0,EAName+"-"+NumOfTrades,MagicNumber,0,Lime);
                  if(ticket<0)
                  {Print("Error: ",GetLastError()); return(0);}
                  LastBuyPrice=LastBuyPrice();
                  TradeNow=false;
                  FreshOrdP=true;
                 }
              }
           }
     }
     if (TradeNow && total<1)
     {
      PrevCl=iClose(Symbol(),0,2);
      CurrCl=iClose(Symbol(),0,1);
      SellLimit=Bid;
      BuyLimit=Ask;
        if(!ShortTrade && !LongTrade)
        {
         NumOfTrades=total;
           if(PrevCl > CurrCl)
           {
            iLots=fGetLots(OP_SELL);
              if(iLots>0)
              {
               ticket=OpenPendingOrder(OP_SELL,iLots,SellLimit,slip,SellLimit,0,0,EAName+"-"+NumOfTrades,MagicNumber,0,HotPink);
               if(ticket<0){Print(iLots,"Error: ",GetLastError()); return(0);
               }
               LastBuyPrice=LastBuyPrice();
               FreshOrdP=true;
              }
           }
           else
           {
            iLots=fGetLots(OP_BUY);
              if(iLots>0)
              {     
               //ticket=OpenPendingOrder(OP_BUY,iLots,BuyLimit,slip,BuyLimit,0,0,EAName+"-"+NumOfTrades,MagicNumber,0,Lime);
               if(ticket<0){Print(iLots,"Error: ",GetLastError()); return(0);}
               LastSellPrice=LastSellPrice();
               FreshOrdP=true;
              }
           }
        }
      TradeNow=false;
     }
   total=CountTrades();
   AveragePrice=0;
   Count=0;
   for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)
         continue;
      if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
         if(OrderType()==OP_BUY || OrderType()==OP_SELL)
           {
            AveragePrice=AveragePrice+OrderOpenPrice()*OrderLots();
            Count=Count + OrderLots();
           }
     }
   if(total > 0)
      AveragePrice=NormalizeDouble(AveragePrice/Count, Digits);
   if(FreshOrdP)
      for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
        {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)
            continue;
         if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
            if(OrderType()==OP_BUY)
              {
               PriceTarget=AveragePrice+(TakeProfit*0.0001);
               BuyTarget=PriceTarget;
               Stopper=AveragePrice-(Stoploss*Point);
               flag=1;
              }
         if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
            if(OrderType()==OP_SELL)
              {
               PriceTarget=AveragePrice-(TakeProfit*0.0001);
               SellTarget=PriceTarget;
               Stopper=AveragePrice+(Stoploss*Point);
               flag=1;
              }
        }
   if(FreshOrdP)
      if(flag==1)
        {
         for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
           {
            OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
            if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)
               continue;
            if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
            {
              if(OrderStopLoss()>0)
              {
                if(OrderModify(OrderTicket(),AveragePrice,OrderStopLoss(),PriceTarget,0,Yellow)==true)
                {
                  if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true && OrderType()==OP_BUY)
                    SendMail("Aeron: Buy order modified",
                         "Order Type: BUY\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
                  if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true && OrderType()==OP_SELL)
                    SendMail("Aeron: Sell order modified",
                         "Order Type: SELL\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
                }
              }
              else
              {
                if(OrderModify(OrderTicket(),AveragePrice,OrderOpenPrice()+Stoploss*0.0001,PriceTarget,0,Yellow)==true)
                {
                  if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true && OrderType()==OP_BUY)
                    SendMail("Aeron: Buy order modified",
                         "Order Type: BUY\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
                  if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true && OrderType()==OP_SELL)
                    SendMail("Aeron: Sell order modified",
                         "Order Type: SELL\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
                }
              }
            }
            FreshOrdP=false;
           }
        }
  
   }
   else
   {
      Alert("Please Activate your copy\nof AeronForexAutoTrader");
   }
     }
  else
  {
   Alert("Works only on EURUSD 1 Minute Chart", "\nbut You are trying to use it on ", Symbol(), " ", Period());
  }
}
}
else
{
{  
  if (TradeSafe==true)
   {
      TradeNow=false;
      MagicNumber=MagicNumber3;
   }
   else
   {
      MagicNumber=12378;
   }
  
  
  //int ppp=Period();
  if (ppp==1 && "EURUSD"==StringSubstr(Symbol(),0,6))
  {  
   //double ActivKey, calcKey, len, i, j, k;
   ActivKey=0; calcKey=0; len=0; i=0; j=0; k=0;
   //string ii;
   
   if (IsDemo()==true)
   {      
/*code deleted*/
   }
   else
   {
/*code deleted*/
   }
    
   if ((IsDemo()==false && ActivKey==calcKey) || IsDemo()==true)
   {
      //string sComm;
      sComm="Aeron Forex Auto Trader by AERON";
   
      sComm=sComm+"\nBroker: " + AccountCompany();
      if (IsDemo()==true)
      {
         sComm=sComm+"\nDemo Account: " + AccountNumber();
      }
      else
      {
         sComm=sComm+"\nReal Account: " + AccountNumber();   
      }
   
   ////////////////////
   flagTrade=false;
   //double LBLots;
   //double LSLots;
   //int ccc;
   LBLots=0;
   LSLots=0;
   ccc=0;
   
   if (TradeSafe==true)
   {
     FreshOrdP=false;
     
     int m5;
    if (CountTrades()==0)
    {
      n3=1;
    }
    if (CountTrades()>1)
    {
      for(m5=0;m5<=OrdersTotal()-1;m5++)
      {
        OrderSelect(m5,SELECT_BY_POS,MODE_TRADES);
        if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber3))
        {
          if(OrderType()==OP_BUY)
          { 
            if (((TimeCurrent()-OrderOpenTime()) > 4*60*60*n3 && (TimeCurrent()-OrderOpenTime()) < 24*60*60) || (TimeCurrent()-OrderOpenTime()) > 72*60*60)
            {
              n3=n3+1;
              if (OrderLots()>=First_Lots_Size)
              {
                body1="Order Type: BUY\nCurrency Symbol: " + Symbol() +
                  "\nOrder Ticket: " + OrderTicket() + 
                  "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                  "\nLots Size: " + First_Lots_Size +
                  "\nOrder Open Price: " + OrderOpenPrice() + 
                  "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                  "\nOrder close price: " + NormalizeDouble(Bid,Digits) +
                  "\nOrder Comment: " + OrderComment() +
                  "\nOrder Magic Number: " + OrderMagicNumber();
                if(OrderClose(OrderTicket(),First_Lots_Size,NormalizeDouble(Bid,Digits),2)==true)
                   SendMail("Aeron: Buy order closed",body1);
              }
              //////////////////
              break;
            }
            if (TimeCurrent()-OrderOpenTime() > 60)
            {
              int m6;
              for(m6=OrdersTotal()-1;m6>=0;m6--)
              {
                OrderSelect(m6,SELECT_BY_POS,MODE_TRADES);
                if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber3))
                {
                  if(OrderType()==OP_BUY && Bid-OrderOpenPrice() > TakeProfit*0.0001)
                  {
                    body1="Order Type: BUY\nCurrency Symbol: " + Symbol() +
                     "\nOrder Ticket: " + OrderTicket() + 
                     "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                     "\nLots Size: " + OrderLots() +
                     "\nOrder Open Price: " + OrderOpenPrice() + 
                     "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                     "\nOrder close price: " + NormalizeDouble(Bid,Digits) +
                     "\nOrder Comment: " + OrderComment() +
                     "\nOrder Magic Number: " + OrderMagicNumber();
                    if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),2)==true)
                        SendMail("Aeron: Buy order closed",body1);
                    FreshOrdP=true;
                    break;
                  }
                }
              }
            }
          }
          if(OrderType()==OP_SELL)
          { 
            if (((TimeCurrent()-OrderOpenTime()) > 4*60*60*n3 && (TimeCurrent()-OrderOpenTime()) < 24*60*60) || (TimeCurrent()-OrderOpenTime()) > 72*60*60)
            {
              
              n3=n3+1;
              if (OrderLots()>=First_Lots_Size)
              {
                body1="Order Type: SELL\nCurrency Symbol: " + Symbol() +
                  "\nOrder Ticket: " + OrderTicket() + 
                  "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                  "\nLots Size: " + First_Lots_Size +
                  "\nOrder Open Price: " + OrderOpenPrice() + 
                  "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                  "\nOrder close price: " + NormalizeDouble(Ask,Digits) +
                  "\nOrder Comment: " + OrderComment() +
                  "\nOrder Magic Number: " + OrderMagicNumber();
                if(OrderClose(OrderTicket(),First_Lots_Size,NormalizeDouble(Ask,Digits),2)==true)
                   SendMail("Aeron: Sell order closed",body1);
              }
              ///////////////////
              break;
            }
            if (TimeCurrent()-OrderOpenTime() > 60)
            {
              int m7;
              for(m7=OrdersTotal()-1;m7>=0;m7--)
              {
                OrderSelect(m7,SELECT_BY_POS,MODE_TRADES);
                if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber3))
                {
                  if(OrderType()==OP_BUY && OrderOpenPrice()-Ask > TakeProfit*0.0001)
                  {
                    body1="Order Type: BUY\nCurrency Symbol: " + Symbol() +
                     "\nOrder Ticket: " + OrderTicket() + 
                     "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                     "\nLots Size: " + OrderLots() +
                     "\nOrder Open Price: " + OrderOpenPrice() + 
                     "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                     "\nOrder close price: " + NormalizeDouble(Ask,Digits) +
                     "\nOrder Comment: " + OrderComment() +
                     "\nOrder Magic Number: " + OrderMagicNumber();
                    if(OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),2)==true)
                        SendMail("Aeron: Buy order closed",body1);
                    FreshOrdP=true;
                    break;
                  }
                }
              }
            }
          }
        }
      }
    }
     
     if (AccountLeverage() != 999)
     {
       prcRequired=AccountFreeMargin()*(FreeMargin_for_AutoLots/100);
       mrgRequired=MarketInfo(Symbol(),MODE_MARGINREQUIRED) * MarketInfo(Symbol(),MODE_MINLOT);
       if(AutoLots==false)
       {
         Lots=First_Lots_Size;
       }
       
       int h3;
       if ( mrgRequired <= prcRequired )
       {
         //sComm=sComm+"\nYou are doing Safe Trading";
         flagTrade=true;
         h3=mrgRequired * (100/FreeMargin_for_AutoLots) * AccountLeverage()/100;
         sComm=sComm+"\nFree Margin should be atleast: " + DoubleToStr(h3*0.75,0) + " [recommended]";
         forResetLoss=h3*0.75;
       }
       else
       {
         h3=mrgRequired * (100/FreeMargin_for_AutoLots) * AccountLeverage()/100;
         sComm=sComm+"\nFree Margin should be atleast: " + DoubleToStr(h3*0.75,0) + " [recommended]";
         forResetLoss=h3*0.75;
       }
     }
     else
     {
       Lots=0;
       ////Alert("Account Leverage should be 100 or LESS\nYour Account Leverage is: ", AccountLeverage());
     }
   }
   else
   {
      //sComm=sComm+"\nYou are doing Trading at Your Own Risk";
   }
      
   tp=TakeProfit;
   //sComm=sComm+"\nTake Profit: " + tp;
   Comment(sComm);
   
     if(timeprev1==Time[0])
     {
      return(0);
     }
     
   timeprev1=Time[0];
   CurrentPairProfit=CalculateProfit();
   total=CountTrades();
   
   PipsNext=PositionsGap;
   if(total>=1)
   {
     PipsNext=PositionsGap * MathPow(PositionsGap_Factor,(total-1));
   }
     
     if (total==0)
     {
      flag=0;
     }
   //double LastBuyLots;
   //double LastSellLots;
   LastBuyLots=0;
   LastSellLots=0;
   
     for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
     {
      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)continue;
      if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
           if(OrderType()==OP_BUY)
           {
            LongTrade=true;
            ShortTrade=false;
            LastBuyLots=OrderLots();
            break;
           }
      if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
           if(OrderType()==OP_SELL)
           {
            LongTrade=false;
            ShortTrade=true;
            LastSellLots=OrderLots();
            break;
           }
     }

     if(total>0 && total<=MaxTrades)
     {
      RefreshRates();
      LastBuyPrice=LastBuyPrice();
      LastSellPrice=LastSellPrice();
      
        if (ExtremeProtection == false)
        {
         //PipsNext=75;
        }
        else
        {
         PipsNext=75*total*2.5;
        }
      
        if(LongTrade && (LastBuyPrice - Ask)>=(PipsNext*0.0001))
        {
         TradeNow=true;
        }
        if(ShortTrade && (Bid - LastSellPrice)>=(PipsNext*0.0001))
        {
         TradeNow=true;
        }
     }
     if (total < 1)
     {
      ShortTrade=false;
      LongTrade=false;
      TradeNow=true;
      StartEquity=AccountEquity();
     }
     if (TradeNow)
     {
      LastBuyPrice=LastBuyPrice();
      LastSellPrice=LastSellPrice();
        if(ShortTrade)
        {
           if(CloseClose)
           {
            fOrderCloseMarket(false,true);
            iLots=NormalizeDouble(LotIncre*LastSellLots,LotsDigits);
           }
           else
           {
            iLots=fGetLots(OP_SELL);
           }
           if(AddAdd)
           {
            NumOfTrades=total;
              if(iLots>0)
              {
               RefreshRates();
               ticket=OpenPendingOrder(OP_SELL,iLots,Bid,slip,Ask,0,0,EAName+"-"+NumOfTrades,MagicNumber,0,HotPink);
               if(ticket<0){Print("Error: ",GetLastError()); return(0);}
               LastSellPrice=LastSellPrice();
               TradeNow=false;
               FreshOrdP=true;
              }
           }
        }
        else if (LongTrade)
        {
              if(CloseClose)
              {
               fOrderCloseMarket(true,false);
               iLots=NormalizeDouble(LotIncre*LastBuyLots,LotsDigits);
              }
              else
              {
               iLots=fGetLots(OP_BUY);
              }
              if(AddAdd)
              {
               NumOfTrades=total;
                 if(iLots>0)
                 {
                  ticket=OpenPendingOrder(OP_BUY,iLots,Ask,slip,Bid,0,0,EAName+"-"+NumOfTrades,MagicNumber,0,Lime);
                  if(ticket<0)
                  {Print("Error: ",GetLastError()); return(0);}
                  LastBuyPrice=LastBuyPrice();
                  TradeNow=false;
                  FreshOrdP=true;
                 }
              }
           }
     }
     if (TradeNow && total<1)
     {
      PrevCl=iClose(Symbol(),0,2);
      CurrCl=iClose(Symbol(),0,1);
      SellLimit=Bid;
      BuyLimit=Ask;
        if(!ShortTrade && !LongTrade)
        {
         NumOfTrades=total;
           if(PrevCl > CurrCl)
           {
            iLots=fGetLots(OP_SELL);
              if(iLots>0)
              {
               ticket=OpenPendingOrder(OP_SELL,iLots,SellLimit,slip,SellLimit,0,0,EAName+"-"+NumOfTrades,MagicNumber,0,HotPink);
               if(ticket<0){Print(iLots,"Error: ",GetLastError()); return(0);
               }
               LastBuyPrice=LastBuyPrice();
               FreshOrdP=true;
              }
           }
           else
           {
            iLots=fGetLots(OP_BUY);
              if(iLots>0)
              {     
               ticket=OpenPendingOrder(OP_BUY,iLots,BuyLimit,slip,BuyLimit,0,0,EAName+"-"+NumOfTrades,MagicNumber,0,Lime);
               if(ticket<0){Print(iLots,"Error: ",GetLastError()); return(0);}
               LastSellPrice=LastSellPrice();
               FreshOrdP=true;
              }
           }
        }
      TradeNow=false;
     }
   total=CountTrades();
   AveragePrice=0;
   Count=0;
   for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)
         continue;
      if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
         if(OrderType()==OP_BUY || OrderType()==OP_SELL)
           {
            AveragePrice=AveragePrice+OrderOpenPrice()*OrderLots();
            Count=Count + OrderLots();
           }
     }
   if(total > 0)
      AveragePrice=NormalizeDouble(AveragePrice/Count, Digits);
   if(FreshOrdP)
      for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
        {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)
            continue;
         if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
            if(OrderType()==OP_BUY)
              {
               PriceTarget=AveragePrice+(TakeProfit*0.0001);
               BuyTarget=PriceTarget;
               Stopper=AveragePrice-(Stoploss*Point);
               flag=1;
              }
         if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
            if(OrderType()==OP_SELL)
              {
               PriceTarget=AveragePrice-(TakeProfit*0.0001);
               SellTarget=PriceTarget;
               Stopper=AveragePrice+(Stoploss*Point);
               flag=1;
              }
        }
   if(FreshOrdP)
      if(flag==1)
        {
         for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
           {
            OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
            if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)
               continue;
            if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
            {
              if(OrderStopLoss()>0)
              {
                if(OrderModify(OrderTicket(),AveragePrice,OrderStopLoss(),PriceTarget,0,Yellow)==true)
                {
                  if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true && OrderType()==OP_BUY)
                    SendMail("Aeron: Buy order modified",
                         "Order Type: BUY\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
                  if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true && OrderType()==OP_SELL)
                    SendMail("Aeron: Sell order modified",
                         "Order Type: SELL\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
                }
              }
              else
              {
              //Alert(OrderOpenPrice()+Stoploss*0.0001);
                if(OrderType()==OP_SELL)
                {
                  if(OrderModify(OrderTicket(),AveragePrice,OrderOpenPrice()+Stoploss*0.0001,PriceTarget,0,Yellow)==true)
                  {
                    if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true && OrderType()==OP_SELL)
                      SendMail("Aeron: Sell order modified",
                         "Order Type: SELL\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
                  }
                }
                else
                if(OrderType()==OP_BUY)
                {
                  if(OrderModify(OrderTicket(),AveragePrice,OrderOpenPrice()-Stoploss*0.0001,PriceTarget,0,Yellow)==true)
                  {
                    if(OrderSelect(OrderTicket(), SELECT_BY_TICKET)==true && OrderType()==OP_BUY)
                      SendMail("Aeron: Buy order modified",
                         "Order Type: BUY\nCurrency Symbol: " + Symbol() +
                         "\nOrder Ticket: " + OrderTicket() + 
                         "\nOrder Modification time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                         "\nLots Size: " + OrderLots() +
                         "\nOrder Open Price: " + OrderOpenPrice() + 
                         "\nStoploss: " + OrderStopLoss() + 
                         "\nTakeprofit: " + OrderTakeProfit() +
                         "\nOrder Comment: " + OrderComment() +
                         "\nOrder Magic Number: " + OrderMagicNumber());
                  }
                }
              }
            }
            FreshOrdP=false;
           }
        }
  
   }
   else
   {
      Alert("Please Activate your copy\nof AeronForexAutoTrader");
   }
     }
  else
  {
   Alert("Works only on EURUSD 1 Minute Chart", "\nbut You are trying to use it on ", Symbol(), " ", Period());
  }
}
}  

}

}
  else
  {
  Alert("License Expired");
  }

}
double ND(double v){return(NormalizeDouble(v,Digits));}
  int fOrderCloseMarket(bool aCloseBuy=true,bool aCloseSell=true)
  {
   int tErr=0;
     for(int i=OrdersTotal()-1;i>=0;i--)
     {
        if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
           if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
           {
              if(OrderType()==OP_BUY && aCloseBuy)
              {
               RefreshRates();
                 if(!IsTradeContextBusy())
                 {
                    body1="Order Type: BUY\nCurrency Symbol: " + Symbol() +
                      "\nOrder Ticket: " + OrderTicket() + 
                      "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                      "\nLots Size: " + OrderLots() +
                      "\nOrder Open Price: " + OrderOpenPrice() + 
                      "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                      "\nOrder close price: " + NormalizeDouble(Bid,Digits) +
                      "\nOrder Comment: " + OrderComment() +
                      "\nOrder Magic Number: " + OrderMagicNumber();
                    if(!OrderClose(OrderTicket(),OrderLots(),ND(Bid),5,CLR_NONE))
                    {
                     Print("Error close BUY "+OrderTicket());
                     tErr=-1;
                    }
                    else
                       SendMail("Aeron: Buy order closed",body1);
                 }
                 else
                 {
                  static int lt1=0;
                    if(lt1!=iTime(NULL,0,0))
                    {
                     lt1=iTime(NULL,0,0);
                     Print("Need close BUY "+OrderTicket()+". Trade Context Busy");
                    }
                  return(-2);
                 }
              }
              if(OrderType()==OP_SELL && aCloseSell)
              {
               RefreshRates();
                 if(!IsTradeContextBusy())
                 {
                    body1="Order Type: SELL\nCurrency Symbol: " + Symbol() +
                      "\nOrder Ticket: " + OrderTicket() + 
                      "\nOrder open time: " + TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS) +
                      "\nLots Size: " + OrderLots() +
                      "\nOrder Open Price: " + OrderOpenPrice() + 
                      "\nOrder close time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                      "\nOrder close price: " + NormalizeDouble(Ask,Digits) +
                      "\nOrder Comment: " + OrderComment() +
                      "\nOrder Magic Number: " + OrderMagicNumber();
                    if(!OrderClose(OrderTicket(),OrderLots(),ND(Ask),5,CLR_NONE))
                    {
                     Print("Error close SELL "+OrderTicket());
                     tErr=-1;
                    }
                    else
                       SendMail("Aeron: Sell order closed",body1);
                 }
                 else
                 {
                  static int lt2=0;
                    if(lt2!=iTime(NULL,0,0))
                    {
                     lt2=iTime(NULL,0,0);
                     Print("Need close SELL "+OrderTicket()+". Trade Context Busy");
                    }
                  return(-2);
                 }
              }
           }
        }
     }
   return(tErr);
  }
  double fGetLots(int aTradeType)
  {
   double tLots;
     switch(MMType)
     {   
         case 0:
            tLots=Lots;
            break;
         case 1:
           if(lotsdigit5 != 1)
           {  
             tLots=NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),LotsDigits);
             //iLots=NormalizeDouble(LotIncre*LastSellLots,LotsDigits);
           }
           else
           {
             tLots=NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),2);
             tLots=tLots-NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),1);
             //Alert(NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),2));
             //Alert(NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),1));
             tLots1=NormalizeDouble(tLots,2);
             
             //Alert(tLots1);
             if(tLots1==0)
             {
               tLots=NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),1);
             }
             if(tLots1==0.01)
             {
               tLots=NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),1);
             }
             if(tLots1==0.02)
             {
               tLots=NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),1);
             }
             if(tLots1==0.03)
             {
               tLots=NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),1) + 0.05; 
             }
             if(tLots1==0.04)
             {
               tLots=NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),1) + 0.05; 
             }
             if(tLots1==-0.05)
             {
              //Alert("sdfgsdgsd55555");
               tLots=NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),1) - 0.10 + 0.05; 
             }
             if(tLots1==-0.04)
             {
               tLots=NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),1) - 0.10 + 0.05; 
             }
             if(tLots1==-0.03)
             {
               tLots=NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),1) - 0.10 + 0.05; 
             }
             //Alert(tLots1+"safasF");
             if(tLots1==-0.02)
             {
               tLots=NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),1);
              // Alert("sdfgsdgsd22222");
             }
             if(tLots1==-0.01)
             {
               tLots=NormalizeDouble(Lots*MathPow(LotIncre,CountTrades()),1);
             }
           }
           
           break;
         case 2:
            int LastClosedTime=0;
            tLots=Lots;
              for(int i=OrdersHistoryTotal()-1;i>=0;i--)
              {
                 if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
                 {
                    if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
                    {
                       if(LastClosedTime<OrderCloseTime())
                       {
                        LastClosedTime=OrderCloseTime();
                          if(OrderProfit()<0)
                          {
                           tLots=NormalizeDouble(OrderLots()*LotIncre,LotsDigits);
                          }
                          else
                          {
                           tLots=Lots;
                          }
                       }
                    }
                 }
                 else
                 {
                  return(-3);
                 }
              }
            break;
        }
        if(AccountFreeMarginCheck(Symbol(),aTradeType,tLots)<=0)
        {
         return(-1);
        }
        if(GetLastError()==134)
        {
         return(-2);
        }
      return(tLots);
     }
         int CountTrades()
           {
            int count=0;
            int trade;
            for(trade=OrdersTotal()-1;trade>=0;trade--)
              {
               OrderSelect(trade,SELECT_BY_POS,MODE_TRADES);
               if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)
                  continue;
               if(OrderSymbol()==Symbol()&&OrderMagicNumber()==MagicNumber)
                  if(OrderType()==OP_SELL || OrderType()==OP_BUY)
                     count++;
              }
            return(count);
           }
           int CountTradesB()
           {
            int count=0;
            int trade;
            for(trade=OrdersTotal()-1;trade>=0;trade--)
              {
               OrderSelect(trade,SELECT_BY_POS,MODE_TRADES);
               //if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber1)
                 // continue;
               if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber2 || OrderMagicNumber()==MagicNumber3))
                  if(OrderType()==OP_BUY)
                     count++;
              }
            return(count);
           }
           int CountTradesS()
           {
            int count=0;
            int trade;
            for(trade=OrdersTotal()-1;trade>=0;trade--)
              {
               OrderSelect(trade,SELECT_BY_POS,MODE_TRADES);
               //if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber2)
                 // continue;
               if(OrderSymbol()==Symbol()&&(OrderMagicNumber()==MagicNumber2 || OrderMagicNumber()==MagicNumber3 ))
                  if(OrderType()==OP_SELL)
                     count++;
              }
            return(count);
           }

         int OpenPendingOrder(int pType,double pLots,double pLevel,int sp, double pr, int sl, int tp,string pComment,int pMagic,datetime pExpiration,color pColor)
           {
            int ticket=0;
            int err=0;
            int c=0;
            int NumberOfTries=100;
            switch(pType)
              {
               case OP_BUY:
                  if(TimeCurrent() > closeDuration1)
                  {
                  for(c=0;c < NumberOfTries;c++)
                    {
                     RefreshRates();
                     
                     indIch=iIchimoku(Symbol(),0,9,26,52,1,0);
                     indMA=iMA(Symbol(),0,1,0,MODE_SMA,PRICE_CLOSE,0);
                     indENV2=iEnvelopes(Symbol(),0,99,MODE_EMA,0,PRICE_CLOSE,0.08,MODE_LOWER,1);

                     if (High[0]<High[1]&&High[1]<High[2]&&High[2]<High[3]&&Open[0]<Open[1]&&Open[1]<Open[2]&&Open[2]<Open[3])
                     {
                     }
                     else
                     {
                                          
                     if (Ask<indENV2 && CloseAllTrade==false)
                     //if (High[0]>High[1]&&High[1]>High[2]&&High[2]>High[3]&&Open[0]>Open[1]&&Open[1]>Open[2]&&Open[2]>Open[3])
                     {
                       if (CountTrades()==1)
                       {
                         indENV22=iEnvelopes(Symbol(),0,99,MODE_EMA,0,PRICE_CLOSE,0.15,MODE_LOWER,1);
                         if(Ask>indENV22)
                         //if (High[0]>High[1]&&High[1]>High[2]&&High[2]>High[3]&&Open[0]>Open[1]&&Open[1]>Open[2]&&Open[2]>Open[3])
                         {
                           ticket=OrderSend(Symbol(),OP_BUY,pLots,NormalizeDouble(Ask,Digits),sp,StopLong(Bid,sl),TakeLong(Ask,tp),OrderComments,pMagic,pExpiration,pColor);
                           if(OrderSelect(ticket, SELECT_BY_TICKET)==true)
                             SendMail("Aeron: Buy order opened",
                                      "Order Type: BUY\nCurrency Symbol: " + Symbol() +
                                      "\nOrder Ticket: " + ticket + 
                                      "\nOrder open time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                                      "\nLots Size: " + OrderLots() +
                                      "\nOrder Price: " + OrderOpenPrice() + 
                                      "\nStoploss: " + OrderStopLoss() + 
                                      "\nTakeprofit: " + OrderTakeProfit() +
                                      "\nOrder Comment: " + OrderComment() +
                                      "\nOrder Magic Number: " + OrderMagicNumber());
                           closeDuration1=0;
                           break;
                         }
                       }
                       else
                       {
                         //ticket=OrderSend(Symbol(),OP_BUY,pLots,NormalizeDouble(Ask,Digits),sp,StopLong(Bid,sl),TakeLong(Ask,tp),pComment,pMagic,pExpiration,pColor);
                         //break;
                       }
                       if (CountTrades() >= 2)
                       {
                         indENV22=iEnvelopes(Symbol(),0,99,MODE_EMA,0,PRICE_CLOSE,0.2,MODE_LOWER,1);
                         if(Ask>indENV22)
                         //if (High[0]>High[1]&&High[1]>High[2]&&High[2]>High[3]&&Open[0]>Open[1]&&Open[1]>Open[2]&&Open[2]>Open[3])
                         {
                           ticket=OrderSend(Symbol(),OP_BUY,pLots,NormalizeDouble(Ask,Digits),sp,StopLong(Bid,sl),TakeLong(Ask,tp),OrderComments,pMagic,pExpiration,pColor);
                           if(OrderSelect(ticket, SELECT_BY_TICKET)==true)
                              SendMail("Aeron: Buy order opened",
                                  "Order Type: BUY\nCurrency Symbol: " + Symbol() +
                                  "\nOrder Ticket: " + ticket + 
                                  "\nOrder open time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                                  "\nLots Size: " + OrderLots() +
                                  "\nOrder Price: " + OrderOpenPrice() + 
                                  "\nStoploss: " + OrderStopLoss() + 
                                  "\nTakeprofit: " + OrderTakeProfit() +
                                  "\nOrder Comment: " + OrderComment() +
                                  "\nOrder Magic Number: " + OrderMagicNumber());                           
                           closeDuration1=0;
                           break;
                         }
                       }
                       else
                       {
                         ticket=OrderSend(Symbol(),OP_BUY,pLots,NormalizeDouble(Ask,Digits),sp,StopLong(Bid,sl),TakeLong(Ask,tp),OrderComments,pMagic,pExpiration,pColor);
                         if(OrderSelect(ticket, SELECT_BY_TICKET)==true)
                            SendMail("Aeron: Buy order opened",
                                  "Order Type: BUY\nCurrency Symbol: " + Symbol() +
                                  "\nOrder Ticket: " + ticket + 
                                  "\nOrder open time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                                  "\nLots Size: " + OrderLots() +
                                  "\nOrder Price: " + OrderOpenPrice() + 
                                  "\nStoploss: " + OrderStopLoss() + 
                                  "\nTakeprofit: " + OrderTakeProfit() +
                                  "\nOrder Comment: " + OrderComment() +
                                  "\nOrder Magic Number: " + OrderMagicNumber());
                         closeDuration1=0;
                         break;
                       }
                     }
                     
                     }
                     
                     err=GetLastError();
                     if(err==0)
                       {
                        break;
                       }
                     else
                       {
                        if(err==4 || err==137 ||err==146 || err==136)
                          {
                           Sleep(5000);
                           continue;
                          }
                        else
                          {
                           break;
                          }
                       }
                    }
                  break;
                  }
               case OP_SELL:
                  if(TimeCurrent() > closeDuration2)
                  {
                  for(c=0;c < NumberOfTries;c++)
                    {
                    RefreshRates();
                    
                    indIch=iIchimoku(Symbol(),0,9,26,52,1,0);
                    indMA=iMA(Symbol(),0,1,0,MODE_SMA,PRICE_CLOSE,0);
                    indENV1=iEnvelopes(Symbol(),0,99,MODE_EMA,0,PRICE_CLOSE,0.08,MODE_UPPER,1);
                    
                    if (High[0]>High[1]&&High[1]>High[2]&&High[2]>High[3]&&Open[0]>Open[1]&&Open[1]>Open[2]&&Open[2]>Open[3])
                    {
                    }
                    else
                    {
                    
                    if (Bid>indENV1 && CloseAllTrade==false)                     
                    //if (High[0]<High[1]&&High[1]<High[2]&&High[2]<High[3]&&Open[0]<Open[1]&&Open[1]<Open[2]&&Open[2]<Open[3])
                    {
                      if(CountTrades()==1)
                      {
                        indENV11=iEnvelopes(Symbol(),0,99,MODE_EMA,0,PRICE_CLOSE,0.15,MODE_UPPER,1);
                        if (Bid<indENV11)
                        if (High[0]<High[1]&&High[1]<High[2]&&High[2]<High[3]&&Open[0]<Open[1]&&Open[1]<Open[2]&&Open[2]<Open[3])
                        {
                          ticket=OrderSend(Symbol(),OP_SELL,pLots,NormalizeDouble(Bid,Digits),sp,StopShort(Ask,sl),TakeShort(Bid,tp),OrderComments,pMagic,pExpiration,pColor);
                          if(OrderSelect(ticket, SELECT_BY_TICKET)==true)
                             SendMail("Aeron: Sell order opened",
                                  "Order Type: SELL\nCurrency Symbol: " + Symbol() +
                                  "\nOrder Ticket: " + ticket + 
                                  "\nOrder open time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                                  "\nLots Size: " + OrderLots() +
                                  "\nOrder Price: " + OrderOpenPrice() + 
                                  "\nStoploss: " + OrderStopLoss() + 
                                  "\nTakeprofit: " + OrderTakeProfit() +
                                  "\nOrder Comment: " + OrderComment() +
                                  "\nOrder Magic Number: " + OrderMagicNumber());
                          closeDuration2=0;
                          break;
                        }
                      }
                      else
                      {
                        //ticket=OrderSend(Symbol(),OP_SELL,pLots,NormalizeDouble(Bid,Digits),sp,StopShort(Ask,sl),TakeShort(Bid,tp),pComment,pMagic,pExpiration,pColor);
                        //break;
                      }
                      if(CountTrades() >= 2)
                      {
                        indENV11=iEnvelopes(Symbol(),0,99,MODE_EMA,0,PRICE_CLOSE,0.2,MODE_UPPER,1);
                        if (Bid<indENV11)
                        if (High[0]<High[1]&&High[1]<High[2]&&High[2]<High[3]&&Open[0]<Open[1]&&Open[1]<Open[2]&&Open[2]<Open[3])
                        {
                          ticket=OrderSend(Symbol(),OP_SELL,pLots,NormalizeDouble(Bid,Digits),sp,StopShort(Ask,sl),TakeShort(Bid,tp),OrderComments,pMagic,pExpiration,pColor);
                          if(OrderSelect(ticket, SELECT_BY_TICKET)==true)
                             SendMail("Aeron: Sell order opened",
                                  "Order Type: SELL\nCurrency Symbol: " + Symbol() +
                                  "\nOrder Ticket: " + ticket + 
                                  "\nOrder open time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                                  "\nLots Size: " + OrderLots() +
                                  "\nOrder Price: " + OrderOpenPrice() + 
                                  "\nStoploss: " + OrderStopLoss() + 
                                  "\nTakeprofit: " + OrderTakeProfit() +
                                  "\nOrder Comment: " + OrderComment() +
                                  "\nOrder Magic Number: " + OrderMagicNumber());
                          closeDuration2=0;
                          break;
                        }
                      }
                      else
                      {
                        ticket=OrderSend(Symbol(),OP_SELL,pLots,NormalizeDouble(Bid,Digits),sp,StopShort(Ask,sl),TakeShort(Bid,tp),OrderComments,pMagic,pExpiration,pColor);
                        if(OrderSelect(ticket, SELECT_BY_TICKET)==true)
                             SendMail("Aeron: Sell order opened",
                                  "Order Type: SELL\nCurrency Symbol: " + Symbol() +
                                  "\nOrder Ticket: " + ticket + 
                                  "\nOrder open time: " + TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + 
                                  "\nLots Size: " + OrderLots() +
                                  "\nOrder Price: " + OrderOpenPrice() + 
                                  "\nStoploss: " + OrderStopLoss() + 
                                  "\nTakeprofit: " + OrderTakeProfit() +
                                  "\nOrder Comment: " + OrderComment() +
                                  "\nOrder Magic Number: " + OrderMagicNumber());
                        closeDuration2=0;
                        break;
                      }
                    }
                    
                    }
                     
                    err=GetLastError();
                     if(err==0)
                       {
                        break;
                       }
                     else
                       {
                        if(err==4 || err==137 ||err==146 || err==136)
                          {
                           Sleep(5000);
                           continue;
                          }
                        else
                          {
                           break;
                          }
                       }
                    }
                  break;
                  }
              }

            return(ticket);
           }
         double StopLong(double price,int stop)
           {
            if(stop==0)
               return(0);
            else
               return(price-(stop*Point));
           }
         double StopShort(double price,int stop)
           {
            if(stop==0)
               return(0);
            else
               return(price+(stop*Point));
           }
         double TakeLong(double price,int take)
           {
            if(take==0)
               return(0);
            else
               return(price+(take*Point));
           }
         double TakeShort(double price,int take)
           {
            if(take==0)
               return(0);
            else
               return(price-(take*Point));
           }
         double CalculateProfit()
           {
            double Profit=0;
            for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
              {
               OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
               if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)
                  continue;
               if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
                  if(OrderType()==OP_BUY || OrderType()==OP_SELL)
                    {
                     Profit=Profit+OrderProfit();
                    }
              }
            return(Profit);
           }
         double LastBuyPrice()
           {
            double oldorderopenprice=0, orderprice;
            int cnt, oldticketnumber=0, ticketnumber;
            for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
              {
               OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
               if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)
                  continue;
               if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber && OrderType()==OP_BUY)
                 {
                  ticketnumber=OrderTicket();
                  if(ticketnumber>oldticketnumber)
                    {
                     orderprice=OrderOpenPrice();
                     oldorderopenprice=orderprice;
                     oldticketnumber=ticketnumber;
                    }
                 }
              }
            return(orderprice);
           }
         double LastSellPrice()
           {
            double oldorderopenprice=0, orderprice;
            int cnt, oldticketnumber=0, ticketnumber;
            for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
              {
               OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
               if(OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber)
                  continue;
               if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber && OrderType()==OP_SELL)
                 {
                  ticketnumber=OrderTicket();
                  if(ticketnumber>oldticketnumber)
                    {
                     orderprice=OrderOpenPrice();
                     oldorderopenprice=orderprice;
                     oldticketnumber=ticketnumber;
                    }
                 }
              }
            return(orderprice);
           }