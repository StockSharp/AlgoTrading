//+------------------------------------------------------------------+
//|                                                       Stufic.mq4 |
//|                                                      Tomas Hruby |
//|                                            http://tomashruby.com |
//+------------------------------------------------------------------+
#property copyright "Tomas Hruby"
#property link      "http://tomashruby.com" 

/*
   Lots - Lots is fixed, without money management, it must be manualy adjusted to 10K USD
   SL, TP it means 2000 for 5 digits, 200 for 4 digits brokers
   UseTrailingSL - Only when SL pip size value is set
   ProfitUSD - Close all positions if AccountProfit() reach ProfitUSD value percent, if you adjust Lots size you must adjust also Profit and LossUSD size with the same ratio
   LossUSD  - Close all positions if AccountProfit() reach LossUSD value (remember the realtion between LossUSD, ProfitUSD and Lots size!!!)
   LossUSDTrade = Close immediately trades with this loss (USD)
   CounterTrend = Its means that our signal can give us opposite information. Just use it on some markets
   OpenBarEnter = process only at each bar open
   HighLowBars = number of bars where we search for iHighest and iLowest
   SameOrderAfterPips = Open another order after X pips (10pips = value 100 at 5 digits borker)
   OppositeLotsMultiplier = Multiply sum of opened lots for opening in the opposite direction (if signal indicating trend change)
   DirectMultiplier = aggressive Lots multipling of new opposite orders
   ReverseClosePositions = close all opposite orders if there is opposite signal
   LotMaxLimit = Max lot value for each new order
   TimeStart = start hour 0 - 12
   TimeEnd = end hour 12 - 24
   SignalMode = 1: MA crossing slow MA (in uptrend), 2: MA crossing slow MA, 3: MA crossing slow MA with price, 4: MA crossing slow MA with price and Williams Perc Range was  in the zone, 5: MA crossing slow MA with price and Williams Perc Range is crossing out from overbought or oversold zone
   EAPeriod =  Period for indicators MA and iHighest and iLowest, 0 = M1, 1 = M5, etc... see the init() section
   FastMAMode, SlowMAMode: 0 = sma, 1 = ema, 2 = smma, 3 = lwma
   yMAShift = MA shift for previous MA values
*/

extern string  StrategyName   = "Stufic Pro";
extern string  VER_trader     = "1.1";
extern double  Lots           = 0.2; 
extern double  TP             = 0;
extern double  SL             = 0;
extern bool    UseTrailingSL  = false;
extern double  ProfitUSD      = 0;
extern double  LossUSD        = 0;
extern bool    ProfitUSDCloseOne = false;
extern bool    LossUSDCloseOne = false;
extern double  LossUSDTrade   = 0;
extern bool    CounterTrend   = false;
extern bool    OpenBarEnter   = false;


extern string  Orders_Manipulation = "--------------------------------";
extern bool    ReverseClosePositions   = false;
extern bool    DirectMultiplier        = true;
extern int     SameOrderAfterPips      = 600;
extern double  OppositeLotsMultiplier  = 0.8;
extern double  AnotherOrderLotsMultiplier = 0.0;
extern double  LotMaxLimit             = 3;
extern int     OrdersMaxLimitDir       = 1000;

//Magic number for identification of Expert Advisor
extern int     MagicNumber    = 1982;

extern string  Time_Filter = "--------------------------------";
extern int     TimeStart      = 5;
extern int     TimeEnd        = 20;

extern string  Signal_Values  = "--------------------------------";
extern int     SignalMode     = 2;
extern int     HighLowBars    = 4;
extern int     TrendBars      = 10;
extern int     EAPeriod       = 2;

extern string  Stochastic_filter = "--------------------------------";
extern int     STOPeriod      = 14;
extern bool    StochOpposite  = false;
extern int     STOShift       = 2; 

extern string  Williams_Percentage_Range_filter = "--------------------------------";
extern int     WPRPeriod      = 14;
extern int     WPRShift       = 14;
extern bool    WilliamsOpposite= false;

