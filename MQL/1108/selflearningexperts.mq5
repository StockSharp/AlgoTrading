//+------------------------------------------------------------------+
//|                                          SelfLearningExperts.mq5 |
//|                                         Copyright 2012, Integer. |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+

#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property version   "1.00"

#property description "Expert rewritten from MQL4, the author is lsv (http://www.mql4.com/ru/users/lsv), link to original - http://codebase.mql4.com/ru/635"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CSymbolInfo Sym;
CPositionInfo Pos;

input bool     ReadHistory    =  false;         /*ReadHistory*/      // Reading of saved learning history.
input bool     SaveHistory    =  false;         /*SaveHistory*/      // Save the learning history. The history is saved at the end of testing, when working in the tester. When working on the account - regularly, as retraining.
input double   Lots           =  0.1;           /*Lots*/             // Volume of position
input int      Nidelt         =  20;            /*Nidelt*/           // Number of different patterns
input int      Nstop          =  1;             /*Nstop*/            // Number of virtual positions parameters (different values of stoploss and takeprofit, stoploss and takeprofit are equal)
input int      dstop          =  250;           /*dstop*/            // Step of changing virtual positions parameters(stoploss and takeprofit)
input double   forg           =  1.05;          /*forg*/             // Rate of forgetting learning results,  the value should be a little more than 1
input double   Probab         =  0.8;           /*Probab*/           // Probability level ( defined by learning results ) at which the opening position
input int      NN             =  10;            /*NN*/               // Pattern size, not more than 12 
input int      delta          =  1;             /*delta*/            // Step of changing pattern parameter
input bool     ReplaceStops   =  false;         /*ReplaceStops*/     // Modify stoploss/takeprofit with new signals of opening. Transfer of stoploss/takeprofit is made only on the direction of the position.
input int      Trailing       =  0;             /*Trailing*/         // Trailing level, if value is 0 - then trailing off.

bool     Trade_BuyOpenSignal     =  false;   // Variables for trade signals 
bool     Trade_SellOpenSignal    =  false; 
bool     Trade_BuyCloseSignal    =  false; 
bool     Trade_SellCloseSignal   =  false;   

double   MarketPattern[12][30];           // Array for data of current patterns which defined by prices
double   MarketPatternLastPrice[30];      // Last pattern price, for determine, if the pattern has changed and it will need to open a virtual order

double   PatternDeltaParameter[30];       // Array with parameter, which need for determine different patterns 
int      StopInPointsParameter[3];        // Array with stoploss and takeprofit values

int      BinaryPattern[50][30];           // auxiliary array for pattern in binary form

string   Store_FileName;                  // File name for storing probabilities, which determined in the results of virtual trade
datetime Store_LastSaveTime;              // Time of last data update in the file
int      Store_TypesCount;                // Maximum value of number of binary pattern type
int      Store_ChangesCount;              // Number of changes in the arrays of probabilities

double   Store_PowerUp[5000][30][3];      // Probability of move up
double   Store_PowerDn[5000][30][3];      // Probability of move down
int      Store_TradesCount[5000][30][3];  // Number of virtual orders

bool     Store_IsLoaded;                  // File is loaded

bool     VO_Exists[5000][30][3];          // Array of virtual order existence by number of binary pattern (form) type, pattern parameter (delta) and stoploss/takeprofit value
double   VO_OpenPrice[5000][30][3];       // Array with prices of virtual orders openning
datetime VO_OpenTime[5000][30][3];        // Array with time of virtual orders openning

datetime LastBuyOpen, LastSellOpen;
int Trade_TakeProfit, Trade_StopLoss;
int Trade_OrdersCount;

int Ncomb;

