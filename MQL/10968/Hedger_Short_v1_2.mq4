//Author: Markus Haberzettl
//This does not guarantee successful trades or any trading functionality at all!!


extern double Preis = 0;            //Preis = Price
extern double StopLoss = 0;
extern double TakeProfit = 0;
extern double Lots = 0.1;
extern double NSL_21_Wert = 5;
extern double NewTP_21_Wert = 5;
extern bool RiskHedge = false;         // 2x (2/5 Ä besser) // "/" als Trennstrich, nur gerundete int-Werte
extern bool RiskSL = true;             // 1x (11  Ä besser)
extern int RiskSLN = 100;
extern bool Regel_75_50 = false;
extern bool k_aktivieren = true;

int init ()
{
  if (RiskSL == true && RiskHedge == true) return(0);
  double spread=MarketInfo(Symbol(),MODE_SPREAD)*Point;  
         OrderSend(Symbol(),OP_SELLLIMIT,Lots,Preis,0,StopLoss,0,"Selbst gesetzte Short-Order",0,CLR_NONE);       //TP = 0 wg. 6.
         OrderSend(Symbol(),OP_BUYSTOP,Lots,Preis+spread,0,Preis-spread,0,"Hedge zur Short-Order",0,CLR_NONE);    //TP = 0 wg. 5.  
return(0);
}


bool OM_1, OM_3, aktiv_1, aktiv_2, aktiv_4, aktiv_5, aktiv_6, k_aktiv, aktiv_RiskHedge, aktiv_72, aktiv_RiskHedgeSL, a, d, k_multiplier;
int i_2, k, hst_total;
double NewSL_21, NewTP_21;



