//+------------------------------------------------------------------+
//|                                                RideAlligator.mq4 |
//|            Copyright © 2011 http://www.mql4.com/ru/users/rustein |
//-------------------------------------------------------------------+
//-------------------------------------------------------------------+
extern int    AlligatorPeriod = 5;
//+------------------------------------------------------------------+
extern int    AlliggatorMODE  = 3; // 0=SMA,1=EMA,2=SSMA,3=LWMA
//+------------------------------------------------------------------+
extern double RiskFactor      = 0.5;
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|            DO NOT MODIFY ANYTHING BELOW THIS LINE!!!             |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
int start(){int wtf;double Lots;double MinLot=MarketInfo(Symbol(),MODE_MINLOT);
if(MinLot<=0.01)wtf=2;if(MinLot>=0.1)wtf=1;double MMLot=NormalizeDouble(AccountBalance()*RiskFactor/100.00/100.00,wtf);
if(MMLot>=MinLot)Lots=MMLot;else Lots=MinLot;double A1=MathRound(AlligatorPeriod*1.61803398874989);
double A2=MathRound(A1*1.61803398874989);double A3=MathRound(A2*1.61803398874989);
double LipsNow=iAlligator(NULL,0,A3,A2,A2,A1,A1,AlligatorPeriod,AlliggatorMODE,PRICE_MEDIAN,MODE_GATORLIPS,1);
double LipsPre=iAlligator(NULL,0,A3,A2,A2,A1,A1,AlligatorPeriod,AlliggatorMODE,PRICE_MEDIAN,MODE_GATORLIPS,2);
double JawsNow=iAlligator(NULL,0,A3,A2,A2,A1,A1,AlligatorPeriod,AlliggatorMODE,PRICE_MEDIAN,MODE_GATORJAW,1);
double JawsPre=iAlligator(NULL,0,A3,A2,A2,A1,A1,AlligatorPeriod,AlliggatorMODE,PRICE_MEDIAN,MODE_GATORJAW,2);
double Teeth=iAlligator(NULL,0,A3,A2,A2,A1,A1,AlligatorPeriod,AlliggatorMODE,PRICE_MEDIAN,MODE_GATORTEETH,1);
if(LipsNow>JawsNow&&Teeth<JawsNow&&LipsPre<JawsPre&&OrdersTotal()<1)
{OrderSend(Symbol(),OP_BUY,Lots,NormalizeDouble(Ask,Digits),0,0,0,"RideAlligator",0,0,CLR_NONE);}          
if(LipsNow<JawsNow&&Teeth>JawsNow&&LipsPre>JawsPre&&OrdersTotal()<1)
{OrderSend(Symbol(),OP_SELL,Lots,NormalizeDouble(Bid,Digits),0,0,0,"RideAlligator",0,0,CLR_NONE);}
int Orders=OrdersTotal();for(int i=0;i<Orders;i++)
{if(!(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)))continue;if(OrderSymbol()!=Symbol())continue;
{if(OrderStopLoss()!=JawsNow&&JawsNow>0&&JawsNow!=EMPTY_VALUE)
{if(OrderType()==OP_BUY&&(OrderStopLoss()==0||JawsNow>OrderStopLoss()))
{OrderModify(OrderTicket(),OrderOpenPrice(),JawsNow,OrderTakeProfit(),0,CLR_NONE);}
if(OrderType()==OP_SELL&&(OrderStopLoss()==0||JawsNow<OrderStopLoss()))
{OrderModify(OrderTicket(),OrderOpenPrice(),JawsNow,OrderTakeProfit(),0,CLR_NONE);}}}}
return(0);}
//+------------------------------------------------------------------+
//|---------------------------// END //--------(24/08/2011)----------|
//|            Copyright © 2011 http://www.mql4.com/ru/users/rustein |
//-------------------------------------------------------------------+----------------------------------------------------+