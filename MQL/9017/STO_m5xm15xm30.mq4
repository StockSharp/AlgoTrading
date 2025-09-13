//+------------------------------------------------------------------+
//|                                                   Hibrido v1.mq4 |
//|                                            Rafael Maia de Amorim |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Rafael Maia de Amorim"
#property link      "http://www.metaquotes.net"

//---- input parameters
extern int       TP=30;
extern int       SL=10;
extern double    Lots=0.1;
extern int       Shift = 3;
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
   double  p=Point;
   int     OrdersPerSymbol=0;
   int     cnt=0;
   if(AccountFreeMargin()<(1000*Lots))        {Print("Não possui dinheiro suficiente"); return(0);}
   if(Bars<100)                               {Print("-----NO BARS "); return(0);}
   
   OrdersPerSymbol=0;
   for(cnt=OrdersTotal();cnt>=0;cnt--)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if( OrderSymbol()==Symbol() )
        {
         OrdersPerSymbol++;
        }
     }
   //Abre as ordens apenas se não possuir nenhuma ordem aberta por Simbolo
   if(OrdersPerSymbol<1)
     {
      if(Sinal() == 1)
		  {
         OrderSend(Symbol(),OP_BUY,Lots,Ask,0,Ask-(SL*p),Ask+(TP*p),"Compra  "+CurTime(),0,0,White);
         return(0);
         Sleep(30000);
        }
        
      if(Sinal() == 2)
        {
         OrderSend(Symbol(),OP_SELL,Lots,Bid,0,Bid+(SL*p),Bid-(TP*p),"Venda "+CurTime(),0,0,Red);
         return(0);
         Sleep(30000);
        }
     }



   return(0);
  }
  
//+------------------------------------------------------------------+
//| Gerador de sinais                                          |
//+------------------------------------------------------------------+  
int Sinal()
{
   double STOM5ValueS0, STOM5SignalS0, STOM5ValueS1, STOM5SignalS1;
   double STOM15Value, STOM15Signal, STOM30Value, STOM30Signal;
   
   int Direcao = 0; // 1 indica compra , 2 indica venda
   
   STOM5ValueS0 = iStochastic(Symbol(),5,5,3,3,MODE_SMA,1,MODE_MAIN,0);
   STOM5SignalS0 = iStochastic(Symbol(),5,5,3,3,MODE_SMA,1,MODE_SIGNAL,0);
   STOM5ValueS1  = iStochastic(Symbol(),5,5,3,3,MODE_SMA,1,MODE_MAIN,Shift);
   STOM5SignalS1 = iStochastic(Symbol(),5,5,3,3,MODE_SMA,1,MODE_SIGNAL,Shift);
   
   STOM15Value = iStochastic(Symbol(),15,5,3,3,MODE_SMA,1,MODE_MAIN,0);
   STOM15Signal = iStochastic(Symbol(),15,5,3,3,MODE_SMA,1,MODE_SIGNAL,0);
   STOM30Value = iStochastic(Symbol(),30,5,3,3,MODE_SMA,1,MODE_MAIN,0);
   STOM30Signal = iStochastic(Symbol(),30,5,3,3,MODE_SMA,1,MODE_SIGNAL,0);
   
   if (STOM5ValueS0 > STOM5SignalS0 && STOM5ValueS1 < STOM5SignalS1 && STOM15Value > STOM15Signal && STOM30Value > STOM30Signal)
   {
      Direcao = 1;
   }
   else if (STOM5ValueS0 < STOM5SignalS0 && STOM5ValueS1 > STOM5SignalS1 && STOM15Value < STOM15Signal && STOM30Value < STOM30Signal)
   {
      Direcao = 2;
   }
   else
   {
      Direcao = 0;
   }
   return (Direcao);

}