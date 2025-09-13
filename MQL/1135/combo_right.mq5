//+------------------------------------------------------------------+
//|                                                  Combo_Right.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description ""
#property version   "1.00"

#property description "Expert rewritten from MQ4, the author is Reshetov (http://www.mql4.com/ru/users/Reshetov), link to original - http://codebase.mql4.com/ru/2719"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CSymbolInfo Sym;
CPositionInfo Pos;
   
//--- input parameters

input double                           tp1               =  500;              /*tp1*/           // Takeprofit when opening a position by the basic trading signal
input double                           sl1               =  500;              /*sl1*/           // Stoploss when opening a position by the basic trading signal
input int                              CCIPeriod         =  10;	            /*CCIPeriod*/     // CCI period
input ENUM_APPLIED_PRICE               CCIPrice          =  PRICE_OPEN;	      /*CCIPrice*/      // CCI price

input int                              x12               =  100;              /*x12*/           // Weights of the sale perceptron
input int                              x22               =  100;
input int                              x32               =  100;
input int                              x42               =  100;
input double                           tp2               =  500;              /*tp2*/           // Takeprofit when opening a position by the sale perceptron signal
input double                           sl2               =  500;              /*sl2*/           // Stoploss when opening a position by the sale perceptron signal
input int                              p2                =  20;               /*p2*/            // The period of history data which covers the sale perceptron

input int                              x13               =  100;              /*x13*/           // Weights of the buying perceptron
input int                              x23               =  100;
input int                              x33               =  100;
input int                              x43               =  100;
input double                           tp3               =  500;              /*tp3*/           // Takeprofit when opening a position by the buying perceptron signal
input double                           sl3               =  500;              /*sl3*/           // Stoploss when opening a position by the buying perceptron signal
input int                              p3                =  20;               /*p3*/            // The period of history data which covers the buying perceptron

input int                              x14               =  100;              /*x14*/           // Weights of the general perceptron
input int                              x24               =  100;
input int                              x34               =  100;
input int                              x44               =  100;
input int                              p4                =  20;               /*p4*/            // The period of history data which covers the general perceptron

input int                              pass              =  1;                /*pass*/          // Expert mode: 1 - basic system, 1 - sale perceptron, 2 - buying perceptron, 4 - all perceptrons, operating mode.
input double                           lots              =  0.1;
input int                              Shift             =  1;                /*Shift*/         // Bar from which the price data are used: 0 - shaped bar, 1 - the first shaped bar

double w11;
double w12;
double w13;
double w14;

double w21;
double w22;
double w23;
double w24;

double w31;
double w32;
double w33;
double w34;

int sh11;
int sh12;
int sh13;
int sh14;
int sh15;

int sh21;
int sh22;
int sh23;
int sh24;
int sh25;

int sh31;
int sh32;
int sh33;
int sh34;
int sh35;

double output1;
double output2;
double output3;

int CCIHandle=INVALID_HANDLE;


int          prevtime = 0;
double       sl = 10;
double       tp = 10;

int Handle=INVALID_HANDLE;

datetime ctm[1];
datetime LastTime;
double lot,slv,msl,tpv,mtp;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   w11=x12-100;
   w12=x22-100;
   w13=x32-100;
   w14=x42-100;
      
   w21=x13-100;
   w22=x23-100;
   w23=x33-100;
   w24=x43-100;
      
   w31=x14-100;
   w32=x24-100;
   w33=x34-100;
   w34=x44-100;
   
   sh11=Shift;
   sh12=Shift+p2;
   sh13=Shift+p2*2;
   sh14=Shift+p2*3;
   sh15=Shift+p2*4;
   
   sh21=Shift;
   sh22=Shift+p3;
   sh23=Shift+p3*2;
   sh24=Shift+p3*3;
   sh25=Shift+p3*4;      
   
   sh31=Shift;
   sh32=Shift+p4;
   sh33=Shift+p4*2;
   sh34=Shift+p4*3;
   sh35=Shift+p4*4;     

   // Loading indicators...
   
   CCIHandle=iCCI(_Symbol,PERIOD_CURRENT,CCIPeriod,CCIPrice);

   if(CCIHandle==INVALID_HANDLE){
      Alert("Failed to loading the indicator, try again");
      return(-1);
   }   
   
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
   if(CCIHandle!=INVALID_HANDLE)IndicatorRelease(CCIHandle);
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){

   if(CopyTime(_Symbol,PERIOD_CURRENT,0,1,ctm)==-1){
      return;
   }
   if(Shift==0 || ctm[0]!=LastTime){
      
      sl=sl1;
      tp=tp1;      
      
      // Indicators
      double Signal;
      if(!Supervisor(Signal)){
         return;
      }
 
      // Open
      if(!Pos.Select(_Symbol)){
            if(Signal>0){ 
               if(!Sym.RefreshRates())return;         
               if(!SolveLots(lot))return;
               slv=SolveBuySL(sl);
               tpv=SolveBuyTP(tp);
                  if(CheckBuySL(slv) && CheckBuyTP(tpv)){
                     Trade.SetDeviationInPoints(Sym.Spread()*3);
                     if(!Trade.Buy(lot,_Symbol,0,slv,tpv,"")){
                        return;
                     }
                  }
                  else{
                     Print("Buy position does not open, stoploss or takeprofit is near");
                  }         
            }
            // Sell
            if(Signal<0){
               if(!Sym.RefreshRates())return;         
               if(!SolveLots(lot))return;
               slv=SolveSellSL(sl);
               tpv=SolveSellTP(tp);
                  if(CheckSellSL(slv) && CheckSellTP(tpv)){
                     Trade.SetDeviationInPoints(Sym.Spread()*3);
                     if(!Trade.Sell(lot,_Symbol,0,slv,tpv,"")){
                        return;
                     }
                  }
                  else{
                     Print("Sell position does not open, stoploss or takeprofit is near");
                  }          
            }
      }            
      LastTime=ctm[0];
   }
}