double Ask,Bid,point;
bool PatternInitialized=false;
double slv,tpv;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

      for(int ip = 1;ip<=Nidelt;ip++){ 
         PatternDeltaParameter[ip]=delta*ip; 
      } 
      
      for(int is=1;is<=Nstop;is++){ //3
         StopInPointsParameter[is]=dstop*is;       
      }    
      
   Store_FileName="FD_"+Symbol();
   Store_LastSaveTime=0;
   Store_TypesCount=0;
   Store_ChangesCount=0;   
   
   Store_IsLoaded=false;
   
   ReadHistoryFile();    

   if(!Sym.Name(_Symbol)){
      Alert("Failed to initialize CSymbolInfo, try again");    
      return(-1);
   }

   Print("Expert initialization was completed");
   return(0);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason){
   SaveHistoryInTester();
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){

   if(!PatternInitialized){
      double tmp[1];
         for(int ip = 1;ip<=Nidelt;ip++){ 
            for(int i=1;i<=NN-1;i++){
                  if(CopyClose(_Symbol,PERIOD_CURRENT,i-1,1,tmp)==-1){
                     return;
                  }
               MarketPattern[i][ip]=tmp[0];
            }               
         } 
      PatternInitialized=true;
   }
   
   if(!Sym.RefreshRates()){
      return;  
   }  

   Ask=Sym.Ask();
   Bid=Sym.Bid();
   point=Sym.Point();   

   AnaliticModule(); // After this function can use variables with trading signals
  
   TradeModule();

}

void TradeModule(){

   if(Trade_BuyCloseSignal || Trade_SellCloseSignal){
      if(Pos.Select(_Symbol)){
         if(Trade_BuyCloseSignal && Pos.PositionType()==POSITION_TYPE_BUY){
            if(!Trade.PositionClose(_Symbol,Sym.Spread()*3)){
               return;
            }
         }
         if(Trade_SellCloseSignal && Pos.PositionType()==POSITION_TYPE_SELL){
            if(!Trade.PositionClose(_Symbol,Sym.Spread()*3)){
               return;
            }         
         }
      }
   }
   
   if(Trade_BuyOpenSignal || Trade_SellOpenSignal){
      if(!Pos.Select(_Symbol)){
         if(Trade_BuyOpenSignal && !Trade_SellOpenSignal && !Trade_BuyCloseSignal){
            slv=SolveBuySL(Trade_StopLoss);
            tpv=SolveBuyTP(Trade_TakeProfit);
               if(CheckBuySL(slv) && CheckBuyTP(tpv)){
                  Trade.SetDeviationInPoints(Sym.Spread()*3);
                  if(!Trade.Buy(Lots,_Symbol,0,slv,tpv,"")){
                     return;
                  }
               }
               else{
                  Print("Buy position does not open, stoploss or takeprofit is near");
               }  
         }
         if(Trade_SellOpenSignal && !Trade_BuyOpenSignal && !Trade_SellCloseSignal){
            slv=SolveSellSL(Trade_StopLoss);
            tpv=SolveSellTP(Trade_TakeProfit);
               if(CheckSellSL(slv) && CheckSellTP(tpv)){
                  Trade.SetDeviationInPoints(Sym.Spread()*3);
                  if(!Trade.Sell(Lots,_Symbol,0,slv,tpv,"")){
                     return;
                  }
               }
               else{
                  Print("Sell position does not open, stoploss or takeprofit is near");
               }            
         }         
      }
   }

   if(ReplaceStops){
      ChangeSLByNewSignal(Trade_StopLoss,Trade_TakeProfit); // shift of stops in repeated orders to the opening position of the same type 
   }      
      
   fSimpleTrailing();
   
}


