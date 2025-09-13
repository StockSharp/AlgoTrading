//+------------------------------------------------------------------+
//|                                                 VivaLasVegas.mq4 |
//|                      Copyright © 2010, MetaQuotes Software Corp. |
//|                                        when-money-makes-money.com|
//+------------------------------------------------------------------+
#property copyright "zzuegg"
#property link      "when-money-makes-money.com"

#define MODE_WIN 1
#define MODE_LOSS 2

#define MODE_MARTINGALE 1
#define MODE_NEGATIVE_PYRAMIDE 2
#define MODE_LABOUCHERE 3
#define MODE_OSCARS_GRIND 4
#define MODE_31_SYSTEM 5

double Labouchere_Series[] = {1,2,3};
 

int PointMod=0;
extern int TP_SL=50;
extern double BaseLotSize=1;
extern int GameStrategy=1;
extern int Seed=0;





int Sys31.series[]={1,1,1,2,2,4,4,8,8};
int Sys31.current=0;
bool Sys31.doubleUP=false;
void SYS31.updateLots(int result,double lots){
   switch(result){ 
      case MODE_WIN:{
         if(Sys31.doubleUP==false){
            Sys31.doubleUP=true;
         }else{
            Sys31.doubleUP=false;
            Sys31.current=0;
         }
         break;
      }
      case MODE_LOSS:{
         if(Sys31.doubleUP==false){
            Sys31.current++;
            Sys31.current=Sys31.current%ArraySize(Sys31.series);
         }else{
            Sys31.doubleUP=false;
         }
         break;
      }
   }
}
double SYS31.getLotsize(){
   if(Sys31.doubleUP==false){
      return(Sys31.series[Sys31.current]*BaseLotSize);
   }else{
      return(Sys31.series[Sys31.current]*BaseLotSize*2);   
   }
}


double OscarsGrind.NextLots=0;
double OscarsGrind.CurrentResult=0;
void OscarsGrind.updateLots(int result,double lots){
   switch(result){ 
      case MODE_WIN:{
         
         OscarsGrind.CurrentResult+=lots;
         if(OscarsGrind.CurrentResult>=BaseLotSize){
            OscarsGrind.NextLots=BaseLotSize;
            OscarsGrind.CurrentResult=0;
            break;
         }
         OscarsGrind.NextLots=(OscarsGrind.NextLots)+BaseLotSize;
         OscarsGrind.NextLots=MathMin(OscarsGrind.NextLots,BaseLotSize+MathAbs(OscarsGrind.CurrentResult));
         
      break;
      }
      case MODE_LOSS:{
         OscarsGrind.CurrentResult-=lots;
      break;
      }
   }
}
double OscarsGrind.getLotsize(){
   if(OscarsGrind.NextLots==0) OscarsGrind.NextLots=BaseLotSize;
   return (OscarsGrind.NextLots);
}


double currentSeries[];
void Labouchere.updateLots(int result,double lots){
   double tmp[];
   int i=0;
   switch(result){ 
      case MODE_WIN:{
         if(ArraySize(currentSeries)>2){
            ArrayResize(tmp,ArraySize(currentSeries)-2);
            for(i=0;i<ArraySize(tmp);i++){
               tmp[i]=currentSeries[i+1];
            }
            ArrayResize(currentSeries,ArraySize(tmp));
            for(i=0;i<ArraySize(tmp);i++){
               currentSeries[i]=tmp[i];
            }
         }else{
            ArrayResize(currentSeries,ArraySize(Labouchere_Series));
            for(i =0;i<ArraySize(currentSeries);i++){
               currentSeries[i]=Labouchere_Series[i];
            }
         }
         break;
      }
      case MODE_LOSS:{
         ArrayResize(currentSeries,ArraySize(currentSeries)+1);
         currentSeries[ArraySize(currentSeries)-1]=currentSeries[0]+currentSeries[ArraySize(currentSeries)-2];
         break;
      }
   }
}
double Labouchere.getLotsize(){
   if(ArraySize(currentSeries)==0){
      ArrayResize(currentSeries,ArraySize(Labouchere_Series));
      for(int i=0;i<ArraySize(Labouchere_Series);i++){
         currentSeries[i]=Labouchere_Series[i];
      }
   }
   if(ArraySize(currentSeries)>1){
      return((currentSeries[0]+currentSeries[ArraySize(currentSeries)-1])*BaseLotSize);
   }else{
      return(currentSeries[0]*BaseLotSize);
   }   
}


