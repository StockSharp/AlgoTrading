//+------------------------------------------------------------------+
//|                                                   Perceptron.mq5 |
//|                        Copyright 2010, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, Rafael Maia de Amorim."
#property copyright "rdamorim@click21.com.br"
#property link      "http://www.mql5.com"
#property version   "1.00"

#include <Trade\Trade.mqh>
MqlTradeRequest trReq;
MqlTradeResult trRez;

input int SL=100;       // Stop Loss
input int TP=40;        // Take Profit
input int SinMax=5;
input int SinMin=0;
input int MAGIC=999;   // MAGIC number

input double SinPlus=0.03;
input double SinMinus=0.03;

int   LastTradeType=0;
int sl;
int tp;
double Balance=0;
double LastBalance=0;

//ma signal (IND1)
input int         ma1 = 5;
input int         ma2 = 9;
int               h_ma1=0;
double            ma1_buffer[];
int               h_ma2=0;
double            ma2_buffer[];
//end of ma signal

//RSI signal (IND2)
input int         RSI=14;
int               h_rsi=0;
double            rsi_buffer[];
//end RSI Signal

//CCI signal (IND3)
input int         CCI=14;
int               h_cci=0;
double            cci_buffer[];
//end CCI Signal

//ma signal (IND4)
input int         ma_a=5;
int               h_ma_a=0;
double            ma1_buffer_a[];
//end of ma signal

//IAO Signal
input int         IAO=20;
int               h_ao=0;
double            ao_buffer[];
//end IAO Signal

//start of Height Indicators
double            NNS1IND2 = 1;
double            NNS1IND3 = 1;
double            NNS1IND4 = 1;
double            NNS1IND5 = 1;

double            NNS2IND1 = 1;
double            NNS2IND3 = 1;
double            NNS2IND4 = 1;
double            NNS2IND5 = 1;

double            NNS3IND1 = 1;
double            NNS3IND2 = 1;
double            NNS3IND4 = 1;
double            NNS3IND5 = 1;

double            NNS4IND1 = 1;
double            NNS4IND2 = 1;
double            NNS4IND3 = 1;
double            NNS4IND5 = 1;

double            NNS5IND1 = 1;
double            NNS5IND2 = 1;
double            NNS5IND3 = 1;
double            NNS5IND4 = 1;

//end of Height Indicators

//start of Height NeuralNetworks
double            NNS1 = 1;
double            NNS2 = 1;
double            NNS3 = 1;
double            NNS4 = 1;
double            NNS5 = 1;
//end of Height NeuralNetworks

//last start of Height Indicators
int               LAST1IND2 = 0;
int               LAST1IND3 = 0;
int               LAST1IND4 = 0;
int               LAST1IND5 = 0;

int               LAST2IND1 = 0;
int               LAST2IND3 = 0;
int               LAST2IND4 = 0;
int               LAST2IND5 = 0;

int               LAST3IND1 = 0;
int               LAST3IND2 = 0;
int               LAST3IND4 = 0;
int               LAST3IND5 = 0;

int               LAST4IND1 = 0;
int               LAST4IND2 = 0;
int               LAST4IND3 = 0;
int               LAST4IND5 = 0;

int               LAST5IND1 = 0;
int               LAST5IND2 = 0;
int               LAST5IND3 = 0;
int               LAST5IND4 = 0;


//end last of Height Indicators

//last start of Height NeuralNetworks
double            LASTNNS1 = 1;
double            LASTNNS2 = 1;
double            LASTNNS3 = 1;
double            LASTNNS4 = 1;
double            LASTNNS5 = 1;
//end last of Height NeuralNetworks

//last profit
double            LASTPROFIT=0;
int               LASTTYPEORDER=0;
int setLast=0;
double StartBrainReturn=0;
double NNReturn=0;
double brainReturn=0;
int IND1_V = 0;
int IND2_V = 0;
int IND3_V = 0;
int IND4_V = 0;
int IND5_V = 0;
double NN1_V = 0;
double NN2_V = 0;
double NN3_V = 0;
double NN4_V = 0;
double NN5_V = 0;
string Valor = "";
double profit= 0;
//ned of last profit
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   ZeroMemory(trReq);
//Set default vaules for all new order requests
   trReq.action=TRADE_ACTION_DEAL;
   trReq.magic=MAGIC;
   trReq.symbol=Symbol();                 // Trade symbol
   trReq.volume=0.1;                      // Requested volume for a deal in lots
   trReq.deviation=1;                     // Maximal possible deviation from the requested price
   trReq.type_filling=ORDER_FILLING_FOK;  // Order execution type
   trReq.type_time=ORDER_TIME_GTC;        // Order execution time
   trReq.comment="Ultimate Neural";

   h_ma1=iMA(Symbol(),Period(),ma1,0,MODE_SMA,PRICE_CLOSE);
   h_ma2=iMA(Symbol(),Period(),ma2,0,MODE_SMA,PRICE_CLOSE);
   h_rsi=iRSI(Symbol(),Period(),RSI,PRICE_CLOSE);
   h_cci=iCCI(Symbol(),Period(),CCI,PRICE_TYPICAL);
   h_ma_a=iMA(Symbol(),Period(),ma_a,0,MODE_SMA,PRICE_CLOSE);
   h_ao=iAO(Symbol(),Period());
//input parameters are ReadOnly
   tp=TP;
   sl=SL;
//end
//Suppoprt for acount with 5 decimals
   if(_Digits==5)
     {
      sl*=10;
      tp*=10;
     }
//end
   LastBalance=AccountInfoDouble(ACCOUNT_BALANCE);
   return(0);
