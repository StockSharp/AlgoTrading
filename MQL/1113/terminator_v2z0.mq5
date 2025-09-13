//+------------------------------------------------------------------+
//|                                                 TradeChannel.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description ""
#property version   "1.00"

#property description "Expert rewritten from MQL4, the author: Alejandro Galindo and Tom Maneval, published on mql4.com by Scriptor (http://www.mql4.com/ru/users/Scriptor), link - http://codebase.mql4.com/ru/1711"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;

//+------------------------------------------------------------------+
//| Base class of the trade signals options                          |
//+------------------------------------------------------------------+
class CTradeSignals{
   protected:
      bool m_buy;
      bool m_sell;
   public:
      virtual bool Init(){
         return(false);    
      }
      virtual bool Refresh(){
         return(false);      
      }
      virtual void DeInit(){}      
      bool SigBuy(){
         return(m_buy);      
      }
      bool SigSell(){
         return(m_sell);
      }
};   

enum ESigType{
   MACD=0,
   Pivot=1,
   SupRes=2,
   i_TrendRSI=3,
   i_TrendRSISto=4,
   i_TrRSIStoMFI=5
};

enum EBBLine{
   Base=BASE_LINE,
   Upper=UPPER_BAND,
   Lower=LOWER_BAND
};




input bool                             Trade_ON                =  true;                   /*Trade_ON*/               // Can open a position   
input double                           Lots                    =  0.1;                    /*Lots*/                   // Lot, MaximumRisk parameter works with zero value.
input double                           MaximumRisk             =  0.05;                   /*MaximumRisk*/            // Risk (valid for Lots=0).
input int                              StopLoss                =  2500;                   /*StopLoss*/               // Stoploss in points, 0 - without stoploss.
input int                              TakeProfit              =  500;                    /*TakeProfit*/             // Start position takeprofit in points
input int                              TakeProfit2             =  100;                    /*TakeProfit2*/            // Takeprofit when need to add in points
input int                              MaxCount                =  10;                     /*MaxCount*/               // Maximum number of openings in the same direction, -1 - unlimited
input int                              DoubleCount             =  5;                      /*DoubleCount*/            // Number of transactions with lot multiplication coefficient as 2, other open with 1.5
input int                              Pips                    =  500;                    /*Pips*/                   // Level of adding in points
input int                              Trailing                =  0;                      /*Trailing*/               // Trailing level, if value is 0 - then trailing off.
input int                              Shift                   =  1;                      /*Shift*/                  // Bar on which indicators are checked: 0 - shaped bar, 1 - the first shaped bar
input bool                             ReverseCondition        =  false;                  /*ReverseCondition*/       // Change the buy and sell signals
input ESigType                         OPEN_POS_BASED_ON       =  MACD;                   /*OPEN_POS_BASED_ON*/      // Type of trade signals
input int                              MACD_FastPeriod         =  14;	                  /*MACD_FastPeriod*/        // Period of fast лю MACD
input int                              MACD_SlowPeriod         =  26;	                  /*MACD_SlowPeriod*/        // Period of slow лю MACD
input ENUM_APPLIED_PRICE               MACD_Price              =  PRICE_CLOSE;	         /*MACD_Price*/             // MACD price
input int                              Pivot_DayStartHour      =  0;                      /*Pivot_DayStartHour*/     // Hour of day start
input int                              Pivot_DayStartMinute    =  0;                      /*Pivot_DayStartMinute*/   // Minute of day start
input bool                             Pivot_AttachSundToMond  =  true;                   /*Pivot_AttachSundToMond*/ // Attach the sunday bars to monday
input int                              SupRes_iPeriod          =  70;                     /*SupRes_iPeriod*/         // Support_and_Resistance indicator period
input ENUM_APPLIED_PRICE               iT_Price                =  PRICE_CLOSE;	         /*iT_Price*/               // type of price at which the calculated amount of price and Bollinger Bands
input int                              iT_BBPeriod             =  10;	                  /*iT_BBPeriod*/            // BB period
input int                              iT_BBShift              =  0;	                     /*iT_BBShift*/             // BB shift
input double                           iT_BBDeviation          =  2;	                     /*iT_BBDeviation*/         // BB deviation
input ENUM_APPLIED_PRICE               iT_BBPrice              =  PRICE_CLOSE;	         /*iT_BBPrice*/             // BB price
input EBBLine                          iT_BBLine               =  BASE_LINE;              /*iT_BBLine*/              // Used line of the Bollinger Bands
input int                              iT_BullsBearsPeriod     =  6;	                     /*iT_BullsBearsPeriod*/    // Bulls Bears Power period
input int                              RSI_Period              =  14;	                  /*RSI_Period*/             // RSI period
input ENUM_APPLIED_PRICE               RSI_Price               =  PRICE_CLOSE;	         /*RSI_Price*/              // RSI price
input int                              St_KPeriod              =  8;	                     /*St_KPeriod*/             // K stochastic period
input int                              St_DPeriod              =  3; 	                  /*St_DPeriod*/             // D stochastic period
input int                              St_SPeriod              =  4;	                     /*St_SPeriod*/             // S stochastic period
input ENUM_MA_METHOD                   St_Method               =  MODE_SMA;	            /*St_Method*/              // Stochastic method
input ENUM_STO_PRICE                   St_Price                =  STO_LOWHIGH;	         /*St_Price*/               // Stochastic price
input int                              St_UpperLevel           =  80; 	                  /*St_UpperLevel*/          // Top level of stochastic
input int                              St_LowerLevel           =  20;	                  /*St_LowerLevel*/          // Lower level of stochastic
input int                              MFI_Period              =  5; 	                  /*MFI_Period*/             // MFI period
input ENUM_APPLIED_VOLUME              MFI_Volume              =  VOLUME_TICK;	         /*MFI_Volume*/             // MFI volume


