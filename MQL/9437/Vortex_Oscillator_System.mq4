//+------------------------------------------------------------------+
//|Vortex Oscillator System.mq4                                      |
//|This system is based on the Vortex Oscillator indicator           |
//|                                                                  |
//+------------------------------------------------------------------+
//#include <stdlib.mqh>

#property copyright "Copyright 2009 under Creative Commons BY-SA License by Neil D. Rosenthal and Michael Moses"
#property link      "http://creativecommons.org/licenses/by-sa/3.0/"

#define  MAGIC_NUMBER			2345
#define  SLIPPAGE             	3
#define  COMMENTARY_LONG      	"Vortex Oscillator Buy"
#define  COMMENTARY_SHORT     	"Vortex Oscillator Sell"
#define  MODE_VO                0

//---- input parameters
extern int	   		   VI_Length = 14;
extern double 				Lots = 0.1;   //lot size for trades
extern bool 	Use_Buy_StopLoss = false; //tells program to use the Buy Stop or not, default is FALSE
extern bool   Use_Buy_TakeProfit = false; //tells program to use Buy TakeProfit or not, default is FALSE
extern bool    Use_Sell_StopLoss = false; //tells program to use the Sell Stop or not, default is FALSE
extern bool  Use_Sell_TakeProfit = false; //tells program to use Sell TakeProfit or not, default is FALSE
extern double 			  VO_Buy = -0.75; //VO value at which a Buy order is initiated
extern double    VO_Buy_StopLoss = -1.00; //if Use_Buy_StopLoss = true, if this VO value is hit the Buy order is closed
extern double  VO_Buy_TakeProfit = 0.00;  //if Use_Buy_TakeProfit = true, if this VO value is hit the Buy order is closed
extern double 			 VO_Sell = 0.75;  //VO value at which a Sell order is initiated
extern double 	VO_Sell_StopLoss = 1.00;  //if Use_Sell_StopLoss = true, if this VO value is hit the Sell order is closed
extern double VO_Sell_TakeProfit = 0.00;  //if Use_Sell_TakeProfit = true, if this VO value is hit the Sell order is closed

