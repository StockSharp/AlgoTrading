//+------------------------------------------------------------------+
//|                                                    disturbed.mq4 |
//|                          Copyright © 2011, Titanium Technologies |
//|                                         http://www.pasdesite.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Titanium Technologies"
#property link      "http://www.pasdesite.com"
#include <stdlib.mqh>      //librairie pour la gestion des erreurs entre autre
#include <WinUser32.mqh>   //librairie pour se servir des boîtes de dialogues

extern double lot = 0.1;      // lot de l'ordre
extern int gain = 2;          // nombre de fois de spread où il faudra clôturer

int magic = 666;
bool pos;
int stop;
int cntp, cntg;
string sym;
int ordre;
int spread;
double prixA, prixV;
double eqA, eqV, eqAF, eqVF;
int ticketA, ticketV, ticketF;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   sym = Symbol();
   stop = 0;
   pos= false;             // position double ouverte ou non
   cntg = 0;               // nombre de position gagnées
   cntp = 0;               // nombre de position perdues
   ordre = 0;              // nombre d'ordres restants
   spread = MarketInfo (sym, MODE_SPREAD);
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   ObjectDelete ("eqA = ");
   ObjectDelete ("eqV = ");
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   if (stop == 1) {
      Print ("L\'EA est désactivé...");
      return (0);
   }
   int retour, error;
   if (pos == false) {
      Print ("premier tick");
      debug ();
      retour = OrderSend (sym, OP_BUY, lot, Ask, 3, 0, 0, "J\'achète !!!", magic, 0, Blue);
      if (retour < 1) {
            error = GetLastError();
            Print ("erreur d\'ordre d\'achat: (",error,"): ", ErrorDescription(error));
            return (0);
      }
      OrderSelect(OrdersTotal()-1, SELECT_BY_POS, MODE_TRADES);
      ticketA = OrderTicket ();
      prixA = OrderOpenPrice ();
      eqA = NormalizeDouble (Ask + spread * Point, Digits);
      eqAF = NormalizeDouble (Ask + (gain * spread * Point), Digits);
      ordre++;
      affichage ("eqA = ", eqA, Blue);
      retour = OrderSend (sym, OP_SELL, lot, Bid, 3, 0, 0, "Je vends !!!", magic, 0, Red);
      if (retour < 1) {
            error = GetLastError();
            Print ("erreur d\'ordre de vente: (",error,"): ", ErrorDescription(error));
            return (0);
      }
      OrderSelect(OrdersTotal()-1, SELECT_BY_POS, MODE_TRADES);
      ticketV = OrderTicket ();
      prixV = OrderOpenPrice ();
      eqV = NormalizeDouble (Bid - spread * Point, Digits);
      eqVF = NormalizeDouble (Bid - (gain * spread * Point), Digits);
      ordre++;
      affichage ("eqV = ", eqV, Red);
      pos = true;         
   }
   if (pos == true)  {
      if (Bid == eqA)   {
         Print ("Je ferme parce que ordre de vente a perdu son spread");
         fermer (ticketV);
         verif ();
         return (0);
      }
      if (Bid == eqV)   {
         Print ("Je ferme parce que ordre d\'achat a perdu son spread");
         fermer (ticketA);
         verif ();
         return (0);
      }
      if (ordre == 2)   {
         Print ("prix entre les spreads");
         return (0);
      }
      //===> ordre = 1 <=> surveiller position restante
      if (ticketF == ticketA) {              // si ordre restant est une vente (short)
         if (Ask <= eqVF)   {   // si gain de deux spread
            Print ("Je ferme parce que gain sur ordre de vente");
            fermer (ticketV);
            verif ();
            return (0);            
         }
         if (Ask == eqV)   {   // si perte de deux spread
            Print ("Je ferme parce que perte aussi sur ordre de vente");
            fermer (ticketV);
            verif ();
            return (0);            
         }
         return (0);
      }  else if (ticketF == ticketV)  {     // si ordre restant est un achat (long)
         if (Bid >= eqAF)   {   // si gain de deux spread
            Print ("Je ferme parce que gain sur ordre d\'achat");
            fermer (ticketA);
            verif ();
            return (0);            
         }
         if (Bid == eqA)   {   // si perte de deux spread
            Print ("Je ferme parce que perte aussi sur ordre d\'achat");
            fermer (ticketA);
            verif ();
            return (0);            
         }
         return (0);
      }
      Print ("Erreur programme; cette ligne n\'aurait pas due être exécutée!!!");
      debug ();
      
   }
      
