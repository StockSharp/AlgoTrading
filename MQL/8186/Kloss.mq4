//+------------------------------------------------------------------+
//|                                                         Kloss.mq4 |
//|                                                  Copyright © 2008|
//|                                                        k59@op.pl |
//+------------------------------------------------------------------+
#property copyright "by klopka"
#property link      "k59@op.pl"
extern double Lots       =0;   
extern double Prots      =0.2;
extern double max_lot = 5;                                   
extern double StopLoss   =48;     
extern double TakeProfit =152;           
bool Work=true;                    
string Symb;                       
int start()
  {
   int
   Total,Tip=-1,Ticket;                          
   double                         
   MA_2_t,linia_CCI_1,Stoch,Period_MA_2,Lot,Lts,Min_Lot,Step,Free,One_Lot,Price,SL,TP;                              
   bool
   Ans  =false,Cls_B=false,Cls_S=false,Opn_B=false,Opn_S=false;                     
   
   if(Bars < Period_MA_2)                       
     {
      Alert("<bars.EA does not work.");
      return;                                   
     }
   if(Work==false)                              
     {
      Alert("critical error.");
      return;                                   
     }
   
   Symb=Symbol();                               
   Total=0;                                     
   for(int i=1; i<=OrdersTotal(); i++)          
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) 
        {                                       
         if (OrderSymbol()!=Symb)continue;      
         if (OrderType()>1)                     
           {
            Alert("Open order was found. EA does not work.");
            return;                             
           }
         Total++;                               
         if (Total>1)                           
           {
            Alert("A few open orders. EA does not work.");
            return;                             
           }
         Ticket=OrderTicket();                  
         Tip   =OrderType();                    
         Price =OrderOpenPrice();               
         SL    =OrderStopLoss();                
         TP    =OrderTakeProfit();              
         Lot   =OrderLots();                    
        }
     }
   /*Indicators*/
   
  MA_2_t=iMA(NULL,PERIOD_M5,1,0,MODE_LWMA,PRICE_TYPICAL,5); 
  linia_CCI_1=iCCI(NULL,PERIOD_M5,10,PRICE_WEIGHTED,0);
  Stoch=iStochastic(NULL,PERIOD_M5,5,3,3,MODE_SMA,0,MODE_MAIN,0);
   
   
   // buy
   if(linia_CCI_1<-120 && Stoch < 30 && Open[1]>MA_2_t)
      
     {                                          
      Opn_B=true;                               
      Cls_S=true;                               
     }
    // sell
     if(linia_CCI_1>120 && Stoch > 70 && Close[1]<MA_2_t)
            
     {                                          
      Opn_S=true;                               
      Cls_B=true;                               
     }
//-----------------------------------------------------------------
   // close
   while(true)                                  
     {
      if (Tip==0 && Cls_B==true)                
        {                                       
         Alert("The test of lock the Buy",Ticket,"Expectation on answer");
         RefreshRates();                        
         Ans=OrderClose(Ticket,Lot,Bid,2,White);      
         if (Ans==true)                         
           {
            Alert ("Close buy",Ticket);
            break;                              
           }
         if (Fun_Error(GetLastError())==1)      
            continue;                           
         return;                                
        }

      if (Tip==1 && Cls_S==true)                
        {                                       
         Alert("The test of lock the sell",Ticket,"Expectation on answer");
         RefreshRates();                        
         Ans=OrderClose(Ticket,Lot,Ask,2,White);      
         if (Ans==true)                         
           {
            Alert ("closed Sell",Ticket);
            break;                              
           }
         if (Fun_Error(GetLastError())==1)      
            continue;                           
         return;                                
        }
      break;                                    
     }
   // amount orders
   RefreshRates();                              
   Min_Lot=MarketInfo(Symb,MODE_MINLOT);        
   Free   =AccountFreeMargin();                 
   One_Lot=MarketInfo(Symb,MODE_MARGINREQUIRED);
   Step   =MarketInfo(Symb,MODE_LOTSTEP);       

   if (Lots > 0)                                
      Lts =Lots;                                
   else                                         
      Lts=MathFloor(Free*Prots/One_Lot/Step)*Step;

   if(Lts < Min_Lot) Lts=Min_Lot;
   else  if(Lts>max_lot) Lts=max_lot;               
   if (Lts*One_Lot > Free)                      
     {
      Alert("no money", Lts," lot");
      return;                                   
     }
//-----------------------------------------------------------------
   // Open orders
   while(true)                                  
     {
      if (Total==0 && Opn_B==true)              
        {                                       
         RefreshRates();                        
         SL=Bid - New_Stop(StopLoss)*Point;     
         TP=Bid + New_Stop(TakeProfit)*Point;   
         Alert("The test open Buy. Expectation on answer");
         Ticket=OrderSend(Symb,OP_BUY,Lts,Ask,2,SL,TP,"buy",0,0,Green);
         if (Ticket > 0)                        
           {
            Alert ("open buy",Ticket);
            return;                             
           }
         if (Fun_Error(GetLastError())==1)      
            continue;                           
         return;                                
        }
      if (Total==0 && Opn_S==true)              
        {                                       
         RefreshRates();                        
         SL=Ask + New_Stop(StopLoss)*Point;     
         TP=Ask - New_Stop(TakeProfit)*Point;   
         Alert("The test open Sell. Expectation on answer");
         Ticket=OrderSend(Symb,OP_SELL,Lts,Bid,2,SL,TP,"sell",0,0,Red);
              
         if (Ticket > 0)                        
           {
            Alert ("Open Sell ",Ticket);
            return;                             
           }
         if (Fun_Error(GetLastError())==1)      
            continue;                           
         return;                                
        }
      break;                                    
     }
   return;                                      
  }
// errors

int Fun_Error(int Error)                        
  {
   switch(Error)
     {                                                    
      case  4: Alert("error 4");
         Sleep(3000);                           
         return(1);                             
      case 135:Alert("error 135");
         RefreshRates();                        
         return(1);                             
      case 136:Alert("error 136");
         while(RefreshRates()==false)           
            Sleep(1);                           
         return(1);                             
      case 137:Alert("error 137");
         Sleep(3000);                           
         return(1);                             
      case 146:Alert("error 146");
         Sleep(500);                            
         return(1);                             
         // Bledy krytyczne
      case  2: Alert("error 2");
         return(0);                             
      case  5: Alert("error 5");
         Work=false;                            
         return(0);                             
      case 64: Alert("error 64");
         Work=false;                            
         return(0);                             
      case 133:Alert("error 133");
         return(0);                             
      case 134:Alert("error 134");
         return(0);                             
      default: Alert("error",Error);   
         return(0);                             
     }
  }
int New_Stop(int Parametr)                      
  {
   int Min_Dist=MarketInfo(Symb,MODE_STOPLEVEL);
   if (Parametr<Min_Dist)                       
     {
      Parametr=Min_Dist;                        
      Alert("> distance stop limit");
     }
   return(Parametr);                            
  }

