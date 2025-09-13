//+------------------------------------------------------------------+
//|                                            Stoch_TP_TS_V3103.mq4 |
//|                                                                  |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright ""
#property link      ""

int time = 0;
extern int minut = 56;
extern int orders = 2;
extern int trailstop = 15;
extern int start = 95;
extern int stolos = 830;
extern int diff = 175;
extern int sign = 0;
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
//----
   double lot;
   if (AccountBalance() >= orders*diff*1000) lot=100;
   else if (AccountBalance() < orders*diff)
   {  Print("Not enough money!");   }
   else lot=(MathRound(AccountBalance()/(orders*diff))/10);
   
   if ((time != TimeHour(TimeCurrent())) && (minut == TimeMinute(TimeCurrent())))
   {
      time = TimeHour(TimeCurrent());
      double base = iStochastic(Symbol(),Period(),5,3,3,0,0,0,0);
      double signal = iStochastic(Symbol(),Period(),5,3,3,0,0,1,0);
      int ticket;
      int total=OrdersTotal();
      if(total<orders)
      {
         if (((signal - base) >= sign) && (base <= 80) && (base >= 20))
         {
            while(true)
            {
               ticket=OrderSend(Symbol(),OP_SELL,lot,Bid,3,0,0,"SELL",0,0,Red);
               if(ticket<=0) Print("Error = ",GetLastError());
               else { Print("ticket = ",ticket); break; }
               //---- 10 seconds wait
               Sleep(10000);
            }
            PlaySound("alert2.wav");
         }
         else if (((base - signal) > sign) && (base <= 80) && (base >= 20))
         {
            while(true)
            {
               ticket=OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"BUY",0,0,Green);
               if(ticket<=0) Print("Error = ",GetLastError());
               else { Print("ticket = ",ticket); break; }
               //---- 10 seconds wait
               Sleep(10000);
            }
            PlaySound("alert2.wav");
         }
      }
   }
   for (int i = 0; i < OrdersTotal(); i++) 
	{
		OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
		double TSTP = trailstop * Point;
		double STLO = stolos * Point;
		double TKPR = (start + 2 * start) * Point;

		if (OrderComment() == "BUY")
		{
			if ((Bid - OrderOpenPrice()) >= start * Point)
			{
				if (OrderStopLoss() < (Bid - TSTP))
				{
					OrderModify(OrderTicket(), OrderOpenPrice(), Bid - TSTP, 0, Red);
					PlaySound("expert.wav");
				}
			}
			else if (OrderStopLoss() == 0)
			{
			   OrderModify(OrderTicket(), OrderOpenPrice(), OrderOpenPrice() - STLO, OrderOpenPrice() + TKPR,0);
			}
		}
		else if (OrderComment() == "SELL")
		{
			if ((OrderOpenPrice() - Ask) >= start * Point)
			{
				if (OrderStopLoss() > (Ask + TSTP))
				{
					OrderModify(OrderTicket(), OrderOpenPrice(), Ask + TSTP, 0, Red);
					PlaySound("expert.wav");
				}
			}
			else if (OrderStopLoss() == 0)
			{
			   OrderModify(OrderTicket(), OrderOpenPrice(), OrderOpenPrice() + STLO, OrderOpenPrice() - TKPR,0);
			}
		}
   }
//----
   return(0);
  }
//+------------------------------------------------------------------+