//+------------------------------------------------------------------+
//|                                                Dual StopLoss.mq4 |
//|                                       Copyright 2021, RayanTech  |
//|                                      http://t.me/MegaSYS_Support |
//+------------------------------------------------------------------+
#property copyright " Copyright 2021, RayanTech "
#property link      "http://t.me/MegaSYS_Support"
#property version   "1.00"
#property strict

extern double WhenToClose			= 10;			// Points to Close Order Just Before StopLoss Hit

double STOPLEVEL	= (int)MarketInfo(Symbol(), MODE_STOPLEVEL);

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
//---

//---
return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
//---

}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
//---
	CloseBeforeStopLoss();
}
//+------------------------------------------------------------------+


//+------------------------------------------------------------------+
//| CLOSE ORDER BEFORE STOPLOSS						 		 											 |
//+------------------------------------------------------------------+

void CloseBeforeStopLoss()
{
	for (int i= OrdersTotal()-1 ; i>= 0 ; i--)
		{		
			if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))	//-------------> Expert searches all open orders in the pool
				{
					if (OrderMagicNumber()!= -1) //----------------------------> If you would like this EA acts on special orders, replace this section with >> if (OrderMagicNumber()== "Your desired Number")
						{
							if (OrderStopLoss()!= 0)	//---------------------------> Orders WITHOUT Stop-loss will be ignored.
								{
									if (OrderType()== OP_BUY) //-----------------------> BUY Orders will be processed
										{						
											if (MarketInfo(OrderSymbol(), MODE_BID)- OrderStopLoss() <= getPoint(WhenToClose + STOPLEVEL))
												{
													//---
													bool BUYCLOSE = OrderClose(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(),MODE_BID),	0, clrOrange);

													if(!BUYCLOSE)
														{
															Print("Error in CloseBeforeStopLoss. Error code=", GetLastError());
															return;
														}
													
													if(BUYCLOSE)break;
												}
										}
								
									if (OrderType()	== OP_SELL) //---------------------> SELL Orders will be processed
										{						
											if (OrderStopLoss() - MarketInfo(OrderSymbol(),MODE_ASK)	<= getPoint(WhenToClose + STOPLEVEL))
												{
													//---
													bool SELLCLOSE = OrderClose(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK), 0, clrOrange);

													if(!SELLCLOSE)
														{
															Print("Error in CloseBeforeStopLoss. Error code=", GetLastError());
															return;
														}
													
													if(SELLCLOSE)break;
												}
										}
								}
						}
				}
		}
}
//+------------------------------------------------------------------+





//+------------------------------------------------------------------+
//| Point Value Converter Function 			                 						 |
//+------------------------------------------------------------------+
double	getPoint(double value)
{
	return (NormalizeDouble (value * Point , Digits));
}
//+------------------------------------------------------------------+