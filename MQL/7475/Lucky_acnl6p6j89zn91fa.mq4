//+------------------------------------------------------------------+
//|                                                                  |
//|                                                                  |
//|                                                                  |
//+------------------------------------------------------------------+

double a, b; 
bool first=true; 
extern int Shift = 3; 
extern int Limit = 18;




//---------------------------------------------------------------------------- 
int start()
{ 
  if (first) 
   {
      a=Ask; 
      b=Bid; 
      first=false; 
      return(0);
   } 
 
  if (Ask-a>=Shift*Point) 
    {
    OrderSend(Symbol(),OP_SELL,GetLots(),Bid,3,0,0,"",0,0,CLR_NONE);
    } 
  if (b-Bid>=Shift*Point) 
    {
    OrderSend(Symbol(),OP_BUY,GetLots(),Ask,3,0,0,"",0,0,CLR_NONE);
    } 

  a=Ask;  
  b=Bid; 
  
  CloseAll(); 
return(0);} 
//----------------------------------------------------------------------------- 
void CloseAll() 
{ 
  for (int cnt = OrdersTotal()-1 ; cnt >= 0; cnt--) 
  { 
    OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES); 
    if (OrderSymbol() == Symbol()) 
    { 
      if ((OrderProfit()>0)) 
      { 
        if(OrderType()==OP_BUY)  OrderClose(OrderTicket(),OrderLots(),Bid,3,CLR_NONE); 
        if(OrderType()==OP_SELL) OrderClose(OrderTicket(),OrderLots(),Ask,3,CLR_NONE); 
      } 
      else 
      { 
        if((OrderType()==OP_BUY)  && (((OrderOpenPrice()-Ask)/Point) > Limit)) 
          OrderClose(OrderTicket(),OrderLots(),Bid,3,CLR_NONE); 
        if((OrderType()==OP_SELL) && (((Bid-OrderOpenPrice())/Point) > Limit)) 
          OrderClose(OrderTicket(),OrderLots(),Ask,3,CLR_NONE); 
      } 
    } 
  } 
} 



//-------------------------------------------------------------------------- 



double GetLots() 
{ 

return (NormalizeDouble(AccountFreeMargin()/10000,1)); 
  
} 
//------------------------------------------------------------------------- 