//+------------------------------------------------------------------+
//| Self-learning of function through the virtual trade              |
//+------------------------------------------------------------------+
void AnaliticModule(){  

   Trade_BuyOpenSignal=false; 
   Trade_SellOpenSignal=false; 
   Trade_BuyCloseSignal=false; 
   Trade_SellCloseSignal=false;     
   

   
      for(int ip=1;ip<=Nidelt;ip++){// By all current patterns with different parameter
         if(MathAbs(Ask-MarketPattern[1][ip])>point*(PatternDeltaParameter[ip]-0.5)){ //2 // Adds a new point to a pattern, the pattern is shifted
            Push(ip,Ask);
         }
      }            
      for(int ip=1;ip<=Nidelt;ip++){ 
         if(MathAbs(MarketPattern[1][ip]-MarketPatternLastPrice[ip])<=point*0.5)continue;
         // market situation is changed by delt[ip] parameter
         SetPriceChangeDirection(ip); // set the pattern in binary form
         Ncomb=SolvePatternCodedNumber(ip,Store_TypesCount); // definition of a unique serial number of pattern by its binary expression
            // Open virtual position 
            for(int is=1;is<=Nstop;is++){// for each level of the stop and profit
               if(!VO_Exists[Ncomb][ip][is] && TimeToUpdate(VO_OpenTime[Ncomb][ip][is])) {//4 // Provided that the cell has not the "order", otherwise have to wait for the outcome of this order
                  VO_Exists[Ncomb][ip][is]=true; 
                  VO_OpenPrice[Ncomb][ip][is]=Ask; 
                  VO_OpenTime[Ncomb][ip][is]=TimeCurrent();
               }
            }
            // Closing of virtual position and calculation of movement probability in a particular direction with a coefficient of "forgetting"
            for(int is=1;is<=Nstop;is++){
               StopInPointsParameter[is]=dstop*is; 
                  for(int ti=0;ti<=Store_TypesCount-1;ti++){
                     if(VO_Exists[ti][ip][is]){
                        if(Ask-VO_OpenPrice[ti][ip][is]>point*StopInPointsParameter[is]){//6
                           VO_Exists[ti][ip][is]=false; 
                           Store_PowerUp[ti][ip][is]=Store_PowerUp[ti][ip][is]/forg+1; 
                           Store_PowerDn[ti][ip][is]=Store_PowerDn[ti][ip][is]/forg; 
                           Store_TradesCount[ti][ip][is]++;
                        }
                        if(VO_OpenPrice[ti][ip][is]-Ask>point*StopInPointsParameter[is]){//6
                           VO_Exists[ti][ip][is]=false;                         
                           Store_PowerDn[ti][ip][is]=Store_PowerDn[ti][ip][is]/forg+1; 
                           Store_PowerUp[ti][ip][is]=Store_PowerUp[ti][ip][is]/forg; 
                           Store_TradesCount[ti][ip][is]++;
                        }
                     }
                  }
            }
            // Checking, if there is an opening of real position
            for(int is=1;is<=Nstop;is++){
               // === Sell ===   
               double prob;  
               prob=Store_PowerUp[Ncomb][ip][is]/(Store_PowerUp[Ncomb][ip][is]+Store_PowerDn[Ncomb][ip][is]+0.0001);                
                  if(prob>Probab && Store_TradesCount[Ncomb][ip][is]>10 && TimeToUpdate(LastBuyOpen))  {//4
                     Trade_TakeProfit=StopInPointsParameter[is]; 
                     Trade_StopLoss=StopInPointsParameter[is];  
                     Trade_BuyOpenSignal=true; 
                     CheckSetSellCloseSignal(prob,Trade_SellCloseSignal);
                  }
               CheckSetSellCloseSignal2(ip,is,prob,Trade_SellCloseSignal);  
               // === Buy ===
               prob=Store_PowerDn[Ncomb][ip][is]/(Store_PowerUp[Ncomb][ip][is]+Store_PowerDn[Ncomb][ip][is] + 0.0001); 
                  if(prob>Probab && Store_TradesCount[Ncomb][ip][is]>10 && TimeToUpdate(LastSellOpen)){//4
                     Trade_TakeProfit=StopInPointsParameter[is]; 
                     Trade_StopLoss=StopInPointsParameter[is]; 
                     Trade_SellOpenSignal=1; 
                     CheckSetBuyCloseSignal(prob,Trade_BuyCloseSignal);
                  }
               CheckSetBuyCloseSignal2(ip,is,prob,Trade_SellCloseSignal);
            }
         Store_ChangesCount++;
         MarketPatternLastPrice[ip]=MarketPattern[1][ip]; 
      }
   SaveHistoryOnAccount();      
}   


//+------------------------------------------------------------------+
//| Add a new point in the pattern, and the removal of old           |
//+------------------------------------------------------------------+
void Push(int aPattern,double aValue){
      for(int i=1;i<=NN - 1; i++){ //3
         MarketPattern[NN+1-i][aPattern]=MarketPattern[NN-i][aPattern];  
      } //3
   MarketPattern[1][aPattern]=aValue; 
}  

//+------------------------------------------------------------------+
//| Getting of binary pattern                                        |
//+------------------------------------------------------------------+
void SetPriceChangeDirection(int aPattern){
   for(int i=1;i<=NN-1;i++){ //3
      if(MarketPattern[i][aPattern]>MarketPattern[i+1][aPattern]){//4
         BinaryPattern[i][aPattern]=1;
      }
      else {
         BinaryPattern[i][aPattern]=0;
      }
   }
}