//----
   return(0);
  }
  
//+------------------------------------------------------------------+
//| fonction de fermeture d'ordres                                   |
//+------------------------------------------------------------------+  
int fermer (int ticket)   {
   int error, retour;
   double prix;
   if (ticket == ticketA)  {
      prix = NormalizeDouble (Bid, Digits);
   }  else if (ticket == ticketV)   {
      prix = NormalizeDouble (Ask, Digits);
   }
   retour = OrderClose(ticket,lot,prix,3,Orange);
   if (retour < 1) {
      error = GetLastError();
      Alert ("erreur de fermeture d\'ordre : (",error,"): ", ErrorDescription(error)," sur ", ticket);
      Print ("Ask = ", Ask, " et Bid = ", Bid, " et prix = ", prix);
      debug ();
      return (0);
   }
   ordre--;
   OrderSelect(ticket, SELECT_BY_TICKET);
   OrderPrint();
   if (OrderProfit() <= 0)  {
      cntp++;
   }  else{
      cntg++;
   }
   ticketF = OrderTicket ();
   Print ("ticket = ", ticket);
   debug ();
   return (ticketF);

}

//+------------------------------------------------------------------+
//| fonction de vérification de gains/pertes                         |
//+------------------------------------------------------------------+
void verif  () {
   int icon;
   string mess;
   //MB_ICONSTOP, MB_ICONQUESTION, MB_ICONWARNING, MB_ICONASTERISK
   if (cntp + cntg >= 5)   {
      if (cntp == cntg) {
         mess = "Autant de gains que de pertes, continuez?";
         icon = 32;
      }  else if (cntp >= cntg) {
         mess = "Plus de pertes que de gains, continuez?";
         icon = 48;
      }  else if (cntp >= cntg + 7) {
         mess = "Beaucoup trop de pertes, voulez-vous vraiment continuer?";
         icon = 16;
      }  else if (cntp < cntg)   {
         mess = "Les gains arrivent, continuez bien sûr?";
         icon = 64;
      }
      int button = MessageBox (mess, "Récapitulatif des gains/pertes",  MB_YESNO|icon|MB_TOPMOST);
      if (button == IDYES) {
         pos = 0;
         ordre = 0;
         return (0);
      }  else if (button == IDNO)   {
         stop =1;
         return (0);
      }
   }
   pos = 0;
   ordre = 0;
   return (0);

}
//+------------------------------------------------------------------+
//| fonction de déboguage                                            |
//+------------------------------------------------------------------+
void debug ()  {
    Print ("stop = ", stop, " pos = ", pos, " ordre = ", ordre);
    Print ("ticketA = ", ticketA, " tivketV = ", ticketV, " ticketF = ", ticketF);
    Print ("eqA= ", eqA, " eqAF = ", eqAF);
    Print ("eqV= ", eqV, " eqVF = ", eqVF);
} 

//+------------------------------------------------------------------+
//| fonction d'affichage                                             |
//+------------------------------------------------------------------+
void affichage (string nom, double niveau, color couleur)   {
   ObjectDelete (nom);
   ObjectCreate (nom, OBJ_HLINE, 0, 0, niveau, 0, 0, 0, 0);
   ObjectSet (nom, OBJPROP_STYLE, STYLE_SOLID);
   ObjectSet (nom, OBJPROP_COLOR, couleur);
   return (0);
}

  
//+------------------------------------------------------------------+