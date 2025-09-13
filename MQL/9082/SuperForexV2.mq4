//+------------------------------------------------------------------+
//|                                               Super Forex V2.mq4 |
//|                      Copyright © 2009, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

//---- input parameters
extern string Autor = "Rafael Maia de Amorim";
extern string Desc1 = "If you make money with this EA, please help me to start..";
extern string Desc2 = "alertpay or paypal: rdamorim@click21.com.br";
extern string Desc3 = "Thanks a lot";
extern double    TakeProfit=109;
extern double    StopLoss=9;
extern double    Lots=0.1;
extern double    Pos=62;
extern double    Neg=42;
extern int       SET=4;
extern int       TrailingStop = 6;
extern int       NumeroMagico = 1000;
extern int       MaxLots=100;


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
   double  RSI = 0;
   int     OrdersPerSymbol=0;
   int     cnt=0;
   if(AccountFreeMargin()<(1000*Lots))        {Print("Não possui dinheiro suficiente"); return(0);}
   if(Bars<100)                               {Print("-----NO BARS "); return(0);}
   
   Lots = AccountBalance() / 10000;
   if (Lots > MaxLots) {Lots = MaxLots;}
   RSI=iRSI(Symbol(),0,SET,PRICE_CLOSE,0);
   
   //protege lucro
    int Total = OrdersTotal(); // Numero Total de ordens no MetaTrader      
    int NumOrdensBuy  = 0;             // Numero de ordens buy neste consultor       
    int NumOrdensSell = 0;             // Numero de ordens sell neste consultor       
    int Ticket        = 0;             // Numero de ordem requisitada ao servidor       
    int Lucro         = 0;             // Lucro da ordem atual       
    int Atual         = 0;             // Diferenta atual entre SL e o preço 
      
    int TipoOrd       = -1;            // Tipo de ordem a a abri -1 = nenhuma       
    int i;                             // utilizado nos loops   
    double NovoSL  = 0; // Novo valor para o SL          
    double LocalTP = 0; // TP calculado para a ordem
 
         
    double LocalSL = 0; // SL calculado para a ordem          
    double Preco;       // preço para abrir a ordem
 
    // Conta as ordens abertas por este sistema   
    for(i=0;i<Total;i++) 
    { 
    // passa por todas as ordens abertas     
    // seleciono a ordem da lista de ordens      
    // pela localização da mesma na lista     
    OrderSelect(i,SELECT_BY_POS,MODE_TRADES);     // se a ordem pertence a este par e tem este número mágico     
    if ((OrderSymbol()==Symbol()) && (OrderMagicNumber()==NumeroMagico)) 
    {
           // Cacula o lucro da ordem em pips       
           Lucro = OrderProfit() / Point;         
           // Zera variavel para novo SL       
           NovoSL = 0;       
           if (OrderType()==OP_BUY) 
           {         
               NumOrdensBuy++; 
               // Conta se for Buy         
               //se tem trailing entao calculoa novo SL         
               if (TrailingStop>0) 
               {           
               // Calcula quantos pips do preço atual esta o SL           
               // lembre-se na ordem buy o SL esta sempre abaixo do preço           
               Atual = (OrderClosePrice()-OrderStopLoss())/Point;           
               // so usa o trailing se estiver com lucro maior que o proprio trailing           
                // e se o SL esta abaixo do Trailing esperado           
                if ((Lucro>TrailingStop) && (Atual>TrailingStop))             
                NovoSL = OrderClosePrice() - (TrailingStop*Point);         
                }        
            }        
            if (OrderType()==OP_SELL) 
            {
                     NumOrdensSell++;
                      // Conta se for Sell         
                      //se tem trailing entao calculoa novo SL 
                      if (TrailingStop>0) 
                      {           
                                 // Calcula quantos pips do preço atual esta o SL
                                 // lembre-se na ordem sell o SL esta sempre acima do preço
                                 Atual = (OrderStopLoss()-OrderClosePrice())/Point;
                                 // so usa o trailing se estiver com lucro maior que o proprio trailing            
                                 // e se o SL esta abaixo do Trailing esperado           
                                 if ((Lucro>TrailingStop) && (Atual>TrailingStop))              
                                 NovoSL = OrderClosePrice() + (TrailingStop*Point);         
                      }        
             }        
             // Se tiver um novo SL entao programa ele na ordem
             if (NovoSL>0) 
             {
                      // tenta modificar a ordem         
                      //bool Ok = 
                      OrderModify(OrderTicket(), OrderOpenPrice(),NovoSL,OrderTakeProfit(), 0,Red);
                      // verifica se houve algum erro na requisição de modificação          
                      //if (!Ok) Print(" Erro número : ",ErrorDescription(GetLastError()));
              }      
         }    
    }
   
   
   
   //fim   
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
      if(RSI < Neg)
		  {
         OrderSend(Symbol(),OP_BUY,Lots,Ask,0,Ask-(StopLoss*p),Ask+(TakeProfit*p),"Compra  "+CurTime(),NumeroMagico,0,White);
         return(0);
        }
        
      if(RSI > Pos)
        {
         OrderSend(Symbol(),OP_SELL,Lots,Bid,0,Bid+(StopLoss*p),Bid-(TakeProfit*p),"Venda "+CurTime(),NumeroMagico,0,Red);
         return(0);
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
            // Verifica se ocorreu inversão
            if( RSI > Pos )
              {
               OrderClose(OrderTicket(),Lots,Bid,0,White);
              }
           } // if BUY


         if(OrderType()==OP_SELL)
           {
            if ( RSI < Neg )
              {
               OrderClose(OrderTicket(),Lots,Ask,0,Red);
              }
           } //if SELL
           
        } // if(OrderSymbol)
        
     } // for



   return(0);
  }
//+------------------------------------------------------------------+