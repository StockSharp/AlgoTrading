
int count;
//--------------------------------------------------------------------
int start()                        
  {
   double MA;                        
//--------------------------------------------------------------------
                                     
   MA=iMA(NULL,0,14,0,MODE_SMA,PRICE_CLOSE,0); 
//--------------------------------------------------------------------

 
 if (Bid==MA)
count=0;  
 if (Bid-MA>0)             
 count+=1;       
if (count==150 && Bid-MA>0.004)    

OrderSend(Symbol(),OP_SELL,1.0,Bid,3,0,Bid-30*Point); 
if (count>0 && Bid-MA<0)
count=0;

if (count<0 && Bid-MA>0)
count=0;


if (Bid-MA<0)
count-=1;
if (count==-150 && Bid-MA<-0.004)
 OrderSend(Symbol(),OP_BUY,1.0,Ask,3,0,Bid+30*Point);


 

  
   
  
   return;                           
  }