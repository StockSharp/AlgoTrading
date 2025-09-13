//+------------------------------------------------------------------+
//|                                                         RGT2.mq4 |
//|                        Copyright 2012, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
double Indicateur1;
double Indicateur2;
double IndicateurXLab_ZZ;
double IndicateurBolH;
double IndicateurBolM;
double IndicateurBolM1;
double IndicateurBolL;
double sl=0;

// entrées utilisateur =============================
extern double Lots = 0.01;
extern double MinPipsProfit = 30;
extern double Ea_StopLoss = 70;
extern double Ea_Trailing_Stop = 35;
extern int Slippage = 3;
extern int RsiH = 90;
extern int RsiL = 10;
extern int RsiPeriod = 8;
extern int Magic=686; 
//==================================================

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----

// -------------------------------------------

//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
Indicateur1 = iRSI(Symbol(),0,RsiPeriod,PRICE_CLOSE,0);
Indicateur2 = iRSI(Symbol(),0,RsiPeriod,PRICE_CLOSE,1);

IndicateurBolH = iBands(Symbol(),0,20,2,0,PRICE_CLOSE,1,0);
IndicateurBolM = iBands(Symbol(),0,20,2,0,PRICE_CLOSE,0,0);
IndicateurBolM1 = iBands(Symbol(),0,20,2,0,PRICE_CLOSE,0,1);
IndicateurBolL = iBands(Symbol(),0,20,2,0,PRICE_CLOSE,2,0);

bool Ea_En_Action = false;
bool Ea_Vente_On = false;
bool Ea_Achat_On = false;

int Ea_Ticket_Nbr = 0;
int total=OrdersTotal();

double Ea_Pips = 0;
double Ea_Profit = 0;
double Ea_Open = 0;
double Ea_Open_StopLoss = 0;



if (total > 0)
  {
  for(int pos=0;pos<total;pos++)
    {
    // detecte si le robot a deja un trade en cour
     if(OrderSelect(pos,SELECT_BY_POS)==false) continue;
       if(OrderMagicNumber() == Magic) 
         {
         Ea_En_Action = true;
         if (OrderType() == 0) {Ea_Achat_On = true ; Ea_Vente_On = false;}
         if (OrderType() == 1) {Ea_Achat_On = false; Ea_Vente_On = true ;}
         Ea_Ticket_Nbr = OrderTicket();
         Ea_Profit = OrderProfit()+OrderCommission()+OrderSwap();
         Ea_Open = OrderOpenPrice();
         Ea_Open_StopLoss = OrderStopLoss();
         }
      }
    } 
    
    
if (Ea_En_Action == false)// ouvrir une position
  {
  sl = 0;
  // Achat
  if (
  Indicateur1 < RsiL && 
  Ask<IndicateurBolL
  ){OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,0,0,"RGT EA RSI",Magic,0,CLR_NONE);}
  // Fin acheter
  
  // Vente
  if (
  Indicateur1 > RsiH && 
  Bid>IndicateurBolH
  ){OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,0,0,"RGT EA RSI",Magic,0,CLR_NONE);}
  }
  
  
if (Ea_Achat_On)// verifier si doit etre cloturé
  {
  Ea_Pips = (Bid - Ea_Open)/Point;
  OrderSelect(Ea_Ticket_Nbr,SELECT_BY_TICKET);// selection de la position d'achat ouverte par l'EA
  
// ajuster StopLoss
  if(OrderStopLoss()==0){OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Ea_StopLoss*Point,OrderTakeProfit(),0,Blue);return(0);}

// Deplacer le StopLoss quand le prix depasse un certain seuil.
  if (sl < Ea_Pips - Ea_Trailing_Stop)
    {
    sl=Ea_Pips - Ea_Trailing_Stop ;
    if (sl> MinPipsProfit){OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+sl*Point,OrderTakeProfit(),0,Blue);return(0);}
    }

  }
if (Ea_Vente_On == true)// verifier si doit etre cloturé    
  {
    Ea_Pips = (Ea_Open-Ask)/Point;
  OrderSelect(Ea_Ticket_Nbr,SELECT_BY_TICKET);// selection de la position d'achat ouverte par l'EA
  
// ajuster StopLoss
  if(OrderStopLoss()==0){OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Ea_StopLoss*Point,OrderTakeProfit(),0,Blue);return(0);}
// Deplacer le StopLoss quand le prix depasse un certain seuil.
  if (sl < Ea_Pips - Ea_Trailing_Stop)
    {
    sl=Ea_Pips - Ea_Trailing_Stop;
    if (sl> MinPipsProfit){OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-sl*Point,OrderTakeProfit(),0,Blue);return(0);}
    }
  }
  
  
Comment(
  "Ea en cours de trade : " +Ea_En_Action + "\n",
  "Ea Mode achat : " +Ea_Achat_On  + "\n",
  "Ea Mode vente : " +Ea_Vente_On  + "\n",
  "Ea ticket Nbr : " +Ea_Ticket_Nbr+ "\n",
  "Ea profit : "     +Ea_Profit    + " ("+Ea_Pips+" pips) \n",
  "Ea Open : "       +Ea_Open +"\n",
  sl + " / " +  Ea_Trailing_Stop  + " / " + (Ea_Pips - Ea_Trailing_Stop)+ "\n"
  );
//----
   return(0);
  }
//+------------------------------------------------------------------+