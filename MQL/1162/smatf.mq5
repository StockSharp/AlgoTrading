//+------------------------------------------------------------------+
//|                                                        Smatf.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "Expert rewritten from MQL4, the author is - http://www.mql4.com/ru/users/zotkindm, link to original - http://codebase.mql4.com/ru/2974"
#property version   "1.00"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;

// Timeframes
input ENUM_TIMEFRAMES   TF1               =  PERIOD_M15; 
input ENUM_TIMEFRAMES   TF2               =  PERIOD_H1;
input ENUM_TIMEFRAMES   TF3               =  PERIOD_H4;
// лю periods
input int               maTrendPeriodv_1  =  5;
input int               maTrendPeriodv_2  =  8;
input int               maTrendPeriodv_3  =  13;
input int               maTrendPeriodv_4  =  21;
input int               maTrendPeriodv_5  =  34;

input int               Shift             =  1;                /*Shift*/            // Bar on which indicators are checked: 0 - shaped bar, 1 - the first shaped bar
input int               OpenLevel         =  0;                /*OpenLevel*/        // Opening level 0 or 1
input int               CloseLevel        =  0;                /*CloseLevel*/       // Closing level 0 or 1, 2 - off
input double            Lots              =  0.1;              /*Lots*/             // Lot
input int               StopLoss          =  550;              /*StopLoss*/         // Stoploss in points, 0 - without stoploss.
input int               TakeProfit        =  550;              /*TakeProfit*/       // Takeprofit in points, 0 - without takeprofit.
input int               Trailing          =  0;                /*Trailing*/         // Trailing level, if value is 0 - then trailing off.

int HandleMA[3][5];
int Handle_AC_TF1=INVALID_HANDLE;
int Handle_AC_TF3=INVALID_HANDLE;

struct SMAV{
   double c[1];
   double p[1];
};

SMAV mav[3][5];
double ac1_0[1],ac1_1[1],ac1_2[1],ac1_3[1];
double ac3_0[1],ac3_1[1],ac3_2[1],ac3_3[1];

double MaH11v,  MaH41v, MaD11v, MaH1pr1v, MaH4pr1v, MaD1pr1v;
double MaH12v,  MaH42v, MaD12v, MaH1pr2v, MaH4pr2v, MaD1pr2v;
double MaH13v,  MaH43v, MaD13v, MaH1pr3v, MaH4pr3v, MaD1pr3v;
double MaH14v,  MaH44v, MaD14v, MaH1pr4v, MaH4pr4v, MaD1pr4v;
double MaH15v,  MaH45v, MaD15v, MaH1pr5v, MaH4pr5v, MaD1pr5v;

double  acv;
double  ac1v;
double  ac2v;
double  ac3v;

double  ac03v;
double  ac13v;
double  ac23v;
double  ac33v;

double u1x5v, u1x8v, u1x13v, u1x21v, u1x34v;
double u2x5v, u2x8v, u2x13v, u2x21v, u2x34v;
double u3x5v, u3x8v, u3x13v, u3x21v, u3x34v;
double u1acv, u2acv, u3acv;

double d1x5v, d1x8v, d1x13v, d1x21v, d1x34v;
double d2x5v, d2x8v, d2x13v, d2x21v, d2x34v;
double d3x5v, d3x8v, d3x13v, d3x21v, d3x34v;
double d1acv, d2acv, d3acv;

double uitog1v;
double uitog2v;
double uitog3v;

double ditog1v;
double ditog2v;
double ditog3v;

int Signal;