extern string  Moving_Average_settings = "--------------------------------";
extern int     MAmode         = 1; 
extern int     FastMAPeriod   = 2;
extern int     MidMAPeriod   = 2;
extern int     SlowMAPeriod   = 30;
extern int     yMAShift       = 2; 

//action
int      A_DONOTHING             = -1,
         A_OPENBUY               = 0,
         A_OPENSELL              = 1,
         CLOSE_WORST             = 1,
         CLOSE_BEST              = 2;

int      LastOrderType           = -1, 
         Timeframe, TimeframeP,
         CandleTimeframe;
double   LastOrderOpenPriceBuy   = 0,
         LastOrderOpenPriceSell  = 0,
         StopLoss,
         TargetProfit,
         StartBalance            = 0,
         ActualTradeLossLimit    = 0;
bool     MoneyManagement = false, 
         TimeIsOk;         

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
     if(EAPeriod==0){         Timeframe = PERIOD_M1; TimeframeP = PERIOD_M15;
     }else if(EAPeriod == 1){ Timeframe = PERIOD_M5; TimeframeP = PERIOD_M30;
     }else if(EAPeriod == 2){ Timeframe = PERIOD_M15; TimeframeP = PERIOD_H1;
     }else if(EAPeriod == 3){ Timeframe = PERIOD_M30; TimeframeP = PERIOD_H4;
     }else if(EAPeriod == 4){ Timeframe = PERIOD_H1; TimeframeP = PERIOD_D1;
     }else if(EAPeriod == 5){ Timeframe = PERIOD_H4; TimeframeP = PERIOD_D1;
     }else if(EAPeriod == 6){ Timeframe = PERIOD_D1; TimeframeP = PERIOD_D1;
     }else{                   Timeframe = PERIOD_W1; TimeframeP = PERIOD_D1;
     }
     return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {

   bool OpenBar=true;
   if(OpenBarEnter) if(iVolume(Symbol(),0,0)>1) OpenBar=false;
  
   if(OpenBar){
  
      bool  upcandle    = false, 
            downcandle  = false, 
            downtrend   = false,
            uptrend     = false;
      int   ticket, 
            action      = A_DONOTHING;
      double Lot        = 0,
             Profit     = 0,
             STO = 0,
             SLPriceNearCandle = 0;
      StopLoss          = 0; 
      TargetProfit      = 0;
   
      RefreshRates();
   
      if(IsOptimization()){ //there are no trades of other EA's so we can get profit from global variable
         Profit = AccountProfit();
      }else{   //in real trading we need to know only profit of orders by Stufic
         Profit = StuficProfit();
      }
      //Check profit or loss and then close all if there are reasons:
      if(ProfitUSD>0 && Profit >= ProfitUSD){
         if(ProfitUSDCloseOne){
            CloseOne(CLOSE_BEST);
         }else{
            CloseAll();
         }
      }
      if(LossUSD>0   && Profit <= LossUSD*-1){  
         if(LossUSDCloseOne){
            CloseOne(CLOSE_WORST);
         }else{
            CloseAll();
         }
      }
      if(LossUSDTrade>0) ActualTradeLossLimit = LossUSDTrade*-1;
   
      if(MoneyManagement){
         Lot = LotsResize(0,Lots);
      }else{
         Lot = LotsResize(Lots,0);
      }
   
      if(TimeStart>=Hour() && Hour()<TimeEnd){
         TimeIsOk = true;
      }else{
         TimeIsOk = false;
      }
      
      if(TimeIsOk){      
        //market data for signal   
        
        if(SignalMode > 2){
           int    iH1 = iHighest(Symbol(),Timeframe,MODE_HIGH,HighLowBars,0);
           int    iL1 = iLowest(Symbol(),Timeframe,MODE_LOW,HighLowBars,0);
           double pH1 = iHigh(Symbol(),Timeframe,iH1);  
           double pL1 = iLow(Symbol(),Timeframe,iL1);
        }
        if(SignalMode > 3 && SignalMode < 6){
           double  WPR = iWPR(Symbol(), TimeframeP, WPRPeriod,0);
           double yWPR = iWPR(Symbol(),TimeframeP, WPRPeriod,WPRShift); //y means yesterday or previous or something like before
        }
        
        double  FastMAprice = iMA(Symbol(), Timeframe, FastMAPeriod, 0, MAmode, PRICE_CLOSE, 0); 
        double yFastMAprice = iMA(Symbol(), Timeframe, FastMAPeriod, 0, MAmode, PRICE_CLOSE, yMAShift);
        
//        double  MidMAprice = iMA(Symbol(), Timeframe, MidMAPeriod, 0, MAmode, PRICE_CLOSE, 0); 
//        double yMidMAprice = iMA(Symbol(), Timeframe, MidMAPeriod, 0, MAmode, PRICE_CLOSE, yMAShift); 
         
        double  SlowMAprice = iMA(Symbol(), Timeframe, SlowMAPeriod, 0, MAmode, PRICE_CLOSE, 0); 
        double ySlowMAprice = iMA(Symbol(), Timeframe, SlowMAPeriod, 0, MAmode, PRICE_CLOSE, yMAShift); 
                  
        if(SignalMode == 0){
          //Slow MA is in uptrend and Fast MA crossing up
          if(ySlowMAprice<SlowMAprice && yFastMAprice < ySlowMAprice && FastMAprice>SlowMAprice){  uptrend=true;}
          //opposite
          if(ySlowMAprice>SlowMAprice && yFastMAprice > ySlowMAprice && FastMAprice<SlowMAprice){  downtrend=true;} 
   
        }else if(SignalMode == 1){
          //Slow MA is in downtrend and Fast MA crossing up
          if(ySlowMAprice>SlowMAprice && yFastMAprice < ySlowMAprice && FastMAprice>SlowMAprice){  uptrend=true;}
          //opposite
          if(ySlowMAprice>SlowMAprice && yFastMAprice > ySlowMAprice && FastMAprice<SlowMAprice){  downtrend=true;}
        
        }else if(SignalMode == 2){
          //Fast MA simply crossing up thru Slow MA
          if(yFastMAprice < ySlowMAprice && FastMAprice>SlowMAprice){  uptrend=true;}
          //opposite
          if(yFastMAprice > ySlowMAprice && FastMAprice<SlowMAprice){  downtrend=true;}
        
        }else if(SignalMode == 3){
   
         //Fast MA crossing up thru Slow MA && price is over Fast Slow MA && previous price was below Fast Slow MA (range is crossing up thru MA)
          if(yFastMAprice < ySlowMAprice && FastMAprice>SlowMAprice 
            && Ask>SlowMAprice && Ask>FastMAprice
            && pL1<SlowMAprice && pL1<FastMAprice
            ){  uptrend=true; }
          //opposite
          if(yFastMAprice > ySlowMAprice && FastMAprice<SlowMAprice 
            && Bid<SlowMAprice && Bid<FastMAprice
            && pH1>SlowMAprice && pH1>FastMAprice
            ){  downtrend=true; }
        
        }else if(SignalMode == 4){
   
         //Fast MA crossing up thru Slow MA && price is over Fast Slow MA && previous price was below Fast Slow MA (range is crossing up thru MA) && Williams was oversold
          if(yFastMAprice < ySlowMAprice && FastMAprice>SlowMAprice 
            && Ask>SlowMAprice && Ask>FastMAprice
            && pL1<SlowMAprice && pL1<FastMAprice
            ){  
               if(yWPR < -70){
                  if(WilliamsOpposite){ downtrend=true; }else{ uptrend=true; }
               }
            }
          
          //opposite
          if(yFastMAprice > ySlowMAprice && FastMAprice<SlowMAprice 
            && Bid<SlowMAprice && Bid<FastMAprice
            && pH1>SlowMAprice && pH1>FastMAprice
            ){  
               if(yWPR > -30){
                  if(WilliamsOpposite){ uptrend=true; }else{ downtrend=true; }
               }   
            }
                 
        }else if(SignalMode == 5){
        
          //Fast MA crossing up thru Slow MA && price is over Fast Slow MA && previous price was below Fast Slow MA (range is crossing up thru MA) && Williams escaping from oversold zone 
          if(yFastMAprice < ySlowMAprice && FastMAprice>SlowMAprice 
            && Ask>SlowMAprice && Ask>FastMAprice
            && pL1<SlowMAprice && pL1<FastMAprice
            ){  
                if(yWPR < -80 && WPR > -80){
                  if(WilliamsOpposite){ downtrend=true; }else{ uptrend=true; }
               }
            }
          //opposite
          if(yFastMAprice > ySlowMAprice && FastMAprice<SlowMAprice 
            && Bid<SlowMAprice && Bid<FastMAprice
            && pH1>SlowMAprice && pH1>FastMAprice
            ){   
               
               if(yWPR > -20 && WPR < -20){
                  if(WilliamsOpposite){ uptrend=true; }else{ downtrend=true; }
               }   
            
            }

         }else if(SignalMode == 6){
        
          //Fast MA crossing up thru Slow MA && price is over Fast Slow MA && previous price was below Fast Slow MA (range is crossing up thru MA) && Stochastic is at oversold zone
          if(yFastMAprice < ySlowMAprice && FastMAprice > SlowMAprice 
            && Ask>SlowMAprice && Ask>FastMAprice
            && pL1<SlowMAprice && pL1<FastMAprice
            ){  
                STO = iStochastic(Symbol(),Timeframe,STOPeriod,3,5,MODE_SMA,0,MODE_MAIN,STOShift);
                if(STO < 20){ //is oversold
                  if(StochOpposite){ downtrend=true; }else{ uptrend=true; }
               }
            }
          //opposite
          if(yFastMAprice > ySlowMAprice && FastMAprice < SlowMAprice 
            && Bid<SlowMAprice && Bid<FastMAprice
            && pH1>SlowMAprice && pH1>FastMAprice
            ){   
               STO = iStochastic(Symbol(),Timeframe,STOPeriod,3,5,MODE_SMA,0,MODE_MAIN,STOShift);
               if(STO > 80){ //is overbought
                  if(StochOpposite){ uptrend=true; }else{ downtrend=true; }
               }   
            }

         }
         
         int new_signal = -1;
         //create signal from data
         if(CounterTrend){
            if(uptrend){ 
               new_signal=OP_SELL; 
            }else if(downtrend){ 
               new_signal=OP_BUY; 
            }
         }else{
            if(uptrend){ 
               new_signal=OP_BUY; 
            }else if(downtrend){ 
               new_signal=OP_SELL; 
            }
         }
      }//eof TimeIsOk
   
      //set the action: check and update SL
      double BuyLots = 0, SelLots = 0; 
      int BuyOrders = 0, SelOrders = 0; //For OrdersMaxLimitDir
      RefreshRates(); 
      
      if(OrdersTotal()>0){
         for(int r=0;r<OrdersTotal();r++){ //check actual opened orders for actions
           
            if(OrderSelect(r,SELECT_BY_POS,MODE_TRADES)==true){
               if(MagicNumber==OrderMagicNumber()){ //is it order of this EA?
   
                  if(UseTrailingSL && SL>0) UpdateSL(); //Trailing Stop Loss
   
                  if(LossUSDTrade>0 && OrderProfit()+OrderSwap() < ActualTradeLossLimit){ 
                     if(OrderType()==OP_BUY){
                        if(!OrderClose(OrderTicket(),OrderLots(),Bid,3)) Print("Closing error BUY! Ticket: ",OrderTicket(),", Er: ",GetLastError());
                     }else if(OrderType()==OP_SELL){
                        if(!OrderClose(OrderTicket(),OrderLots(),Ask,3)) Print("Closing error SELL! Ticket: ",OrderTicket(),", Er: ",GetLastError());
                     }
                  }else{
                     //Sum of lots for each direction
                     if(OrderType()==OP_BUY){   BuyLots+=OrderLots(); BuyOrders++;}
                     if(OrderType()==OP_SELL){  SelLots+=OrderLots(); SelOrders++; }
                  }
               }
            }
         }
      }
      
      if(TimeIsOk){
         if(new_signal != -1){
            if(new_signal==OP_BUY && OrdersMaxLimitDir > BuyOrders){
               if(AnotherOrderDistance(OP_BUY)){
                  action=A_OPENBUY;
                  
                  if(SelLots>=BuyLots && SelLots>0){
                     if(ReverseClosePositions) CloseAll(OP_SELL);
                     if(DirectMultiplier){
                        Lot=NormalizeDouble(SelLots*OppositeLotsMultiplier,2);
                     }else{
                        Lot=NormalizeDouble(SelLots+(Lot*OppositeLotsMultiplier),2);
                     }
                  }else{

                     if(BuyLots>0){
                        Lot=NormalizeDouble(BuyLots+(Lot*AnotherOrderLotsMultiplier),2);
                     }else{
                       //Lot = Lot; //normal lots open
                     }
                  }
               }else{
                  action=A_DONOTHING;
               }
               
            }else if(new_signal==OP_SELL && OrdersMaxLimitDir > SelOrders){
               if(AnotherOrderDistance(OP_SELL)){
                  action=A_OPENSELL;
                  
                  if(BuyLots>=SelLots && BuyLots>0){
                     if(ReverseClosePositions) CloseAll(OP_BUY);
                     if(DirectMultiplier){
                        Lot=NormalizeDouble(BuyLots*OppositeLotsMultiplier,2);
                     }else{
                        Lot=NormalizeDouble(BuyLots+(Lot*OppositeLotsMultiplier),2);
                     }
                  }else{
                     if(SelLots>0){
                        Lot=NormalizeDouble(SelLots+(Lot*AnotherOrderLotsMultiplier),2);
                     }else{
                        //Lot = Lot; //normal lots open
                     }
                  }
               }else{
                  action=A_DONOTHING;
               }
               
            }//eof OP_BUY OP_SELL
         }//eof action
      
         //We dont need to open bigger positions than we want
         if(Lot>LotMaxLimit){
            Lot=LotMaxLimit;
         }
      
         if(action==A_OPENBUY && Lot>=0.01){
            //SL and TP must be set such from 100.00 (10pips) to 10000.00 (1000 pips)
            if(SL>0) StopLoss       = Ask-(SL*Point);
            if(TP>0) TargetProfit   = Ask+(TP*Point);
            
            ticket = OrderSend(Symbol(),OP_BUY,Lot,Ask,3,NormalizeDouble(StopLoss,Digits),NormalizeDouble(TargetProfit,Digits),"S1; "+DBS((Ask-Bid)/Point,0),MagicNumber,0,CLR_NONE);
            if(ticket>0){  Print("Order BUY opened");  }
          
          }else if(action==A_OPENSELL && Lot>=0.01){
      
            if(SL>0) StopLoss       = Bid+(SL*Point);
            if(TP>0) TargetProfit   = Bid-(TP*Point);
           
            ticket = OrderSend(Symbol(),OP_SELL,Lot,Bid,3,NormalizeDouble(StopLoss,Digits),NormalizeDouble(TargetProfit,Digits),"S1; "+DBS((Ask-Bid)/Point,0),MagicNumber,0,CLR_NONE);
            if(ticket>0){  Print("Order SELL opened");  }
          
          }//eof Action
       }//eof TimeIsOk

      return(0);
    }
   
    return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string DBS(double Num,int Nen){
   return (DoubleToStr(Num,Nen));
}

double LotsResize(double fLots,double fLots10K){
   double rLots=0; //returned lots

   //if 10K is set we adapt the lots size to account size
   if(fLots10K>0){
      rLots=fLots10K*(AccountInfoDouble(ACCOUNT_BALANCE)/10000);
   }else{
      rLots=fLots;
   }

   //We must check minimal step 
   if(MarketInfo(Symbol(),MODE_LOTSTEP)==0.10){
      rLots = MathRound( rLots/MarketInfo(Symbol(),MODE_LOTSTEP)  )  *  MarketInfo(Symbol(),MODE_LOTSTEP);
      rLots = NormalizeDouble(rLots,1);
   }

   //we check minimal and maximal values
   if(rLots<MarketInfo(Symbol(),MODE_MINLOT)){
      rLots=MarketInfo(Symbol(),MODE_MINLOT);
   }else if(rLots>MarketInfo(Symbol(),MODE_MAXLOT)){
      rLots=MarketInfo(Symbol(),MODE_MAXLOT);
   }

   return(NormalizeDouble(rLots,2));
}

//check if we can open another order after predefined distance defined in pips in variable AnotherOrderPips
bool AnotherOrderDistance(int fType){
   bool DistanceOk=true;
   if(OrdersTotal()>0){
      for(int r=0;r<OrdersTotal();r++){
         if(OrderSelect(r,SELECT_BY_POS,MODE_TRADES)==true){
            //is it order of this EA?
            if(MagicNumber==OrderMagicNumber()){ 
               if(OrderType()==OP_BUY && fType==OP_BUY){
                  if(MathAbs(OrderOpenPrice()-Ask)<(SameOrderAfterPips*Point)){  DistanceOk=false;  return(DistanceOk); }
               }else if(OrderType()==OP_SELL && fType==OP_SELL){
                  if(MathAbs(OrderOpenPrice()-Bid)<(SameOrderAfterPips*Point)){  DistanceOk=false;  return(DistanceOk); }
               }
            }
         }
      }
   }
   return(DistanceOk);
}

void CloseAll(int fType = -1){
   bool res;
   int  i;
   int  Total=OrdersTotal();
   if(Total>0){
      for(i=Total-1; i>=0; i--){
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==TRUE){
            if(MagicNumber==OrderMagicNumber()){ 
               if(OrderType()==OP_BUY){
                  if(fType==OP_BUY || fType==-1){  res=OrderClose(OrderTicket(),OrderLots(),Bid,3,CLR_NONE);}
               }
               if(OrderType()==OP_SELL){ 
                  if(fType==OP_SELL || fType==-1){ res=OrderClose(OrderTicket(),OrderLots(),Ask,3,CLR_NONE);}
               }
               if(res!=true){
                  Print("Close All, GetLastError: ",GetLastError());
               }
            }
         }
      }
   }
}