//+------------------------------------------------------------------+
//| Calculation of pattern serial number by his binary form          |
//+------------------------------------------------------------------+
int SolvePatternCodedNumber(int aPattern,int & aPatternsLimit){
   int Num=0;  
   int Mult=1;    
      for(int i=1;i<=NN-1;i++){
         Num=Num+Mult*BinaryPattern[i][aPattern]; 
         Mult=2*Mult;
      }
   aPatternsLimit=Mult;
   return(Num);
}

//+------------------------------------------------------------------+
//| Checking of timeout end                                          |
//+------------------------------------------------------------------+
bool TimeToUpdate(datetime aLastTime){
   return(TimeCurrent()-aLastTime>2*Period()*60);
}   

//+------------------------------------------------------------------+
//| Checking of conditions for buy closing                           |
//+------------------------------------------------------------------+  
void CheckSetBuyCloseSignal(double prob,bool & BuyCloseSignal){
      if(Pos.Select(_Symbol)){
         if(Pos.PositionType()==POSITION_TYPE_BUY){
            double p=ReadProbFromFile("FDlast_buy"+Symbol()+IntegerToString(PeriodSeconds()/60));
               if(prob>(p+0.05)){
                  BuyCloseSignal=true; 
               }
         }
      }
   SaveProbToFile("FDlast_buy"+Symbol()+IntegerToString(PeriodSeconds()/60),prob);
}

//+------------------------------------------------------------------+
//| Checking of conditions for sell closing                          |
//+------------------------------------------------------------------+  
void CheckSetSellCloseSignal(double prob,bool & SellCloseSignal){
      if(Pos.Select(_Symbol)){
         if(Pos.PositionType()==POSITION_TYPE_SELL){
            double p=ReadProbFromFile("FDlast_sell"+Symbol()+IntegerToString(PeriodSeconds()/60));
               if(prob>(p+0.05)){
                  SellCloseSignal=true; 
               }
         }
      }                        
   SaveProbToFile("FDlast_sell"+Symbol()+IntegerToString(PeriodSeconds()/60),prob);
}  

//+------------------------------------------------------------------+
//| Save a one value of current probability in the file              |
//+------------------------------------------------------------------+
void SaveProbToFile(string aFileName,double aProb){
//Print(aFileName);
   int h=FileOpen(aFileName,FILE_CSV|FILE_WRITE|FILE_COMMON); 
   FileWrite(h,aProb); 
   FileClose(h);
}

//+------------------------------------------------------------------+
//| Checking of conditions for buy closing - 2                       |
//+------------------------------------------------------------------+  
void CheckSetBuyCloseSignal2(int aPattern,int aStop,double aProb,bool & SellCloseSignal){
   if(Pos.Select(_Symbol)){
      if(Pos.PositionType()==POSITION_TYPE_BUY){
         if(aProb>0.6 && Store_TradesCount[Ncomb][aPattern][aStop]>10 && (Bid-Pos.PriceOpen())>point*(dstop/2)){//7
            Trade_BuyCloseSignal=true; 
         }
      }
   }
}  

//+------------------------------------------------------------------+
//| Checking of conditions for sell closing 2                        |
//+------------------------------------------------------------------+  
void CheckSetSellCloseSignal2(int aPattern,int aStop,double aProb,bool & SellCloseSignal){                     
   if(Pos.Select(_Symbol)){
      if(Pos.PositionType()==POSITION_TYPE_SELL){
         if(aProb>0.6 && Store_TradesCount[Ncomb][aPattern][aStop]>10 && (Pos.PriceOpen()-Ask)>point*(dstop/2)){
            SellCloseSignal=true; 
         }
      }
   }
} 

//+------------------------------------------------------------------+
//| Save the learning results to a file at work in account           |
//| Called periodically                                              |
//+------------------------------------------------------------------+  
void SaveHistoryOnAccount(){
   if(SaveHistory){ 
      if(!MQL5InfoInteger(MQL5_TESTING)){
         if(Store_ChangesCount>10 && TimeCurrent()>=Store_LastSaveTime){ //1
            Store_ChangesCount=0; 
            WriteDataToFile();
         } //1
      }  
   }
}   