int start ()
{

Comment(
"\n", "Preis:      ", Preis,
"\n", "StopLoss:   ", StopLoss,
"\n", "TakeProfit: ", TakeProfit,
"\n", "75_50:      ", Regel_75_50,
"\n", "RiskSL:     ", RiskSL,
"\n", "RiskSLN:    ", RiskSLN,
"\n", "RiskHedge:  ", RiskHedge
);


double spread=MarketInfo(Symbol(),MODE_SPREAD)*Point;
int total=OrdersTotal();

      //if (aktiv_RiskHedgeSL == true && total == 1) aktiv_RiskHedgeSL = false;
     
      //Beenden des Trades, wenn Hedge 4x Verlust gemacht hat
      //if (total == 1 && aktiv_RiskHedge == true && aktiv_RiskHedgeSL == false) k++;
      if (total == 1 && aktiv_RiskHedge == true && k_multiplier == false) {k++; k_multiplier = true;}

      if (k == 3 && k_aktiv == false && k_aktivieren == true) {
         OrderSelect(0,SELECT_BY_POS);
         if (OrderType() == OP_SELL) bool OM_E=OrderModify(OrderTicket(),0,StopLoss,Preis - 7*spread,0,CLR_NONE);
         if (OM_E == true) Print ("Ende initiiert");
         k_aktiv = true;
      }
   
      //2.
      if (Bid>(Preis+spread) && total == 2 && aktiv_2 == false && aktiv_5 == false && aktiv_RiskHedge == false) 
      {
         Print ("2. aktiviert");
         OrderSelect(1,SELECT_BY_POS);
         if (OrderType() == OP_BUY) OM_1=OrderModify(OrderTicket(),0,Preis+spread,0,0,CLR_NONE);
         if (OM_1 == true) aktiv_2 = true;
      }
      
      //3.+4.+1.
      if (total == 1) OrderSelect(0,SELECT_BY_POS);
      if (total == 1 && OrderType() == OP_SELL && Ask < Preis) {  
         Print ("3.+4.+1. aktiviert");
         if (aktiv_2 == false) k++;
         OrderSend(Symbol(),OP_BUYSTOP,Lots,Preis+spread,0,Preis-spread,0,"Hedge zur Short-Order",0,CLR_NONE);
         if (OrdersTotal() == 2) {
            aktiv_4 = true;
            aktiv_2 = false;
            aktiv_RiskHedge = false;
            k_multiplier = false;
            if (aktiv_72 == true) {
               OrderSelect(0,SELECT_BY_POS);
               if (OrderType() == OP_SELL) OrderModify(OrderTicket(),0,StopLoss,0,0,CLR_NONE);
            }
            aktiv_72 = false;
            Print ("k: ", k);
         }
      }
      
      //5. Trailing-Hedge beim SL-Eintritt der NO     
      //5.1 Hedge-SL wird auf urspr¸nglich geplanten TP gezogen
      double NewSL = StopLoss-1.1*spread; //Normal w‰re StopLoss-spread der TP des Hedges //geht das auf dauer??? durchtesten, da eigentlich zu knapp (wohl eher 1.1*spread oder so
      if (Ask >= StopLoss && aktiv_5 == false && total > 0)  {
         Print ("5.1 aktiviert");
         OrderSelect(0,SELECT_BY_POS);
         if (OrderType() == OP_BUY) {
            aktiv_5=OrderModify(OrderTicket(),0,NewSL,0,0,CLR_NONE);
            }
      }      
      //5.2 Trailing-SL
      if (aktiv_5 == true && total > 0) {
         Print ("5.2 aktiviert");
         OrderSelect(0,SELECT_BY_POS);
         if (OrderType() == OP_BUY) {
            if (Bid >= StopLoss && a == false) {
               bool OM_52=OrderModify(OrderTicket(),0,StopLoss,0,0,CLR_NONE);
               if (OM_52 == true) a = true;
            }
            if (Bid > (OrderStopLoss()+spread) && a == true) {
               NewSL_21 = OrderStopLoss() + (Bid - OrderStopLoss() - NSL_21_Wert*Point);  //Wert darf 1 nicht ¸berschreiten, da n‰chster SL sonst niegriger liegen w¸rde!! 
               //Statistisch bester Wert (Wert x Anzahl): 0.8x1 0.4x1 0.6x1 0.5x1 0.1x1
               NormalizeDouble(NewSL_21,Digits);
               if (NewSL_21 > OrderStopLoss()) {  
                  OrderModify(OrderTicket(),0,NewSL_21,0,0,CLR_NONE);
               }
            }
         }          
      } //schlieﬂen von if (aktiviere 5)     
      
      //6. Trailing-TP bei TP-Eintritt der NO
      
      double NewTP = TakeProfit + 0.5*spread; //NewTP zieht SL nach!!!  //hier nicht das Problem von 5., da TP = Bid ist und vorher auch schon war
      if (Ask <= TakeProfit && aktiv_6 == false && total > 0)  {        //                                    //
         OrderSelect(0,SELECT_BY_POS);                                  //
         if (OrderType() == OP_SELL) {   
            Print ("6.1 aktiviert");                                  //
            aktiv_6=OrderModify(OrderTicket(),0,NewTP,0,0,CLR_NONE);    //an TP = SL denken!!
            }
      }      
      //6.2 Trailing-SL
      if (aktiv_6 == true && total > 0) {                               //
         OrderSelect(0,SELECT_BY_POS);                                  //
         if (OrderType() == OP_SELL) {   
            if (Ask < (OrderStopLoss()-spread)) {   
               Print ("6.2 aktiviert");                                       //
               NewTP_21 = OrderStopLoss() - (OrderStopLoss() - Ask - NewTP_21_Wert*Point); //
               NormalizeDouble(NewTP_21,Digits);                        //
               if (NewTP_21 < OrderStopLoss()) {                        //
                  OrderModify(OrderTicket(),0,NewTP_21,0,0,CLR_NONE);   //
               }
            }
         }          
      } //schlieﬂen von if (aktiviere 6)
      
   //75-50-Regel
   OrderSelect(0,SELECT_BY_POS);
   if (OrderType() == OP_SELL && d == false && Regel_75_50 == true) {
      double b,c;
      b = (Preis - TakeProfit)*0.75;
      c = Preis - ((Preis - TakeProfit)*0.5);
      if (Ask <= (Preis - b)) d=OrderModify(OrderTicket(),0,c,0,0,CLR_NONE);
      if (d == true) Print ("75-50 aktiv");
   }
    
   //7.1 RisikominimierungsHedge
   if (RiskHedge == true) {
      OrderSelect(0,SELECT_BY_POS);
      if (total == 1 && OrderType() == OP_SELL && aktiv_2 == true && Ask > (Preis+3*spread)) {
         OrderSend(Symbol(),OP_BUY,Lots,Ask,0,Preis+spread,0,"RiskHedge",0,0,CLR_NONE); 
         aktiv_RiskHedge = true;
      }
   }
 /*
   if (aktiv_RiskHedge == true && aktiv_RiskHedgeSL == false) { //Zieht SL des Hedges auf 0-Niveau nach
      OrderSelect(1,SELECT_BY_POS);
      if (OrderType() == OP_BUY && Bid > OrderOpenPrice() && total == 2) {    
         aktiv_RiskHedgeSL=OrderModify(OrderTicket(),0,OrderOpenPrice()-spread,0,0,CLR_NONE);
      }
   }
   */
      //7.1.1 RiskSL-Absicherung f¸r RiskHedge
      //sollte wegen Ask > (Preis+3*spread) eigentlich nie greifen
   if (RiskHedge == true && OrdersTotal() == 1 && Ask > Preis+RiskSLN*Point*spread) {
      OrderSelect(0,SELECT_BY_POS);
      if (OrderType() == OP_SELL) OrderClose(OrderTicket(),Lots,Ask,1,CLR_NONE);
   }
   
   //7.2 SL der NO nachziehen anstatt Risikohedge
   if (RiskSL == true && aktiv_72 == false) {
      OrderSelect(0,SELECT_BY_POS);
      if (k_aktiv == false && total == 1 && OrderType() == OP_SELL && aktiv_2 == true && StopLoss >= Preis+RiskSLN*Point*spread) bool OM_RSL1=OrderModify(OrderTicket(),0,Preis+RiskSLN*Point*spread,0,0,CLR_NONE);
      if (OM_RSL1 == true) aktiv_72 = true;
      if (k_aktiv == true && total == 1 && OrderType() == OP_SELL && aktiv_2 == true && StopLoss >= Preis+RiskSLN*Point*spread) bool OM_RSL2=OrderModify(OrderTicket(),0,Preis+RiskSLN*Point*spread,Preis - 7*spread,0,CLR_NONE);
      if (OM_RSL2 == true) aktiv_72 = true; 
   }
  
   //Lˆschen offener Pending-Order, wenn NO rausfliegt
   if (total > 0) OrderSelect(0,SELECT_BY_POS);
   if (aktiv_4 ==  true && OrderType() != OP_SELL && total != 0 && OrderType() != OP_BUY) OrderDelete(OrderTicket());

return(0);
}



int deinit ()
{
   while (OrdersTotal()>0) {
      OrderSelect(0,SELECT_BY_POS);
      if (OrderType() == OP_BUY) OrderClose(OrderTicket(),Lots,Bid,1,CLR_NONE);
      if (OrderType() == OP_SELL)OrderClose(OrderTicket(),Lots,Ask,1,CLR_NONE);
      if (OrderType() != OP_BUY && OrderType() != OP_SELL) OrderDelete(OrderTicket(),CLR_NONE);
   }        

return(0);
}