void CloseOne(int fKind = -1){
   bool res;
   int  i;
   int  Total=OrdersTotal();
   int Ticket = 0;
   double Profit = 0;
   
   if(Total>0){
      for(i=Total-1; i>=0; i--){
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==TRUE){
            if(MagicNumber==OrderMagicNumber()){ 
               
               if(fKind == CLOSE_WORST){
                  if(OrderProfit() < Profit && OrderProfit() < 0){
                     Profit = OrderProfit();
                     Ticket = OrderTicket();
                  } 
               }else if(fKind == CLOSE_BEST){
                  if(OrderProfit() > Profit && OrderProfit() > 0){
                     Profit = OrderProfit();
                     Ticket = OrderTicket();
                  } 
               }//eof kind of order
            }///eof magic
         }
      }
   }               
   
   if(Ticket>0){
      if(OrderSelect(Ticket,SELECT_BY_TICKET)==TRUE){
         if(OrderType()==OP_BUY){
            res=OrderClose(Ticket,OrderLots(),Bid,3,CLR_NONE);
         }
         if(OrderType()==OP_SELL){ 
            res=OrderClose(Ticket,OrderLots(),Ask,3,CLR_NONE);
         }
         if(res!=true){
            Print("Close One "+fKind+", GetLastError: ",GetLastError());
         }
      }
   }
}

