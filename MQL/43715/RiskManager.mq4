//+------------------------------------------------------------------+
//|                                                  RiskManager.mq4 |
//|                                       Copyright 2023, Noah Kurtz |
//|                                              noahkurtz.epizy.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, Noah Kurtz"
#property link      "noahkurtz.epizy.com"
#property version   "1.00"
#property strict
input bool Long = false;
input bool Short = false;
input double MaxSize = 5;
input double Layers = 100;
input int LevelLength = 75;
input double Level = 100;
input double SLMultiple = 200;
input double TPMultiple = 5;
input int ClosePL = 50;
input double Capital = 0;
input double RiskLimit = 400;
input double ProfitTarget = 300;
input bool MultiPairTrading = true;
input double HedgeLevel = 70;
input double HedgeRatio = 0.75;
input bool CloseAtBreakEven = false;
input bool HardClose = false;
bool Active;
double InitialCapital;
double EquityDDLimit;
double CloseProfit;
double Risk;
double Positions;
double Size;
double Sizeon;
double BECapital;
double CloseEquity;
double Health;
double PositionSize;
string symbol;
bool Recorded;
bool Hedge;
bool Stop;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(Capital == 0){
   InitialCapital = AccountBalance();
   }
   else{
   InitialCapital = Capital;
   }
   
   EquityDDLimit = InitialCapital - RiskLimit;
   CloseProfit = InitialCapital + ProfitTarget;
   symbol = Symbol();
   Recorded = false;
   Stop = false;
   Size = MaxSize/Layers;
   Risk = AccountBalance() - EquityDDLimit;
   CloseEquity = AccountBalance() + ClosePL;
   Hedge = false;
//---
   return(INIT_SUCCEEDED);
  }

