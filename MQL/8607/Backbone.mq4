//+--------------------------------------------------------------------------------------+
//|                                                                         Backbone.mq4 |
//|                                                               Copyright © 2008, gpwr |
//|                                                                   gpwr9k95@yahoo.com |
//+--------------------------------------------------------------------------------------+
#property copyright "Copyright © 2008, gpwr"
//Global constants
#define MNo 0
//Input parameters
extern double   MaxRisk     =0.5; //Maximum risk for all trades at any time
extern int      ntmax       =10;  //Maximum number of trades in one direction
extern int      TakeProfit  =170; // 
extern int      StopLoss    =40; //0: disable; >0: enable
extern int      TrailingStop=300;//0: disable; >0: enable (StopLoss must be enabled too)

//Global variables
int LastPosition; // 1 = long, -1 = short, 0 = none
int PrevBars;
double BidMax, AskMin;

//Initialize-----------------------------------------------------------------------------+
int init()
{
   LastPosition=0;
   BidMax=0.0;
   AskMin=10000.0;
   PrevBars=Bars;
   return(0);
}

//Start----------------------------------------------------------------------------------+
int start()
{
   if(Bars>PrevBars)
   {
   //Finding the first entry point
      if(LastPosition==0)
      {
         if(Bid>BidMax) BidMax=Bid;
         if(Ask<AskMin) AskMin=Ask;
         if(Bid<BidMax-Point*TrailingStop) LastPosition=-1;
         if(Ask>AskMin+Point*TrailingStop) LastPosition=1;
      }
   
   //Begin Trading--------------------------------------------------------------------------+
      double   lots,SL,TP;
      double   Spread   =Ask-Bid;
      int      Slippage =Spread/Point;
      int      nt       =OrdersTotal();
      
   //Modifying stop-loss of open orders------------------------------------------------------+
      if(TrailingStop>0 && StopLoss>0)
         for(int i=0;i<nt;i++)
         {
            if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
            if(OrderType()==OP_BUY)
            {
               if(Bid-OrderOpenPrice()>Point*TrailingStop)
                  if(OrderStopLoss()<Bid-Point*TrailingStop)
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Blue);
            }
            else if(OrderType()==OP_SELL)
            {
               if(Ask+OrderOpenPrice()<Point*TrailingStop)
                  if(OrderStopLoss()>Ask+Point*TrailingStop)
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
            }
         }
      
   //Sending OPEN LONG order----------------------------------------------------------------+ 
      if((LastPosition==-1 && nt==0) || (LastPosition==1 && nt>0 && nt<ntmax))
      {
         lots=Vol(Symbol(),nt);
         if(lots>0.0)
         {
            if(StopLoss!=0) SL=Ask-StopLoss*Point;
            else SL=0;
            if(TakeProfit!=0) TP=Ask+TakeProfit*Point;
            else TP=0;
            OrderSend(Symbol(),OP_BUY,lots,Ask,Slippage,SL,TP,TimeToStr(Time[0]),MNo,0,Blue);
            LastPosition=1;
         }
      }
   //Sending OPEN SHORT order---------------------------------------------------------------+ 
      else if((LastPosition==1 && nt==0) || (LastPosition==-1 && nt>0 && nt<ntmax))      
      {
         lots=Vol(Symbol(),nt);
         if(lots>0.0)
         {
            if(StopLoss!=0) SL=Bid+StopLoss*Point;
            else SL=0;
            if(TakeProfit!=0) TP=Bid-TakeProfit*Point;
            else TP=0;
            OrderSend(Symbol(),OP_SELL,lots,Bid,Slippage,SL,TP,TimeToStr(Time[0]),MNo,0,Red);
            LastPosition=-1;
         }  
      }
      PrevBars=Bars;
   }
   return(0);
}

//Lots Calculation Function------------------------------------------------------------------+
double Vol( string symbol, // symbol=symbol of the currency pair, for example "EURUSD"
            int nt)     // nt=number of open orders in the same direction
  {
   string first   =StringSubstr(symbol,0,3);          // first symbol, for example      EUR
   string second  =StringSubstr(symbol,3,3);          // second symbol, for example     USD
   string currency=AccountCurrency();                 // account currency, for example  USD
   double leverage=AccountLeverage();                 // leverage, for example          100
   double bid     =MarketInfo(symbol,MODE_BID);       // bid price
   double contract=MarketInfo(symbol,MODE_LOTSIZE);   // size of 1 lot, for example     100,000
   double lot_min =MarketInfo(Symbol(),MODE_MINLOT);
   double lot_max =MarketInfo(Symbol(),MODE_MAXLOT);
   double lot_step=MarketInfo(Symbol(),MODE_LOTSTEP);
   double vol;
//---- Only allow standard Forex symbols XXXYYY
   if(StringLen(symbol)!=6)
     {
      Print("Lots: '",symbol,"' must be standard forex symbol XXXYYY");
      return(0.0);
     }
//---- Check data
   if(bid<=0 || contract<=0) 
     {
      Print("Lots: no market information for '",symbol,"'");
      return(0.0);
     }
   if(lot_min<0 || lot_max<=0.0 || lot_step<=0.0) 
     {
      Print("Lots: invalid MarketInfo() results [",lot_min,",",lot_max,",",lot_step,"]");
      return(0);
     }
   if(AccountLeverage()<=0)
     {
      Print("Lots: invalid AccountLeverage() [",AccountLeverage(),"]");
      return(0);
     }
     
//Calulating lots based on margin call scenario
   double frac=1.0/(ntmax/MaxRisk-nt);
//---- If one of the currencies in the pair is the same as the account currency
   if(first==currency)
      vol=NormalizeDouble(AccountFreeMargin()*frac*leverage/contract,2);      // USDxxx
   if(second==currency)
      vol=NormalizeDouble(AccountFreeMargin()*frac*leverage/contract/bid,2);  // xxxUSD
//---- If neither currency in the pair is the same as the account currency
   string base=currency+first;                                       // USDxxx
   if(MarketInfo(base,MODE_BID)>0)
      vol=NormalizeDouble(AccountFreeMargin()*frac*leverage*MarketInfo(base,MODE_BID)/contract,2);
   base=first+currency;                                              // xxxUSD
   if(MarketInfo(base,MODE_BID)>0)
      vol=NormalizeDouble(AccountFreeMargin()*frac*leverage/contract/MarketInfo(base,MODE_BID),2);
      
//Calculating lots based on StopLoss
   double volSL=NormalizeDouble(AccountFreeMargin()*frac/contract/(StopLoss*Point),2);
   
//Select the smallest value for lots
   if(volSL<vol) vol=volSL;

//--- check lot min, max and step
   vol=NormalizeDouble(vol/lot_step,0)*lot_step;
   if(vol<lot_min) vol=0.0;
   if(vol>lot_max) vol=lot_max;
   return(vol);
}