datetime ctm[1];
datetime LastTime;
double lot,slv,msl,tpv,mtp;
string gvp;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   ENUM_TIMEFRAMES tf[3];
   int per[5];
   
   tf[0]=TF1;
   tf[1]=TF2;
   tf[2]=TF3;
   
   per[0]=maTrendPeriodv_1;
   per[1]=maTrendPeriodv_2;
   per[2]=maTrendPeriodv_3;
   per[3]=maTrendPeriodv_4;
   per[4]=maTrendPeriodv_5;
   
   bool Err=false;
   for(int t=0;t<3;t++){
      for(int p=0;p<5;p++){
         HandleMA[t][p]=iMA(_Symbol,tf[t],per[p],0,MODE_SMA,PRICE_CLOSE);
            if(HandleMA[t][p]==INVALID_HANDLE){
               Err=true;
               break;
            }
      }
   }
   
   if(!Err){
      Handle_AC_TF1=iAC(_Symbol,TF1);
      Handle_AC_TF3=iAC(_Symbol,TF3);
      Err=(Handle_AC_TF1==INVALID_HANDLE || Handle_AC_TF3==INVALID_HANDLE);
   }
   
   if(Err){
      Alert("Failed to loading the indicator, try again");
      return(-1);   
   }

   // Preparation of global variables names
   gvp=MQL5InfoString(MQL5_PROGRAM_NAME)+"_"+_Symbol+"_"+IntegerToString(PeriodSeconds()/60)+"_"+IntegerToString(AccountInfoInteger(ACCOUNT_LOGIN));
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE)==ACCOUNT_TRADE_MODE_DEMO)gvp=gvp+"_d";
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE)==ACCOUNT_TRADE_MODE_REAL)gvp=gvp+"_r";
   if(MQL5InfoInteger(MQL5_TESTING))gvp=gvp+"_t";
   DeleteGV();
   
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

   for(int t=0;t<3;t++){
      for(int p=0;p<5;p++){
         if(HandleMA[t][p]!=INVALID_HANDLE){
            IndicatorRelease(HandleMA[t][p]);
         }
      }
   }      
   
   if(Handle_AC_TF1!=INVALID_HANDLE){
      IndicatorRelease(Handle_AC_TF1);   
   }
   if(Handle_AC_TF3!=INVALID_HANDLE){
      IndicatorRelease(Handle_AC_TF3);      
   }
                        
   DeleteGV();   
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){

   if(CopyTime(_Symbol,PERIOD_CURRENT,0,1,ctm)==-1){
      return;
   }
   if(Shift==0 || ctm[0]!=LastTime){
      
      // Indicators
      if(!Indicators()){
         return;
      }   
      
      // Signals
      bool CloseBuy=SignalCloseBuy();
      bool CloseSell=SignalCloseSell();
      bool OpenBuy=SignalOpenBuy();
      bool OpenSell=SignalOpenSell();

      // Close
      if(Pos.Select(_Symbol)){
         if(CloseBuy && Pos.PositionType()==POSITION_TYPE_BUY){
            if(!Sym.RefreshRates()){
               return;  
            }
            if(!Trade.PositionClose(_Symbol,Sym.Spread()*3)){
               return;
            }
         }
         if(CloseSell && Pos.PositionType()==POSITION_TYPE_SELL){
            if(!Sym.RefreshRates()){
               return;  
            }         
            if(!Trade.PositionClose(_Symbol,Sym.Spread()*3)){
               return;
            }
         }         
      }
      
      // Open
      if(!Pos.Select(_Symbol)){
            if(OpenBuy && !OpenSell && !CloseBuy){ 
               if(!Sym.RefreshRates())return;         
               if(!SolveLots(lot))return;
               slv=SolveBuySL(StopLoss);
               tpv=SolveBuyTP(TakeProfit);
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
            if(OpenSell && !OpenBuy && !CloseSell){
               if(!Sym.RefreshRates())return;         
               if(!SolveLots(lot))return;
               slv=SolveSellSL(StopLoss);
               tpv=SolveSellTP(TakeProfit);
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
   
   fSimpleTrailing();

}

//+------------------------------------------------------------------+
//|   Function of data copy for indicators and price                 |
//+------------------------------------------------------------------+
bool Indicators(){
   for(int t=0;t<3;t++){
      for(int p=0;p<5;p++){
         if(CopyBuffer(HandleMA[t][p],0,Shift,1,mav[t][p].c)==-1 ||
            CopyBuffer(HandleMA[t][p],0,Shift+1,1,mav[t][p].p)==-1
         )return(false);
      }
   }  
   if(
      CopyBuffer(Handle_AC_TF1,0,Shift,1,ac1_0)==-1 || 
      CopyBuffer(Handle_AC_TF1,0,Shift+1,1,ac1_1)==-1 || 
      CopyBuffer(Handle_AC_TF1,0,Shift+2,1,ac1_2)==-1 || 
      CopyBuffer(Handle_AC_TF1,0,Shift+3,1,ac1_3)==-1 || 
      CopyBuffer(Handle_AC_TF3,0,Shift,1,ac3_0)==-1 || 
      CopyBuffer(Handle_AC_TF3,0,Shift+1,1,ac3_1)==-1 || 
      CopyBuffer(Handle_AC_TF3,0,Shift+2,1,ac3_2)==-1 || 
      CopyBuffer(Handle_AC_TF3,0,Shift+3,1,ac3_3)==-1
   )return(false);    
   
   MaH11v=mav[0][0].c[0];   MaH1pr1v=mav[0][0].p[0];
   MaH12v=mav[0][1].c[0];   MaH1pr2v=mav[0][1].p[0];
   MaH13v=mav[0][2].c[0];   MaH1pr3v=mav[0][2].p[0];
   MaH14v=mav[0][3].c[0];   MaH1pr4v=mav[0][3].p[0];
   MaH15v=mav[0][4].c[0];   MaH1pr5v=mav[0][4].p[0];
   
   MaH41v=mav[1][0].c[0];   MaH4pr1v=mav[1][0].p[0];
   MaH42v=mav[1][1].c[0];   MaH4pr2v=mav[1][1].p[0];
   MaH43v=mav[1][2].c[0];   MaH4pr3v=mav[1][2].p[0];
   MaH44v=mav[1][3].c[0];   MaH4pr4v=mav[1][3].p[0];
   MaH45v=mav[1][4].c[0];   MaH4pr5v=mav[1][4].p[0];
   
   MaD11v=mav[2][0].c[0];   MaD1pr1v=mav[2][0].p[0];
   MaD12v=mav[2][1].c[0];   MaD1pr2v=mav[2][1].p[0];
   MaD13v=mav[2][2].c[0];   MaD1pr3v=mav[2][2].p[0];
   MaD14v=mav[2][3].c[0];   MaD1pr4v=mav[2][3].p[0];
   MaD15v=mav[2][4].c[0];   MaD1pr5v=mav[2][4].p[0];
   
   
   if(MaH11v < MaH1pr1v) {u1x5v = 0; d1x5v = 1;}
   if(MaH11v > MaH1pr1v) {u1x5v = 1; d1x5v = 0;}
   if(MaH11v == MaH1pr1v){u1x5v = 0; d1x5v = 0;}           
   if(MaH41v < MaH4pr1v) {u2x5v = 0; d2x5v = 1;}           
   if(MaH41v > MaH4pr1v) {u2x5v = 1; d2x5v = 0;}
   if(MaH41v == MaH4pr1v){u2x5v = 0; d2x5v = 0;}           
   if(MaD11v < MaD1pr1v) {u3x5v = 0; d3x5v = 1;}           
   if(MaD11v > MaD1pr1v) {u3x5v = 1; d3x5v = 0;}
   if(MaD11v == MaD1pr1v){u3x5v = 0; d3x5v = 0;} 
   
   if(MaH12v < MaH1pr2v) {u1x8v = 0; d1x8v = 1;}
   if(MaH12v > MaH1pr2v) {u1x8v = 1; d1x8v = 0;}
   if(MaH12v == MaH1pr2v){u1x8v = 0; d1x8v = 0;}           
   if(MaH42v < MaH4pr2v) {u2x8v = 0; d2x8v = 1;}           
   if(MaH42v > MaH4pr2v) {u2x8v = 1; d2x8v = 0;}
   if(MaH42v == MaH4pr2v){u2x8v = 0; d2x8v = 0;}           
   if(MaD12v < MaD1pr2v) {u3x8v = 0; d3x8v = 1;}             
   if(MaD12v > MaD1pr2v) {u3x8v = 1; d3x8v = 0;}
   if(MaD12v == MaD1pr2v){u3x8v = 0; d3x8v = 0;}
   
   if(MaH13v < MaH1pr3v) {u1x13v = 0; d1x13v = 1;}
   if(MaH13v > MaH1pr3v) {u1x13v = 1; d1x13v = 0;}
   if(MaH13v == MaH1pr3v){u1x13v = 0; d1x13v = 0;}             
   if(MaH43v < MaH4pr3v) {u2x13v = 0; d2x13v = 1;}           
   if(MaH43v > MaH4pr3v) {u2x13v = 1; d2x13v = 0;}
   if(MaH43v == MaH4pr3v){u2x13v = 0; d2x13v = 0;}           
   if(MaD13v < MaD1pr3v) {u3x13v = 0; d3x13v = 1;}           
   if(MaD13v > MaD1pr3v) {u3x13v = 1; d3x13v = 0;}
   if(MaD13v == MaD1pr3v){u3x13v = 0; d3x13v = 0;}
   
   if(MaH14v < MaH1pr4v) {u1x21v = 0; d1x21v = 1;}
   if(MaH14v > MaH1pr4v) {u1x21v = 1; d1x21v = 0;}
   if(MaH14v == MaH1pr4v){u1x21v = 0; d1x21v = 0;}             
   if(MaH44v < MaH4pr4v) {u2x21v = 0; d2x21v = 1;}           
   if(MaH44v > MaH4pr4v) {u2x21v = 1; d2x21v = 0;}
   if(MaH44v == MaH4pr4v){u2x21v = 0; d2x21v = 0;}           
   if(MaD14v < MaD1pr4v) {u3x21v = 0; d3x21v = 1;}           
   if(MaD14v > MaD1pr4v) {u3x21v = 1; d3x21v = 0;}
   if(MaD14v == MaD1pr4v){u3x21v = 0; d3x21v = 0;} 
   
   if(MaH15v < MaH1pr5v) {u1x34v = 0; d1x34v = 1;}
   if(MaH15v > MaH1pr5v) {u1x34v = 1; d1x34v = 0;}
   if(MaH15v == MaH1pr5v){u1x34v = 0; d1x34v = 0;}             
   if(MaH45v < MaH4pr5v) {u2x34v = 0; d2x34v = 1;}           
   if(MaH45v > MaH4pr5v) {u2x34v = 1; d2x34v = 0;}
   if(MaH45v == MaH4pr5v){u2x34v = 0; d2x34v = 0;}           
   if(MaD15v < MaD1pr5v) {u3x34v = 0; d3x34v = 1;}           
   if(MaD15v > MaD1pr5v) {u3x34v = 1; d3x34v = 0;}
   if(MaD15v == MaD1pr5v){u3x34v = 0; d3x34v = 0;}   

   acv=ac1_0[0];
   ac1v=ac1_1[0];
   ac2v=ac1_2[0];
   ac3v=ac1_3[0];
   
   if((ac1v>ac2v && ac2v>ac3v && acv<0 && acv>ac1v)||(acv>ac1v && ac1v>ac2v && acv>0)) {u1acv = 3; d1acv = 0;}
   if((ac1v<ac2v && ac2v<ac3v && acv>0 && acv<ac1v)||(acv<ac1v && ac1v<ac2v && acv<0)) {u1acv = 0; d1acv = 3;}
   if((((ac1v<ac2v || ac2v<ac3v) && acv<0 && acv>ac1v) || (acv>ac1v && ac1v<ac2v && acv>0))
   || (((ac1v>ac2v || ac2v>ac3v) && acv>0 && acv<ac1v) || (acv<ac1v && ac1v>ac2v && acv<0)))
   {u1acv = 0; d1acv = 0;}

   ac03v=ac3_0[0];
   ac13v=ac3_1[0];
   ac23v=ac3_2[0];
   ac33v=ac3_3[0];

   if((ac13v>ac23v && ac23v>ac33v && ac03v<0 && ac03v>ac13v)||(ac03v>ac13v && ac13v>ac23v && ac03v>0)) {u3acv = 3; d3acv = 0;}     
   if((ac13v<ac23v && ac23v<ac33v && ac03v>0 && ac03v<ac13v)||(ac03v<ac13v && ac13v<ac23v && ac03v<0)) {u3acv = 0; d3acv = 3;}     
   if((((ac13v<ac23v || ac23v<ac33v) && ac03v<0 && ac03v>ac13v) || (ac03v>ac13v && ac13v<ac23v && ac03v>0))
   || (((ac13v>ac23v || ac23v>ac33v) && ac03v>0 && ac03v<ac13v) || (ac03v<ac13v && ac13v>ac23v && ac03v<0)))
   {u3acv = 0; d3acv = 0;}   
      

   uitog1v = (u1x5v + u1x8v + u1x13v + u1x21v + u1x34v + u1acv) * 12.5;
   uitog2v = (u2x5v + u2x8v + u2x13v + u2x21v + u2x34v + u2acv) * 12.5;
   uitog3v = (u3x5v + u3x8v + u3x13v + u3x21v + u3x34v + u3acv) * 12.5;
 
   ditog1v = (d1x5v + d1x8v + d1x13v + d1x21v + d1x34v + d1acv) * 12.5;
   ditog2v = (d2x5v + d2x8v + d2x13v + d2x21v + d2x34v + d2acv) * 12.5;
   ditog3v = (d3x5v + d3x8v + d3x13v + d3x21v + d3x34v + d3acv) * 12.5;

   Signal=0; 
   Comment("It is not recommended to open positions. WAIT.");
   if (uitog1v>50  && uitog2v>50  && uitog3v>50)  {Signal=1; Comment("Not bad time to open a BUY position");}
   if (ditog1v>50  && ditog2v>50  && ditog3v>50)  {Signal=-1;Comment("Not bad time to open a SELL position");}
   if (uitog1v>=75 && uitog2v>=75 && uitog3v>=75) {Signal=2; Comment("A GOOD TIME to open a BUY position");}
   if (ditog1v>=75 && ditog2v>=75 && ditog3v>=75) {Signal=-2;Comment("A GOOD TIME to open a SELL position");}
      

   
   return(true);
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool SignalOpenBuy(){
   return(Signal>OpenLevel);
}

//+------------------------------------------------------------------+
//|   Function for determining sell signals                          |
//+------------------------------------------------------------------+
bool SignalOpenSell(){
   return(Signal<-OpenLevel);
}

//+------------------------------------------------------------------+
//|   Function for determining buy close signals                     |
//+------------------------------------------------------------------+
bool SignalCloseBuy(){
   return(Signal<-CloseLevel);
}

//+------------------------------------------------------------------+
//|   Function for determining sell close signals                    |
//+------------------------------------------------------------------+
bool SignalCloseSell(){
   return(Signal>CloseLevel);
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
   aLots=Lots;         
   bool rv=true;   
   return(rv);
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
//|   Function to delete the global variables with gvp prefix        | 
//+------------------------------------------------------------------+
void DeleteGV(){
   if(MQL5InfoInteger(MQL5_TESTING)){
      for(int i=GlobalVariablesTotal()-1;i>=0;i--){
         if(StringFind(GlobalVariableName(i),gvp,0)==0){
            GlobalVariableDel(GlobalVariableName(i));
         }
      }
   }
}   