void OnTick()
  {
  
 double Price = iClose(Symbol(),PERIOD_CURRENT,0);
 double Price2 = iClose(Symbol(),PERIOD_CURRENT,1);
 double ATR = iATR(Symbol(),PERIOD_CURRENT,14,0);
 double CCI = iCCI(Symbol(),PERIOD_CURRENT,LevelLength,PRICE_CLOSE,0);
 double TP = ATR*TPMultiple;
 double SL = ATR*SLMultiple;
 double CurrentRisk = AccountEquity() - EquityDDLimit;
 double Vol = iVolume(Symbol(),PERIOD_CURRENT,1);
 double AVGvol;
 double CumVol;
 
 for(int i = 1; i <= 50; i ++){
 CumVol += iVolume(Symbol(),PERIOD_CURRENT,i);
 if(i == 50){
 AVGvol = CumVol/49;
 CumVol = 0;
 }
 
 if(AVGvol > Vol){
 Active = true;
 }
 
 else{
 Active = false;
 }
 
 }
 
 Sizeon = OrdersTotal() / Layers;
 
 Health = NormalizeDouble(((CurrentRisk/Risk) * 100),1);
 PositionSize = NormalizeDouble(((OrdersTotal() / Layers) * 100),2);
 
       ObjectCreate("Health",OBJ_LABEL,0,0,0);
       ObjectSet("Health",OBJPROP_CORNER,CORNER_LEFT_UPPER);
       ObjectSet("Health",OBJPROP_XDISTANCE,900);
       ObjectSet("Health",OBJPROP_YDISTANCE,0);
       ObjectSetText("Health","System Health: " + Health + " %",14,"Arial",White);
       
       ObjectCreate("PositionSize",OBJ_LABEL,0,0,0);
       ObjectSet("PositionSize",OBJPROP_CORNER,CORNER_LEFT_UPPER);
       ObjectSet("PositionSize",OBJPROP_XDISTANCE,900);
       ObjectSet("PositionSize",OBJPROP_YDISTANCE,30);
       ObjectSetText("PositionSize","Position size : " + PositionSize + " %",14,"Arial",White);
       
       ObjectCreate("Active",OBJ_LABEL,0,0,0);
       ObjectSet("Active",OBJPROP_CORNER,CORNER_LEFT_UPPER);
       ObjectSet("Active",OBJPROP_XDISTANCE,900);
       ObjectSet("Active",OBJPROP_YDISTANCE,60);
       
       if(Stop == false){
       
       if(Hedge == true){
       ObjectSetText("Active","System Active : Hedging",14,"Arial",White);
       }
       
        if(Hedge == false){
       ObjectSetText("Active","System Active : Trading",14,"Arial",White);
       }
       
        }
        
        if(Stop == true){
       ObjectSetText("Active","System Inactive",14,"Arial",White);
        }
 if(isNewBar() == true && Hedge != true && Active == true){   
     
 if(Short == true && (Sizeon * 100) < Health && Stop == false && Price > Price2 && CCI > Level){
 OrderSend(Symbol(),OP_SELL,Size,Bid,5,Bid+SL,Bid-TP,NULL);
 }
 
 if(Long == true && (Sizeon * 100) < Health && Stop == false && Price < Price2 && CCI < -Level){
 OrderSend(Symbol(),OP_BUY,Size,Ask,5,Ask-SL,Ask+TP,NULL);
 }
 }
 
 if(Health < HedgeLevel && Hedge == false && MultiPairTrading == false){
 Hedge = true;
 Positions = OrdersTotal();
 CloseEquity = InitialCapital;
 }
 
 if(Health > HedgeLevel){
 Hedge = false;
 }
 
 if(Hedge == true && Stop == false){
 if(OrdersTotal() < Positions * (1+HedgeRatio)){
 
 if(Long == true && isNewBar()){
 
 OrderSend(Symbol(),OP_SELL,Size,Bid,5,Bid+SL,Bid-TP,NULL);
 }
 
 if(Short == true && isNewBar()){
 OrderSend(Symbol(),OP_BUY,Size,Ask,5,Ask-SL,Ask+TP,NULL);
 }
 
 }
 }
 
 
if(AccountEquity() <= EquityDDLimit){
     Stop = true;
     for(int i = OrdersTotal()-1; i>= 0; i--){
     OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
     if(Symbol() == symbol){
     if(OrderType() == ORDER_TYPE_SELL){
     OrderClose(OrderTicket(),OrderLots(),Ask,0,NULL);
     }
     }
     } 
      
     for(int i=OrdersTotal()-1; i>= 0; i--){
     OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
     if(Symbol() == symbol){
     if(OrderType() == ORDER_TYPE_BUY){
     OrderClose(OrderTicket(),OrderLots(),Bid,0,NULL);
     }
     }
     }   
  }
  
  if(CloseAtBreakEven == true && Recorded == false){
  BECapital = AccountBalance() - ClosePL;
  Recorded = true;
  }
  
  if(CloseAtBreakEven == true && AccountEquity() >= BECapital){
  Stop = true;
  for(int i = OrdersTotal()-1; i>= 0; i--){
     OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
     if(Symbol() == symbol){
     if(OrderType() == ORDER_TYPE_SELL){
     OrderClose(OrderTicket(),OrderLots(),Ask,0,NULL);
     }
     }
     } 
      
     for(int i=OrdersTotal()-1; i>= 0; i--){
     OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
     if(Symbol() == symbol){
     if(OrderType() == ORDER_TYPE_BUY){
     OrderClose(OrderTicket(),OrderLots(),Bid,0,NULL);
     }
     }
     }   
 }
 
  if(ProfitTarget != 0 && AccountEquity() >= CloseProfit){
  Stop = true;
  for(int i = OrdersTotal()-1; i>= 0; i--){
     OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
     if(Symbol() == symbol){
     if(OrderType() == ORDER_TYPE_SELL){
     OrderClose(OrderTicket(),OrderLots(),Ask,0,NULL);
     }
     }
     } 
      
     for(int i=OrdersTotal()-1; i>= 0; i--){
     OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
     if(Symbol() == symbol){
     if(OrderType() == ORDER_TYPE_BUY){
     OrderClose(OrderTicket(),OrderLots(),Bid,0,NULL);
     }
     }
     }   
 }
 
 if(AccountEquity() >= CloseEquity){
  for(int i = OrdersTotal()-1; i>= 0; i--){
     OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
     if(Symbol() == symbol){
     if(OrderType() == ORDER_TYPE_SELL){
     OrderClose(OrderTicket(),OrderLots(),Ask,0,NULL);
     }
     }
     } 
      
     for(int i=OrdersTotal()-1; i>= 0; i--){
     OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
     if(Symbol() == symbol){
     if(OrderType() == ORDER_TYPE_BUY){
     OrderClose(OrderTicket(),OrderLots(),Bid,0,NULL);
     }
     }
     }
     Stop = true;
     if(OrdersTotal()== 0){
  CloseEquity = AccountEquity() + ClosePL;
  Hedge = false;
  Stop = false;
  }
 }
  
  if(HardClose == true){
   Stop = true;
  for(int i = OrdersTotal()-1; i>= 0; i--){
     OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
     if(Symbol() == symbol){
     if(OrderType() == ORDER_TYPE_SELL){
     OrderClose(OrderTicket(),OrderLots(),Ask,0,NULL);
     }
     }
     } 
      
     for(int i=OrdersTotal()-1; i>= 0; i--){
     OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
     if(Symbol() == symbol){
     if(OrderType() == ORDER_TYPE_BUY){
     OrderClose(OrderTicket(),OrderLots(),Bid,0,NULL);
     }
     }
     }   
 }
  }
  
  
  bool isNewBar(){

   datetime CurrentBarTime = iTime(Symbol(),Period(),0);
   static datetime prevBarTime = CurrentBarTime;
   
   if(CurrentBarTime>prevBarTime){
   prevBarTime = CurrentBarTime;
   return(true);
   }
   
   return(false);
   
   
   }
  
//+------------------------------------------------------------------+
