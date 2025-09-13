//+------------------------------------------------------------------+
//|                                                     5matf.mq4 |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007"
#property link      ""

extern int StopLoss=0;
extern int TakeProfit=10;
extern int TrailingStop=0;
extern double Lots=0.1;
extern int Slippage=3;

extern int OpenLevel=0;//Уровень открытия 0 или 1
extern int CloseLevel=1;//Уровень закрытия 0 или 1

extern int TF1 = 15;
extern int TF2 = 60;
extern int TF3 = 240;
extern int maTrendPeriodv_1 = 5;
extern int maTrendPeriodv_2 = 8;
extern int maTrendPeriodv_3 = 13;
extern int maTrendPeriodv_4 = 21;
extern int maTrendPeriodv_5 = 34;

int Signal;
double SL,TP;
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
   Comment("");
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
{
TREND_alexcud();
int Total=0;
for(int cnt=0;cnt<OrdersTotal();cnt++)
   {
   OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
   if(OrderSymbol()==Symbol())
      {
      Total++;
      if(OrderType()==OP_BUY)
         {
         if(Signal<-CloseLevel)
            {
            OrderClose(OrderTicket(),OrderLots(),Bid,Slippage);
            return(0);
            }
         if(TrailingStop>0
         && Bid-OrderOpenPrice()>Point*TrailingStop
         && OrderStopLoss()<Bid-Point*TrailingStop)
            {
            OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0);
            return(0);
            }
         }
      if(OrderType()==OP_SELL)
         {
         if(Signal>CloseLevel)
            {
            OrderClose(OrderTicket(),OrderLots(),Ask,Slippage);
            return(0);
            }
         if(TrailingStop>0
         && OrderOpenPrice()-Ask>Point*TrailingStop
         && (OrderStopLoss()>Ask+Point*TrailingStop || OrderStopLoss()==0))
            {
            OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0);
            return(0);
            }
         }
      }
   }
