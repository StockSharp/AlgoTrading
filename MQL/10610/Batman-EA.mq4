//+------------------------------------------------------------------+
//|                                                    Batman-EA.mq4 |
//|                Copyright © 2011, 4xLine Group Farshad Saremifar. |
//|                                        http://www.4xline.com     |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, 4xLine Group,Farshad Saremifar"
#property link      "http://www.4xline.com"

//---- indicator parameters
extern int BackPeriod   = 1000;
extern int ATRPeriod   = 7;
extern double Factor = 1.1;
extern bool TypicalPrice = false;

extern string TP_SL="--------TakeProfit&Stoploss Management-------";
extern int   Static_SL=0;
extern int   Static_TP=39;
extern double fibo=1.212;
extern string MM="--------Money Management-------";
extern int Orders=4;           
extern double Lot=0.1;
extern double LotExponent = 2;  
extern int lotdecimal = 2; 
extern int Protect_Profit=0;
extern string Other="---Other Settings----";
extern int MagicNumber=1080;
extern int slippage=20;
extern string Working_Hours="-Enter Start Hours & End Time Which expert must trade-";
extern int StartHour=0;
extern int EndHour=24;
int MyPoint=1;
int New_Bar;                                             
int Time_0;
static datetime LastTradeBarTime;
bool TradeNow = FALSE, LongTrade = FALSE, ShortTrade = FALSE;
bool  NewOrdersPlaced = FALSE;
int cnt = 0;
double PriceTarget, StartEquity, BuyTarget, SellTarget;
double AveragePrice, SellLimit, BuyLimit;
double LastBuyPrice, LastSellPrice, Spread;
double Stopper = 0.0;
double LOT;
bool flag;
double distance=0;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
    
    LastTradeBarTime=Time[1];
   
 if(MarketInfo(Symbol(),MODE_DIGITS)==3||MarketInfo(Symbol(),MODE_DIGITS)==5)MyPoint=10; else MyPoint=1; 
 LOT=Lot;
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
//----
  
   New_Bar=0; 
         if (Time_0 != Time[0])                                  
              {
                  New_Bar= 1;      
                  Time_0 = Time[0]; 
               }
   int total = CountTrades();
  if (New_Bar==1)
  {
  // if(TrailingStop>0)MoveTrailingStop(MagicNumber);
  // if (Protect_Profit>0)Save_Profit(); 
//----

    double buy_bat   = iCustom(Symbol(),0,"BAT",BackPeriod,ATRPeriod,Factor,TypicalPrice,0,1);  
    double sell_bat  = iCustom(Symbol(),0,"BAT",BackPeriod,ATRPeriod,Factor,TypicalPrice,1,1); 
    double buy_bat1  = iCustom(Symbol(),0,"BAT",BackPeriod,ATRPeriod,Factor,TypicalPrice,0,2);  
    double sell_bat1 = iCustom(Symbol(),0,"BAT",BackPeriod,ATRPeriod,Factor,TypicalPrice,1,2); 
 
  int signal=0; 
  
  if (buy_bat1==EMPTY_VALUE&&buy_bat!=EMPTY_VALUE) {signal =1;distance=sell_bat1-buy_bat;}
  if (sell_bat1==EMPTY_VALUE&&sell_bat!=EMPTY_VALUE) {signal =2;distance=sell_bat-buy_bat1;}
  
   if (check_time(StartHour,EndHour))
         
         {
         double PipStep=distance/(Orders+1);
           int ticket=0;
            double StopLoss,TakeProfit,LotSize;
            if (total ==0) {
      ShortTrade = FALSE;
      LongTrade = FALSE;
      TradeNow = TRUE;
      StartEquity = AccountEquity();
   }

               if(signal==1&&LastTradeBarTime!=Time[0])
                 
                   {
                  
                      CLOSEORDER("Sell");
                      StopLoss=0;
                      
                      if (Static_TP<0)TakeProfit=NormalizeDouble(Ask+(NormalizeDouble(distance/fibo ,Digits) ),Digits);
                      else TakeProfit=NormalizeDouble(Ask+(Static_TP*Point*MyPoint),Digits);
                      
                      ticket=OPENORDERInstant("Buy",Lot,StopLoss,TakeProfit,Symbol(),MagicNumber);  
                      LastTradeBarTime=Time[0];
                      TradeNow = false;
                  }
              
               if(signal==2&&LastTradeBarTime!=Time[0])
                 
                   {
                  
                      CLOSEORDER("Buy");
                      StopLoss=0;
                      if (Static_TP<0)TakeProfit=NormalizeDouble(Bid-(NormalizeDouble(distance/fibo ,Digits)),Digits);
                      else TakeProfit=NormalizeDouble(Bid-(Static_TP*Point*MyPoint),Digits);
                      
                      ticket=OPENORDERInstant("Sell",Lot,StopLoss,TakeProfit,Symbol(),MagicNumber);  
                    LastTradeBarTime=Time[0];
                    TradeNow = false;
                  }
                  
          
         total = CountTrades(); 
                  if (total > 0 && total<Orders ) {
      RefreshRates();
       LastBuyPrice = FindLastBuyPrice();
       LastSellPrice = FindLastSellPrice();
     
      if (buy_bat!=EMPTY_VALUE  && LastBuyPrice - Ask >= PipStep ){ TradeNow = TRUE;}
      if (sell_bat!=EMPTY_VALUE && Bid - LastSellPrice >= PipStep ){ TradeNow = TRUE;}
      if (TradeNow)
      {
      double iLots = NormalizeDouble(Lot * MathPow(LotExponent, total), lotdecimal);
       if(buy_bat!=EMPTY_VALUE )
        {
                   
                      
                      
                      ticket=OPENORDERInstant("Buy",iLots,0,0,Symbol(),MagicNumber);
                     TradeNow=false;
                     LastBuyPrice = FindLastBuyPrice();
               NewOrdersPlaced = TRUE;
        }
         if(sell_bat!=EMPTY_VALUE )
        {
                   
                      ticket=OPENORDERInstant("Sell",iLots,0,0,Symbol(),MagicNumber);
                      TradeNow=false;
                      LastBuyPrice = FindLastBuyPrice();
               NewOrdersPlaced = TRUE;
        }
      } 
      total = CountTrades();
   AveragePrice = 0;
   double Count = 0;
   for (cnt = OrdersTotal() - 1; cnt >= 0; cnt--) {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol() != Symbol() || OrderMagicNumber() != MagicNumber) continue;
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber) {
         if (OrderType() == OP_BUY || OrderType() == OP_SELL) {
            AveragePrice += OrderOpenPrice() * OrderLots();
            Count += OrderLots();
         }
      }
   }
   if (total > 0) AveragePrice = NormalizeDouble(AveragePrice / Count, Digits);
   if (NewOrdersPlaced) {
      for (cnt = OrdersTotal() - 1; cnt >= 0; cnt--) {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol() != Symbol() || OrderMagicNumber() != MagicNumber) continue;
         if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber) {
            if (OrderType() == OP_BUY) {
            
            if (Static_TP<0)PriceTarget= AveragePrice + NormalizeDouble(distance/fibo ,Digits);
             else PriceTarget = AveragePrice + Static_TP*Point*MyPoint;
               
               BuyTarget = PriceTarget;
               
               flag = TRUE;
            }
         }
         if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber) {
            if (OrderType() == OP_SELL) {
            
            if (Static_TP<0)PriceTarget= AveragePrice - NormalizeDouble(distance/fibo ,Digits);
             else PriceTarget = AveragePrice - Static_TP*Point*MyPoint;
               
              
               SellTarget = PriceTarget;
              
               flag = TRUE;
            }
         }
      }
   }
   if (NewOrdersPlaced) {
      if (flag == TRUE) {
         for (cnt = OrdersTotal() - 1; cnt >= 0; cnt--) {
            OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
            if (OrderSymbol() != Symbol() || OrderMagicNumber() != MagicNumber) continue;
            if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber) OrderModify(OrderTicket(), AveragePrice, Stopper, PriceTarget, 0, Yellow);
            NewOrdersPlaced = FALSE;
         }
      }
   }  
   }
         }   
         }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+

