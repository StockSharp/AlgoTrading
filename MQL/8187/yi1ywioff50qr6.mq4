extern double Lots=0.1;
extern int OpenTime=9;
extern int StopLoss=50;
extern int TakeProfit=20;
extern double InitEquity=600;
extern double DeltaEquity=300;
extern double InitLots=0.4;
extern double DeltaLots=0.2;

datetime TimeN;

int init()
{
TimeN=iTime(NULL,0,0);
}

int start()
{
int total=0;
for(int i=0; i<OrdersTotal(); i++)
{
OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
if(OrderSymbol()==Symbol() && OrderMagicNumber()==190208)
total++;
}

if (total > 0) return(0);

datetime TimeC=iTime(NULL,0,0);

//Alert(TimeDayOfWeek(TimeCurrent()),", ",OpenTime,", ",TimeHour(Time[0]));
   if(TimeDayOfWeek(TimeCurrent())==1 && TimeHour(Time[0])==OpenTime)
   {
   double Price1=(High[1]+Low[1]+Close[1])/3;
      if(Open[0]>Price1 && TimeC!=TimeN)
      {
      OrderSend(Symbol(),OP_BUY,Lots(),Ask,0,Ask-StopLoss*Point,Ask+TakeProfit*Point,"",190208,0,Blue);
      TimeN=TimeC;
      return(0);
      }
   }
return(0);
}

double Lots()
{
if(Lots!=0) return(Lots);
   else
   {
   if(AccountEquity()<InitEquity) return(MarketInfo(Symbol(),MODE_MINLOT));
      else
      {
         for(int i1=0; ; i1++)
         {
         if(AccountEquity()>=InitEquity+i1*DeltaEquity && AccountEquity()<InitEquity+(i1+1)*DeltaEquity)
         return(InitLots+i1*DeltaLots);
         }
      }
   }
}