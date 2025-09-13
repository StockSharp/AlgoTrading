//+------------------------------------------------------------------+
//|                                                  Cashmachine.mq4 |
//|                      Copyright © 2004, MetaQuotes Software Corp. |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Mark Johnson."
#property link      "mark.johnson.uk@hotmail.com"

//+------------------------------------------------------------------+
//| Input parameters                                                 |
//+------------------------------------------------------------------+

extern int TakeProfit=10;

extern string Base="EURUSD";
extern string Hedge="USDCHF";

extern int EMAShortPeriod=8;
extern int EMALongPeriod=21;

//+------------------------------------------------------------------+
//| Lets start trading                                               |
//+------------------------------------------------------------------+

int start()

  {

    double FastEMA=0, SlowEMA=0, RSI=0, CorDiff=0;
     
    if( TotalProfit() > TakeProfit )
  
      {
      CloseOrders();
      }

    if( Day() > 5 || ( Day() == 5 && Hour() > 18 ) )
  
      {
      return(0);
      }
   
    FastEMA = iMA(Base,0,EMAShortPeriod,0,MODE_EMA,PRICE_CLOSE,1);

    SlowEMA = iMA(Base,0,EMALongPeriod,0,MODE_EMA,PRICE_CLOSE,1);

    RSI     = iRSI(Base,0,14,PRICE_CLOSE,0);
    
    CorDiff = Cor(Base,Hedge);
         
    if( OrdersTotal() == 0 )
    
      {
      
        if( FastEMA-SlowEMA > 0 && RSI <= 30 && CorDiff < 0 )
      
          {

            if( IsTradeAllowed() )
        
                {
          
                OrderSend(Base,OP_BUY,LotsOptimized(),MarketInfo(Base,MODE_ASK),3,0,0,Base,255,0,CLR_NONE);
          
                OrderSend(Hedge,OP_BUY,LotsOptimized(),MarketInfo(Hedge,MODE_ASK),3,0,0,Hedge,255,0,CLR_NONE);
          
                }

          }
         
       if( FastEMA-SlowEMA < 0 && RSI >= 70 && CorDiff < 0 )
      
         {

            if( IsTradeAllowed() )
        
                {
          
                OrderSend(Base,OP_SELL,LotsOptimized(),MarketInfo(Base,MODE_BID),3,0,0,Base,255,0,CLR_NONE);

                OrderSend(Hedge,OP_SELL,LotsOptimized(),MarketInfo(Hedge,MODE_BID),3,0,0,Hedge,255,0,CLR_NONE);
          
                }
       
          }
        
      }
            
    return(0);
   
  }

//+------------------------------------------------------------------+
//| CLOSE ORDERS                                                     |
//+------------------------------------------------------------------+

bool CloseOrders()

  {  

  if( IsTradeAllowed() )
  
    {
    
    for(int i = OrdersTotal() - 1; i >= 0; i--)

      {         

      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) )

        {

        OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), MarketInfo(OrderSymbol(), MODE_SPREAD), CLR_NONE);

        }

      }
      
    }

  RefreshRates();

  }   

//+------------------------------------------------------------------+
//| CORRELATION                                                      |
//+------------------------------------------------------------------+

double symboldif(string symbol, int shift)

  {

   return(iClose(symbol, 1440, shift) - 
          iMA(symbol, 1440, Period(), 0, MODE_SMA, PRICE_CLOSE, shift));

  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

double powdif(double val)

  {

   return(MathPow(val, 2));

  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

double u(double val1, double val2)

  {

   return((val1*val2));

  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

double Cor(string base, string hedge)

  {  

   double u1 = 0, l1 = 0, s1 = 0;

   for(int i = Period() - 1; i >= 0; i--)

     {

       u1 += u(symboldif(base, i), symboldif(hedge, i));

       l1 += powdif(symboldif(base, i));

       s1 += powdif(symboldif(hedge, i));

     }

   if(l1*s1 > 0) 
       return(u1 / MathSqrt(l1*s1));

  }

//+------------------------------------------------------------------+
//| TOTAL PROFIT                                                     |
//+------------------------------------------------------------------+

double TotalProfit()

  {   

   double MyCurrentProfit = 0;

   for(int cnt = 0; cnt < OrdersTotal(); cnt++)

     {

       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);

       MyCurrentProfit += (OrderProfit() + OrderSwap());

     }

   return(MyCurrentProfit);

  }

//+------------------------------------------------------------------+
//| LOTS OPTIMIZED                                                   |
//+------------------------------------------------------------------+

double LotsOptimized()
  
  {
  
  double Lots = 0;
  
  Lots = NormalizeDouble( AccountFreeMargin() / 100000, 1 );
    
  if( Lots < 0.1 ) Lots = 0.1;
  
    return( Lots );
  
  }