int CountTrades() {
   int count = 0;
   for (int trade = OrdersTotal() - 1; trade >= 0; trade--) {
      OrderSelect(trade, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol() != Symbol() || OrderMagicNumber() != MagicNumber) continue;
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber)
         if (OrderType() == OP_SELL || OrderType() == OP_BUY) count++;
   }
   return (count);
}
//-------------------------------------------------------------------- 
bool check_time(int hour_start,int hour_finish)
    {
  if(hour_start>hour_finish)
       {
        if (Hour() < hour_finish || Hour() >= hour_start)return(true);
       }
else if(hour_start<hour_finish)
       {
        if(Hour()>=hour_start && Hour()<hour_finish)return(true);
       }
else if(hour_start==hour_finish)
       {
        if(Hour()==hour_start)return(true);
       }
return(false);
}
//-----------------------------------------------------------
int Save_Profit(){
if (AccountProfit()>= Protect_Profit)
   {
    for(int i=OrdersTotal()-1;i>=0;i--)
       {
       OrderSelect(i, SELECT_BY_POS);
       int type   = OrderType();
               
       bool result = false;
              
       switch(type)
          {
          //Close opened long positions
          case OP_BUY  : result = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),3,Pink);
                         break;
               
          //Close opened short positions
          case OP_SELL : result = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),3,Pink);
                         break; 
          }
          
       if(result == false)
          {
            Sleep(1000);
          }
     }
     
      Print ("Account Profit Reached. All Open Trades Have Been Closed");
      return(0);
   }  
   
}




