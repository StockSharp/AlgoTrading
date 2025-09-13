extern double Lots=0.1;
extern int TakeProfit=15;
extern int StopLoss=999;
extern int TrailingStop=0;
extern int ReversePoint=250;

datetime TimeN;
bool temp1=false, temp2=false;
double RPoint_High=0, RPoint_Low=0, Reverse_High=0, Reverse_Low=0;

int init()
{
TimeN=0;
}
   
int start()
{
datetime TimeC=iTime(NULL,0,0);

   for(int ii=0; ii<=ReversePoint; ii++)
   {
   double val=iCustom(NULL,0,"RPoint",ReversePoint,0,ii);
   if(val==High[ii]) {RPoint_High=val; break;}
   if(val==Low[ii]) {RPoint_Low=val; break;}
   }

for(int cnt=0; cnt<OrdersTotal(); cnt++)
{
OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
   if(OrderSymbol()==Symbol() && OrderMagicNumber()==50908) 
   {
      if(OrderType()==OP_BUY)
      {
         if(TrailingStop>0)  
              {                 
                  if(Bid-OrderOpenPrice()>Point*TrailingStop)
                  {
                  if(OrderStopLoss()<Bid-Point*TrailingStop)
                  OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
                  }
              }

         if(RPoint_High!=Reverse_High) OrderClose(OrderTicket(),OrderLots(),Bid,10,Violet);
      }  

      if(OrderType()==OP_SELL)
      {
              if(TrailingStop>0)  
              {                 
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
                 {
                  if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                  OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Green);
                 }
              }
         if(RPoint_Low!=Reverse_Low) OrderClose(OrderTicket(),OrderLots(),Ask,10,Violet);
      }
   }
}

int total=0;
for(int i1=0; i1<OrdersTotal(); i1++)
{
OrderSelect(i1, SELECT_BY_POS, MODE_TRADES);
if(OrderSymbol()==Symbol() && OrderMagicNumber()==50908)
total++;
}

if(total>0) return(0);
if(TimeC==TimeN) return(0);

if(temp1==true || (temp1==false && RPoint_High!=Reverse_High))
{
Alert(RPoint_High);
int ticket1=OrderSend(Symbol(),OP_SELL,Lots,Bid,10,Bid+StopLoss*Point,Bid-TakeProfit*Point,"Super",50908,0,Red);
if(ticket1>0) {Reverse_High=RPoint_High; temp1=false; TimeN=TimeC;} else temp1=true;
}

if(temp2==true || (temp2==false && RPoint_Low!=Reverse_Low))
{
Alert(RPoint_Low);
int ticket2=OrderSend(Symbol(),OP_BUY,Lots,Ask,10,Ask-StopLoss*Point,Ask+TakeProfit*Point,"Super",50908,0,Blue);
if(ticket2>0) {Reverse_Low=RPoint_Low; temp2=false; TimeN=TimeC;} else temp2=true;
}

return(0);
}