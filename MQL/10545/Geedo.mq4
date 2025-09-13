//+------------------------------------------------------------------+
//|                                                        geedo.mq4 |
//+------------------------------------------------------------------+

 int TakeProfit_L = 39;   // Take Profit in points
 int StopLoss_L = 147;    // Stop Loss in points
 extern int TakeProfit_S = 15;   // Take Profit in points
 int StopLoss_S = 6000;    // Stop Loss in points
 extern int TradeTime=18;        // Time to enter the market
 int t1=6;              
 int t2=2;                
 int delta_L=6;         
 int delta_S=21;         
 double lot = 0.01;      // Lot size
 int Orders=1;          // maximal number of positions opened at a time }
 int MaxOpenTime=504;
 int BigLotSize = 1;    // By how much lot size is multiplicated in Big lot
 
 int ticket,total,cnt;
 bool cantrade=true;
 double closeprice;
 double tmp;


int globPos()
// the function calculates big lot size
{
int v1=GlobalVariableGet("globalPosic");
GlobalVariableSet("globalPosic",v1+1);
  return(0);
}

int OpenLong(double volume=0.1)
// the function opens a long position with lot size=volume 
{
  int slippage=10;
  string comment="20/200 expert v2 (Long)";
  color arrow_color=Red;
  int magic=0;

    if (GlobalVariableGet("globalBalans")>AccountBalance()) volume=lot*BigLotSize;
  //  if (GlobalVariableGet("globalBalans")>AccountBalance()) if (AutoLot) LotSize();
   
  ticket=OrderSend(Symbol(),OP_BUY,volume,Ask,slippage,Ask-StopLoss_L*Point,
                      Ask+TakeProfit_L*Point,comment,magic,0,arrow_color);
 
  GlobalVariableSet("globalBalans",AccountBalance());                    
  globPos();
//  if (GlobalVariableGet("globalPosic")>25)
//  {
  GlobalVariableSet("globalPosic",0);
  //  }
 
  if(ticket>0)
  {
    if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
    {
      return(0);
    }
    else
      {
        Print("OpenLong(),OrderSelect() - returned an error : ",GetLastError()); 
        return(-1);
      }   
  }
  else 
  {
    Print("Error opening Buy order : ",GetLastError()); 
    return(-1);
  }
}
  
int OpenShort(double volume=0.1)
// The function opens a short position with lot size=volume
{
  int slippage=10;
  string comment="Gabriel Eze Junior >>>SHORT";
  color arrow_color=Red;
  int magic=0;  

  if (GlobalVariableGet("globalBalans")>AccountBalance()) volume=lot*BigLotSize;
   
  ticket=OrderSend(Symbol(),OP_SELL,volume,Bid,slippage,Bid+StopLoss_S*Point,
                      Bid-TakeProfit_S*Point,comment,magic,0,arrow_color);
  GlobalVariableSet("globalBalans",AccountBalance());
  globPos();
//  if (GlobalVariableGet("globalPosic")>25)
//  {
  GlobalVariableSet("globalPosic",0);
  //  }

  if(ticket>0)
  {
    if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
      {
        return(0);
      }
    else
      {
        Print("OpenShort(),OrderSelect() - returned an error : ",GetLastError()); 
        return(-1);
      }    
  }
  else 
  {
    Print("Error opening Sell order : ",GetLastError()); 
    return(-1);
  }
}

int init()
{
  // control of a variable before using
    if(!GlobalVariableCheck("globalBalans"))
    GlobalVariableSet("globalBalans",AccountBalance());
  if(!GlobalVariableCheck("globalPosic"))
    GlobalVariableSet("globalPosic",0);
  return(0);
}

int deinit()
{   
  return(0);
}

int start()
{
  if((TimeHour(TimeCurrent())>TradeTime)) cantrade=true;  
  // check if there are open orders ...
  total=OrdersTotal();
  if(total<Orders)
  {
    // ... if no open orders, go further
    // check if it's time for trade
    if((TimeHour(TimeCurrent())==TradeTime)&&(cantrade))
    {
      // ... if it is
      if (((Open[t1]-Open[t2])>delta_S*Point)) //if it is
      {
        //condition is fulfilled, enter a short position:
        // check if there is free money for opening a short position
        if(AccountFreeMarginCheck(Symbol(),OP_SELL,lot)<=0 || GetLastError()==134)
        {
          Print("Not enough money");
          return(0);
        }
        OpenShort(lot);
        
        cantrade=false; //prohibit repeated trade until the next bar
        return(0);
      }
      if (((Open[t2]-Open[t1])>delta_L*Point)) //if the price increased by delta
      {
        // condition is fulfilled, enter a long position
        // check if there is free money
        if(AccountFreeMarginCheck(Symbol(),OP_BUY,lot)<=0 || GetLastError()==134)
        {
          Print("Not enough money");
          return(0);
        }
        OpenLong(lot);
        
        cantrade=false;
        return(0);
      }
    }
  }



// block of a trade validity time checking, if MaxOpenTime=0, do not check.
   if(MaxOpenTime>0)
   {
      for(cnt=0;cnt<total;cnt++)
      {
         if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES))
         {
            tmp = (TimeCurrent()-OrderOpenTime())/3600.0;
               if (((NormalizeDouble(tmp,8)-MaxOpenTime)>=0))
               {     
                  RefreshRates();
                  if (OrderType()==OP_BUY)
                     closeprice=Bid;
                  else  
                     closeprice=Ask;          
                  if (OrderClose(OrderTicket(),OrderLots(),closeprice,10,Green))
                  {
                  Print("Forced closing of the trade - ¹",OrderTicket());
                     OrderPrint();
                  }
                  else 
                     Print("OrderClose() in block of a trade validity time checking returned an error - ",GetLastError());        
                  } 
               }
               else 
                  Print("OrderSelect() in block of a trade validity time checking returned an error - ",GetLastError());
         } 
      }     
      return(0);
   }