#define IND 10

#import "c:\\HistTraining\\SharedVarsDLLv2.dll"

 
 
 double   GetFloat@4 (int N);
 int   GetInt@4 (int N);
 //   Init@0 
 void   SetFloat@12 (int N, double Val);
 void    SetInt@8 (int N, int Val);
 


#import

 int handle;
 
 
 
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

if (GetInt@4 (97)==1 && OrdersTotal()==0){OrderSend(Symbol(),OP_BUY,0.1,Ask,3,0,0,"",NULL,0,Blue);SetInt@8 (97, 0);}
if (GetInt@4 (98)==1 && OrdersTotal()==0){OrderSend(Symbol(),OP_SELL,0.1,Bid,3,0,0,"",NULL,0,Red);SetInt@8 (98, 0);}
OrderSelect(0,SELECT_BY_POS,MODE_TRADES);
if (GetInt@4 (99)==1 && OrderType()==OP_SELL){OrderClose(OrderTicket(),OrderLots(),Ask,6,White); SetInt@8 (99, 0);}
if (GetInt@4 (99)==1 && OrderType()==OP_BUY){OrderClose(OrderTicket(),OrderLots(),Bid,6,White); SetInt@8 (99, 0);}
 
  
  }