//---
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
void OnTick()
  {

   MqlTick tick; //variable for tick info
   if(!SymbolInfoTick(Symbol(),tick))
     {
      Print("Failed to get Symbol info!");
      return;
     }
   IND1_V = 0;
   IND2_V = 0;
   IND3_V = 0;
   IND4_V = 0;
   IND5_V = 0;
   IND1_V = IND1();
   IND2_V = IND2();
   IND3_V = IND3();
   IND4_V = IND4();
   IND5_V = IND5();
   int orders=OrdersTotal();

   NN1_V = 0;
   NN2_V = 0;
   NN3_V = 0;
   NN4_V = 0;
   NN5_V = 0;
   NN1_V = NN1(IND2_V,IND3_V,IND4_V,IND5_V);
   NN2_V = NN2(IND1_V,IND3_V,IND4_V,IND5_V);
   NN3_V = NN3(IND1_V,IND2_V,IND4_V,IND5_V);
   NN4_V = NN4(IND1_V,IND2_V,IND3_V,IND5_V);
   NN5_V = NN5(IND1_V,IND2_V,IND3_V,IND4_V);


   brainReturn=StartBrain(NN1_V,NN2_V,NN3_V,NN4_V,NN5_V);

   if(brainReturn>0 && LastTradeType!=2 && !PositionSelect(_Symbol))
     {
      printf(Valor);
      SetLastIndNN(IND1_V,IND2_V,IND3_V,IND4_V,IND5_V,NN1_V,NN2_V,NN3_V,NN4_V,NN5_V);
      setLast=1;
      trReq.price=tick.ask;                   // SymbolInfoDouble(NULL,SYMBOL_ASK);
      trReq.sl=tick.ask-_Point*sl;            // Stop Loss level of the order
      trReq.tp=tick.ask+_Point*tp;            // Take Profit level of the order
      trReq.type=ORDER_TYPE_BUY;              // Order type
      LastTradeType=2;
      OrderSend(trReq,trRez);
     }
   else if(brainReturn<0 && LastTradeType!=1 && !PositionSelect(_Symbol))
     {
      printf(Valor);
      SetLastIndNN(IND1_V,IND2_V,IND3_V,IND4_V,IND5_V,NN1_V,NN2_V,NN3_V,NN4_V,NN5_V);
      setLast=1;
      trReq.price=tick.bid;
      trReq.sl=tick.bid+_Point*sl;            // Stop Loss level of the order
      trReq.tp=tick.bid-_Point*tp;            // Take Profit level of the order
      trReq.type=ORDER_TYPE_SELL;             // Order type
      LastTradeType=1;
      OrderSend(trReq,trRez);

      Valor="";
     }
  }
