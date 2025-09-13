//+------------------------------------------------------------------+
//|                                               STO M5xm15xm30.mq4 |
//|                                            Rafael Maia de Amorim |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Rafael Maia de Amorim"
#property link      "http://www.metaquotes.net"
extern string desc1 = "This EA is running on Zulutrade. start date ";
extern string desc2 = "Free signals use the account rdamorim";
extern string desc3 = "www.zulutrade.com?ref=305935";
extern string desc4 = "Thanks a lot!";




//---- input parameters
extern int       TP=30;
extern int       SL=10;
extern double    Lots=0.1;
extern int       Shift = 3;
int lastorder = 0;
extern int sto1 = 5;
extern int sto2 = 15;
extern int sto3 = 30;
extern int K1 = 5;
extern int K2 = 5;
extern int K3 = 5;
extern int KE = 5;




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
      if(Sinal() == 1 && lastorder != 1)
		  {
         OrderSend(Symbol(),OP_BUY,Lots,Ask,0,Ask-(SL*p),Ask+(TP*p),"Compra  "+CurTime(),0,0,White);
         return(0);
         lastorder = 1;
        }
        
      if(Sinal() == 2  && lastorder != 2)
        {
         OrderSend(Symbol(),OP_SELL,Lots,Bid,0,Bid+(SL*p),Bid-(TP*p),"Venda "+CurTime(),0,0,Red);
         return(0);
         lastorder = 2;
        }
     }
//fecha posições
     for(cnt=OrdersTotal();cnt>=0;cnt--)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if( OrderSymbol()==Symbol() )
        {
         if(OrderType()==OP_BUY)
           {
            //Verifica se ocorreu inversão
            if( SignalExit() == 2 )
              {
               OrderClose(OrderTicket(),Lots,Bid,0,White);
              }
           } // if BUY


         if(OrderType()==OP_SELL)
           {
            if (  SignalExit() == 1)
              {
               OrderClose(OrderTicket(),Lots,Ask,0,Red);
              }
           } //if SELL
           
        } // if(OrderSymbol)
        
     } // for


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
   
   STOM5ValueS0 = iStochastic(Symbol(),sto1,K1,3,3,MODE_SMA,1,MODE_MAIN,0);
   STOM5SignalS0 = iStochastic(Symbol(),sto1,K1,3,3,MODE_SMA,1,MODE_SIGNAL,0);
   STOM5ValueS1  = iStochastic(Symbol(),sto1,K1,3,3,MODE_SMA,1,MODE_MAIN,Shift);
   STOM5SignalS1 = iStochastic(Symbol(),sto1,K1,3,3,MODE_SMA,1,MODE_SIGNAL,Shift);
   
   STOM15Value = iStochastic(Symbol(),sto2,K2,3,3,MODE_SMA,1,MODE_MAIN,0);
   STOM15Signal = iStochastic(Symbol(),sto2,K2,3,3,MODE_SMA,1,MODE_SIGNAL,0);
   STOM30Value = iStochastic(Symbol(),sto3,K3,3,3,MODE_SMA,1,MODE_MAIN,0);
   STOM30Signal = iStochastic(Symbol(),sto3,K3,3,3,MODE_SMA,1,MODE_SIGNAL,0);
   
   if (STOM5ValueS0 > STOM5SignalS0 && STOM5ValueS1 < STOM5SignalS1 && STOM15Value > STOM15Signal && STOM30Value > STOM30Signal && Close[0] > Close[1])
   {
      Direcao = 1;
   }
   else if (STOM5ValueS0 < STOM5SignalS0 && STOM5ValueS1 > STOM5SignalS1 && STOM15Value < STOM15Signal && STOM30Value < STOM30Signal && Close[0] < Close[1])
   {
      Direcao = 2;
   }
   else
   {
      Direcao = 0;
   }
   return (Direcao);
}
int SignalExit()
{
   double STOM5ValueS0, STOM5SignalS0;
   int Direcao = 0; 
   
   STOM5ValueS0 = iStochastic(Symbol(),sto1,KE,3,3,MODE_SMA,1,MODE_MAIN,1);
   STOM5SignalS0 = iStochastic(Symbol(),sto1,KE,3,3,MODE_SMA,1,MODE_SIGNAL,1);
   if (STOM5ValueS0 > STOM5SignalS0)
   {
      Direcao = 1;
   }
   else if (STOM5ValueS0 < STOM5SignalS0)
   {
      Direcao = 2;
   }
   else
   {
      Direcao = 0;
   }
}