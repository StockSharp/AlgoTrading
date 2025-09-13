//+------------------------------------------------------------------+
//|                                                       Bleris.mq4 |
//|                                                      Tomas Hruby |
//|                                        http://www.tomashruby.com |
//+------------------------------------------------------------------+
#property copyright "Tomas Hruby"
#property link      "http://www.tomashruby.com" 

extern string StrategyName       = "Bleris"; //
extern string VER_trader         = "1.1"; //13.10.2014

//fidex lots
extern double  Lots              = 0.3;
//lots adapted to deposits
extern double  Lost10K           = 0.0; 
 //SL, TP it means 2000 for 5 digits, 200 for 4 digits brokers
extern double  SL                = 1700;
extern double  TP                = 3600; 
 //If true ATR will be used for setting the SL and TP. Than you must set SL and TP to ratio from 0.1 to 5.0 etc.
extern bool UseATR               = false;
//Trailing SL will be applied
extern bool UseTrailingSL        = false;
//Close all positions if AccountProfit() reach ProfitUSD value
extern double ProfitUSD          = 1600;
//Close all positions if AccountProfit() reach LossUSD value
extern double LossUSD            = 0;
//How many bars will be used for three tested iHighest and iLowest periods
extern int SignalBarSample       = 24;
//What if we use our signal vice versa? 
extern bool CounterTrend         = false;
//Open another order after X pips (60pips = 600 at 5 digits borker)
extern int AnotherOrderPips      = 600;
//When loss reach 0.9 from LossUSD value AND AnotherOrderPips is true open new position with Lots*EscapeLotsMultiplier! - its a lsat chance to go out from all positions!
extern double  EscapeLossUSDRatio    = 0.9;
extern double  EscapeLotsMultiplier = 2.6;
//Multiply sum of opened lots for opening in the opposite direction (if signal indicating trend change)
extern double  OppositeLotsMultiplier  = 0.8;
//1 = period M1, atc... see the init() section
extern int  fPeriod              = 6;
//Max lot value in trading
extern double LotMaxLimit        = 10;
//Magic number for identification of Expert Advisor
extern int  MagicNumber          = 1982;//my birth :-)

//action
int A_DONOTHING = -1;
int A_OPENBUY = 0;
int A_OPENSELL = 1;

int LastOrderType = -1;
double LastOrderOpenPriceBuy = 0, LastOrderOpenPriceSell = 0;
double StopLoss, TargetProfit, ATR;
int Timeframe;
int init()
  {
      if(fPeriod == 0){       Timeframe = PERIOD_M1;
      }else if(fPeriod == 1){       Timeframe = PERIOD_M5;
      }else if(fPeriod == 2){ Timeframe = PERIOD_M15;
      }else if(fPeriod == 3){ Timeframe = PERIOD_M30;
      }else if(fPeriod == 4){ Timeframe = PERIOD_H1;
      }else if(fPeriod == 5){ Timeframe = PERIOD_H4;
      }else if(fPeriod == 6){ Timeframe = PERIOD_D1;
      }else{                  Timeframe = PERIOD_W1;
      }
      return(0);
      
  }