int Handle=INVALID_HANDLE;
datetime ctm[1];
datetime LastTime;
double lot,slv,msl,tpv,mtp;

int   MACD_SignalPeriod       =  1;	   
bool  Pivot_PivotsBufers      =  true; 
bool  Pivot_MidpivotsBuffers  =  false;
bool  Pivot_CamarillaBuffers  =  false;
bool  Pivot_PivotsLines       =  false;
bool  Pivot_MidpivotsLines    =  false;
bool  Pivot_CamarillaLines    =  false;
color Pivot_ClrPivot          =  clrOrange;
color Pivot_ClrS              =  clrRed;
color Pivot_ClrR              =  clrDeepSkyBlue;
color Pivot_ClrM              =  clrBlue;
color Pivot_ClrCamarilla      =  clrYellow;
color Pivot_ClrTxt            =  clrWhite;

CTradeSignals * TradeSignals;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   switch(OPEN_POS_BASED_ON){
      case MACD:
         TradeSignals=new CSigMACD();
      break;
      case Pivot:
         TradeSignals=new CSigPivot();
      break;
      case SupRes:
         TradeSignals=new CSigSupRes();
      break;
      case i_TrendRSI:
         TradeSignals=new CSigi_TrendRSI();     
      break;
      case i_TrendRSISto:
         TradeSignals=new CSigi_TrendRSISto();
      break;
      case i_TrRSIStoMFI:
         TradeSignals=new CSigi_TrRSIStoMFI();     
      break;
   }

   if(!TradeSignals.Init()){
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
   TradeSignals.DeInit();
   delete(TradeSignals);
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
            if(!TradeSignals.Refresh()){
               return;
            }   
   
         bool BuySig;
         bool SellSig;
    
            if(ReverseCondition){
               BuySig=TradeSignals.SigSell();
               SellSig=TradeSignals.SigBuy();      
            }
            else{
               BuySig=TradeSignals.SigBuy();
               SellSig=TradeSignals.SigSell();      
            }
   
         // Open
            if(!Pos.Select(_Symbol)){
               if(BuySig && !SellSig && Trade_ON){ 
                  if(!Sym.RefreshRates())return;         
                  if(!SolveLots(lot))return;
                  slv=0;
                     if(MaxCount==1){
                        slv=SolveBuySL(StopLoss);
                     }
                  tpv=SolveBuyTP(TakeProfit);
                     if(CheckBuySL(slv) && CheckBuyTP(tpv)){
                        Trade.SetDeviationInPoints(Sym.Spread()*3);
                        if(!Trade.Buy(lot,_Symbol,0,slv,tpv,"-")){
                           return;
                        }
                     }
                     else{
                        Print("Buy position does not open, stoploss or takeprofit is near");
                     }         
               }
               // Sell
               if(SellSig && !BuySig && Trade_ON){
                  if(!Sym.RefreshRates())return;         
                  if(!SolveLots(lot))return;
                  slv=0;
                     if(MaxCount==1){               
                        slv=SolveSellSL(StopLoss);
                     }
                  tpv=SolveSellTP(TakeProfit);
                     if(CheckSellSL(slv) && CheckSellTP(tpv)){
                        Trade.SetDeviationInPoints(Sym.Spread()*3);
                        if(!Trade.Sell(lot,_Symbol,0,slv,tpv,"-")){
                           return;
                        }
                     }
                     else{
                        Print("Sell position does not open, stoploss or takeprofit is near");
                     }          
               }
            }    
            else{
               if(!Sym.RefreshRates())return; 
               double Price,StartLots;
               int Index;
               int k=DoubleCount;
                  switch(Pos.PositionType()){
                     case POSITION_TYPE_BUY:
                        if(BuySig){
                           if(!FindLastInPrice(DEAL_TYPE_BUY,Price,Index)){
                              return;
                           }
                           if((Index<MaxCount || MaxCount==-1) && Price-Sym.Ask()>=Sym.Point()*Pips){
                              if(!FindStartLots(DEAL_TYPE_BUY,StartLots))return;
                              lot=StartLots*MathPow(2,MathMin(Index,k-1));
                              if(Index>k-1)lot=lot*MathPow(1.5,Index-k+1);
                              lot=fLotsNormalize(lot);
                              
                              slv=0;
                                 if(StopLoss!=0){
                                    if(Index+1==MaxCount){
                                       slv=SolveBuySL(StopLoss);
                                       slv=MathMin(slv,Sym.NormalizePrice(BuyMSL()-Sym.Point()));     
                                    }
                                 }   
                              tpv=0;
                                 if(TakeProfit2!=0){
                                    tpv=(Pos.PriceOpen()*Pos.Volume()+Sym.Ask()*lot)/(Pos.Volume()+lot)+Sym.Point()*TakeProfit2;
                                    tpv=Sym.NormalizePrice(tpv);
                                    tpv=MathMax(tpv,Sym.NormalizePrice(BuyMTP()+Sym.Point()));
                                 }
                              Trade.SetDeviationInPoints(Sym.Spread()*3);                     
                              Trade.Buy(lot,_Symbol,0,slv,tpv,(Index+1)+"=");
                           }
                        }
                     break;
                     case POSITION_TYPE_SELL:
                        if(SellSig){                  
                           if(!FindLastInPrice(DEAL_TYPE_SELL,Price,Index)){
                              return;
                           }
                           if((Index<MaxCount || MaxCount==-1) && Sym.Bid()-Price>=Sym.Point()*Pips){
                              if(!FindStartLots(DEAL_TYPE_SELL,StartLots))return;
                              lot=StartLots*MathPow(2,MathMin(Index,k-1));
                              if(Index>k-1)lot=lot*MathPow(1.5,Index-k+1);
                              lot=fLotsNormalize(lot);
                              slv=0;
                                 if(StopLoss!=0){
                                       if(Index+1==MaxCount){
                                          slv=SolveSellSL(StopLoss);
                                          slv=MathMax(slv,Sym.NormalizePrice(SellMSL()+Sym.Point())); 
                                       }
                                 }
                              tpv=0;
                                 if(TakeProfit2!=0){
                                    tpv=(Pos.PriceOpen()*Pos.Volume()+Sym.Ask()*lot)/(Pos.Volume()+lot)-Sym.Point()*TakeProfit2;
                                    tpv=Sym.NormalizePrice(tpv); 
                                    tpv=MathMin(tpv,Sym.NormalizePrice(SellMTP()-Sym.Point()));
                                 }
                              Trade.SetDeviationInPoints(Sym.Spread()*3);   
                              Trade.Sell(lot,_Symbol,0,slv,tpv,(Index+1)+"=");
                           }
                        }
                     break;
                  }
            }        
         LastTime=ctm[0];
      }
   
   fSimpleTrailing();

}