//+------------------------------------------------------------------+
//| Call PERCEPTRON Neural Networks                                  |
//+------------------------------------------------------------------+
double StartBrain(double NN1_V,double NN2_V,double NN3_V,double NN4_V,double NN5_V)
  {
   StartBrainReturn = 0;
   StartBrainReturn = (NN1_V * NNS1) + (NN2_V * NNS2) + (NN3_V * NNS3) + (NN4_V * NNS4) + (NN5_V * NNS5);
   return(StartBrainReturn);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double NN1(int IND2_V,int IND3_V,int IND4_V,int IND5_V)
  {
   NNReturn = 0;
   NNReturn = (IND2_V * NNS1IND2) + (IND3_V * NNS1IND3) + (IND4_V * NNS1IND4) + (IND5_V * NNS1IND5);
   return(NNReturn);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double NN2(int IND1_V,int IND3_V,int IND4_V,int IND5_V)
  {
   NNReturn = 0;
   NNReturn = (IND1_V * NNS2IND1) + (IND3_V * NNS2IND3) + (IND4_V * NNS2IND4) + (IND5_V * NNS2IND5);
   return(NNReturn);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double NN3(int IND1_V,int IND2_V,int IND4_V,int IND5_V)
  {
   NNReturn = 0;
   NNReturn = (IND1_V * NNS3IND1) + (IND2_V * NNS3IND2) + (IND4_V * NNS3IND4) + (IND5_V * NNS3IND5);
   return(NNReturn);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double NN4(int IND1_V,int IND2_V,int IND3_V,int IND5_V)
  {
   NNReturn = 0;
   NNReturn = (IND1_V * NNS4IND1) + (IND2_V * NNS4IND2) + (IND3_V * NNS4IND3) + (IND5_V * NNS4IND5);
   return(NNReturn);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double NN5(int IND1_V,int IND2_V,int IND3_V,int IND4_V)
  {
   NNReturn = 0;
   NNReturn = (IND1_V * NNS5IND1) + (IND2_V * NNS5IND2) + (IND3_V * NNS5IND3) + (IND4_V * NNS5IND4);
   return(NNReturn);
  }
//+------------------------------------------------------------------+
//| Trade Signals                                                    |
//+------------------------------------------------------------------+
int IND1(void)//Cross Moving Average
  {
   int sig=0;
   if(CopyBuffer(h_ma1,0,0,3,ma1_buffer)<3)
      return(0);
   if(!ArraySetAsSeries(ma1_buffer,true))
      return(0);

   if(CopyBuffer(h_ma2,0,0,2,ma2_buffer)<2)
      return(0);
   if(!ArraySetAsSeries(ma1_buffer,true))
      return(0);
   if(ma1_buffer[2]<ma2_buffer[1] && ma1_buffer[1]>ma2_buffer[1])
      sig=1;
   else if(ma1_buffer[2]>ma2_buffer[1] && ma1_buffer[1]<ma2_buffer[1])
      sig=-1;
   else sig=0;
   return(sig);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int IND2(void)//RSI OB OS
  {
   int sig=0;
   if(CopyBuffer(h_rsi,0,0,3,rsi_buffer)<3)
      return(0);
   if(!ArraySetAsSeries(rsi_buffer,true))
      return(0);
   if(rsi_buffer[2]<30 && rsi_buffer[1]>30)
      sig=1;
   else if(rsi_buffer[2]>70 && rsi_buffer[1]<70)
      sig=-1;
   else sig=0;
   return(sig);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int IND3()
  {
   int sig=0;
   if(CopyBuffer(h_cci,0,0,3,cci_buffer)<3)
      return(0);
   if(!ArraySetAsSeries(cci_buffer,true))
      return(0);
//--- check the condition and set a value for sig
   if(cci_buffer[2]<-100 && cci_buffer[1]>-100)
      sig=1;
   else if(cci_buffer[2]>100 && cci_buffer[1]<100)
      sig=-1;
   else sig=0;

//--- return the trade signal
   return(sig);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int IND4(void)
  {
   int sig=0;
   if(CopyBuffer(h_ma1,0,0,3,ma1_buffer_a)<3)
      return(0);
   if(!ArraySetAsSeries(ma1_buffer_a,true))
      return(0);
   if(ma1_buffer_a[1]>ma1_buffer_a[2])
      sig=1;
   else if(ma1_buffer_a[1]<ma1_buffer_a[2])
      sig=-1;
   else sig=0;

//--- return the trade signal
   return(sig);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int IND5()
  {
   int sig=0;

   if(CopyBuffer(h_ao,1,0,IAO,ao_buffer)<20)
      return(0);
   if(!ArraySetAsSeries(ao_buffer,true))
      return(0);

//--- check the condition and set a value for sig
   if(ao_buffer[1]==0)
      sig=1;
   else if(ao_buffer[1]==1)
      sig=-1;
   else sig=0;

//--- return the trade signal
   return(sig);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetLastIndNN(int IND1_V,int IND2_V,int IND3_V,int IND4_V,int IND5_V,double NN1_V,double NN2_V,double NN3_V,double NN4_V,double NN5_V)
  {
   LAST1IND2 = IND2_V;
   LAST1IND3 = IND3_V;
   LAST1IND4 = IND4_V;
   LAST1IND5 = IND5_V;

   LAST2IND1 = IND1_V;
   LAST2IND3 = IND3_V;
   LAST2IND4 = IND4_V;
   LAST2IND5 = IND5_V;

   LAST3IND1 = IND1_V;
   LAST3IND2 = IND2_V;
   LAST3IND4 = IND4_V;
   LAST3IND5 = IND5_V;

   LAST4IND1 = IND1_V;
   LAST4IND2 = IND2_V;
   LAST4IND3 = IND3_V;
   LAST4IND5 = IND5_V;

   LAST5IND1 = IND1_V;
   LAST5IND2 = IND2_V;
   LAST5IND3 = IND3_V;
   LAST5IND4 = IND4_V;

//each Neural Sin   
   LASTNNS1 = NN1_V;
   LASTNNS2 = NN2_V;
   LASTNNS3 = NN3_V;
   LASTNNS4 = NN4_V;
   LASTNNS5 = NN5_V;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SinapticsAjust(void)
  {
   ulong ticket=0;
   HistorySelect(0,TimeCurrent());
//---
   setLast=0;
   double Balance=AccountInfoDouble(ACCOUNT_BALANCE);
   profit=LastBalance-Balance;
   LastBalance=Balance;

//1 = buy, 2 == sell
   if(profit>0.0 && LastTradeType==1)
     {
      setLast=0;
      //NN1
      if(LASTNNS1>0)
        {
         if(NNS1<SinMax)
            NNS1=NNS1+SinPlus;
         if(LAST1IND2>0)
           {
            NNS1IND2=NNS1IND2+SinPlus;
           }
         else if(LAST1IND2<0)
           {
            NNS1IND2=NNS1IND2-SinMinus;
           }
         if(LAST1IND3>0)
           {
            NNS1IND3=NNS1IND3+SinPlus;
           }
         else if(LAST1IND3<0)
           {
            NNS1IND3=NNS1IND3-SinMinus;
           }
         if(LAST1IND4>0)
           {
            NNS1IND4=NNS1IND4+SinPlus;
           }
         else if(LAST1IND4<0)
           {
            NNS1IND4=NNS1IND4-SinMinus;
           }
         if(LAST1IND5>0)
           {
            NNS1IND5=NNS1IND5+SinPlus;
           }
         else if(LAST1IND5<0)
           {
            NNS1IND5=NNS1IND5-SinMinus;
           }
        }
      else if(LASTNNS1<0)
        {
         if(NNS1>SinMin)
            NNS1=NNS1-SinMinus;
         if(LAST1IND2>0)
           {
            NNS1IND2=NNS1IND2+SinPlus;
           }
         else if(LAST1IND2<0)
           {
            NNS1IND2=NNS1IND2-SinMinus;
           }
         if(LAST1IND3>0)
           {
            NNS1IND3=NNS1IND3+SinPlus;
           }
         else if(LAST1IND3<0)
           {
            NNS1IND3=NNS1IND3-SinMinus;
           }
         if(LAST1IND4>0)
           {
            NNS1IND4=NNS1IND4+SinPlus;
           }
         else if(LAST1IND4<0)
           {
            NNS1IND4=NNS1IND4-SinMinus;
           }
         if(LAST1IND5>0)
           {
            NNS1IND5=NNS1IND5+SinPlus;
           }
         else if(LAST1IND5<0)
           {
            NNS1IND5=NNS1IND5-SinMinus;
           }
        }
      //NN2
      if(LASTNNS2>0)
        {
         if(NNS2<SinMax)
            NNS2=NNS2+SinPlus;
         if(LAST2IND1>0)
           {
            NNS2IND1=NNS2IND1+SinPlus;
           }
         else if(LAST2IND1<0)
           {
            NNS2IND1=NNS2IND1-SinMinus;
           }
         if(LAST2IND3>0)
           {
            NNS2IND3=NNS2IND3+SinPlus;
           }
         else if(LAST2IND3<0)
           {
            NNS2IND3=NNS2IND3-SinMinus;
           }
         if(LAST2IND4>0)
           {
            NNS2IND4=NNS2IND4+SinPlus;
           }
         else if(LAST2IND4<0)
           {
            NNS2IND4=NNS2IND4-SinMinus;
           }
         if(LAST2IND5>0)
           {
            NNS2IND5=NNS2IND5+SinPlus;
           }
         else if(LAST2IND5<0)
           {
            NNS2IND5=NNS2IND5-SinMinus;
           }
        }
      else if(LASTNNS2<0)
        {
         if(NNS2>SinMin)
            NNS2=NNS2-SinMinus;
         if(LAST2IND1>0)
           {
            NNS2IND1=NNS2IND1+SinPlus;
           }
         else if(LAST2IND1<0)
           {
            NNS2IND1=NNS2IND1-SinMinus;
           }
         if(LAST2IND3>0)
           {
            NNS2IND3=NNS2IND3+SinPlus;
           }
         else if(LAST2IND3<0)
           {
            NNS2IND3=NNS2IND3-SinMinus;
           }
         if(LAST2IND4>0)
           {
            NNS2IND4=NNS2IND4+SinPlus;
           }
         else if(LAST2IND4<0)
           {
            NNS2IND4=NNS2IND4-SinMinus;
           }
         if(LAST2IND5>0)
           {
            NNS2IND5=NNS2IND5+SinPlus;
           }
         else if(LAST2IND5<0)
           {
            NNS2IND5=NNS2IND5-SinMinus;
           }
        }
      //NN3
      if(LASTNNS3>0)
        {
         if(NNS3<SinMax)
            NNS3=NNS3+SinPlus;
         if(LAST3IND1>0)
           {
            NNS3IND1=NNS3IND1+SinPlus;
           }
         else if(LAST3IND1<0)
           {
            NNS3IND1=NNS3IND1-SinMinus;
           }
         if(LAST3IND2>0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         else if(LAST3IND2<0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         if(LAST3IND4>0)
           {
            NNS3IND4=NNS3IND4+SinPlus;
           }
         else if(LAST3IND4<0)
           {
            NNS2IND4=NNS2IND4-SinMinus;
           }
         if(LAST3IND5>0)
           {
            NNS3IND5=NNS3IND5+SinPlus;
           }
         else if(LAST3IND5<0)
           {
            NNS3IND5=NNS3IND5-SinMinus;
           }
        }
      else if(LASTNNS3<0)
        {
         if(NNS3>SinMin)
            NNS3=NNS3-SinMinus;
         if(LAST3IND1>0)
           {
            NNS3IND1=NNS3IND1+SinPlus;
           }
         else if(LAST3IND1<0)
           {
            NNS3IND1=NNS3IND1-SinMinus;
           }
         if(LAST3IND2>0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         else if(LAST3IND2<0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         if(LAST3IND4>0)
           {
            NNS3IND4=NNS3IND4+SinPlus;
           }
         else if(LAST3IND4<0)
           {
            NNS2IND4=NNS2IND4-SinMinus;
           }
         if(LAST3IND5>0)
           {
            NNS3IND5=NNS3IND5+SinPlus;
           }
         else if(LAST3IND5<0)
           {
            NNS3IND5=NNS3IND5-SinMinus;
           }
        }
      //NN4
      if(LASTNNS4>0)
        {
         if(NNS4<SinMax)
            NNS4=NNS4+SinPlus;
         if(LAST4IND1>0)
           {
            NNS4IND1=NNS4IND1+SinPlus;
           }
         else if(LAST4IND1<0)
           {
            NNS4IND1=NNS4IND1-SinMinus;
           }
         if(LAST4IND2>0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         else if(LAST4IND2<0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         if(LAST4IND3>0)
           {
            NNS4IND3=NNS4IND3+SinPlus;
           }
         else if(LAST4IND3<0)
           {
            NNS4IND3=NNS4IND3-SinMinus;
           }
         if(LAST4IND5>0)
           {
            NNS4IND5=NNS4IND5+SinPlus;
           }
         else if(LAST4IND5<0)
           {
            NNS4IND5=NNS4IND5-SinMinus;
           }
        }
      else if(LASTNNS4<0)
        {
         if(NNS4>SinMin)
            NNS4=NNS4-SinMinus;
         if(LAST4IND1>0)
           {
            NNS4IND1=NNS4IND1+SinPlus;
           }
         else if(LAST4IND1<0)
           {
            NNS4IND1=NNS4IND1-SinMinus;
           }
         if(LAST4IND2>0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         else if(LAST4IND2<0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         if(LAST4IND3>0)
           {
            NNS4IND3=NNS4IND3+SinPlus;
           }
         else if(LAST4IND3<0)
           {
            NNS4IND3=NNS4IND3-SinMinus;
           }
         if(LAST4IND5>0)
           {
            NNS4IND5=NNS4IND5+SinPlus;
           }
         else if(LAST4IND5<0)
           {
            NNS4IND5=NNS4IND5-SinMinus;
           }
        }
      //NN5
      if(LASTNNS5>0)
        {
         if(NNS5<SinMax)
            NNS5=NNS5+SinPlus;
         if(LAST5IND1>0)
           {
            NNS5IND1=NNS5IND1+SinPlus;
           }
         else if(LAST5IND1<0)
           {
            NNS5IND1=NNS5IND1-SinMinus;
           }
         if(LAST5IND2>0)
           {
            NNS5IND2=NNS5IND2+SinPlus;
           }
         else if(LAST5IND2<0)
           {
            NNS5IND2=NNS5IND2-SinMinus;
           }
         if(LAST5IND3>0)
           {
            NNS5IND3=NNS5IND3+SinPlus;
           }
         else if(LAST5IND3<0)
           {
            NNS5IND3=NNS5IND3-SinMinus;
           }
         if(LAST5IND4>0)
           {
            NNS5IND4=NNS5IND4+SinPlus;
           }
         else if(LAST5IND4<0)
           {
            NNS5IND4=NNS5IND4-SinMinus;
           }
        }
      else if(LASTNNS5<0)
        {
         if(NNS5>SinMin)
            NNS5=NNS5-SinMinus;
         if(LAST5IND1>0)
           {
            NNS5IND1=NNS5IND1+SinPlus;
           }
         else if(LAST5IND1<0)
           {
            NNS5IND1=NNS5IND1-SinMinus;
           }
         if(LAST5IND2>0)
           {
            NNS5IND2=NNS5IND2+SinPlus;
           }
         else if(LAST5IND2<0)
           {
            NNS5IND2=NNS5IND2-SinMinus;
           }
         if(LAST5IND3>0)
           {
            NNS5IND3=NNS5IND3+SinPlus;
           }
         else if(LAST5IND3<0)
           {
            NNS5IND3=NNS5IND3-SinMinus;
           }
         if(LAST5IND4>0)
           {
            NNS5IND4=NNS5IND4+SinPlus;
           }
         else if(LAST5IND4<0)
           {
            NNS5IND4=NNS5IND4-SinMinus;
           }
        }
     }
   if(profit>0.0 && LastTradeType==2)
     {
      setLast=0;
      //NN1
      if(LASTNNS1<0)
        {
         if(NNS1<SinMax)
            NNS1=NNS1+SinPlus;
         if(LAST1IND2<0)
           {
            NNS1IND2=NNS1IND2+SinPlus;
           }
         else if(LAST1IND2>0)
           {
            NNS1IND2=NNS1IND2-SinMinus;
           }
         if(LAST1IND3<0)
           {
            NNS1IND3=NNS1IND3+SinPlus;
           }
         else if(LAST1IND3>0)
           {
            NNS1IND3=NNS1IND3-SinMinus;
           }
         if(LAST1IND4<0)
           {
            NNS1IND4=NNS1IND4+SinPlus;
           }
         else if(LAST1IND4>0)
           {
            NNS1IND4=NNS1IND4-SinMinus;
           }
         if(LAST1IND5<0)
           {
            NNS1IND5=NNS1IND5+SinPlus;
           }
         else if(LAST1IND5>0)
           {
            NNS1IND5=NNS1IND5-SinMinus;
           }
        }
      else if(LASTNNS1>0)
        {
         if(NNS1>SinMin)
            NNS1=NNS1-SinMinus;
         if(LAST1IND2<0)
           {
            NNS1IND2=NNS1IND2+SinPlus;
           }
         else if(LAST1IND2>0)
           {
            NNS1IND2=NNS1IND2-SinMinus;
           }
         if(LAST1IND3<0)
           {
            NNS1IND3=NNS1IND3+SinPlus;
           }
         else if(LAST1IND3>0)
           {
            NNS1IND3=NNS1IND3-SinMinus;
           }
         if(LAST1IND4<0)
           {
            NNS1IND4=NNS1IND4+SinPlus;
           }
         else if(LAST1IND4>0)
           {
            NNS1IND4=NNS1IND4-SinMinus;
           }
         if(LAST1IND5<0)
           {
            NNS1IND5=NNS1IND5+SinPlus;
           }
         else if(LAST1IND5>0)
           {
            NNS1IND5=NNS1IND5-SinMinus;
           }
        }
      //NN2
      if(LASTNNS2<0)
        {
         if(NNS2<SinMax)
            NNS2=NNS2+SinPlus;
         if(LAST2IND1<0)
           {
            NNS2IND1=NNS2IND1+SinPlus;
           }
         else if(LAST2IND1>0)
           {
            NNS2IND1=NNS2IND1-SinMinus;
           }
         if(LAST2IND3<0)
           {
            NNS2IND3=NNS2IND3+SinPlus;
           }
         else if(LAST2IND3>0)
           {
            NNS2IND3=NNS2IND3-SinMinus;
           }
         if(LAST2IND4<0)
           {
            NNS2IND4=NNS2IND4+SinPlus;
           }
         else if(LAST2IND4>0)
           {
            NNS2IND4=NNS2IND4-SinMinus;
           }
         if(LAST2IND5<0)
           {
            NNS2IND5=NNS2IND5+SinPlus;
           }
         else if(LAST2IND5>0)
           {
            NNS2IND5=NNS2IND5-SinMinus;
           }
        }
      else if(LASTNNS2>0)
        {
         if(NNS2>SinMin)
            NNS2=NNS2-SinMinus;
         if(LAST2IND1<0)
           {
            NNS2IND1=NNS2IND1+SinPlus;
           }
         else if(LAST2IND1>0)
           {
            NNS2IND1=NNS2IND1-SinMinus;
           }
         if(LAST2IND3<0)
           {
            NNS2IND3=NNS2IND3+SinPlus;
           }
         else if(LAST2IND3>0)
           {
            NNS2IND3=NNS2IND3-SinMinus;
           }
         if(LAST2IND4<0)
           {
            NNS2IND4=NNS2IND4+SinPlus;
           }
         else if(LAST2IND4>0)
           {
            NNS2IND4=NNS2IND4-SinMinus;
           }
         if(LAST2IND5<0)
           {
            NNS2IND5=NNS2IND5+SinPlus;
           }
         else if(LAST2IND5>0)
           {
            NNS2IND5=NNS2IND5-SinMinus;
           }
        }
      //NN3
      if(LASTNNS3<0)
        {
         if(NNS3<SinMax)
            NNS3=NNS3+SinPlus;
         if(LAST3IND1<0)
           {
            NNS3IND1=NNS3IND1+SinPlus;
           }
         else if(LAST3IND1>0)
           {
            NNS3IND1=NNS3IND1-SinMinus;
           }
         if(LAST3IND2<0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         else if(LAST3IND2>0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         if(LAST3IND4<0)
           {
            NNS3IND4=NNS3IND4+SinPlus;
           }
         else if(LAST3IND4>0)
           {
            NNS2IND4=NNS2IND4-SinMinus;
           }
         if(LAST3IND5<0)
           {
            NNS3IND5=NNS3IND5+SinPlus;
           }
         else if(LAST3IND5>0)
           {
            NNS3IND5=NNS3IND5-SinMinus;
           }
        }
      else if(LASTNNS3>0)
        {
         if(NNS3>SinMin)
            NNS3=NNS3-SinMinus;
         if(LAST3IND1<0)
           {
            NNS3IND1=NNS3IND1+SinPlus;
           }
         else if(LAST3IND1>0)
           {
            NNS3IND1=NNS3IND1-SinMinus;
           }
         if(LAST3IND2<0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         else if(LAST3IND2>0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         if(LAST3IND4<0)
           {
            NNS3IND4=NNS3IND4+SinPlus;
           }
         else if(LAST3IND4>0)
           {
            NNS2IND4=NNS2IND4-SinMinus;
           }
         if(LAST3IND5<0)
           {
            NNS3IND5=NNS3IND5+SinPlus;
           }
         else if(LAST3IND5>0)
           {
            NNS3IND5=NNS3IND5-SinMinus;
           }
        }
      //NN4
      if(LASTNNS4<0)
        {
         if(NNS4<SinMax)
            NNS4=NNS4+SinPlus;
         if(LAST4IND1<0)
           {
            NNS4IND1=NNS4IND1+SinPlus;
           }
         else if(LAST4IND1>0)
           {
            NNS4IND1=NNS4IND1-SinMinus;
           }
         if(LAST4IND2<0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         else if(LAST4IND2>0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         if(LAST4IND3<0)
           {
            NNS4IND3=NNS4IND3+SinPlus;
           }
         else if(LAST4IND3>0)
           {
            NNS4IND3=NNS4IND3-SinMinus;
           }
         if(LAST4IND5<0)
           {
            NNS4IND5=NNS4IND5+SinPlus;
           }
         else if(LAST4IND5>0)
           {
            NNS4IND5=NNS4IND5-SinMinus;
           }
        }
      else if(LASTNNS4>0)
        {
         if(NNS4>SinMin)
            NNS4=NNS4-SinMinus;
         if(LAST4IND1<0)
           {
            NNS4IND1=NNS4IND1+SinPlus;
           }
         else if(LAST4IND1>0)
           {
            NNS4IND1=NNS4IND1-SinMinus;
           }
         if(LAST4IND2<0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         else if(LAST4IND2>0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         if(LAST4IND3<0)
           {
            NNS4IND3=NNS4IND3+SinPlus;
           }
         else if(LAST4IND3>0)
           {
            NNS4IND3=NNS4IND3-SinMinus;
           }
         if(LAST4IND5<0)
           {
            NNS4IND5=NNS4IND5+SinPlus;
           }
         else if(LAST4IND5>0)
           {
            NNS4IND5=NNS4IND5-SinMinus;
           }
        }
      //NN5
      if(LASTNNS5<0)
        {
         if(NNS5<SinMax)
            NNS5=NNS5+SinPlus;
         if(LAST5IND1<0)
           {
            NNS5IND1=NNS5IND1+SinPlus;
           }
         else if(LAST5IND1>0)
           {
            NNS5IND1=NNS5IND1-SinMinus;
           }
         if(LAST5IND2<0)
           {
            NNS5IND2=NNS5IND2+SinPlus;
           }
         else if(LAST5IND2>0)
           {
            NNS5IND2=NNS5IND2-SinMinus;
           }
         if(LAST5IND3<0)
           {
            NNS5IND3=NNS5IND3+SinPlus;
           }
         else if(LAST5IND3>0)
           {
            NNS5IND3=NNS5IND3-SinMinus;
           }
         if(LAST5IND4<0)
           {
            NNS5IND4=NNS5IND4+SinPlus;
           }
         else if(LAST5IND4>0)
           {
            NNS5IND4=NNS5IND4-SinMinus;
           }
        }
      else if(LASTNNS5>0)
        {
         if(NNS5>SinMin)
            NNS5=NNS5-SinMinus;
         if(LAST5IND1<0)
           {
            NNS5IND1=NNS5IND1+SinPlus;
           }
         else if(LAST5IND1>0)
           {
            NNS5IND1=NNS5IND1-SinMinus;
           }
         if(LAST5IND2<0)
           {
            NNS5IND2=NNS5IND2+SinPlus;
           }
         else if(LAST5IND2>0)
           {
            NNS5IND2=NNS5IND2-SinMinus;
           }
         if(LAST5IND3<0)
           {
            NNS5IND3=NNS5IND3+SinPlus;
           }
         else if(LAST5IND3>0)
           {
            NNS5IND3=NNS5IND3-SinMinus;
           }
         if(LAST5IND4<0)
           {
            NNS5IND4=NNS5IND4+SinPlus;
           }
         else if(LAST5IND4>0)
           {
            NNS5IND4=NNS5IND4-SinMinus;
           }
        }
     }
//tratamento sinopses para perdas
//1 = buy, 2 == sell
   if(profit<0.0 && LastTradeType==1)
     {
      setLast=0;
      //NN1
      if(LASTNNS1>0)
        {
         if(NNS1<SinMax)
            NNS1=NNS1-SinMinus;
         if(LAST1IND2>0)
           {
            NNS1IND2=NNS1IND2-SinMinus;
           }
         else if(LAST1IND2<0)
           {
            NNS1IND2=NNS1IND2+SinPlus;
           }
         if(LAST1IND3>0)
           {
            NNS1IND3=NNS1IND3-SinMinus;
           }
         else if(LAST1IND3<0)
           {
            NNS1IND3=NNS1IND3+SinPlus;
           }
         if(LAST1IND4>0)
           {
            NNS1IND4=NNS1IND4-SinMinus;
           }
         else if(LAST1IND4<0)
           {
            NNS1IND4=NNS1IND4+SinPlus;
           }
         if(LAST1IND5>0)
           {
            NNS1IND5=NNS1IND5-SinMinus;
           }
         else if(LAST1IND5<0)
           {
            NNS1IND5=NNS1IND5+SinPlus;
           }
        }
      else if(LASTNNS1<0)
        {
         if(NNS1>SinMin)
            NNS1=NNS1+SinPlus;
         if(LAST1IND2>0)
           {
            NNS1IND2=NNS1IND2-SinMinus;
           }
         else if(LAST1IND2<0)
           {
            NNS1IND2=NNS1IND2+SinPlus;
           }
         if(LAST1IND3>0)
           {
            NNS1IND3=NNS1IND3-SinMinus;
           }
         else if(LAST1IND3<0)
           {
            NNS1IND3=NNS1IND3+SinPlus;
           }
         if(LAST1IND4>0)
           {
            NNS1IND4=NNS1IND4-SinMinus;
           }
         else if(LAST1IND4<0)
           {
            NNS1IND4=NNS1IND4+SinPlus;
           }
         if(LAST1IND5>0)
           {
            NNS1IND5=NNS1IND5-SinMinus;
           }
         else if(LAST1IND5<0)
           {
            NNS1IND5=NNS1IND5+SinPlus;
           }
        }
      //NN2
      if(LASTNNS2>0)
        {
         if(NNS2<SinMax)
            NNS2=NNS2-SinMinus;
         if(LAST2IND1>0)
           {
            NNS2IND1=NNS2IND1-SinMinus;
           }
         else if(LAST2IND1<0)
           {
            NNS2IND1=NNS2IND1+SinPlus;
           }
         if(LAST2IND3>0)
           {
            NNS2IND3=NNS2IND3-SinMinus;
           }
         else if(LAST2IND3<0)
           {
            NNS2IND3=NNS2IND3+SinPlus;
           }
         if(LAST2IND4>0)
           {
            NNS2IND4=NNS2IND4-SinMinus;
           }
         else if(LAST2IND4<0)
           {
            NNS2IND4=NNS2IND4+SinPlus;
           }
         if(LAST2IND5>0)
           {
            NNS2IND5=NNS2IND5-SinMinus;
           }
         else if(LAST2IND5<0)
           {
            NNS2IND5=NNS2IND5+SinPlus;
           }
        }
      else if(LASTNNS2<0)
        {
         if(NNS2>SinMin)
            NNS2=NNS2+SinPlus;
         if(LAST2IND1>0)
           {
            NNS2IND1=NNS2IND1-SinMinus;
           }
         else if(LAST2IND1<0)
           {
            NNS2IND1=NNS2IND1+SinPlus;
           }
         if(LAST2IND3>0)
           {
            NNS2IND3=NNS2IND3-SinMinus;
           }
         else if(LAST2IND3<0)
           {
            NNS2IND3=NNS2IND3+SinPlus;
           }
         if(LAST2IND4>0)
           {
            NNS2IND4=NNS2IND4-SinMinus;
           }
         else if(LAST2IND4<0)
           {
            NNS2IND4=NNS2IND4+SinPlus;
           }
         if(LAST2IND5>0)
           {
            NNS2IND5=NNS2IND5-SinMinus;
           }
         else if(LAST2IND5<0)
           {
            NNS2IND5=NNS2IND5+SinPlus;
           }
        }
      //NN3
      if(LASTNNS3>0)
        {
         if(NNS3<SinMax)
            NNS3=NNS3-SinMinus;
         if(LAST3IND1>0)
           {
            NNS3IND1=NNS3IND1-SinMinus;
           }
         else if(LAST3IND1<0)
           {
            NNS3IND1=NNS3IND1+SinPlus;
           }
         if(LAST3IND2>0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         else if(LAST3IND2<0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         if(LAST3IND4>0)
           {
            NNS3IND4=NNS3IND4-SinMinus;
           }
         else if(LAST3IND4<0)
           {
            NNS2IND4=NNS2IND4+SinPlus;
           }
         if(LAST3IND5>0)
           {
            NNS3IND5=NNS3IND5-SinMinus;
           }
         else if(LAST3IND5<0)
           {
            NNS3IND5=NNS3IND5+SinPlus;
           }
        }
      else if(LASTNNS3<0)
        {
         if(NNS3>SinMin)
            NNS3=NNS3+SinPlus;
         if(LAST3IND1>0)
           {
            NNS3IND1=NNS3IND1-SinMinus;
           }
         else if(LAST3IND1<0)
           {
            NNS3IND1=NNS3IND1+SinPlus;
           }
         if(LAST3IND2>0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         else if(LAST3IND2<0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         if(LAST3IND4>0)
           {
            NNS3IND4=NNS3IND4-SinMinus;
           }
         else if(LAST3IND4<0)
           {
            NNS2IND4=NNS2IND4+SinPlus;
           }
         if(LAST3IND5>0)
           {
            NNS3IND5=NNS3IND5-SinMinus;
           }
         else if(LAST3IND5<0)
           {
            NNS3IND5=NNS3IND5+SinPlus;
           }
        }
      //NN4
      if(LASTNNS4>0)
        {
         if(NNS4<SinMax)
            NNS4=NNS4-SinMinus;
         if(LAST4IND1>0)
           {
            NNS4IND1=NNS4IND1-SinMinus;
           }
         else if(LAST4IND1<0)
           {
            NNS4IND1=NNS4IND1+SinPlus;
           }
         if(LAST4IND2>0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         else if(LAST4IND2<0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         if(LAST4IND3>0)
           {
            NNS4IND3=NNS4IND3-SinMinus;
           }
         else if(LAST4IND3<0)
           {
            NNS4IND3=NNS4IND3+SinPlus;
           }
         if(LAST4IND5>0)
           {
            NNS4IND5=NNS4IND5-SinMinus;
           }
         else if(LAST4IND5<0)
           {
            NNS4IND5=NNS4IND5+SinPlus;
           }
        }
      else if(LASTNNS4<0)
        {
         if(NNS4>SinMin)
            NNS4=NNS4+SinPlus;
         if(LAST4IND1>0)
           {
            NNS4IND1=NNS4IND1-SinMinus;
           }
         else if(LAST4IND1<0)
           {
            NNS4IND1=NNS4IND1+SinPlus;
           }
         if(LAST4IND2>0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         else if(LAST4IND2<0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         if(LAST4IND3>0)
           {
            NNS4IND3=NNS4IND3-SinMinus;
           }
         else if(LAST4IND3<0)
           {
            NNS4IND3=NNS4IND3+SinPlus;
           }
         if(LAST4IND5>0)
           {
            NNS4IND5=NNS4IND5-SinMinus;
           }
         else if(LAST4IND5<0)
           {
            NNS4IND5=NNS4IND5+SinPlus;
           }
        }
      //NN5
      if(LASTNNS5>0)
        {
         if(NNS5<SinMax)
            NNS5=NNS5-SinMinus;
         if(LAST5IND1>0)
           {
            NNS5IND1=NNS5IND1-SinMinus;
           }
         else if(LAST5IND1<0)
           {
            NNS5IND1=NNS5IND1+SinPlus;
           }
         if(LAST5IND2>0)
           {
            NNS5IND2=NNS5IND2-SinMinus;
           }
         else if(LAST5IND2<0)
           {
            NNS5IND2=NNS5IND2+SinPlus;
           }
         if(LAST5IND3>0)
           {
            NNS5IND3=NNS5IND3-SinMinus;
           }
         else if(LAST5IND3<0)
           {
            NNS5IND3=NNS5IND3+SinPlus;
           }
         if(LAST5IND4>0)
           {
            NNS5IND4=NNS5IND4-SinMinus;
           }
         else if(LAST5IND4<0)
           {
            NNS5IND4=NNS5IND4+SinPlus;
           }
        }
      else if(LASTNNS5<0)
        {
         if(NNS5>SinMin)
            NNS5=NNS5+SinPlus;
         if(LAST5IND1>0)
           {
            NNS5IND1=NNS5IND1-SinMinus;
           }
         else if(LAST5IND1<0)
           {
            NNS5IND1=NNS5IND1+SinPlus;
           }
         if(LAST5IND2>0)
           {
            NNS5IND2=NNS5IND2-SinMinus;
           }
         else if(LAST5IND2<0)
           {
            NNS5IND2=NNS5IND2+SinPlus;
           }
         if(LAST5IND3>0)
           {
            NNS5IND3=NNS5IND3-SinMinus;
           }
         else if(LAST5IND3<0)
           {
            NNS5IND3=NNS5IND3+SinPlus;
           }
         if(LAST5IND4>0)
           {
            NNS5IND4=NNS5IND4-SinMinus;
           }
         else if(LAST5IND4<0)
           {
            NNS5IND4=NNS5IND4+SinPlus;
           }
        }
     }
   if(profit<0.0 && LastTradeType==2)
     {
      setLast=0;
      //NN1
      if(LASTNNS1<0)
        {
         if(NNS1<SinMax)
            NNS1=NNS1-SinMinus;
         if(LAST1IND2<0)
           {
            NNS1IND2=NNS1IND2-SinMinus;
           }
         else if(LAST1IND2>0)
           {
            NNS1IND2=NNS1IND2+SinPlus;
           }
         if(LAST1IND3<0)
           {
            NNS1IND3=NNS1IND3-SinMinus;
           }
         else if(LAST1IND3>0)
           {
            NNS1IND3=NNS1IND3+SinPlus;
           }
         if(LAST1IND4<0)
           {
            NNS1IND4=NNS1IND4-SinMinus;
           }
         else if(LAST1IND4>0)
           {
            NNS1IND4=NNS1IND4+SinPlus;
           }
         if(LAST1IND5<0)
           {
            NNS1IND5=NNS1IND5-SinMinus;
           }
         else if(LAST1IND5>0)
           {
            NNS1IND5=NNS1IND5+SinPlus;
           }
        }
      else if(LASTNNS1>0)
        {
         if(NNS1>SinMin)
            NNS1=NNS1+SinPlus;
         if(LAST1IND2<0)
           {
            NNS1IND2=NNS1IND2-SinMinus;
           }
         else if(LAST1IND2>0)
           {
            NNS1IND2=NNS1IND2+SinPlus;
           }
         if(LAST1IND3<0)
           {
            NNS1IND3=NNS1IND3-SinMinus;
           }
         else if(LAST1IND3>0)
           {
            NNS1IND3=NNS1IND3+SinPlus;
           }
         if(LAST1IND4<0)
           {
            NNS1IND4=NNS1IND4-SinMinus;
           }
         else if(LAST1IND4>0)
           {
            NNS1IND4=NNS1IND4+SinPlus;
           }
         if(LAST1IND5<0)
           {
            NNS1IND5=NNS1IND5-SinMinus;
           }
         else if(LAST1IND5>0)
           {
            NNS1IND5=NNS1IND5+SinPlus;
           }
        }
      //NN2
      if(LASTNNS2<0)
        {
         if(NNS2<SinMax)
            NNS2=NNS2-SinMinus;
         if(LAST2IND1<0)
           {
            NNS2IND1=NNS2IND1-SinMinus;
           }
         else if(LAST2IND1>0)
           {
            NNS2IND1=NNS2IND1+SinPlus;
           }
         if(LAST2IND3<0)
           {
            NNS2IND3=NNS2IND3-SinMinus;
           }
         else if(LAST2IND3>0)
           {
            NNS2IND3=NNS2IND3+SinPlus;
           }
         if(LAST2IND4<0)
           {
            NNS2IND4=NNS2IND4-SinMinus;
           }
         else if(LAST2IND4>0)
           {
            NNS2IND4=NNS2IND4+SinPlus;
           }
         if(LAST2IND5<0)
           {
            NNS2IND5=NNS2IND5-SinMinus;
           }
         else if(LAST2IND5>0)
           {
            NNS2IND5=NNS2IND5+SinPlus;
           }
        }
      else if(LASTNNS2>0)
        {
         if(NNS2>SinMin)
            NNS2=NNS2+SinPlus;
         if(LAST2IND1<0)
           {
            NNS2IND1=NNS2IND1-SinMinus;
           }
         else if(LAST2IND1>0)
           {
            NNS2IND1=NNS2IND1+SinPlus;
           }
         if(LAST2IND3<0)
           {
            NNS2IND3=NNS2IND3-SinMinus;
           }
         else if(LAST2IND3>0)
           {
            NNS2IND3=NNS2IND3+SinPlus;
           }
         if(LAST2IND4<0)
           {
            NNS2IND4=NNS2IND4-SinMinus;
           }
         else if(LAST2IND4>0)
           {
            NNS2IND4=NNS2IND4+SinPlus;
           }
         if(LAST2IND5<0)
           {
            NNS2IND5=NNS2IND5-SinMinus;
           }
         else if(LAST2IND5>0)
           {
            NNS2IND5=NNS2IND5+SinPlus;
           }
        }
      //NN3
      if(LASTNNS3<0)
        {
         if(NNS3<SinMax)
            NNS3=NNS3-SinMinus;
         if(LAST3IND1<0)
           {
            NNS3IND1=NNS3IND1-SinMinus;
           }
         else if(LAST3IND1>0)
           {
            NNS3IND1=NNS3IND1+SinPlus;
           }
         if(LAST3IND2<0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         else if(LAST3IND2>0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         if(LAST3IND4<0)
           {
            NNS3IND4=NNS3IND4-SinMinus;
           }
         else if(LAST3IND4>0)
           {
            NNS2IND4=NNS2IND4+SinPlus;
           }
         if(LAST3IND5<0)
           {
            NNS3IND5=NNS3IND5-SinMinus;
           }
         else if(LAST3IND5>0)
           {
            NNS3IND5=NNS3IND5+SinPlus;
           }
        }
      else if(LASTNNS3>0)
        {
         if(NNS3>SinMin)
            NNS3=NNS3+SinPlus;
         if(LAST3IND1<0)
           {
            NNS3IND1=NNS3IND1-SinMinus;
           }
         else if(LAST3IND1>0)
           {
            NNS3IND1=NNS3IND1+SinPlus;
           }
         if(LAST3IND2<0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         else if(LAST3IND2>0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         if(LAST3IND4<0)
           {
            NNS3IND4=NNS3IND4-SinMinus;
           }
         else if(LAST3IND4>0)
           {
            NNS2IND4=NNS2IND4+SinPlus;
           }
         if(LAST3IND5<0)
           {
            NNS3IND5=NNS3IND5-SinMinus;
           }
         else if(LAST3IND5>0)
           {
            NNS3IND5=NNS3IND5+SinPlus;
           }
        }
      //NN4
      if(LASTNNS4<0)
        {
         if(NNS4<SinMax)
            NNS4=NNS4-SinMinus;
         if(LAST4IND1<0)
           {
            NNS4IND1=NNS4IND1-SinMinus;
           }
         else if(LAST4IND1>0)
           {
            NNS4IND1=NNS4IND1+SinPlus;
           }
         if(LAST4IND2<0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         else if(LAST4IND2>0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         if(LAST4IND3<0)
           {
            NNS4IND3=NNS4IND3-SinMinus;
           }
         else if(LAST4IND3>0)
           {
            NNS4IND3=NNS4IND3+SinPlus;
           }
         if(LAST4IND5<0)
           {
            NNS4IND5=NNS4IND5-SinMinus;
           }
         else if(LAST4IND5>0)
           {
            NNS4IND5=NNS4IND5+SinPlus;
           }
        }
      else if(LASTNNS4>0)
        {
         if(NNS4>SinMin)
            NNS4=NNS4+SinPlus;
         if(LAST4IND1<0)
           {
            NNS4IND1=NNS4IND1-SinMinus;
           }
         else if(LAST4IND1>0)
           {
            NNS4IND1=NNS4IND1+SinPlus;
           }
         if(LAST4IND2<0)
           {
            NNS3IND2=NNS3IND2-SinMinus;
           }
         else if(LAST4IND2>0)
           {
            NNS3IND2=NNS3IND2+SinPlus;
           }
         if(LAST4IND3<0)
           {
            NNS4IND3=NNS4IND3-SinMinus;
           }
         else if(LAST4IND3>0)
           {
            NNS4IND3=NNS4IND3+SinPlus;
           }
         if(LAST4IND5<0)
           {
            NNS4IND5=NNS4IND5-SinMinus;
           }
         else if(LAST4IND5>0)
           {
            NNS4IND5=NNS4IND5+SinPlus;
           }
        }
      //NN5
      if(LASTNNS5<0)
        {
         if(NNS5<SinMax)
            NNS5=NNS5-SinMinus;
         if(LAST5IND1<0)
           {
            NNS5IND1=NNS5IND1-SinMinus;
           }
         else if(LAST5IND1>0)
           {
            NNS5IND1=NNS5IND1+SinPlus;
           }
         if(LAST5IND2<0)
           {
            NNS5IND2=NNS5IND2-SinMinus;
           }
         else if(LAST5IND2>0)
           {
            NNS5IND2=NNS5IND2+SinPlus;
           }
         if(LAST5IND3<0)
           {
            NNS5IND3=NNS5IND3-SinMinus;
           }
         else if(LAST5IND3>0)
           {
            NNS5IND3=NNS5IND3+SinPlus;
           }
         if(LAST5IND4<0)
           {
            NNS5IND4=NNS5IND4-SinMinus;
           }
         else if(LAST5IND4>0)
           {
            NNS5IND4=NNS5IND4+SinPlus;
           }
        }
      else if(LASTNNS5>0)
        {
         if(NNS5>SinMin)
            NNS5=NNS5+SinPlus;
         if(LAST5IND1<0)
           {
            NNS5IND1=NNS5IND1-SinMinus;
           }
         else if(LAST5IND1>0)
           {
            NNS5IND1=NNS5IND1+SinPlus;
           }
         if(LAST5IND2<0)
           {
            NNS5IND2=NNS5IND2-SinMinus;
           }
         else if(LAST5IND2>0)
           {
            NNS5IND2=NNS5IND2+SinPlus;
           }
         if(LAST5IND3<0)
           {
            NNS5IND3=NNS5IND3-SinMinus;
           }
         else if(LAST5IND3>0)
           {
            NNS5IND3=NNS5IND3+SinPlus;
           }
         if(LAST5IND4<0)
           {
            NNS5IND4=NNS5IND4-SinMinus;
           }
         else if(LAST5IND4>0)
           {
            NNS5IND4=NNS5IND4+SinPlus;
           }
        }
     }
  }
//+------------------------------------------------------------------+