//+------------------------------------------------------------------+
//| Save the learning results to a file at work in tester            |
//| Called when expert deinitialization                              |
//+------------------------------------------------------------------+  
void SaveHistoryInTester(){
   if(SaveHistory){ 
      if(MQL5InfoInteger(MQL5_TESTING)){
         WriteDataToFile();
      }  
   }
}

//+------------------------------------------------------------------+
//| Download a one value of last probability from the file           |
//+------------------------------------------------------------------+
double ReadProbFromFile(string aFileName){
   int h=FileOpen(aFileName,FILE_CSV|FILE_READ|FILE_WRITE|FILE_COMMON); 
   double p=FileReadNumber(h); 
   FileClose(h);
   return(p);
}

//+------------------------------------------------------------------+
//| Storing arrays with data for learning in the file                |
//+------------------------------------------------------------------+
void WriteDataToFile(){
   int h=FileOpen(Store_FileName,FILE_CSV|FILE_WRITE|FILE_COMMON); 
   FileWrite(h,TimeCurrent()); 
   FileWrite(h,Store_TypesCount);
   Print("h="+h) ;
      for(int is=1;is<=Nstop;is++){ //2
         for(int ip=1;ip<=Nidelt;ip++){ //3
            for(int ti=0;ti<=Store_TypesCount-1;ti++){ //4
               FileWrite(h,Store_PowerUp[ti][ip][is]); 
               FileWrite(h,Store_PowerDn[ti][ip][is]); 
               FileWrite(h,Store_TradesCount[ti][ip][is]); 
            }//4
         }//3
      } //2
   FileClose(h);  
}

//+------------------------------------------------------------------+
//| Loading a data for learning                                      |
//+------------------------------------------------------------------+
void ReadHistoryFile(){
   if(!Store_IsLoaded){ //1
      Store_IsLoaded=true;
         if(ReadHistory){
            ReadDataFromFile(Store_TypesCount);   
         }
   } //1
}  

//+------------------------------------------------------------------+
//| Reading a file with data for learning                            |
//+------------------------------------------------------------------+
void ReadDataFromFile(int & aTypesCount){
   int h=FileOpen(Store_FileName,FILE_CSV|FILE_READ|FILE_COMMON);
   Store_LastSaveTime=(datetime)FileReadNumber(h); 
   aTypesCount=(int)FileReadNumber(h); 
      for(int is=1;is<=Nstop;is++){ //3
         for(int ip=1;ip<=Nidelt;ip++){ //4
            for(int ti=0;ti<=aTypesCount-1;ti++){ //5
               Store_PowerUp[ti][ip][is]=FileReadNumber(h); 
               Store_PowerDn[ti][ip][is]=FileReadNumber(h); 
               Store_TradesCount[ti][ip][is]=(int)FileReadNumber(h); 
            }//5
         }//4
      }//3  
   FileClose(h); 
}


//+------------------------------------------------------------------+
//|   Function for calculation the buy stoploss                      |
//+------------------------------------------------------------------+
double SolveBuySL(int StopLossPoints){
   if(StopLossPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Ask()-Sym.Point()*StopLossPoints));
}

//+------------------------------------------------------------------+
//|   Function for calculation the buy takeprofit                    |
//+------------------------------------------------------------------+
double SolveBuyTP(int TakeProfitPoints){
   if(TakeProfitPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Ask()+Sym.Point()*TakeProfitPoints));   
}

//+------------------------------------------------------------------+
//|   Function for calculation the sell stoploss                     |
//+------------------------------------------------------------------+
double SolveSellSL(int StopLossPoints){
   if(StopLossPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Bid()+Sym.Point()*StopLossPoints));
}

//+------------------------------------------------------------------+
//|   Function for calculation the sell takeprofit                   |
//+------------------------------------------------------------------+
double SolveSellTP(int TakeProfitPoints){
   if(TakeProfitPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Bid()-Sym.Point()*TakeProfitPoints));   
}