bool FindStartLots(long aType,double & aLots){
      if(!SolveLots(aLots)){
         return(false);
      }         
      if(!HistorySelect(0,TimeCurrent())){
         return(false);
      }
      for(int i=HistoryDealsTotal()-1;i>=0;i--){
         if(Deal.SelectByIndex(i)){
            if(Deal.Symbol()==_Symbol){
               if(Deal.DealType()==aType){
                  if(Deal.Entry()==DEAL_ENTRY_IN){
                     int p=StringFind(Deal.Comment(),"=",0);
                        if(p==-1){
                           aLots=Deal.Volume();
                           return(true);
                        }
                  }
               }
            }
         }
         else{
            return(false);
         }
      }
   return(true); 
}

bool FindLastInPrice(long aType,double & aPrice,int & aIndex){
   aPrice=0;
   aIndex=1;
      if(!HistorySelect(0,TimeCurrent())){
         return(false);
      }
      for(int i=HistoryDealsTotal()-1;i>=0;i--){
         if(Deal.SelectByIndex(i)){
            if(Deal.Symbol()==_Symbol){
               if(Deal.DealType()==aType){
                  if(Deal.Entry()==DEAL_ENTRY_IN){
                     int p=StringFind(Deal.Comment(),"=",0);
                        if(p==-1){
                           aIndex=1;
                        }
                        else{
                           aIndex=StringToInteger(StringSubstr(Deal.Comment(),0,p));
                        }
                     aPrice=Deal.Price();
                     return(true);
                  }
               }
            }
         }
         else{
            return(false);
         }
      }
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
      if(Lots==0){
         aLots=fLotsNormalize(AccountInfoDouble(ACCOUNT_FREEMARGIN)*MaximumRisk/1000.0);        
      }
      else{
         aLots=Lots;         
      }
   return(true);
}

