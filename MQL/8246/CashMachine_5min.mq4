//+------------------------------------------------------------------+
//|                                             CashMachine_5min.mq4 |
//|                                             Puncher Poland© 2008 |
//|                                        http://www.terazpolska.pl |
//+------------------------------------------------------------------+
#property copyright "Puncher Poland© 2008: bemowo@tlen.pl"
#property link      "http://www.terazpolska.pl"

//---- Tu zakladasz zysk maks. i maks. strate
extern double hidden_TakeProfit = 60;
extern double hidden_StopLoss = 30;

extern double Lots = 0.2; // tu definiujesz wilekosc transakcji w lotach
extern double target_tp1 = 20; // tu definiujesz minimalny zakladny pierwszy próg zysku
extern double target_tp2 = 35; // drugi próg zysku
extern double target_tp3 = 50; // trzeci próg zysku

// wskaŸnik DeMarker
extern int pidem=0; //Indicator period
extern int pidemu=14; //Period of averaging for indicator calculation

// wskaŸnik Stochastic Oscillator
extern int pisto=0; //Indicator period
extern int pistok=5; //Period(amount of bars) for the calculation of %K line
extern int pistod=3; //Averaging period for the calculation of %D line
extern int istslow=3; //Value of slowdown

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
    int cnt, ticket, total;
//----
    if(Bars < 100)
      {
        Print("bars less than 100");
        return(0);  
      }
//----
    if(hidden_TakeProfit < 10)
      {
        Print("TakeProfit less than 10");
        return(0);  // check TakeProfit
      }
//----
    total  = OrdersTotal(); 
    if(total < 1) 
      {
        
            if(iDeMarker(NULL,pidem,pidemu,1)<0.30&&iDeMarker(NULL,pidem,pidemu,0)>=0.30)
            {
          
                  if(iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_MAIN,1)<20&&iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_MAIN,0)>=20)
                  {
                     
                     ticket = OrderSend(Symbol(), OP_BUY, Lots, Ask, 3, 0, 0, "Cash machine buy", 12345, 0, Green);
                     
                     if(ticket > 0)
                     {
                     if(OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)) 
                     Print("BUY order opened : ",OrderOpenPrice());
                     }
                     else 
                     Print("Error opening BUY order : ", GetLastError()); 
                     return(0);
                     
                    
                 }
         
          
        }
       
            if(iDeMarker(NULL,pidem,pidemu,1)>0.70&&iDeMarker(NULL,pidem,pidemu,0)<=0.70)
            {
               if(iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_MAIN,1)>80&&iStochastic(NULL,pisto,pistok,pistod,istslow,MODE_EMA,0,MODE_MAIN,0)<=80)
               {
                  
                  ticket = OrderSend(Symbol(), OP_SELL, Lots, Bid, 3, 0, 0, "Cash machine sell", 12345, 0, Red);
                  
                  if(ticket > 0)
                  {
                  if(OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES)) 
                  Print("SELL order opened : ", OrderOpenPrice());
                  }
                  else 
                  Print("Error opening SELL order : ",GetLastError()); 
                  return(0);
                  
                 
               }
         
        }
        return(0);
} 
//----
    for(cnt = 0; cnt < total; cnt++)
      {
        OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
        if(OrderType() <= OP_SELL && OrderSymbol() == Symbol())
          {
            if(OrderType() == OP_BUY)   // jesli pozycja dluga jest otwarta to
              {
                     //zabezpieczamy osiagniety zysk lub akceptujemy maksymalna strate ktora dopuszczamy w hidden_StopLoss
                if(Bid <= (OrderOpenPrice()-(hidden_StopLoss*Point)) || Bid >= (OrderOpenPrice()+(hidden_TakeProfit*Point)) )
                      {
                        OrderClose(OrderTicket(),Lots,Bid,3,Green);
                        return(0);
                      }
                     //zabezpieczamy min. osiagniety zysk progu trzeciego
                if(Bid >= OrderOpenPrice()+(target_tp3*Point))
                     {
                        OrderModify(OrderTicket(), OrderOpenPrice(), Bid - (Point * (target_tp3-13)), Ask + (Point * hidden_TakeProfit), 0, Green);
                        return(0); 
                     }
                     //zabezpieczamy min. osiagniety zysk progu drugiego
                if(Bid >= OrderOpenPrice()+(target_tp2*Point) && Bid < OrderOpenPrice()+(target_tp3*Point))
                     {
                        OrderModify(OrderTicket(), OrderOpenPrice(), Bid - (Point * (target_tp2-13)), Ask + (Point * hidden_TakeProfit), 0, Green);
                        return(0); 
                     }
                     //zabezpieczamy min. osiagniety zysk progu pierwszego
                if(Bid >= OrderOpenPrice()+(target_tp1*Point) && Bid < OrderOpenPrice()+(target_tp3*Point) && Bid < OrderOpenPrice()+(target_tp2*Point))
                     {
                        OrderModify(OrderTicket(), OrderOpenPrice(), Bid - (Point * (target_tp1-13)), Ask + (Point * hidden_TakeProfit), 0, Green);
                        return(0); 
                     }
              }
            else // jesli pozycja krotka jest otwarta to
              {
              //zabezpieczamy osiagniety zysk lub akceptujemy maksymalna strate ktora dopuszczamy w hidden_StopLoss
                if(Ask >= (OrderOpenPrice()+ (hidden_StopLoss * Point)) || Ask <= (OrderOpenPrice()-(hidden_TakeProfit*Point)) )
                      {
                        OrderClose(OrderTicket(),Lots,Ask,3,Red);
                        return(0);
                      }
               //zabezpieczamy min. osiagniety zysk progu trzeciego       
                if(Ask <= OrderOpenPrice()-(target_tp3*Point))
                     {
                       OrderModify(OrderTicket(), OrderOpenPrice(), Ask + (Point * (target_tp3+13)), Bid - (Point * hidden_TakeProfit), Red);
                       return(0);                        
                     }
                 //zabezpieczamy min. osiagniety zysk progu drugiego
                if(Ask <= OrderOpenPrice()-(target_tp2*Point) && Ask > OrderOpenPrice()-(target_tp3*Point) )
                     {
                       OrderModify(OrderTicket(), OrderOpenPrice(), Ask + (Point * (target_tp2+13)), Bid - (Point * hidden_TakeProfit), Red);
                       return(0);                        
                     }
                  //zabezpieczamy min. osiagniety zysk progu pierwszego
                if(Ask <= OrderOpenPrice()-(target_tp1*Point) && Ask > OrderOpenPrice()-(target_tp2*Point) && Ask > OrderOpenPrice()-(target_tp3*Point) )
                     {
                       OrderModify(OrderTicket(), OrderOpenPrice(), Ask + (Point * (target_tp1+13)), Bid - (Point * hidden_TakeProfit), Red);
                       return(0);                        
                     }                
                 return(0);
              }
          }
      }
//----
    return(0);
  }
//+------------------------------------------------------------------+ 