int start()
  {
   bool uptrend = false, downtrend = false;
   int ticket, action = A_DONOTHING, new_signal = -1;  
   double Lot = LotsResize(Lots,Lost10K);
   StopLoss = 0; TargetProfit = 0;

   RefreshRates();   

   //Check profit or loss and then close all if there are reasons:
   if(ProfitUSD>0 && AccountProfit()>=ProfitUSD){ CloseAll(); }
   if(LossUSD>0 && AccountProfit()<=LossUSD*-1){ CloseAll(); }
   
   //get market data for signal
   int iH1=iHighest(Symbol(),Timeframe,MODE_HIGH,SignalBarSample,(SignalBarSample*2));
   int iH2=iHighest(Symbol(),Timeframe,MODE_HIGH,SignalBarSample,SignalBarSample);
   int iH3=iHighest(Symbol(),Timeframe,MODE_HIGH,SignalBarSample,0);
   double pH1 = iHigh(Symbol(),Timeframe,iH1);
   double pH2 = iHigh(Symbol(),Timeframe,iH2);
   double pH3 = iHigh(Symbol(),Timeframe,iH3);
   
   int iL1=iLowest(Symbol(),Timeframe,MODE_LOW,SignalBarSample,(SignalBarSample*2));
   int iL2=iLowest(Symbol(),Timeframe,MODE_LOW,SignalBarSample,SignalBarSample);
   int iL3=iLowest(Symbol(),Timeframe,MODE_LOW,SignalBarSample,0);
   double pL1 = iLow(Symbol(),Timeframe,iL1);
   double pL2 = iLow(Symbol(),Timeframe,iL2);
   double pL3 = iLow(Symbol(),Timeframe,iL3);
  
   //downtrend
   if(pH1>pH2 && pH2>pH3){
      downtrend=true;
   }
   //uptrend
   if(pL1<pL2 && pL2<pL3){
      uptrend=true;
   }
   //End of market data 
   
   //create signal from data
   if(uptrend==true && downtrend==false){
      
      if(CounterTrend==false){
         new_signal=OP_BUY;
         action = A_OPENBUY;
      }else{
         new_signal=OP_SELL;
         action = A_OPENSELL;
      }
   }   
   if(downtrend==true && uptrend==false){
      
      if(CounterTrend==false){
         new_signal=OP_SELL;
         action = A_OPENSELL;
      }else{
         new_signal=OP_BUY;
         action = A_OPENBUY;
      }
   }
   //end of signal
   
   //set the action: check, open or close
   if(new_signal>-1){
      double BuyLots = 0;
      double SelLots = 0;
      if(OrdersTotal()>0){
         int LatestTime = 0;
         for (int r=0;r<OrdersTotal();r++)  //check actual opened orders for actions
         {
            if(OrderSelect(r,SELECT_BY_POS,MODE_TRADES) == true){
               if(MagicNumber == OrderMagicNumber()){ //is it order of this EA?
                  
                  if(UseTrailingSL) UpdateSL(); //Trailing Stop Loss
                  
                  //Sum of lots for each direction
                  if(OrderType()==OP_BUY){   BuyLots += OrderLots(); } 
                  if(OrderType()==OP_SELL){  SelLots += OrderLots(); }
               }
            }
         }
      }
      
      if(new_signal==OP_BUY){
         if(AnotherOrderDistance(OP_BUY)){ 
            action = A_OPENBUY; 
            
            if(SelLots>BuyLots){
               Lot = NormalizeDouble(SelLots+(Lot*OppositeLotsMultiplier),2);
            }else{    
               if(BuyLots>0){
                  Lot = NormalizeDouble(BuyLots+Lot,2);
               }else{
                  //Lot = Lot; //normal lots open
               }
            }
            //this is the last chance = escape from the bad positions
            if(AccountProfit() < -1*(LossUSD*EscapeLossUSDRatio)){
              Lot=NormalizeDouble(Lot*EscapeLotsMultiplier,2);
            }
         }else{
            action = A_DONOTHING; 
         }
      }else if(new_signal==OP_SELL){
      
         if(AnotherOrderDistance(OP_SELL)){ 
            action = A_OPENSELL; 
            if(BuyLots>SelLots){
               Lot = NormalizeDouble(BuyLots+(Lot*OppositeLotsMultiplier),2);
            }else{
               if(SelLots>0){
                  Lot = NormalizeDouble(SelLots+Lot,2);
               }else{
                  //Lot = Lot; //normal lots open
               }
            }
            //this is the last chance = escape from the bad positions
            if(AccountProfit() < -1*(LossUSD*EscapeLossUSDRatio)){
               Lot=NormalizeDouble(Lot*EscapeLotsMultiplier,2);
            }
         }else{
            action = A_DONOTHING; 
         }
      }
   }

   //We dont need to open bigger positions than we want
   if(Lot>LotMaxLimit){
      Lot = LotMaxLimit;
   }
   
   if(action==A_OPENBUY && Lot >= 0.01){
      
      if(UseATR){ 
         //SL and TP must be set such from 0.1 to 5.0
         ATR = iATR(Symbol(),Timeframe,SignalBarSample*3,0);
         if(SL>0) StopLoss       = NormalizeDouble(Ask-(SL*ATR),Digits);
         if(TP>0) TargetProfit   = NormalizeDouble(Ask+(TP*ATR),Digits);
      }else{ 
         //SL and TP must be set such from 100.00 (10pips) to 10000.00 (1000 pips)
         if(SL>0) StopLoss       = Ask-(SL*Point);
         if(TP>0) TargetProfit   = Ask+(TP*Point);      
      }
      
      ticket = OrderSend(Symbol(),OP_BUY,Lot,Ask,3,StopLoss,TargetProfit,"",MagicNumber,0,CLR_NONE); 
      if(ticket>0){ 
         Print("Order BUY opened");
      }
   }else if(action==A_OPENSELL && Lot >= 0.01){
   
      if(UseATR){
         ATR = iATR(Symbol(),Timeframe,SignalBarSample*3,0);
         if(SL>0) StopLoss       = NormalizeDouble(Bid+(SL*ATR),Digits);
         if(TP>0) TargetProfit   = NormalizeDouble(Bid-(TP*ATR),Digits);
      }else{
         if(SL>0) StopLoss       = Bid+(SL*Point);
         if(TP>0) TargetProfit   = Bid-(TP*Point);
      }
      ticket = OrderSend(Symbol(),OP_SELL,Lot,Bid,3,StopLoss,TargetProfit,"",MagicNumber,0,CLR_NONE); 
      if(ticket>0){ 
         Print("Order SELL opened");
      }
   }
   return(0);
}

