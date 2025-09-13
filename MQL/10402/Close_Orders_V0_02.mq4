//+------------------------------------------------------------------+
//|                                                CloseOrders.mq4   |
//|                                                                  |
//|                                                                  |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|History:                                                           
//|Ver 0.01 2009.5.26                                                
//|Ver 0.02 2011.11.06  
//|   1. Added time control                                            
//|   2. changed some names for convenient use                                                
//+------------------------------------------------------------------+

#property copyright "Robbie Ruan Ver 0.02 2011.11.06"
#property link      "robbie.ruan@gmail.com"
extern bool    CloseAllSymbols = true; // if CloseAllSymbols = false, just close the symbol you are applying your EA; if CloseAllSymbols = true, it will close all symbol satisfying the upper condition.
extern bool    CloseOpenLongOrders = true; // if close open positions
extern bool    CloseOpenShortOrders = true; // if close open positions
extern bool    ClosePendingLongOrders = true;
extern bool    ClosePendingShortOrders = true;
extern string  Note1 = "If JustCloseSpecificMagicNumber is 0, close all MagicNumber orders;if JustCloseSpecificMagicNumber is not 0, just close the specific MagicNumber orders";
extern int     JustCloseSpecificMagicNumber = 0;
extern string  Note2 = "Just close orders whose open prices are with this range";
extern bool    JustCloseOrdersWithinTheRange = false;
extern double  CloseRangeHigh = 0;           // close orders within the specified region, both should be above 0 if JustCloseOrdersWithinTheRange = false
extern double  CloseRangeLow = 0;

extern bool    EnableCloseTimeControl = false;
extern double  StartCloseTime  = 2.00;
extern double  StopCloseTime   = 2.30;
extern string  Note3 = "Time set 0.00~23.59";

string GridName = "Grid";       // identifies the grid. allows for several co-existing grids
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
      //---- 
      #property show_inputs                  // shows the parameters  
      //----
      //   GridName = StringConcatenate( "Grid", Symbol() );
      return(0);
  }
  
//+------------------------------------------------------------------------+
//| closes  orders                                                      |
//+------------------------------------------------------------------------+
void CloseOrders()
{
  int total = OrdersTotal();
  for(int i=total-1;i>=0;i--)
  {
    OrderSelect(i, SELECT_BY_POS);
    int type    = OrderType();
    bool result = false;

    if ( ( OrderSymbol()==Symbol() || CloseAllSymbols == true ) && ( OrderMagicNumber() == JustCloseSpecificMagicNumber || JustCloseSpecificMagicNumber == 0 ) ) // only look if mygrid and symbol...
    {
    
      // close orders within the specified region.
      if ( (!JustCloseOrdersWithinTheRange && (CloseRangeLow==0) && (CloseRangeHigh ==0)) || ( JustCloseOrdersWithinTheRange && (CloseRangeLow >0) && (CloseRangeHigh >0) && (OrderOpenPrice()<CloseRangeHigh) && (OrderOpenPrice()>CloseRangeLow) ) )
      {
         // close pending Long orders
         if ( ClosePendingLongOrders == true )
         {
            switch(type)
            {
               //Close pending orders
               case OP_BUYLIMIT  : result = OrderDelete( OrderTicket() ); break;
               case OP_BUYSTOP   : result = OrderDelete( OrderTicket() ); break;
            }  
         }
         
         // close pending Short orders
         if ( ClosePendingShortOrders == true )
         {
            switch(type)
            {
               //Close pending orders
               case OP_SELLLIMIT : result = OrderDelete( OrderTicket() ); break;
               case OP_SELLSTOP  : result = OrderDelete( OrderTicket() ); break;
            }
         }
                 
         // close open Long orders
         if ( CloseOpenLongOrders == true )
         {
            switch(type)
            {
               //Close opened long positions
               case OP_BUY       : result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID), 5, Red );  break;
            }
         }
         // close open Short orders
         if (CloseOpenShortOrders == true )
         {
            switch(type)
            {
               //Close opened short positions
               case OP_SELL      : result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK), 5, Red );  break;   
            }
         }
         
      } // if region control end here
      
    } // if symgol and magicnumber control end here
               
    
    if(result == false)
    {
//     Alert("Order " , OrderTicket() , " failed to close. Error:" , GetLastError() );
//     Sleep(3000);
    }  
    
    
  } //for end here
  
  return;
}
 
//+------------------------------------------------------------------+
//| script program start function                                    |
//+------------------------------------------------------------------+
int start()
  {
//---- 
   int i;
      
   int    CurrentHour = Hour();                  
   double CurrentMinute = Minute();              
   double CurrentTime = CurrentHour + CurrentMinute/100;
   
   //Print("CurrentTime:",CurrentTime);

   if ( EnableCloseTimeControl == true )
   {
      if ( StartCloseTime < StopCloseTime )
      {
         //example, execute close operation in 19->20->21->22
         if (CurrentTime < StartCloseTime || CurrentTime >= StopCloseTime )
         {
            return(0);
         }
      }
      
      else if ( StartCloseTime > StopCloseTime )
      {
         //example, execute close operation in 22->23->0->1
         if ( CurrentTime < StartCloseTime && CurrentTime >= StopCloseTime )
         {
            return(0);
         }
         
      }
      
      else if ( StartCloseTime == StopCloseTime )
      {
         return(0);
      }
   }

   CloseOrders();
  
   return(0);
  }
//+------------------------------------------------------------------+