//---- global variables
bool  CheckForExistingOrders; // Turns order checking routine on and off
bool          FractionalPips; // Does your broker use fractional pip pricing?
bool 		 LongSetupExists; // Flag for Long setups
bool 		ShortSetupExists; // Flag for Short setups
datetime	      CurrentBar; // The bar that just opened
double             VortexOsc; // Value of the Vortex Oscillator
int             TicketNumber;
int              TypeOfOrder; // Buy or Sell
string 		    CurrencyPair; // The current chart's symbol

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
{
    //----
    CheckForExistingOrders = true;
    FractionalPips = false;
	CurrencyPair = Symbol();
	CurrentBar = Time[0];
	TicketNumber = -1;
	TypeOfOrder = -1;
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
	if(CheckForExistingOrders) //This retrieves an existing order's info in case MT4 or your computer crashes
	{
        int TotalOfOrders = OrdersTotal();
        if(TotalOfOrders > 0)
        {
			for(int i = TotalOfOrders - 1; i >= 0; i--) //Loop through all the orders 
			{
				if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
				{
					if(MAGIC_NUMBER == OrderMagicNumber() && CurrencyPair == OrderSymbol()) //An order exists for this pair
					{    
						TicketNumber = OrderTicket();				
						TypeOfOrder = OrderType();
						Lots = OrderLots();
						break; // Break out of the for loop once the right order is found
					} // end if(MAGIC_NUMBER == OrderMagicNumber() && CurrencyPair == OrderSymbol())
				} // end if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
			} //end for(int i = TotalOfOrders - 1; i >= 0; i--)
        } //end if(TotalOfOrders >= 1)
		CheckForExistingOrders = false; //Done - turn off this routine
    } //end if(CheckForExistingOrders)                		
	
	if(NewBar(CurrentBar)) //A new bar has just opened
	{
		CurrentBar = Time[0];
		VortexOsc = iCustom(CurrencyPair,0,"Vortex Oscillator",VI_Length,MODE_VO,1); //Value from the just-closed bar
		Print("VortexOsc = ", VortexOsc);
		if(Use_Buy_StopLoss)
		{
			if(VortexOsc <= VO_Buy && VortexOsc > VO_Buy_StopLoss) //Two conditions must be met if using the stop
			{
				LongSetupExists = true;
				ShortSetupExists = false;
			} //if(VortexOsc <= VO_Buy && VortexOsc > VO_Buy_StopLoss)
		} //end if(Use_Buy_StopLoss)
		else //Use_Buy_StopLoss is false
		{
			if(VortexOsc <= VO_Buy) //Only one condition must be met if not using the stop
			{
				LongSetupExists = true;
				ShortSetupExists = false;
			} //end if(VortexOsc <= VO_Buy)
		} // end else Use_Buy_StopLoss is false
		if(Use_Sell_StopLoss)
		{
			if(VortexOsc >= VO_Sell && VortexOsc < VO_Sell_StopLoss) //Two conditions must be met if using the stop
			{
				ShortSetupExists = true;
				LongSetupExists = false;
			} //end if(VortexOsc >= VO_Sell && VortexOsc < VO_Sell_StopLossLoss)
		} // end if(Use_Sell_StopLoss)
		else //Use_Sell_StopLoss is false
		{
			if(VortexOsc >= VO_Sell) //Only one condition must be met if not using the stop
			{
				ShortSetupExists = true;
				LongSetupExists = false;			
			} //end if(VortexOsc >= VO_Sell)
		} // end else Use_Sell_StopLoss is false
		if(VortexOsc >= VO_Buy && VortexOsc <= VO_Sell) //When the oscillator is between the extremes...
		{
			LongSetupExists = false; //...set both flags to false
			ShortSetupExists = false;
		} //end if(VortexOsc >= VO_Buy && VortexOsc <= VO_Sell)

		if(LongSetupExists)
			Print("LongSetupExists is TRUE");
		else
			Print("LongSetupExists is FALSE");
		if(ShortSetupExists)
			Print("ShortSetupExists is TRUE");
		else
			Print("ShortSetupExists is FALSE");
		
		if(LongSetupExists) 
		{
			if(TypeOfOrder != OP_BUY)//We are either flat or short 
			{
                if(TicketNumber >= 0) //If we are short 
                    OrderClose(TicketNumber,Lots,Ask,SLIPPAGE,Red); //close the existing Short trade
				//Open a Long trade
				Wait();
				RefreshRates();
				TicketNumber = OrderSend(CurrencyPair,OP_BUY,Lots,Ask,SLIPPAGE,
										0,0,COMMENTARY_LONG,MAGIC_NUMBER,0,LimeGreen); 			
				if(TicketNumber >= 0)
				{
					TypeOfOrder = OP_BUY; //Now we are long
					LongSetupExists = false; //This prevents additional Buy orders from being executed
				} // end if(TicketNumber >= 0)
				else //OrderSend failed
				{
					TypeOfOrder = -1; //We are flat instead
					LongSetupExists = false;
				} //end else OrderSend failed
            } //end if(TypeOfOrder != OP_BUY)
		} // end if(LongSetupExists)
		else if(ShortSetupExists) 
		{
			if(TypeOfOrder != OP_SELL) //We are either flat or long 
			{
                if(TicketNumber >= 0) //If we are long 
                    OrderClose(TicketNumber,Lots,Bid,SLIPPAGE,LimeGreen); //close the existing Long trade
				//Open a Short trade
				Wait();
				RefreshRates();
				TicketNumber = OrderSend(CurrencyPair,OP_SELL,Lots,Bid,SLIPPAGE,
										0,0,COMMENTARY_SHORT,MAGIC_NUMBER,0,Red); 
				if(TicketNumber >= 0)
				{
					TypeOfOrder = OP_SELL; //Now we are short
					ShortSetupExists = false; //This prevents additional Sell orders from being executed
				} // end if(TicketNumber >= 0)
				else //OrderSend failed
				{
					TypeOfOrder = -1; //We are flat instead
					ShortSetupExists = false;
				} //end else OrderSend failed
            } // end if(TypeOfOrder != OP_SELL)
		} //end else if(ShortSetupExists)        
	} // end if(NewBar(CurrentBar))
    
    //Monitor the open position
	if(TypeOfOrder == OP_BUY) //Long trade exists
	{
		if(Use_Buy_StopLoss) 
		{
			if(VortexOsc <= VO_Buy_StopLoss) //Stopped out!
			{
				OrderClose(TicketNumber,Lots,Bid,SLIPPAGE,LimeGreen); //Close the existing Long trade
				TicketNumber = -1;
				TypeOfOrder = -1; //Now we are flat
			} //end if(VortexOsc <= VO_Buy_StopLoss) 
		} //end if(Use_Buy_StopLoss) 
		if(Use_Buy_TakeProfit)
		{
			if(VortexOsc >= VO_Buy_TakeProfit) //Profit target achieved!
			{
				OrderClose(TicketNumber,Lots,Bid,SLIPPAGE,LimeGreen); //Close the existing Long trade
				TicketNumber = -1;
				TypeOfOrder = -1; //Now we are flat				
			} //end if(VortexOsc >= VO_Buy_TakeProfit)
		} //end if(Use_Buy_TakeProfit)
	} //end if(TypeOfOrder == OP_BUY) 
	else if(TypeOfOrder == OP_SELL) //Short trade exists
	{
		if(Use_Sell_StopLoss) 
		{
			if(VortexOsc >= VO_Sell_StopLoss) //Stopped out!
			{
				OrderClose(TicketNumber,Lots,Ask,SLIPPAGE,Red); //Close the existing Short trade
				TicketNumber = -1;
				TypeOfOrder = -1;  //Now we are flat
			} //end if(VortexOsc <= VO_Buy_StopLoss) 
		} //end if(Use_Sell_StopLoss) 
		if(Use_Sell_TakeProfit)
		{
			if(VortexOsc <= VO_Sell_TakeProfit) //Profit target achieved!
			{
				OrderClose(TicketNumber,Lots,Ask,SLIPPAGE,Red); //Close the existing Short trade
				TicketNumber = -1;
				TypeOfOrder = -1; //Now we are flat				
			} //end if(VortexOsc >= VO_Sell_TakeProfit)
		} //end if(Use_Sell_TakeProfit)
	} // end else if(TypeOfOrder == OP_SELL)
    //----
    return(0);
}
//+-------------------------------------------------------------------------+

//+---------------------Custom Functions------------------------------------+

//+-------------------------------------------------------------------------+
//|                         New Bar                                         +   
//+-------------------------------------------------------------------------+   
// This function return the value true if the current bar/candle was just formed
bool NewBar(datetime PreviousBar)
{
    if(PreviousBar < Time[0])
        return(true);
    else
        return(false);
}

//+-------------------------------------------------------------------------+
//+                               Wait                                      +
//+-------------------------------------------------------------------------+
void Wait()
{
    while(IsTradeContextBusy()) 
        Sleep(50);
} 