//Get account profit mady only by orders of stufic
double StuficProfit(){
   double profit = 0;
   RefreshRates();
   int Total=OrdersTotal();
   if(Total>0){
      for(int i=Total-1; i>=0; i--){
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==TRUE){
            if(MagicNumber==OrderMagicNumber()){ 
               profit +=OrderSwap()+OrderProfit();
            }
         }
      }
   }
   return(profit);
}

//Trailing SL
bool UpdateSL(){
   RefreshRates();
   double fAsk    = MarketInfo(Symbol(),MODE_ASK);
   double fBid    = MarketInfo(Symbol(),MODE_BID);
   double fPoint  = MarketInfo(Symbol(),MODE_POINT);
   double fDigits = MarketInfo(Symbol(),MODE_DIGITS);
   double fSL;
   if(MagicNumber==OrderMagicNumber()){ 
      if(SL>0){
         if(OrderType()==OP_BUY){
            fSL = fAsk-(SL*fPoint);
            if(OrderStopLoss()<fSL && OrderStopLoss()!=0){
               if(OrderModify(OrderTicket(),NormalizeDouble(OrderOpenPrice(),fDigits),NormalizeDouble(fSL,fDigits),NormalizeDouble(OrderTakeProfit(),fDigits),0,Yellow)){
                  return(true);
               }else{
                  return(false);
               }
            }
   
         }else if(OrderType()==OP_SELL){
            fSL = fBid+(SL*fPoint);
            if(OrderStopLoss()>fSL && OrderStopLoss()!=0){
               if(OrderModify(OrderTicket(),NormalizeDouble(OrderOpenPrice(),fDigits),NormalizeDouble(fSL,fDigits),NormalizeDouble(OrderTakeProfit(),fDigits),0,Yellow)){
                  return(true);
               }else{
                  return(false);
               }
            }
         }
      }
   }
   return(false);
}