//-------------------------------------------------
int CntOrd(string Type, int Magic,string symbol)
{
int _CntOrd=0;
int i;
   if (Type=="All")
       {

          for (i=OrdersTotal()-1; i>=0; i--)
            {                                               
             if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
               {
                 if (OrderSymbol()==symbol&&OrderMagicNumber() == Magic){_CntOrd++;}
                }    
            }
         return(_CntOrd);  
       }
   else if (Type=="Sell")
       {

          for (i=OrdersTotal()-1; i>=0; i--)
            {                                               
             if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
               {
                 if (OrderSymbol()==symbol&&OrderMagicNumber() == Magic&& OrderType()==OP_SELL ){_CntOrd++;}
                }    
            }
         return(_CntOrd);  
       }    
   else if (Type=="Buy")
       {

          for (i=OrdersTotal()-1; i>=0; i--)
            {                                               
             if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
               {
                if (OrderSymbol()==symbol&&OrderMagicNumber() == Magic&&OrderType()==OP_BUY){_CntOrd++;}
                }    
            }
         return(_CntOrd);  
       }        
  else return(0); 
}

//-----------------------------------------

void CLOSEORDER(string ord)
{
   for (int i=OrdersTotal()-1; i>=0; i--)
   {                                               
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
      {if(OrderMagicNumber()==MagicNumber)
      {
         if (OrderSymbol()!=Symbol()||OrderMagicNumber()!=MagicNumber) continue;
         
         if ((OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP )&& ord=="PendingBuy"){OrderDelete(OrderTicket(),Yellow);continue;}// Close Buy
         if ((OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP )&& ord=="PendingSell"){OrderDelete(OrderTicket(),Yellow);continue;}// Close Buy
        
         if (OrderType()==OP_BUY && ord=="Buy"){OrderClose(OrderTicket(),OrderLots(),MarketInfo( OrderSymbol( ) ,MODE_BID),slippage,White);continue;}// Close Buy
         if (OrderType()==OP_SELL && ord=="Sell"){OrderClose(OrderTicket(),OrderLots(),MarketInfo( OrderSymbol( ) ,MODE_ASK),slippage,White);continue;}// Close Sell
       } 
      }  
   }
} 
//-------------------------------------------------------------------- 
  