double Martingale.NextLots=0;
void Martingale.updateLots(int result,double lots){
   switch(result){ case MODE_WIN:{Martingale.NextLots=BaseLotSize;break;}case MODE_LOSS:{Martingale.NextLots=Martingale.NextLots*2;break;}}
}
double Martingale.getLotsize(){
   if(Martingale.NextLots==0) Martingale.NextLots=BaseLotSize;
   return (Martingale.NextLots);
}

double NP.NextLots=0;
void NP.updateLots(int result,double lots){
   switch(result){ case MODE_WIN:{NP.NextLots=NP.NextLots/2;if(NP.NextLots<BaseLotSize) NP.NextLots=BaseLotSize;break;}case MODE_LOSS:{NP.NextLots=NP.NextLots*2;break;}}
}
double NP.getLotsize(){
   if(NP.NextLots==0) NP.NextLots=BaseLotSize;
   return (NP.NextLots);
}

void updateLots(int mmm,int result,double lots){
   switch(mmm){
      case MODE_MARTINGALE: {Martingale.updateLots(result,lots);break;}  
      case MODE_NEGATIVE_PYRAMIDE: {NP.updateLots(result,lots);break;}  
      case MODE_LABOUCHERE: {Labouchere.updateLots(result,lots);break;}
      case MODE_OSCARS_GRIND: {OscarsGrind.updateLots(result,lots);break;} 
      case MODE_31_SYSTEM: {SYS31.updateLots(result,lots);break;} 
   }
}

double getLotsize(int mmm){
   switch(mmm){
      case MODE_MARTINGALE: {return(Martingale.getLotsize());break;}  
      case MODE_NEGATIVE_PYRAMIDE: {return(NP.getLotsize());break;}  
      case MODE_LABOUCHERE: {return(Labouchere.getLotsize());break;}  
      case MODE_OSCARS_GRIND: {return(OscarsGrind.getLotsize());break;} 
      case MODE_31_SYSTEM: {return(SYS31.getLotsize());break;}  
   }
}

void init(){
   MathSrand(Seed);
   if(Digits==5||Digits==3){
      PointMod=10;
   }else{
      PointMod=1;
   }
}


int Trade=-1;

int start()
  {
   if(Trade!=-1){
      OrderSelect(Trade,SELECT_BY_TICKET);
      if(OrderCloseTime()!=0){
         if(OrderProfit()>0){
            updateLots(GameStrategy,MODE_WIN,OrderLots());
         }else{
            updateLots(GameStrategy,MODE_LOSS,OrderLots());         
         }
      Trade=-1;
      }
   }else{
      int direction=MathRand()%2;
      switch (direction){
         case 1:{
           Trade=OrderSend(Symbol(),OP_BUY,getLotsize(GameStrategy),Ask,0,NormalizeDouble(Ask-TP_SL*PointMod*Point,Digits),NormalizeDouble(Ask+TP_SL*PointMod*Point,Digits),"",0,CLR_NONE);         
         break;
         }
         case 0:{
           Trade=OrderSend(Symbol(),OP_SELL,getLotsize(GameStrategy),Bid,0,NormalizeDouble(Bid+TP_SL*PointMod*Point,Digits),NormalizeDouble(Bid-TP_SL*PointMod*Point,Digits),"",0,CLR_NONE);                 
         break;
         }
      }
   }
   Comment(Trade);
   return(0);
  }
//+------------------------------------------------------------------+