//+------------------------------------------------------------------+
//|                                                           AG.mq4 |
//|                                                    Alksnis Gatis |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Alksnis Gatis"
#property link      "http://www.metaquotes.net"

extern double     Lots=     1;
extern double     F_EMA=   15;
extern double     S_EMA=   18;
extern double     SMA=     30;
extern double     ORDER=   10;
extern int        chk=      0;
//+------------------------------------------------------------------+
//|          expert start making money - sometimes                   |
//+------------------------------------------------------------------+
int start()
  {
   int  ticket, total;
   double SAR,SA;
   SAR=iMACD(Symbol(),0,F_EMA,S_EMA,SMA,PRICE_WEIGHTED,MODE_MAIN,1);
   SA=iMACD(Symbol(),0,F_EMA,S_EMA,SMA,PRICE_WEIGHTED,MODE_SIGNAL,1);
   double SAR1,SA1;
   SAR1=iMACD(Symbol(),0,S_EMA*2,F_EMA*2,SMA*2,PRICE_WEIGHTED,MODE_MAIN,1);
   SA1=iMACD(Symbol(),0,S_EMA*2,F_EMA*2,SMA*2,PRICE_WEIGHTED,MODE_SIGNAL,1);
  
   
   total=OrdersTotal();
   if(total<ORDER)
     {
      if(AccountFreeMargin()<(1000*Lots))
        {
         Print("NO MONEY = ", AccountFreeMargin());
         return(0);
        }
     
        {
         chk=1;
         Print("OK OK OK OK OK!");
        }
      if(chk==1)
        {
//------------------ OPEN SEEL ---------------------//
      if((SAR<SA)&&(SA>0)&&(SAR1<SA1)&&(SA1>0))
           {
            ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,0,
            "SAR position:",16385,0,Red);
            if(ticket<1)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)==False)
                  Print("OPEN SELL ORDER :  ",OrderOpenPrice());
              }
            else
              {
               Print("-----ERROR-----  opening SEEL order : ",GetLastError());
               return(0);
              }
           }
//------------------ OPEN BUY ---------------------//
         if((SAR>SA)&&(SA<0)&&(SAR1>SA1)&&(SA1<0))
           {
            ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,
            "SAR position:",16385,0,Blue);
            if(ticket<1)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)==False)
                  Print("OPEN BUY ORDER ",OrderOpenPrice());
              }
            else
              {
               Print("-----ERROR-----  opening BUY order : ",GetLastError());
               return(0);
              }
           }
        }
      return(0);
     }
 //------------------ CLOSE BUY ---------------------//
     {
      OrderSelect(SELECT_BY_POS, MODE_TRADES);
         if(OrderType()==OP_BUY && OrderSymbol()==Symbol()) 
            if((SAR1<SA1)&&(SA>0))
              {
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Black); // OUT
               return(0); 
 //------------------ CLOSE SELL --------------------//       
              }
        OrderSelect(SELECT_BY_POS, MODE_TRADES);
          if(OrderType()==OP_SELL && OrderSymbol()==Symbol())
           if((SAR1>SA1)&&(SA<0))
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,White); // OUT
               return(0);
              }
           
           }
  return(0);
  }
//+-------------------- GAME OWER --------------------------------+