//+------------------------------------------------------------------+
//|                                                   ADX Simple.mq4 |
//|                                            Rafael Maia de Amorim |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Rafael Maia de Amorim"
#property link      "http://www.metaquotes.net"

//---- input parameters
extern string Autor = "Rafael Maia de Amorim";
extern string Desc1 = "If you make money with this EA, please help me with any value";
extern string Desc2 = "alertpay or paypal: rdamorim@click21.com.br";
extern string Desc3 = "Thanks a lot";
extern double    Lots=0.01;
extern int ADX = 25;
extern int NumeroMagico = 100;
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
//----
   double  p=Point;
   int     OrdersPerSymbol=0;
   int     cnt=0;
   if(AccountFreeMargin()<(1000*Lots))        {Print("Não possui dinheiro suficiente"); return(0);}
   if(Bars<100)                               {Print("-----NO BARS "); return(0);}
   OrdersPerSymbol=0;
   Lots = AccountBalance() / 10000;
   
     for(cnt=OrdersTotal();cnt>=0;cnt--)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if( OrderSymbol()==Symbol() )
        {
         OrdersPerSymbol++;
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
            // Verifica se ele atingiu o lucro desejado
            if( SinalExit() == 2)
              {
               OrderClose(OrderTicket(),Lots,Bid,0,White);
               //Fechado = 1;
              }
           } // if BUY


         if(OrderType()==OP_SELL)
           {
            // did we make our desired SELL profit?
            if ( SinalExit() == 1 )
              {
               OrderClose(OrderTicket(),Lots,Ask,0,Red);
               //Fechado = 1;
              }
           } //if SELL
           
        } // if(OrderSymbol)
        
     } // for
   //Abre as ordens apenas se não possuir nenhuma ordem aberta por Simbolo
   if(OrdersPerSymbol<1)
     {
      if(Sinal() == 1)
		  {
         OrderSend(Symbol(),OP_BUY,Lots,Ask,0,0,0,"Buy  "+CurTime(),NumeroMagico,0,White);
         return(0);
        }
        
      if(Sinal() == 2)
        {
         OrderSend(Symbol(),OP_SELL,Lots,Bid,0,0,0,"Sell "+CurTime(),NumeroMagico,0,Red);
         return(0);
        }
     }



 
//----
   return(0);
  }
  
  //+------------------------------------------------------------------+
//| Gerador de sinais                                          |
//+------------------------------------------------------------------+  
int Sinal()
{
 //set ADX Trend
    int Direcao = 0; // 1 indica compra , 2 indica venda
   double ADXMinus = 0;
   double ADXPlus = 0;
   int ADXTrend = 0;  //1 = buy, 2 = sell

   ADXMinus = iADX(Symbol(),0,ADX,PRICE_LOW,MODE_MINUSDI,0);
   ADXPlus = iADX(Symbol(),0,ADX,PRICE_LOW,MODE_PLUSDI,0);
   
   //ADX Main Value
   double ADXMain = 0;
   ADXMain = iADX(Symbol(),0,ADX,PRICE_LOW,MODE_MAIN,0);
   double ADXMain1 = 0;
   ADXMain1 = iADX(Symbol(),0,ADX,PRICE_LOW,MODE_MAIN,1);
   
   //end ADX Main Value
   
   if (ADXMinus < ADXPlus && ADXMain > ADXMain1)
   {
      Direcao = 1;
   }
   else if (ADXMinus > ADXPlus && ADXMain > ADXMain1) 
   {
      Direcao = 2;
   }
   //end ADX
   


   return (Direcao);

}
int SinalExit()
{
 //set ADX Trend
    int Direcao = 0; // 1 indica compra , 2 indica venda
   double ADXMinus = 0;
   double ADXPlus = 0;
   int ADXTrend = 0;  //1 = buy, 2 = sell

   ADXMinus = iADX(Symbol(),0,ADX,PRICE_LOW,MODE_MINUSDI,0);
   ADXPlus = iADX(Symbol(),0,ADX,PRICE_LOW,MODE_PLUSDI,0);
   
   
   //end ADX Main Value
   
   if (ADXMinus < ADXPlus)
   {
      Direcao = 1;
   }
   else if (ADXMinus > ADXPlus) 
   {
      Direcao = 2;
   }
   //end ADX
   


   return (Direcao);

}
//+------------------------------------------------------------------+