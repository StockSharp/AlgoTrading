//+------------------------------------------------------------------+
//|Vortex Indicator System.mq4                                       |
//|This system is based on the Trading Idea section of the article   |
//|"The Vortex Indicator" which appeared in the January 2010 issue   |
//|of "Technical Analysis of Stocks & Commodities".                  |
//+------------------------------------------------------------------+
//#include <stdlib.mqh>

#property copyright "Copyright 2009 under Creative Commons BY-SA License by Neil D. Rosenthal"
#property link      "http://creativecommons.org/licenses/by-sa/3.0/"

#define  MAGIC_NUMBER			1234
#define  SLIPPAGE             	3
#define  COMMENTARY_LONG      	"Vortex Indicator Buy"
#define  COMMENTARY_SHORT     	"Vortex Indicator Sell"
#define  MODE_PLUSVI            0
#define  MODE_MINUSVI           1

//---- input parameters
extern int VI_Length = 14;
extern double   Lots = 0.1; // Size of position in lots

//---- global variables
bool  CheckForExistingOrders; // Turns order checking routine on and off
bool		 LongSetupExists; // Long trade setup flag
bool        ShortSetupExists; // Short trade setup flag
datetime	      CurrentBar; // The bar that just opened
double				 PlusVI1; // VI+ line of the Vortex Indicator at the 1st bar back
double				 PlusVI2; // VI+ line of the Vortex Indicator at the 2nd bar back
double 				MinusVI1; // VI- line of the Vortex Indicator at the 1st bar back
double 				MinusVI2; // VI- line of the Vortex Indicator at the 2nd bar back
double	   		EntryTrigger; // Price at which an entry will be triggered
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
		ObjectDelete("LongEntry");
		ObjectDelete("ShortEntry");
		PlusVI1 = iCustom(CurrencyPair,0,"Vortex Indicator",VI_Length,MODE_PLUSVI,1);
		PlusVI2 = iCustom(CurrencyPair,0,"Vortex Indicator",VI_Length,MODE_PLUSVI,2);
		MinusVI1 = iCustom(CurrencyPair,0,"Vortex Indicator",VI_Length,MODE_MINUSVI,1);
		MinusVI2 = iCustom(CurrencyPair,0,"Vortex Indicator",VI_Length,MODE_MINUSVI,2);
		Print("PlusVI1 = ",PlusVI1,", MinusVI1 = ",MinusVI1);
		Print("PlusVI2 = ",PlusVI2,", MinusVI2 = ",MinusVI2);
		if(PlusVI2 <= MinusVI2 && PlusVI1 > MinusVI1) //Setup condition for Long trade
		{
			if(TypeOfOrder != OP_BUY)//We are either flat or short 
			{
                if(TicketNumber >= 0) //If we are short 
                {
                    OrderClose(TicketNumber,Lots,Ask,SLIPPAGE,Red); //close the existing Short trade
                    TicketNumber = -1;
                    TypeOfOrder = -1; //Now we are flat
                } //end if(TicketNumber >= 0)
                LongSetupExists = true;
                ShortSetupExists = false;
                EntryTrigger = High[1]; //Set the entry trigger to the high of the VI+/VI- crossover bar
            } //end if(TypeOfOrder != OP_BUY)
		} // end if(PlusVI2 <= MinusVI2 && PlusVI1 > MinusVI1)
		else if(MinusVI2 <= PlusVI2 && MinusVI1 > PlusVI1) //Setup condition for a Short trade
		{
			if(TypeOfOrder != OP_SELL) //We are either flat or long 
			{
                if(TicketNumber >= 0) //If we are long 
                {
                    OrderClose(TicketNumber,Lots,Bid,SLIPPAGE,LimeGreen); //close the existing Long trade
                    TicketNumber = -1;
                    TypeOfOrder = -1; //Now we are flat
                } //end if(TicketNumber >= 0)
                ShortSetupExists = true;
                LongSetupExists = false;
                EntryTrigger = Low[1]; //Set the entry trigger to the low of the VI+/VI- crossover bar
            } // end if(TypeOfOrder != OP_SELL)
		} //end else if(MinusVI2 <= PlusVI2 && MinusVI1 > PlusVI1)
        
		if(LongSetupExists)
			Print("LongSetupExists is TRUE");
		else
			Print("LongSetupExists is FALSE");
		if(ShortSetupExists)
			Print("ShortSetupExists is TRUE");
		else
			Print("ShortSetupExists is FALSE");
	} // end if(NewBar(CurrentBar))

	if(LongSetupExists) //Long trade Setup
	{
		ObjectDelete("LongEntry");
		ObjectCreate("LongEntry",OBJ_HLINE,0,Time[0],EntryTrigger); //Horizontal line to show you where your entry point is located
		ObjectSet("LongEntry",OBJPROP_COLOR,LimeGreen);
		if(Bid > EntryTrigger) //Long trade Trigger
		{
			Wait();
			RefreshRates();
			TicketNumber = OrderSend(CurrencyPair,OP_BUY,Lots,Ask,SLIPPAGE,
										0,0,COMMENTARY_LONG,MAGIC_NUMBER,0,LimeGreen);
			if(TicketNumber >= 0)
			{
				ObjectDelete("LongEntry");
				TypeOfOrder = OP_BUY; //Now we are long
				LongSetupExists = false; //This prevents additional Buy orders from being executed
			} // end if(TicketNumber >= 0)
			else //OrderSend failed
			{
				ObjectDelete("LongEntry");
				TypeOfOrder = -1; //We are flat instead
				LongSetupExists = false;
			} //end else OrderSend failed
		} //end if(Bid >= EntryTrigger) 
	} //end if(LongSetupExists)
	else if(ShortSetupExists) //Short trade Setup
	{
		ObjectDelete("ShortEntry");
		ObjectCreate("ShortEntry",OBJ_HLINE,0,Time[0],EntryTrigger); //Horizontal line to show you where your entry point is located
		ObjectSet("ShortEntry",OBJPROP_COLOR,Red);
		if(Ask < EntryTrigger) //Short trade Trigger
		{
			Wait();
			RefreshRates();
			TicketNumber = OrderSend(CurrencyPair,OP_SELL,Lots,Bid,SLIPPAGE,
										0,0,COMMENTARY_SHORT,MAGIC_NUMBER,0,Red);
			if(TicketNumber >= 0)
			{
				ObjectDelete("ShortEntry");
				TypeOfOrder = OP_SELL; //Now we are short 
				ShortSetupExists = false; //This prevents additional Sell orders from being executed
			} // end if(TicketNumber >= 0)
			else //OrderSend failed, usually due to improper stop loss
			{
				ObjectDelete("ShortEntry");
				TypeOfOrder = -1; //We are flat instead
				ShortSetupExists = false;
			} //end else OrderSend failed
		} //end if(Ask <= EntryTrigger)
	} // end if(ShortSetupExists)
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