double LotsResize(double fLots, double fLots10K){
   double rLots = 0; //returned lots
   
   //if 10K is set we adapt the lots size to account size
   if(fLots10K>0){
      rLots = fLots10K*(AccountInfoDouble(ACCOUNT_BALANCE)/10000);
   }else{
      rLots = fLots;
   }
   
   //We must check minimal step 
   if(MarketInfo(Symbol(),MODE_LOTSTEP) == 0.10){
      rLots = MathRound( rLots/MarketInfo(Symbol(),MODE_LOTSTEP)  )  *  MarketInfo(Symbol(),MODE_LOTSTEP);
      rLots = NormalizeDouble(rLots,1);
   }

   //we check ¨minimal and maximal values
   if(rLots < MarketInfo(Symbol(),MODE_MINLOT)){
      rLots = MarketInfo(Symbol(),MODE_MINLOT);
   }else if(rLots > MarketInfo(Symbol(),MODE_MAXLOT)){
      rLots = MarketInfo(Symbol(),MODE_MAXLOT);
   }
   
   return (NormalizeDouble(rLots,2));
}

//checking if we can open another order after some pips distance
bool AnotherOrderDistance(int fType){
   bool DistanceOk = true;
   
   if(OrdersTotal()>0){
      for (int r=0;r<OrdersTotal();r++){
         if(OrderSelect(r,SELECT_BY_POS,MODE_TRADES) == true){
            if(MagicNumber == OrderMagicNumber()){ //is it order of this EA?
               
               if(OrderType()==OP_BUY && fType==OP_BUY){  
         
                  if(MathAbs(OrderOpenPrice()-Ask) < (AnotherOrderPips*Point) ){
                     DistanceOk = false;
                  }
               
               }else if(OrderType()==OP_SELL && fType==OP_SELL){ 
                  if(MathAbs(OrderOpenPrice()-Bid) < (AnotherOrderPips*Point) ){
                     DistanceOk = false;
                  }
               }               
            }
         }
      }
   }
   return(DistanceOk);
}


void CloseAll(){
   bool res;
   int  i,pos;
   int  Total=OrdersTotal();
   
   if(Total>0){
      for(i=Total-1; i>=0; i--){
         if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == TRUE){
            pos=OrderType();
            
            if(pos==OP_BUY){  res=OrderClose(OrderTicket(), OrderLots(), Bid, 3, CLR_NONE);}
            if(pos==OP_SELL){ res=OrderClose(OrderTicket(), OrderLots(), Ask, 3, CLR_NONE);}
            if(res!=true){
               Print("Close All, GetLastError: ",GetLastError());
            }
         }
      }
   }
}

//Trailing SL
bool UpdateSL(){
   double fSL;
   RefreshRates();
   double fAsk  = MarketInfo(Symbol(),MODE_ASK);
   double fBid = MarketInfo(Symbol(),MODE_BID);
   double fPoint = MarketInfo(Symbol(),MODE_POINT);
   double fDigits = MarketInfo(Symbol(),MODE_DIGITS);
   
   if(SL>0){
      if(OrderType()==OP_BUY){
         fSL = fAsk-(SL*Point);
         if(OrderStopLoss()< fSL && OrderStopLoss()!=0){ 
            if(OrderModify(OrderTicket(),NormalizeDouble(OrderOpenPrice(),fDigits),NormalizeDouble(fSL,fDigits),NormalizeDouble(OrderTakeProfit(),fDigits),0,Yellow)){
              return(true);
            }else{
              return(false);
            }
         }

      }else if(OrderType()==OP_SELL){ 
         fSL = fBid+(SL*Point); 
         if(OrderStopLoss() > fSL && OrderStopLoss()!=0){ 
            if(OrderModify(OrderTicket(),NormalizeDouble(OrderOpenPrice(),fDigits),NormalizeDouble(fSL,fDigits),NormalizeDouble(OrderTakeProfit(),fDigits),0,Yellow)){
              return(true);
            }else{
              return(false);
            }

         }
      }
   }
   return(false);
}