//+------------------------------------------------------------------+
//|   Function for calculation the minimum stoploss of buy           |
//+------------------------------------------------------------------+
double BuyMSL(){
   return(Sym.NormalizePrice(Sym.Bid()-Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for calculation the minimum takeprofit of buy         |
//+------------------------------------------------------------------+
double BuyMTP(){
   return(Sym.NormalizePrice(Sym.Ask()+Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for calculation the minimum stoploss of sell          |
//+------------------------------------------------------------------+
double SellMSL(){
   return(Sym.NormalizePrice(Sym.Ask()+Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for calculation the minimum takeprofit of sell        |
//+------------------------------------------------------------------+
double SellMTP(){
   return(Sym.NormalizePrice(Sym.Bid()-Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for checking the buy stoploss                         |
//+------------------------------------------------------------------+
bool CheckBuySL(double StopLossPrice){
   if(StopLossPrice==0)return(true);
   return(StopLossPrice<BuyMSL());
}

//+------------------------------------------------------------------+
//|   Function for checking the buy takeprofit                       |
//+------------------------------------------------------------------+
bool CheckBuyTP(double TakeProfitPrice){
   if(TakeProfitPrice==0)return(true);
   return(TakeProfitPrice>BuyMTP());
}

//+------------------------------------------------------------------+
//|   Function for checking the sell stoploss                        |
//+------------------------------------------------------------------+
bool CheckSellSL(double StopLossPrice){
   if(StopLossPrice==0)return(true);
   return(StopLossPrice>SellMSL());
}

//+------------------------------------------------------------------+
//|   Function for checking the sell takeprofit                      |
//+------------------------------------------------------------------+
bool CheckSellTP(double TakeProfitPrice){
   if(TakeProfitPrice==0)return(true);
   return(TakeProfitPrice<SellMTP());
}

//+------------------------------------------------------------------+
//| Simple Trailing function                                         |
//+------------------------------------------------------------------+
void fSimpleTrailing(){
   if(Trailing<=0){
      return;
   }         
   if(!Pos.Select(_Symbol)){
      return;
   }         
   if(!Sym.RefreshRates()){
      return;  
   }   
   double nsl,tmsl,psl;  
   switch(Pos.PositionType()){
      case POSITION_TYPE_BUY:
         nsl=Sym.NormalizePrice(Sym.Bid()-_Point*Trailing);
            if(nsl>=Sym.NormalizePrice(Pos.PriceOpen())){
               if(nsl>Sym.NormalizePrice(Pos.StopLoss())){
                  tmsl=Sym.NormalizePrice(Sym.Bid()-_Point*Sym.StopsLevel());
                     if(nsl<tmsl){
                        Trade.PositionModify(_Symbol,nsl,Pos.TakeProfit());
                     }
               }
            }
      break;
      case POSITION_TYPE_SELL:
         nsl=Sym.NormalizePrice(Sym.Ask()+_Point*Trailing);
            if(nsl<=Sym.NormalizePrice(Pos.PriceOpen())){
               psl=Sym.NormalizePrice(Pos.StopLoss());
                  if(nsl<psl || psl==0){
                     tmsl=Sym.NormalizePrice(Sym.Ask()+_Point*Sym.StopsLevel());
                        if(nsl>tmsl){
                           Trade.PositionModify(_Symbol,nsl,Pos.TakeProfit());
                        }
                  }
            }      
      break;
   }
}

//+------------------------------------------------------------------+
// Shift of stoploss and takeprofit opening orders with the new      |
// signal                                                            |
// Made only on the direction                                        |
//+------------------------------------------------------------------+
void ChangeSLByNewSignal(int stop,int take){
   if(Pos.Select(_Symbol)){
      if(Pos.PositionType()==POSITION_TYPE_BUY && Trade_BuyOpenSignal){
         double nsl=Sym.NormalizePrice(Bid-point*stop);
            if(nsl>Sym.NormalizePrice(Pos.StopLoss())){
               Trade.PositionModify(_Symbol,nsl,Sym.NormalizePrice(Bid+point*take));
            }
      }
      if(Pos.PositionType()==POSITION_TYPE_SELL &&  Trade_SellOpenSignal){
         double nsl=Sym.NormalizePrice(Ask+point*stop);   
            if(nsl<Sym.NormalizePrice(Pos.StopLoss())){
               Trade.PositionModify(_Symbol,nsl,Sym.NormalizePrice(Ask-point*take));
            }
      }
   }
}    
