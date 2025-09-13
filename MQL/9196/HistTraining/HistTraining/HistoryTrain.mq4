#define IND 10

#import "c:\\HistTraining\\SharedVarsDLLv2.dll"

 
 
 double   GetFloat@4 (int N);
 int   GetInt@4 (int N);
 //   Init@0 
 void   SetFloat@12 (int N, double Val);
 void    SetInt@8 (int N, int Val);
 


#import

 int handle;
 int magn;
 int k=0;
 int k1=0;
 int f=0;
 
 
void init()
{
SetInt@8 (97, 0);
SetInt@8 (98, 0);
SetInt@8 (99, 0);
}

void deinit()
{
}
 
 
int start()
  {
  
  
 if(OrdersTotal()==0){magn=0;} 
  
  

if (GetInt@4 (97)==1)
{

if(OrderSend(Symbol(),OP_BUY,GetFloat@4 (1),Ask,3,0,0,"",magn,0,Blue)){SetInt@8(10,magn);SetInt@8(11,1);SetFloat@12 (10, Ask);magn++;}
SetInt@8 (97, 0);
}


if (GetInt@4 (98)==1)
{

if(OrderSend(Symbol(),OP_SELL,GetFloat@4 (1),Bid,3,0,0,"",magn,0,Red)){SetInt@8(10,magn);SetInt@8(11,2);SetFloat@12 (10, Bid);magn++;}
SetInt@8 (98, 0);
}





if (GetInt@4 (99)==1)
{

for(k=0;k<OrdersTotal();k++)
{
OrderSelect(k,SELECT_BY_POS,MODE_TRADES);
if(OrderMagicNumber()==GetInt@4(20))
{
break;
}
}
}


if (GetInt@4 (99)==1 && OrderType()==OP_SELL)
{
OrderClose(OrderTicket(),OrderLots(),Ask,6,White); SetInt@8 (99, 0);
SetInt@8 (97, 0);
SetInt@8 (98, 0);
}




if (GetInt@4 (99)==1 && OrderType()==OP_BUY)
{
OrderClose(OrderTicket(),OrderLots(),Bid,6,White); SetInt@8 (99, 0);
SetInt@8 (97, 0);
SetInt@8 (98, 0);
}
 
 
 
 
 if (GetInt@4 (30)==1)
{
for (f=OrdersTotal();f>=0;f--)
{
OrderSelect(f,SELECT_BY_POS,MODE_TRADES);
   if (OrderType()==OP_BUY)
   {
   OrderClose(OrderTicket(),OrderLots(),Bid,6,White); 
   SetInt@8 (97, 0);
   SetInt@8 (98, 0);
   }
   if (OrderType()==OP_SELL)
   {
   OrderClose(OrderTicket(),OrderLots(),Ask,6,White); 
   SetInt@8 (97, 0);
   SetInt@8 (98, 0);
   }
}
SetInt@8 (30, 0);
}



 
  
  }

