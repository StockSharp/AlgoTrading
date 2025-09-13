//+------------------------------------------------------------------+
//|                                                Haiken Ashi-2.mq4 |
//|                      Copyright © 2009, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"


extern int Okrug = 2;  // ќкругление числа с плавающей зап€той до указанной точности

extern int ProcentOtSvobDepo = 1; // процент риска от депозита

extern double MinLot = 0.10; // минимальный лот дл€ торговли, если расчетный меньше минимального лота (зависит от счета)  

extern int MagicNumber = 888; // ѕо этому номеру о4рентируетс€ эксперт,
                              // каждый эксперт открывает, модифицирует, удал€ет свои ордера со своим MagicNumber 
                              
extern double Porog = 0.00050;  // ѕорог не ближе которого можно выставить отложенный ордер

extern double SL = 0.00050; // StopLoss
extern double TP = 0.00100; // TakeProfit
extern double P  = 0.00020; //  оличество пунктов на прорыв


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
  int ticket;
  int err;
  int q=0;
 
double L=iLow(NULL,NULL,1);
double H=iHigh(NULL,NULL,1);
double C=iClose(NULL,NULL,0);
double O=iOpen(NULL,NULL,0);
double Spred=MarketInfo(Symbol(),MODE_SPREAD)*Point;
double SvobDepo=NormalizeDouble(AccountBalance()-AccountMargin(),Okrug);
double Risk=NormalizeDouble((SvobDepo*ProcentOtSvobDepo/100),Okrug);
double Lot=NormalizeDouble(Risk/(SL/0.0001)*0.1,Okrug);
double Proverka1=H-C;
double Proverka2=C-L;

//===================== ќпределени€ лота и риска ===================================

if ( Lot < MinLot )
 {
   Lot=MinLot;
 }
Comment( "\n","ƒопустимый риск ",ProcentOtSvobDepo, "% = ",Risk," от свободных средств ",SvobDepo," в лотах = ",Lot);

//====================== провер€ем ордера на открытие
 for( q=0;q<OrdersTotal();q++)
 {
  if (OrderSelect(q, SELECT_BY_POS, MODE_TRADES) && OrderSymbol()==Symbol())
   {
// провер€ем позиции если есть открытые ордера и сравниваем показатели с индикатором
   if (OrderType()==OP_BUYSTOP)
     {
       return(0); 
     }
   if (OrderType()==OP_SELLSTOP)
     {
       return(0); 
     }
   }  
 }


//======================= условие на  ORDER BUY ===============================
 
if  (Proverka1 >= Porog && Proverka2 >= Porog && O<H)     
  {
    ticket=OrderSend(Symbol(),OP_BUYSTOP,Lot,H+P+Spred,0,H+P-SL+Spred,H+P+TP+Spred,NULL,0,iTime( Symbol(), PERIOD_D1, 0 ) + 86400);
    if (ticket==-1)
      {
        err=GetLastError();
        Print("error(",err,")");
      }
  }
else 
  {
    Comment("\n","Ќе возможно выставить OP_BUYSTOP",
            "\n","÷ена не соответствует условию входа в рынок");
  }     
  
   //================================ условие на  ORDER SELL ==================== 
if  (Proverka1 >= Porog && Proverka2 >= Porog && O>L)  
  {   
    ticket=OrderSend(Symbol(),OP_SELLSTOP,Lot,L-P,0,L-P+SL,L-P-TP,NULL,0,iTime( Symbol(), PERIOD_D1, 0 ) + 86400);
    if (ticket==-1)
      {
         err=GetLastError();
         Print("error(",err,")");
      }
  }
else 
  {
    Comment("\n","Ќе возможно выставить OP_SELLSTOP",
            "\n","÷ена не соответствует условию входа в рынок");
  }     
}
//+------------------------------------------------------------------+