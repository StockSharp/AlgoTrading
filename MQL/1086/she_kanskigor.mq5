//+------------------------------------------------------------------+
//|                                                SHE_kanskigor.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "Rewritten from MQL4. Link to the original publication - http://codebase.mql4.com/ru/376, author: kanskigor (http://www.mql4.com/ru/users/kanskigor)"
#property version   "1.00"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CSymbolInfo Sym;
CPositionInfo Pos;

//--- input parameters
input double         Lots              =  0.1;  /*Lots*/             // Position volume
input int            Profit            =  350;  /*Profit*/           // Take Profit in points. 0 - no Take Profit
input int            Stop              =  550;  /*Stop*/             // Stop Loss in points. 0 - no Stop Loss
input int            Slippage          =  50;   /*Slippage*/         // Permissible slippage
input string         Symb              =  "*";  /*Symb*/             // Trade symbol. If the value is *, then the chart symbol is used
input int            StartTimeHour     =  0;    /*StartTimeHour*/    // The hour when the position opens
input int            StartTimeMinute   =  5;    /*StartTimeMinute*/  // The minute when the position opens
                                                
string StartTime;
string SMB;
bool trade;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   StartTime=IntegerToString(StartTimeHour)+":"+IntegerToString(StartTimeMinute);

   SMB=Symb;
   StringTrimLeft(SMB);
   StringTrimRight(SMB);
   if(SMB=="*")SMB=Symbol();
   
   if(!Sym.Name(SMB)){
      Alert("CSymbolInfo initialization error, please try again");    
      return(-1);
   }
   
   Trade.SetDeviationInPoints(Slippage);   

   Print("Initialization of the Expert Advisor complete");   
   
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){

   datetime TimeStart=StringToTime(StartTime);
   
   if(TimeCurrent()<TimeStart || TimeCurrent()>TimeStart+300){
      trade=false; 
      return; 
   }   
   if(trade){
      return;   
   }
   
   if(Pos.Select(SMB)){
      trade=true;
      return;
   }
   
   int b=0;
   double Cl1[1],Op1[1];
   
   if(
      CopyClose(SMB,PERIOD_D1,1,1,Cl1)==-1 ||
      CopyOpen(SMB,PERIOD_D1,1,1,Op1)==-1
   ){
      return;
   }      
   
   if(Op1[0]>Cl1[0]){
      b=1;
   }
   else if(Op1[0]<Cl1[0]){
      b=-1;   
   }     
   else{
      trade=true;
      return;
   } 
   
   if(b==1){
      if(!Sym.RefreshRates()){
         return;    
      }         
      double stoplevel=0;
      double profitlevel=0;      
      if(Stop!=0){
         stoplevel=Sym.NormalizePrice(Sym.Ask()-Sym.Point()*Stop);
      }         
      if(Profit!=0){
         profitlevel=Sym.NormalizePrice(Sym.Ask()+Sym.Point()*Profit);
      }         
      if(Trade.Buy(Lots,SMB,0,stoplevel,profitlevel)){
         trade=true;
      }
   }
   if(b==-1){
      if(!Sym.RefreshRates()){
         return;    
      }       
      double stoplevel=0;
      double profitlevel=0;      
      if(Stop!=0){
         stoplevel=Sym.NormalizePrice(Sym.Bid()+Sym.Point()*Stop);
      }         
      if(Profit!=0){
         profitlevel=Sym.NormalizePrice(Sym.Bid()-Sym.Point()*Profit);
      }     
      if(Trade.Sell(Lots,SMB,0,stoplevel,profitlevel)){
         trade=true;
      }         
   }         
   
}
//+------------------------------------------------------------------+