int Trend(){
   
   bool downtrend, uptrend;
   
   //get market data for signal
   int iH0=iHighest(Symbol(),Timeframe,MODE_HIGH,TrendBars*3,0);
   int iH1=iHighest(Symbol(),Timeframe,MODE_HIGH,TrendBars,(TrendBars*2));
   int iH2=iHighest(Symbol(),Timeframe,MODE_HIGH,TrendBars,TrendBars);
   int iH3=iHighest(Symbol(),Timeframe,MODE_HIGH,TrendBars,0);
   double pH0 = iHigh(Symbol(),Timeframe,iH0);
   double pH1 = iHigh(Symbol(),Timeframe,iH1);
   double pH2 = iHigh(Symbol(),Timeframe,iH2);
   double pH3 = iHigh(Symbol(),Timeframe,iH3);
   
   int iL0=iLowest(Symbol(),Timeframe,MODE_LOW,TrendBars*3,0);
   int iL1=iLowest(Symbol(),Timeframe,MODE_LOW,TrendBars,(TrendBars*2));
   int iL2=iLowest(Symbol(),Timeframe,MODE_LOW,TrendBars,TrendBars);
   int iL3=iLowest(Symbol(),Timeframe,MODE_LOW,TrendBars,0);
   double pL0 = iLow(Symbol(),Timeframe,iL0);
   double pL1 = iLow(Symbol(),Timeframe,iL1);
   double pL2 = iLow(Symbol(),Timeframe,iL2);
   double pL3 = iLow(Symbol(),Timeframe,iL3);
   
   //downtrend
   if(pH1>pH2 && pH2>pH3 && Bid<pL0){ //downtrend and this is new low
      downtrend=true;
   }
   //uptrend
   if(pL1<pL2 && pL2<pL3 && Ask>pH0){ //uptrend and this is new high
      uptrend=true;
   }

   if(uptrend==true && downtrend==false){
      return(1);
   
   }else if(downtrend==true && uptrend==false){
      return(2);
   }else{
      return(0);
   }
}

//+------------------------------------------------------------------+

