//+------------------------------------------------------------------+
//|                                                     франк_уд.mq4 |
//|                     Copyright © 2006, Рамиль Сафиуллович Иргизов |
//|                                                popcorn@aaanet.ru |
//+------------------------------------------------------------------+
#define m  20050611
//----
extern int tp = 65;
extern int sh = 41;
//----
datetime lastt; 
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int kol_buy()
  {
   int kol_ob = 0;
//----
   for(int i = 0; i < OrdersTotal(); i++)
     {
       if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == false) 
           break;
       //----
       if(OrderType() == OP_BUY)  
           kol_ob++;
     }
   return(kol_ob);
  }    
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int kol_sell()
  {
   int kol_os = 0;
//----
   for(int i = 0; i < OrdersTotal(); i++)
     {
       if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == false) 
           break;
       //----
       if(OrderType() == OP_SELL)  
           kol_os++;
     }
   return(kol_os);
  }  
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   int slip, i, ii, tic, total, kk, gle;
   double lotsi = 0.0;
   bool sob = false, sos = false, scb = false, scs = false;
   int kb, kb_max = 0;
   kb = kol_buy() + 1;
   double M_ob[11][8];
   ArrayResize(M_ob,kb);
   int ks = 0, ks_max = 0;
   ks = kol_sell() + 1;
   double M_os[11][8];
   ArrayResize(M_os,ks);
   ArrayInitialize(M_ob, 0.0);
   int kbi = 0;
//----
   for(i = 0; i < OrdersTotal(); i++)
     {
       if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == false) 
           break;
       //----
       if(OrderSymbol() == Symbol() && OrderType() == OP_BUY)
         {
           kbi++;
           M_ob[kbi][0] = OrderTicket();
           M_ob[kbi][1] = OrderOpenPrice();
           M_ob[kbi][2] = OrderLots();
           M_ob[kbi][3] = OrderType();
           M_ob[kbi][4] = OrderMagicNumber();
           M_ob[kbi][5] = OrderStopLoss();
           M_ob[kbi][6] = OrderTakeProfit();
           M_ob[kbi][7] = OrderProfit();
         }
     } 
   M_ob[0][0] = kb; 
   double max_lot_b = 0.0;
//----
   for(i = 1; i < kb; i++)
       if(M_ob[i][2] > max_lot_b)
         {
           max_lot_b = M_ob[i][2];
           kb_max = i;
         }
   double buy_lev_min = M_ob[kb_max][1];   
   ArrayInitialize(M_os,0.0);
   int ksi = 0;
//----
   for(i = 0; i < OrdersTotal(); i++)
     {
       if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) 
           break;
       //----
       if(OrderSymbol()==Symbol() && OrderType()==OP_SELL)
         {
           ksi++;
           M_os[ksi][0] = OrderTicket();
           M_os[ksi][1] = OrderOpenPrice();
           M_os[ksi][2] = OrderLots();
           M_os[ksi][3] = OrderType();
           M_os[ksi][4] = OrderMagicNumber();
           M_os[ksi][5] = OrderStopLoss();
           M_os[ksi][6] = OrderTakeProfit();
           M_os[ksi][7] = OrderProfit();
         }
     } 
   M_os[0][0] = ks; 
   double max_lot_s = 0.0;
//----
   for(i = 1;i < ks; i++)
       if(M_os[i][2] > max_lot_s)
         {
           max_lot_s = M_os[i][2];
           ks_max = i;
         }
   double sell_lev_max = M_os[ks_max][1];    
//----
   if(Bars < 100 || IsTradeAllowed() == false) 
       return(0); 
   sob = (kol_buy() < 1 || buy_lev_min - sh*Point > Ask) && 
          AccountFreeMargin() > AccountBalance()*0.5;
   sos = (kol_sell() < 1 || sell_lev_max + sh*Point < Bid) &&
          AccountFreeMargin() > AccountBalance()*0.5;
//----
   if(M_ob[kb_max][2] > 0.0)
       scb = M_ob[kb_max][7] / (M_ob[kb_max][2]*10) > tp;
//----
   if(M_os[ks_max][2] > 0.0)
       scs = M_os[ks_max][7] / (M_os[ks_max][2]*10) > tp;
   kk = 0;
   ii = 0;
//----
   if(scb)
     {
       while(kol_buy() > 0 && kk < 3)
         {
           for(i = 1; i <= kb; i++)
             {
               ii = M_ob[i][0];
               //----
               if(!OrderClose(ii,M_ob[i][2],Bid,slip,White)) 
                 {
                   gle = GetLastError();
                   kk++;
                   Print("Ошибка №", gle, " при close buy ", kk);
                   Sleep(6000);
                   RefreshRates();  
                 }
             }
           kk++;
         }
     }
   kk = 0;  
   ii = 0; 
//----
   if(scs)
     {
       while(kol_sell() > 0 && kk < 3)
         {
           for(i = 1; i <= ks; i++)
             {
               ii = M_os[i][0];
               //----
               if(!OrderClose(ii,M_os[i][2], Ask, slip, White))
                 {
                   gle = GetLastError();
                   kk++;
                   Print("Ошибка №", gle, " при close sell ", kk);
                   Sleep(6000);
                   RefreshRates();  
                 }
             }
           kk++;
         }
     }
   kk = 0; 
   tic = -1;  
//----
   if(sob) 
     {
       if(max_lot_b == 0.0)
           lotsi = 0.1;
       else 
           lotsi = 2.0*max_lot_b;
       //----
       while(tic == -1 && kk < 3)
         {
           tic = OrderSend(Symbol(), OP_BUY, lotsi, Ask, slip, 0, Ask + (tp + 25)*Point, 
                           " ", m, 0, Yellow);
           Print("tic_buy=", tic);
           //----
           if(tic==-1)
             {
               gle = GetLastError();
               kk++;               
               Print("Ошибка №", gle, " при buy ", kk);
               Sleep(6000);
               RefreshRates();   
             }
         }   
       lastt = CurTime();
       return;
     }
   tic = -1;
   kk = 0;  
//----
   if(sos) 
     {
       if(max_lot_s == 0.0)
           lotsi = 0.1;
       else 
           lotsi = 2.0*max_lot_s;
       //----
       while(tic == -1 && kk < 3)
         {
           tic = OrderSend(Symbol(), OP_SELL, lotsi, Bid, slip, 0, Bid - (tp + 25)*Point, 
                           " ", m, 0, Red);
           Print("tic_sell=", tic);
           //----
           if(tic == -1)
             {
               gle = GetLastError();
               kk++;               
               Print("Ошибка №", gle, " при sell ", kk);
               Sleep(6000);
               RefreshRates();   
             }
          }
       lastt = CurTime();
       return;
     }        
  }
//+------------------------------------------------------------------+   