//+------------------------------------------------------------------+
int OPENORDERInstant(string ord,double Lots,double SL,double TP,string symbol,int magic )
{
   int error,err;
   double   dg, pa, pb;
   
  
   
      if (ord=="Buy" )
      {
     
      dg=MarketInfo(symbol, MODE_DIGITS);
    pa=MarketInfo(symbol, MODE_ASK);
    pb=MarketInfo(symbol, MODE_BID);
     error=OrderSend(symbol,OP_BUY,Lots, NormalizeDouble(pa,dg),slippage,0,NormalizeDouble(TP,dg),"SystemX-"+symbol,magic,0,Blue);
        
      } 
      if (ord=="Sell") 
      {
   
      dg=MarketInfo(symbol, MODE_DIGITS);
    pa=MarketInfo(symbol, MODE_ASK);
    pb=MarketInfo(symbol, MODE_BID);
     error=OrderSend(symbol,OP_SELL,Lots,NormalizeDouble(pb,dg),slippage,0,NormalizeDouble(TP,dg),"SystemX-"+symbol,magic,0,Red);
      }
      if (error==-1)
      {  
         ShowERROR(error,pb,SL,TP,symbol,ord);
         RefreshRates();
 
      }
     
   
return (error);
} 
//--------------------------------------------------------------------
void ShowERROR(int Ticket,double Zone,double SL,double TP,string symbol,string ord)
{
   int err=GetLastError();
   switch ( err )
   {                  
      case 1:                                                                               return;
      case 2:   Print("Error Connection with trade server absent    ",Ticket," ",symbol);return;
      case 3:   Print("Error Invalid trade parameters   Ticket ",  Ticket," ",symbol);return;
      case 130: Print("Error Invalid stops  Ticket Zone:",Zone,"SL:",SL,"TP:",TP," ",symbol,ord);return;
      case 134: Print("Error Not enough money   ",                 Ticket," ",symbol);return;
      case 146: Print("Error Trade context is busy. Zone:",Zone,"SL:",SL,"TP:",TP," ",symbol,ord);return;
      case 129: Print("Error Invalid price Zone:",Zone,"SL:",SL,"TP:",TP," ",symbol,ord);return;
      case 131: Print("Error Invalid volume Zone:",Zone,"SL:",SL,"TP:",TP," ",symbol,ord);return;
      case 4051:Print("Error Invalid function parameter value ", Ticket," ",symbol);return;
      case 4105:Print("Error No order selected ",                Ticket," ",symbol);return;
      case 4063:Print("Error Integer parameter expected ",       Ticket," ",symbol);return;
      case 4200:Print("Error Object exists already.",            Ticket," ",symbol);return;
      default:  Print("Unknown Error " ,err,"   Ticket ", Ticket," ",symbol);return;
   }
}

bool wasTP(){
   if (StringFind(OrderComment(), "[tp]", 0) != -1){
      return(True);
   }else{
      return(false);
   }
}

bool wasSL(){
   if (StringFind(OrderComment(), "[sl]", 0) != -1){
      return(True);
   }else{
      return(false);
   }
}
/////

double calclot()
{

int last_trade=HistoryTotal();
if(last_trade>0)
  {
   if(OrderSelect(last_trade-1,SELECT_BY_POS,MODE_HISTORY)==true)
     {
      if(OrderSymbol()==Symbol() )
         {
          if (wasTP()==true) LOT=Lot;
          if (wasSL()==true) LOT=LOT*2;
         }
     }
  }


}



double FindLastBuyPrice() {
   double oldorderopenprice;
   int oldticketnumber;
   double unused = 0;
   int ticketnumber = 0;
   for (int cnt = OrdersTotal() - 1; cnt >= 0; cnt--) {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol() != Symbol() || OrderMagicNumber() != MagicNumber) continue;
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber && OrderType() == OP_BUY) {
         oldticketnumber = OrderTicket();
         if (oldticketnumber > ticketnumber) {
            oldorderopenprice = OrderOpenPrice();
            unused = oldorderopenprice;
            ticketnumber = oldticketnumber;
         }
      }
   }
   return (oldorderopenprice);
}

double FindLastSellPrice() {
   double oldorderopenprice;
   int oldticketnumber;
   double unused = 0;
   int ticketnumber = 0;
   for (int cnt = OrdersTotal() - 1; cnt >= 0; cnt--) {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol() != Symbol() || OrderMagicNumber() != MagicNumber) continue;
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber && OrderType() == OP_SELL) {
         oldticketnumber = OrderTicket();
         if (oldticketnumber > ticketnumber) {
            oldorderopenprice = OrderOpenPrice();
            unused = oldorderopenprice;
            ticketnumber = oldticketnumber;
         }
      }
   }
   return (oldorderopenprice);
}