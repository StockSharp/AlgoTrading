
#property copyright "Shevss"
#property link      "http://www.metaquotes.net"


extern int       per1=12;
extern int       per2=72;
extern double       uroven=0.3;
extern int tp=50;
extern int sl=50;
extern double lot=0.1;

double q1,q2,slb,sls,tpb,tps,ac1,ac2,ac0;

bool New_Bar;
int kup, prod, order;

int start()
  {

if (OrdersTotal()==1)
return;


New_Bar=false ;
Fun_New_Bar();  //                                                
if (New_Bar==false) 
return;
//___________________________________________________________________

q1= iCustom (NULL,0,"Ravi",per1,per2,0,1) ; 
q2= iCustom (NULL,0,"Ravi",per1,per2,0,2) ; 
ac1=iAC(NULL, 0 ,1);
ac2 =iAC(NULL, 0 ,2);
ac0 =iAC(NULL, 0 ,0);




//____________________________________________________________________

slb=Bid-sl*Point ; sls=Ask+sl*Point; tpb= Bid+tp*Point; tps= Ask-tp*Point;

//____________________________________________________________________
if   (   ac1>ac2 && ac2>0 && q1>q2 && q1> uroven )                                    
   {
       order =  OrderSend ( Symbol(), 0, lot, Ask, 3,slb,tpb );
   }
if (  ac1<ac2 && ac2<0 &&  q1<q2 && q1< -uroven   )                                      
   {
       order =  OrderSend ( Symbol(), 1, lot, Bid, 3,sls,tps);
    
   }
   return(0);
  }
//+------------------------------------------------------------------+


void Fun_New_Bar()
{

 static datetime New_Time=0;
 New_Bar=false;
 if(New_Time!=Time[0])
       {
        New_Time=Time[0];
        New_Bar=true; 
       }
}
//______________________________________________________


