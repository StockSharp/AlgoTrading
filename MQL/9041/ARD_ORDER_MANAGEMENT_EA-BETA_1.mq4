//+------------------------------------------------------------------+
//|                                                      CCIW_EA.mq4 |
//|                           Copyright © 2009, ARDIAMSYAH WOHAN ARD |
//|                                      http://www.dailysignal.co.cc|
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, ARDIANSYAH WOHAN ARD"
#property link      "http://www.DAILYSIGNAL.co.cc"
#define BUY  1 
#define SELL 2
#define CLOSE 3
#define MODIFY 4
#define NO_ORDER 0
extern int slippage=4,magic=0,LotsDigit=1;
extern double sl = 50,tp = 100,lotsize = 5,m_sl=20,m_tp=100;
extern string comment = "Placing Order";
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {

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
  // if(iStochastic(NULL,0,5,3,3,MODE_SMA,0,MODE_MAIN,1)>80)  {orders(BUY);}
   //if(iStochastic(NULL,0,5,3,3,MODE_SMA,0,MODE_MAIN,1)<20) {orders(SELL);}
//----
   return(0);
  }
//+------------------------------------------------------------------+
bool open_order(int cmd)
{
   int ticket,err;
   double price,stoploss,takeprofit,m_stoploss,m_takeprofit, lots = NormalizeDouble(AccountFreeMargin()/lotsize/1000,LotsDigit);
   switch(cmd)
   {
    case BUY:
    while(true)
    {
         RefreshRates();
         price = Ask;
         stoploss = Bid-sl*Point;
         takeprofit = Ask+tp*Point;
         ticket = OrderSend(Symbol(),OP_BUY,lots,price,slippage,stoploss,takeprofit,comment,magic,0,CLR_NONE);
         if (ticket<0)
         {
          int ctrl=0;
          err = GetLastError();
          if (err==135) RefreshRates();
          else if(err==129)
           {
             while(true) 
             {
               if(ctrl==0) {int bid2=Bid;int ask = Ask;ctrl=1;}
               RefreshRates();
               if(bid2 != Bid||ask != Ask) {break;}
               }
            }
          else 
          {
           Print("Buying Order Failed == with error ",err); 
           return(false);
           break;
          }
         }
         else
         {
          Print("Buying order .......");
          return(true);
          break;
         }
        }
       break;
    case SELL:
      while(true)
      { 
         RefreshRates();
         price = Bid;
         stoploss = Ask+sl*Point;
         takeprofit = Bid-tp*Point;
         ticket = OrderSend(Symbol(),OP_SELL,lots,price,slippage,stoploss,takeprofit,comment,magic,0,CLR_NONE);
         if (ticket<0)
         {
          ctrl=0;
          err = GetLastError();
          if (err==135) RefreshRates();
          else if(err==129)
           {
             while(true) 
             {
               if(ctrl==0) {bid2=Bid;ask = Ask;ctrl=1;}
               RefreshRates();
               if(bid2 != Bid||ask !=Ask) {break;}
               }
            }
          else 
          {
           Print("Selling Order Failed == with error ",err); 
           return(false);
           break;
          }
         }
         else
         {
          Print("Selling order .......");
          return(true);
          break;
         }
      }
      break;
      case CLOSE:
        int total,i;
        bool cls;
        total = OrdersTotal();
        if (total==0) return(true);
        if (total>0)
         {
           for(int j=0 ; j<3 ;j++)
           {
          for(i=0; i<total; i++)
           {
             if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
                  {
                     if(Symbol() == OrderSymbol())
                        {
                           RefreshRates();
                           if (OrderType() == OP_BUY) price=Bid;
                           if (OrderType() == OP_SELL) price=Ask;
                           cls=OrderClose(OrderTicket(),OrderLots(),price,0,CLR_NONE);
                           if(cls==false){ 
                              ctrl=0;
                              err = GetLastError();
                              if (err==135) { RefreshRates();i++;}
                              else if(err==129)
                                    {
                                       while(true) 
                                       {
                                          if(ctrl==0) {bid2=Bid;ask = Ask;ctrl=1;}
                                          RefreshRates();
                                          if(bid2 != Bid||ask != Ask){i++; break;}
                                       }
                                    }
                               else 
                                 {
                                    Print("Closing Order Failed == with error ",err); 
                                    return(false);  
                                    break;
                                   }
                               }
                              else
                              {
                                 Print("Closing Order = OK");
                                 i--;
                              }
                        } 
                  }
             }
         } 
        return(true) ;
     }
     break;
     case MODIFY:
       total = OrdersTotal();
       if (total==0) return(true);
       if (total>0)
         {
           for(j=0 ; j<3 ;j++)
           {
            for(i=0; i<total; i++)
            {
               if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
                  {
                     if(Symbol() == OrderSymbol())
                        {
                           RefreshRates();
                           price=OrderOpenPrice();
                           if (OrderType() == OP_BUY) 
                           {
                              m_stoploss =Bid-m_sl*Point;
                              m_takeprofit = Ask+m_tp*Point;
                           }
                           if (OrderType() == OP_SELL) price=Ask;
                           {
                              m_stoploss = Ask+m_sl*Point;
                              m_takeprofit =Bid-m_tp*Point;
                           }
                           cls=OrderModify(OrderTicket(),price,m_stoploss,m_takeprofit,0,CLR_NONE);
                           if(cls==false){ 
                              ctrl=0;
                              err = GetLastError();
                              if (err==135) { RefreshRates();i++;}
                              else if(err==129)
                                    {
                                       while(true) 
                                       {
                                          if(ctrl==0) {bid2=Bid;ask = Ask;ctrl=1;}
                                          RefreshRates();
                                          if(bid2 != Bid||ask != Ask){i++; break;}
                                       }
                                    }
                               else 
                                 {
                                    Print("Closing Order Failed == with error ",err); 
                                    return(false);  
                                    break;
                                   }
                               }
                              else
                              {
                                 Print("Closing Order = OK");
                                 i--;
                              }
                        } 
                  }
             }
         } 
        return(true) ;
     }
     break;
     case NO_ORDER:
     Print("NO ORDER JOB");
     return(true);
     break;
     default :
     Print("ERROR cmd NO CODE GENERATED or INVALID CODE INSERTED");
     return(false);
     break; 
   }
  return(true);
}

void orders(int order)
{
 if(order==BUY)
 {
   if(open_order(CLOSE)==true) open_order(BUY);
 }  
 if(order==SELL)
 {
   if(open_order(CLOSE)==true) open_order(SELL);
  }
}