//+------------------------------------------------------------------+
//|   Lot normalization function                                     |
//+------------------------------------------------------------------+
double fLotsNormalize(double aLots){
   aLots-=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   aLots/=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP);
   aLots=MathRound(aLots);
   aLots*=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP);
   aLots+=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   aLots=NormalizeDouble(aLots,2);
   aLots=MathMin(aLots,SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX));
   aLots=MathMax(aLots,SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN));   
   return(aLots);
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

class CSigMACD:public CTradeSignals{
   protected:
      int m_Handle;
      double m_val[1];
      double m_val1[1];      
   public:
      bool Init(){
         m_Handle=iMACD(NULL,PERIOD_CURRENT,MACD_FastPeriod,MACD_SlowPeriod,MACD_SignalPeriod,MACD_Price);
         return(m_Handle!=INVALID_HANDLE);
      }
      bool Refresh(){
         if(
            CopyBuffer(m_Handle,0,Shift,1,m_val)==-1 ||
            CopyBuffer(m_Handle,0,Shift+1,1,m_val1)==-1         
         )return(false);
         m_buy=(m_val[0]>m_val1[0]);
         // MACD is growing.
         m_sell=(m_val[0]<m_val1[0]);
         // MACD falls.
         return(true);
      }
      void DeInit(){
         if(m_Handle!=INVALID_HANDLE)IndicatorRelease(m_Handle);
      }         
};