bool Supervisor(double & Signal) {
   double BasicSig=0;
   if(!basicTradingSystem(BasicSig))return(false);
   Signal=0;
   if(pass==4){
      if(!Perceptron(output1,sh11,sh12,sh13,sh14,sh15,w11,w12,w13,w14) ||
         !Perceptron(output2,sh21,sh22,sh23,sh24,sh25,w21,w22,w23,w24) ||
         !Perceptron(output3,sh31,sh32,sh33,sh34,sh35,w31,w32,w33,w34)   
      )return(false);         
      if(output3>0){
         if(output2>0){
            sl=sl3;
            tp=tp3;
            Signal=1;
            return(true);
         }
       }
       else{
         if(output1<0){
            sl=sl2;
            tp=tp2;
            Signal=-1;
            return(true);
         }
      }
      Signal=BasicSig;
      return(true);
   }
   if(pass==3) {
      if(!Perceptron(output2,sh21,sh22,sh23,sh24,sh25,w21,w22,w23,w24))return(false);
      if(output2>0){
         sl=sl3;
         tp=tp3;
         Signal=1;
         return(true);
       } 
       else {
         Signal=BasicSig;
         return(true);
       }
   }
   if(pass==2){
      if(!Perceptron(output1,sh11,sh12,sh13,sh14,sh15,w11,w12,w13,w14))return(false);
      if(output1<0) {
         sl=sl2;
         tp=tp2;
         Signal=-1;
         return(true);
       } 
       else{
         Signal=BasicSig;
         return(true);
       }
   }   
   Signal=BasicSig;
   return(true);
}
   
double Perceptron(double & output,int sh1,int sh2,int sh3,int sh4,int sh5,double w1,double w2,double w3,double w4){
   double Csh1[1],Osh2[1],Osh3[1],Osh4[1],Osh5[1];
   if(CopyClose(_Symbol,PERIOD_CURRENT,sh1,1,Csh1)==-1 ||
      CopyClose(_Symbol,PERIOD_CURRENT,sh2,1,Osh2)==-1 ||
      CopyClose(_Symbol,PERIOD_CURRENT,sh3,1,Osh3)==-1 ||
      CopyClose(_Symbol,PERIOD_CURRENT,sh4,1,Osh4)==-1 ||
      CopyClose(_Symbol,PERIOD_CURRENT,sh5,1,Osh5)==-1
   )return(false);
   double a1=Csh1[0]-Osh2[0];
   double a2=Osh2[0]-Osh3[0];
   double a3=Osh3[0]-Osh4[0];
   double a4=Osh4[0]-Osh5[0];
   output=w1*a1+w2*a2+w3*a3+w4*a4;
   return(true);
}

bool basicTradingSystem(double & aSig){
   aSig=0;
   double cci0[1];
   if(CopyBuffer(CCIHandle,0,Shift,1,cci0)==-1)return(false);
   aSig=cci0[0];
   return(true);
}

//+------------------------------------------------------------------+
//|   Function of data copy for indicators and price                 |
//+------------------------------------------------------------------+
bool Indicators(){

   return(true);
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool SignalOpenBuy(){

   return(false);
}

//+------------------------------------------------------------------+
//|   Function for determining sell signals                          |
//+------------------------------------------------------------------+
bool SignalOpenSell(){

   return(false);
}

//+------------------------------------------------------------------+
//|   Function for determining buy close signals                     |
//+------------------------------------------------------------------+
bool SignalCloseBuy(){

   return (false);
}

//+------------------------------------------------------------------+
//|   Function for determining sell close signals                    |
//+------------------------------------------------------------------+
bool SignalCloseSell(){

   return (false);
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
//|   The function which define the lot by the result of trade       |
//+------------------------------------------------------------------+
bool SolveLots(double & aLots){
   aLots=lots;         
   return(true);
}