if(Total==0)
   {
   if(Signal>OpenLevel)
      {
      SL=0;TP=0;
      if(StopLoss>0)   SL=Ask-Point*StopLoss;
      if(TakeProfit>0) TP=Ask+Point*TakeProfit;
      OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,SL,TP,NULL,0,0);
      return(0);
      }
   if(Signal<OpenLevel)
      {
      SL=0;TP=0;
      if(StopLoss>0)   SL=Bid+Point*StopLoss;
      if(TakeProfit>0) TP=Bid-Point*TakeProfit;
      OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,SL,TP,NULL,0,0);
      return(0);
      }
   }
  return(0);
}
//+------------------------------------------------------------------+
void TREND_alexcud()
   {
double MaH11v,  MaH41v, MaD11v, MaH1pr1v, MaH4pr1v, MaD1pr1v;
double MaH12v,  MaH42v, MaD12v, MaH1pr2v, MaH4pr2v, MaD1pr2v;
double MaH13v,  MaH43v, MaD13v, MaH1pr3v, MaH4pr3v, MaD1pr3v;
double MaH14v,  MaH44v, MaD14v, MaH1pr4v, MaH4pr4v, MaD1pr4v;
double MaH15v,  MaH45v, MaD15v, MaH1pr5v, MaH4pr5v, MaD1pr5v;

double u1x5v, u1x8v, u1x13v, u1x21v, u1x34v;
double u2x5v, u2x8v, u2x13v, u2x21v, u2x34v;
double u3x5v, u3x8v, u3x13v, u3x21v, u3x34v;
double u1acv, u2acv, u3acv;

double d1x5v, d1x8v, d1x13v, d1x21v, d1x34v;
double d2x5v, d2x8v, d2x13v, d2x21v, d2x34v;
double d3x5v, d3x8v, d3x13v, d3x21v, d3x34v;
double d1acv, d2acv, d3acv;

MaH11v=iMA(NULL,TF1,maTrendPeriodv_1,0,MODE_SMA,PRICE_CLOSE,0);   MaH1pr1v=iMA(NULL,TF1,maTrendPeriodv_1,0,MODE_SMA,PRICE_CLOSE,1);
MaH12v=iMA(NULL,TF1,maTrendPeriodv_2,0,MODE_SMA,PRICE_CLOSE,0);   MaH1pr2v=iMA(NULL,TF1,maTrendPeriodv_2,0,MODE_SMA,PRICE_CLOSE,1);
MaH13v=iMA(NULL,TF1,maTrendPeriodv_3,0,MODE_SMA,PRICE_CLOSE,0);   MaH1pr3v=iMA(NULL,TF1,maTrendPeriodv_3,0,MODE_SMA,PRICE_CLOSE,1);
MaH14v=iMA(NULL,TF1,maTrendPeriodv_4,0,MODE_SMA,PRICE_CLOSE,0);   MaH1pr4v=iMA(NULL,TF1,maTrendPeriodv_4,0,MODE_SMA,PRICE_CLOSE,1);
MaH15v=iMA(NULL,TF1,maTrendPeriodv_5,0,MODE_SMA,PRICE_CLOSE,0);   MaH1pr5v=iMA(NULL,TF1,maTrendPeriodv_5,0,MODE_SMA,PRICE_CLOSE,1);
   
MaH41v=iMA(NULL,TF2,maTrendPeriodv_1,0,MODE_SMA,PRICE_CLOSE,0);   MaH4pr1v=iMA(NULL,TF2,maTrendPeriodv_1,0,MODE_SMA,PRICE_CLOSE,1);
MaH42v=iMA(NULL,TF2,maTrendPeriodv_2,0,MODE_SMA,PRICE_CLOSE,0);   MaH4pr2v=iMA(NULL,TF2,maTrendPeriodv_2,0,MODE_SMA,PRICE_CLOSE,1);
MaH43v=iMA(NULL,TF2,maTrendPeriodv_3,0,MODE_SMA,PRICE_CLOSE,0);   MaH4pr3v=iMA(NULL,TF2,maTrendPeriodv_3,0,MODE_SMA,PRICE_CLOSE,1);
MaH44v=iMA(NULL,TF2,maTrendPeriodv_4,0,MODE_SMA,PRICE_CLOSE,0);   MaH4pr4v=iMA(NULL,TF2,maTrendPeriodv_4,0,MODE_SMA,PRICE_CLOSE,1);
MaH45v=iMA(NULL,TF2,maTrendPeriodv_5,0,MODE_SMA,PRICE_CLOSE,0);   MaH4pr5v=iMA(NULL,TF2,maTrendPeriodv_5,0,MODE_SMA,PRICE_CLOSE,1);
     
MaD11v=iMA(NULL,TF3,maTrendPeriodv_1,0,MODE_SMA,PRICE_CLOSE,0);   MaD1pr1v=iMA(NULL,TF3,maTrendPeriodv_1,0,MODE_SMA,PRICE_CLOSE,1);
MaD12v=iMA(NULL,TF3,maTrendPeriodv_2,0,MODE_SMA,PRICE_CLOSE,0);   MaD1pr2v=iMA(NULL,TF3,maTrendPeriodv_2,0,MODE_SMA,PRICE_CLOSE,1);
MaD13v=iMA(NULL,TF3,maTrendPeriodv_3,0,MODE_SMA,PRICE_CLOSE,0);   MaD1pr3v=iMA(NULL,TF3,maTrendPeriodv_3,0,MODE_SMA,PRICE_CLOSE,1);
MaD14v=iMA(NULL,TF3,maTrendPeriodv_4,0,MODE_SMA,PRICE_CLOSE,0);   MaD1pr4v=iMA(NULL,TF3,maTrendPeriodv_4,0,MODE_SMA,PRICE_CLOSE,1);
MaD15v=iMA(NULL,TF3,maTrendPeriodv_5,0,MODE_SMA,PRICE_CLOSE,0);   MaD1pr5v=iMA(NULL,TF3,maTrendPeriodv_5,0,MODE_SMA,PRICE_CLOSE,1);
     
     if (MaH11v < MaH1pr1v) {u1x5v = 0; d1x5v = 1;}
     if (MaH11v > MaH1pr1v) {u1x5v = 1; d1x5v = 0;}
     if (MaH11v == MaH1pr1v){u1x5v = 0; d1x5v = 0;}           
     if (MaH41v < MaH4pr1v) {u2x5v = 0; d2x5v = 1;}           
     if (MaH41v > MaH4pr1v) {u2x5v = 1; d2x5v = 0;}
     if (MaH41v == MaH4pr1v){u2x5v = 0; d2x5v = 0;}           
     if (MaD11v < MaD1pr1v) {u3x5v = 0; d3x5v = 1;}           
     if (MaD11v > MaD1pr1v) {u3x5v = 1; d3x5v = 0;}
     if (MaD11v == MaD1pr1v){u3x5v = 0; d3x5v = 0;} 
     
     if (MaH12v < MaH1pr2v) {u1x8v = 0; d1x8v = 1;}
     if (MaH12v > MaH1pr2v) {u1x8v = 1; d1x8v = 0;}
     if (MaH12v == MaH1pr2v){u1x8v = 0; d1x8v = 0;}           
     if (MaH42v < MaH4pr2v) {u2x8v = 0; d2x8v = 1;}           
     if (MaH42v > MaH4pr2v) {u2x8v = 1; d2x8v = 0;}
     if (MaH42v == MaH4pr2v){u2x8v = 0; d2x8v = 0;}           
     if (MaD12v < MaD1pr2v) {u3x8v = 0; d3x8v = 1;}             
     if (MaD12v > MaD1pr2v) {u3x8v = 1; d3x8v = 0;}
     if (MaD12v == MaD1pr2v){u3x8v = 0; d3x8v = 0;}
     
     if (MaH13v < MaH1pr3v) {u1x13v = 0; d1x13v = 1;}
     if (MaH13v > MaH1pr3v) {u1x13v = 1; d1x13v = 0;}
     if (MaH13v == MaH1pr3v){u1x13v = 0; d1x13v = 0;}             
     if (MaH43v < MaH4pr3v) {u2x13v = 0; d2x13v = 1;}           
     if (MaH43v > MaH4pr3v) {u2x13v = 1; d2x13v = 0;}
     if (MaH43v == MaH4pr3v){u2x13v = 0; d2x13v = 0;}           
     if (MaD13v < MaD1pr3v) {u3x13v = 0; d3x13v = 1;}           
     if (MaD13v > MaD1pr3v) {u3x13v = 1; d3x13v = 0;}
     if (MaD13v == MaD1pr3v){u3x13v = 0; d3x13v = 0;}
     
     if (MaH14v < MaH1pr4v) {u1x21v = 0; d1x21v = 1;}
     if (MaH14v > MaH1pr4v) {u1x21v = 1; d1x21v = 0;}
     if (MaH14v == MaH1pr4v){u1x21v = 0; d1x21v = 0;}             
     if (MaH44v < MaH4pr4v) {u2x21v = 0; d2x21v = 1;}           
     if (MaH44v > MaH4pr4v) {u2x21v = 1; d2x21v = 0;}
     if (MaH44v == MaH4pr4v){u2x21v = 0; d2x21v = 0;}           
     if (MaD14v < MaD1pr4v) {u3x21v = 0; d3x21v = 1;}           
     if (MaD14v > MaD1pr4v) {u3x21v = 1; d3x21v = 0;}
     if (MaD14v == MaD1pr4v){u3x21v = 0; d3x21v = 0;} 
     
     if (MaH15v < MaH1pr5v) {u1x34v = 0; d1x34v = 1;}
     if (MaH15v > MaH1pr5v) {u1x34v = 1; d1x34v = 0;}
     if (MaH15v == MaH1pr5v){u1x34v = 0; d1x34v = 0;}             
     if (MaH45v < MaH4pr5v) {u2x34v = 0; d2x34v = 1;}           
     if (MaH45v > MaH4pr5v) {u2x34v = 1; d2x34v = 0;}
     if (MaH45v == MaH4pr5v){u2x34v = 0; d2x34v = 0;}           
     if (MaD15v < MaD1pr5v) {u3x34v = 0; d3x34v = 1;}           
     if (MaD15v > MaD1pr5v) {u3x34v = 1; d3x34v = 0;}
     if (MaD15v == MaD1pr5v){u3x34v = 0; d3x34v = 0;}
     
double  acv  = iAC(NULL, TF1, 0);
double  ac1v = iAC(NULL, TF1, 1);
double  ac2v = iAC(NULL, TF1, 2);
double  ac3v = iAC(NULL, TF1, 3);

if((ac1v>ac2v && ac2v>ac3v && acv<0 && acv>ac1v)||(acv>ac1v && ac1v>ac2v && acv>0)) {u1acv = 3; d1acv = 0;}
if((ac1v<ac2v && ac2v<ac3v && acv>0 && acv<ac1v)||(acv<ac1v && ac1v<ac2v && acv<0)) {u1acv = 0; d1acv = 3;}
if((((ac1v<ac2v || ac2v<ac3v) && acv<0 && acv>ac1v) || (acv>ac1v && ac1v<ac2v && acv>0))
|| (((ac1v>ac2v || ac2v>ac3v) && acv>0 && acv<ac1v) || (acv<ac1v && ac1v>ac2v && acv<0)))
{u1acv = 0; d1acv = 0;}
 
double  ac03v = iAC(NULL, TF3, 0);
double  ac13v = iAC(NULL, TF3, 1);
double  ac23v = iAC(NULL, TF3, 2);
double  ac33v = iAC(NULL, TF3, 3);

if((ac13v>ac23v && ac23v>ac33v && ac03v<0 && ac03v>ac13v)||(ac03v>ac13v && ac13v>ac23v && ac03v>0)) {u3acv = 3; d3acv = 0;}     
if((ac13v<ac23v && ac23v<ac33v && ac03v>0 && ac03v<ac13v)||(ac03v<ac13v && ac13v<ac23v && ac03v<0)) {u3acv = 0; d3acv = 3;}     
if((((ac13v<ac23v || ac23v<ac33v) && ac03v<0 && ac03v>ac13v) || (ac03v>ac13v && ac13v<ac23v && ac03v>0))
|| (((ac13v>ac23v || ac23v>ac33v) && ac03v>0 && ac03v<ac13v) || (ac03v<ac13v && ac13v>ac23v && ac03v<0)))
{u3acv = 0; d3acv = 0;}

   double uitog1v = (u1x5v + u1x8v + u1x13v + u1x21v + u1x34v + u1acv) * 12.5;
   double uitog2v = (u2x5v + u2x8v + u2x13v + u2x21v + u2x34v + u2acv) * 12.5;
   double uitog3v = (u3x5v + u3x8v + u3x13v + u3x21v + u3x34v + u3acv) * 12.5;
 
   double ditog1v = (d1x5v + d1x8v + d1x13v + d1x21v + d1x34v + d1acv) * 12.5;
   double ditog2v = (d2x5v + d2x8v + d2x13v + d2x21v + d2x34v + d2acv) * 12.5;
   double ditog3v = (d3x5v + d3x8v + d3x13v + d3x21v + d3x34v + d3acv) * 12.5;

   Signal=0; Comment("Не рекомендуется открывать позиции. ЖДИТЕ.");
   if (uitog1v>50  && uitog2v>50  && uitog3v>50)  {Signal=1; Comment("Неплохой момент для открытия позиции BUY");}
   if (ditog1v>50  && ditog2v>50  && ditog3v>50)  {Signal=-1;Comment("Неплохой момент для открытия позиции SELL");}
   if (uitog1v>=75 && uitog2v>=75 && uitog3v>=75) {Signal=2; Comment("УДАЧНЫЙ момент для открытия позиции BUY");}
   if (ditog1v>=75 && ditog2v>=75 && ditog3v>=75) {Signal=-2;Comment("УДАЧНЫЙ момент для открытия позиции SELL");}
   return(0);
   }
//+------------------------------------------------------------------+