class CSigPivot:public CTradeSignals{
   protected:
      int m_Handle;
      double m_val[1];
      double m_cl[1];
   public:
      bool Init(){
         m_Handle=iCustom(_Symbol,PERIOD_CURRENT,"Pivot Lines TimeZone",Pivot_DayStartHour,Pivot_DayStartMinute,Pivot_PivotsBufers,Pivot_MidpivotsBuffers,Pivot_CamarillaBuffers,Pivot_PivotsLines,Pivot_MidpivotsLines,Pivot_CamarillaLines,Pivot_ClrPivot,Pivot_ClrS,Pivot_ClrR,Pivot_ClrM,Pivot_ClrCamarilla,Pivot_ClrTxt,Pivot_AttachSundToMond);
         return(m_Handle!=INVALID_HANDLE); 
      }
      bool Refresh(){
         if(   CopyBuffer(m_Handle,0,Shift,1,m_val)==-1 ||
               CopyClose(_Symbol,PERIOD_CURRENT,Shift,1,m_cl)==-1
         )return(false);      
         m_buy=(m_cl[0]>m_val[0]);
         // The bar close price is greater than Pivot.
         m_sell=(m_cl[0]<m_val[0]);         
         // The bar close price is less than Pivot.
         return(true);      
      }
      void DeInit(){
         if(m_Handle!=INVALID_HANDLE)IndicatorRelease(m_Handle);
      }         
};

class CSigSupRes:public CTradeSignals{
   protected:
      int m_Handle;
      double m_valS[1];
      double m_valR[1];
      double m_cl[1];
      double m_valS1[1];
      double m_valR1[1];
      double m_cl1[1];      
   public:
      bool Init(){
         m_Handle=iCustom(_Symbol,PERIOD_CURRENT,"Support_and_Resistance",SupRes_iPeriod);
         return(m_Handle!=INVALID_HANDLE);   
      }
      bool Refresh(){
         if(Shift==0){
            if(   CopyBuffer(m_Handle,0,Shift,1,m_valS)==-1 ||
                  CopyBuffer(m_Handle,1,Shift,1,m_valR)==-1 ||
                  CopyClose(_Symbol,PERIOD_CURRENT,Shift,1,m_cl)==-1
            )return(false); 
            m_buy=(m_cl[0]==m_valR[0]);
            // The bar close price is equal to the resistance.
            m_sell=(m_cl[0]==m_valS[0]);
            // The bar close price is equal to the support.
         }
         else{
            if(   
                  CopyBuffer(m_Handle,0,Shift,1,m_valS)==-1 ||
                  CopyBuffer(m_Handle,1,Shift,1,m_valR)==-1 ||
                  CopyClose(_Symbol,PERIOD_CURRENT,Shift,1,m_cl)==-1 || 
                  
                  CopyBuffer(m_Handle,0,Shift+1,1,m_valS1)==-1 ||
                  CopyBuffer(m_Handle,1,Shift+1,1,m_valR1)==-1 ||
                  CopyClose(_Symbol,PERIOD_CURRENT,Shift+1,1,m_cl1)==-1
            )return(false); 
            m_buy=(m_cl[0]>m_valR[0] && m_cl1[0]<=m_valR1[0]);
            // Crossing the line of resistance up.
            m_sell=(m_cl[0]<m_valS[0] && m_cl1[0]>=m_valS1[0]);
            // Crossing the line of support down.
         }
         return(true);      
      }
      void DeInit(){
         if(m_Handle!=INVALID_HANDLE)IndicatorRelease(m_Handle);
      }        
};

class CSigi_TrendRSI:public CTradeSignals{
   protected:
      int m_iTrendHandle;
      int m_RSIHand;      
      double m_it00[1];
      double m_it10[1];
      double m_it01[1];
      double m_rsi_0[1];
      double m_rsi_1[1];
   public:
      bool Init(){
         m_iTrendHandle=iCustom(_Symbol,PERIOD_CURRENT,"i_Trend",iT_Price,iT_BBPeriod,iT_BBShift,iT_BBDeviation,iT_BBPrice,iT_BBLine,iT_BullsBearsPeriod);
         m_RSIHand=iRSI(_Symbol,PERIOD_CURRENT,RSI_Period,RSI_Price);
         return(m_iTrendHandle!=INVALID_HANDLE && m_RSIHand!=INVALID_HANDLE);    
      }
      bool Refresh(){
         if(
            CopyBuffer(m_iTrendHandle,0,Shift,1,m_it00)==-1 || 
            CopyBuffer(m_iTrendHandle,0,Shift+1,1,m_it01)==-1 ||   
            CopyBuffer(m_iTrendHandle,1,Shift,1,m_it10)==-1 ||   
            CopyBuffer(m_RSIHand,0,Shift,1,m_rsi_0)==-1 || 
            CopyBuffer(m_RSIHand,0,Shift+1,1,m_rsi_1)==-1
         )return(false);
         m_buy=   (m_it00[0]>m_it10[0]    && m_it00[0]>m_it01[0] && m_rsi_0[0]>m_rsi_1[0]);
         // The green is growing and more than red, RSI is growing.
         m_sell=  (m_it00[0]<m_it10[0]    && m_it00[0]<m_it01[0] && m_rsi_0[0]<m_rsi_1[0]);
         // The green falls and less than red, RSI falls.
         return(true);      
      }
      void DeInit(){
         if(m_iTrendHandle!=INVALID_HANDLE)IndicatorRelease(m_iTrendHandle);
         if(m_RSIHand!=INVALID_HANDLE)IndicatorRelease(m_RSIHand);
      }         
};

class CSigi_TrendRSISto:public CTradeSignals{
   protected:
      int m_iTrendHandle;
      int m_RSIHand;       
      int m_StHand;
      double m_it00[1];
      double m_it01[1];
      double m_it10[1];
      double m_st00[1];
      double m_st01[1];
      double m_st10[1];   
      double m_rsi0[1];
      double m_rsi1[1];
   public:
      bool Init(){
         m_iTrendHandle=iCustom(_Symbol,PERIOD_CURRENT,"i_Trend",iT_Price,iT_BBPeriod,iT_BBShift,iT_BBDeviation,iT_BBPrice,iT_BBLine,iT_BullsBearsPeriod);
         m_RSIHand=iRSI(_Symbol,PERIOD_CURRENT,RSI_Period,RSI_Price);      
         m_StHand=iStochastic(_Symbol,PERIOD_CURRENT,St_KPeriod,St_DPeriod,St_SPeriod,St_Method,St_Price);
         return(m_iTrendHandle!=INVALID_HANDLE && m_RSIHand!=INVALID_HANDLE && m_StHand!=INVALID_HANDLE);
      }
      bool Refresh(){
         if(
            CopyBuffer(m_iTrendHandle,0,Shift,1,m_it00)==-1 || 
            CopyBuffer(m_iTrendHandle,0,Shift+1,1,m_it01)==-1 ||   
            CopyBuffer(m_iTrendHandle,1,Shift,1,m_it10)==-1 ||        
            CopyBuffer(m_StHand,0,Shift,1,m_st00)==-1 ||
            CopyBuffer(m_StHand,0,Shift+1,1,m_st01)==-1 ||
            CopyBuffer(m_StHand,1,Shift,1,m_st10)==-1 ||
            CopyBuffer(m_RSIHand,0,Shift,1,m_rsi0)==-1 ||
            CopyBuffer(m_RSIHand,0,Shift+1,1,m_rsi1)==-1
         )return(false);            
         double m_Buy5_2=80;
         double m_Buy6_2=20;
         double m_Sell5_2=80;
         double m_Sell6_2=30;
         m_buy=(m_it00[0]>m_it10[0] && m_it00[0]>m_it01[0] && m_st00[0]>m_st10[0] && m_st00[0]>m_st01[0] && m_st00[0]<St_UpperLevel  && m_st00[0]>St_LowerLevel && m_rsi0[0]>m_rsi1[0]);
         // The green is growing and more than red, the main stochastic is growing and more than signal stochastic, located between the upper and lower levels, RSI is growing.
         m_buy=(m_it00[0]<m_it10[0] && m_it00[0]<m_it01[0] && m_st00[0]<m_st10[0] && m_st00[0]<m_st01[0] && m_st00[0]<St_UpperLevel  && m_st00[0]>St_LowerLevel && m_rsi0[0]<m_rsi1[0]);
         // The green falls and less than red, the main stochastic falls and less than signal stochastic, located between the upper and lower levels, RSI falls.
         return(true);      
      }
      void DeInit(){
         if(m_iTrendHandle!=INVALID_HANDLE)IndicatorRelease(m_iTrendHandle);
         if(m_RSIHand!=INVALID_HANDLE)IndicatorRelease(m_RSIHand);
         if(m_StHand!=INVALID_HANDLE)IndicatorRelease(m_StHand);         
      }
};

class CSigi_TrRSIStoMFI:public CTradeSignals{
   protected:
      int m_iTrendHandle;
      int m_RSIHand;       
      int m_StHand;
      int m_MFIHand;
      double m_it00[1];
      double m_it10[1];
      double m_it01[1];
      double m_st00[1];
      double m_st01[1];
      double m_st10[1];
      double m_rsi0[1];  
      double m_rsi1[1];
      double m_mfi0[1];
      double m_mfi1[1];
   public:
      bool Init(){
         m_iTrendHandle=iCustom(_Symbol,PERIOD_CURRENT,"i_Trend",iT_Price,iT_BBPeriod,iT_BBShift,iT_BBDeviation,iT_BBPrice,iT_BBLine,iT_BullsBearsPeriod);
         m_RSIHand=iRSI(_Symbol,PERIOD_CURRENT,RSI_Period,RSI_Price);      
         m_StHand=iStochastic(_Symbol,PERIOD_CURRENT,St_KPeriod,St_DPeriod,St_SPeriod,St_Method,St_Price);
         m_MFIHand=iMFI(_Symbol,PERIOD_CURRENT,MFI_Period,MFI_Volume);
         return(m_iTrendHandle!=INVALID_HANDLE && m_RSIHand!=INVALID_HANDLE && m_StHand!=INVALID_HANDLE && m_MFIHand!=INVALID_HANDLE);      
      }
      bool Refresh(){
         if(
            CopyBuffer(m_iTrendHandle,0,Shift,1,m_it00)==-1 || 
            CopyBuffer(m_iTrendHandle,0,Shift+1,1,m_it01)==-1 ||   
            CopyBuffer(m_iTrendHandle,1,Shift,1,m_it10)==-1 ||        
            CopyBuffer(m_StHand,0,Shift,1,m_st00)==-1 ||
            CopyBuffer(m_StHand,0,Shift+1,1,m_st01)==-1 ||
            CopyBuffer(m_StHand,1,Shift,1,m_st10)==-1 ||
            CopyBuffer(m_RSIHand,0,Shift,1,m_rsi0)==-1 ||
            CopyBuffer(m_RSIHand,0,Shift+1,1,m_rsi1)==-1 ||
            CopyBuffer(m_MFIHand,0,Shift,1,m_mfi0)==-1 || 
            CopyBuffer(m_MFIHand,0,Shift+1,1,m_mfi1)==-1
         )return(false);   
         m_buy=(m_it00[0]>m_it10[0] && m_it00[0]>m_it01[0] && m_st00[0]>m_st10[0] && m_st00[0]>m_st01[0] && m_st00[0]<St_UpperLevel && m_st00[0]>St_LowerLevel && m_rsi0[0]>m_rsi1[0] && m_mfi0[0]>m_mfi1[0]);
         // The green is growing and more than red, the main stochastic is growing and more than signal stochastic, located between the upper and lower levels, RSI is growing, MFI is growing
         m_sell=(m_it00[0]<m_it10[0] && m_it00[0]<m_it01[0] && m_st00[0]<m_st10[0] && m_st00[0]<m_st01[0] && m_st00[0]<St_UpperLevel && m_st00[0]>St_LowerLevel && m_rsi0[0]<m_rsi1[0] && m_mfi0[0]<m_mfi1[0]);
         // The green falls and less than red, the main stochastic falls and less than signal stochastic, located between the upper and lower levels, RSI falls, MFI falls
         return(true);      
      }
      void DeInit(){
         if(m_iTrendHandle!=INVALID_HANDLE)IndicatorRelease(m_iTrendHandle);
         if(m_RSIHand!=INVALID_HANDLE)IndicatorRelease(m_RSIHand);
         if(m_StHand!=INVALID_HANDLE)IndicatorRelease(m_StHand);  
         if(m_MFIHand!=INVALID_HANDLE)IndicatorRelease(m_MFIHand); 
      }
};