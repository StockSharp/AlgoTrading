//+------------------------------------------------------------------+
//|                                                Intraday_b�ta.mq4 |
//|                          Copyright � 2011, Titanium Technologies |
//|                                         http://www.pasdesite.com |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2011, Titanium Technologies"
#property link      "http://www.pasdesite.com"
#include <stdlib.mqh>      //librairie pour la gestion des erreurs entre autre
#include <WinUser32.mqh>   //librairie pour se servir des bo�tes de dialogues

//+------------------------------------------------------------------+
//|   variables externes de l'expert advisor                         |
//+------------------------------------------------------------------+
extern int prsi = 9;       // p�riode de calcul du RSI
extern int trailing = 20;  // traling stop virtuel en pips
extern double lot = 0.1;   // taille du lot

//+------------------------------------------------------------------+
//|   variables globales de l'expert advisor                         |
//+------------------------------------------------------------------+
//----cr�ation des variables et de leur valeur par d�faut
bool New_Bar=false;        // contient false s'il n'y a pas de nouveau chadelier
double tableau[10][10];    // cr�ation du tableau de donn�es
//int son;                   //variable contenant le contexte du son � jouer
bool busy;                 // d�termine si l'ea est occup� ou non � chaque nouveau tick
string mess;               // contient le message de d�bogage
//double sty = 0;            // 0 => pas pris de position, 1 ou 2 sinon
string sym;                // contient le symbole du chart
int cxt = 0;               // contexte de l'EA
int volt = 0;              // relatif � la volatilit�
int compt = 0;             // 1 si volatile () a �t� ex�cut�, 0 sinon
double moy;                // moyenne pour la volatilit�
int magic1 = 12345678;     // num�ro magic du premier ordre
int magic2 = 87654321;     // num�ro magic du deuxi�me ordre
bool achat = true;         // on peut acheter si achat est true
bool vente = true;         // on peut vendre si vente est true
int nbpos = 0;             // nombre de positions prises (2 maximum)

int error;                 // contexte de l'erreur
int ordre = 0;             // type de l'ordre, 1 achet�, 2 vendu
int clo = 0;               // stipule si l'ordre doit �tre ferm� (= 1) ou non (= 0)
double amin;               // m�morise la valeur � laquelle doit �tre ferm� l'ordre d'achat
double vmax;               // m�morise la valeur � laquelle doit �tre ferm� l'ordre de vente
double anull;              // prix auquel l'ordre d'achat est � l'�quilibre
double vnull;              // prix auquel l'ordre de vente est � l'�quilibre
bool up = false;           // indique si l'ordre est gagnant de "trailing" pips en achat
bool down = false;         // indique si l'ordre est gagnant de "trailing" pips en vente


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   sym = Symbol();         // affecte le symbole du chart au string "sym"
   busy = false;           // EA pas encore occup�
   mess = "OK";            // jusqu'ici, tout va bien
   moy = (Open[1] + Close[1])/2;
   
        
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   Comment("");
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+


int start()
  {
//----
   mess = StringConcatenate("De la boucle start() ",mess);
   info (mess);         // print le contexte de l'EA   
   if (busy == true) {
      map();
      return;
   }
   busy = true;         // EA occup�
   map ();
   
  //busy = false;
   mess = "L\'EA n\'a plus rien � faire...";
      
//----
   return(0);
  }

//+------------------------------------------------------------------+
//| fonction de redirection vers les fonctions ad�quates             |
//+------------------------------------------------------------------+
int map  () {
   //mess = "dans la fonction map ()";
   //info (mess);
   if (cxt == 0)  {
      cxt = 10;
   }
   if (cxt == 10) {
      verif_ordres ();
   }
   if (cxt == 20) {
      calcul ();
   }
   if (cxt == 25)  {
      mess = "attends un nouveau chandelier";
      wait_candle ();
   }
   if (cxt == 26) {
      volatile (sym, 30, moy);
      if (compt == 1)   {
         Alert ("Volatilit� v�rifi�e et volt = ", volt," avec valeur = ", moy," Euros");
      }
      info (mess);
   } 
   if (cxt == 30) {
      take_pos ();
   }
   if (cxt == 40) {
      calcul ();
   }
   if (cxt == 50) {
      keep_pos ();
      
   }
   //if (cxt == 65) {
   //   act_ordres ();
   //}

}


//+------------------------------------------------------------------+
//| calcul des valeurs du tableau                                    |
//+------------------------------------------------------------------+
void  calcul ()   {
   //mess = "dans la fonction calcul()";
   //info (mess);
   for (int j=0; j<=9; j++) {     // �num�re les colonnes
      tableau[0][j] = iMA(sym,0,10,0,MODE_SMMA,PRICE_MEDIAN,j);   // MM10
      tableau[1][j] = iMA(sym,0,20,0,MODE_SMMA,PRICE_MEDIAN,j);   // MM20
      tableau[2][j] = tableau[0][j] - tableau[1][j];              // diff�rence MMs
      tableau[3][j] = tableau[0][j] - tableau[0][j+1];            // pente MM10
      tableau[4][j] = tableau[1][j] - tableau[1][j+1];            // pente MM20
      tableau[5][j] = Close[j] - Open[j];                         // signe chandelier
      tableau[6][j] = iRSI(sym,0,prsi,PRICE_MEDIAN,1);            // RSI
      tableau[7][j] = iStochastic(sym,0,5,3,3,MODE_SMA,0,MODE_MAIN,0);   // position achet�e, vendue ou rien
      tableau[8][j] = iLow(sym,0,j);                              // valeur min du chandelier
      tableau[9][j] = iHigh(sym,0,j);                             // valeur max du chandelier
      }
      cxt = 30;      // vers prendre ou pas position
      if (nbpos >= 1)   {
         cxt = 50;   // vers garder position ou pas
      }
   }
   
//+------------------------------------------------------------------+
//| fonction affichant le contexte de l'EA                           |
//+------------------------------------------------------------------+
void  info (string text)  {
   if (busy == true) {
      Print ("L\'EA est occup� et voici le contexte: ", text," et cxt = ", cxt," et nbpos = ", nbpos);
      }  else  {
      Print ("L\'EA n\'est pas occup� mais voici le contexte: ", text);
      }
    }

//+------------------------------------------------------------------+
//| fonction v�rifiant si des ordres ont �t� pass�                   |
//+------------------------------------------------------------------+
void verif_ordres () {
   cxt = 10;
   //mess = "dans la fonction verif_ordres ()";
   //info (mess);
   for (int i = OrdersTotal() - 1; i>=0; i--)   {
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
         if (OrderMagicNumber() == magic1)   {
            if (OrderType() == OP_BUY) {
               achat = false;
            }  else if (OrderType() == OP_SELL) {
               vente = false;
            }
         }
         if (OrderMagicNumber() == magic2)   {
            if (OrderType() == OP_BUY) {
               achat = false;
            }  else if (OrderType() == OP_SELL) {
               vente = false;
            }
          }
      }
      if (nbpos == 2)   {
         cxt = 40;
         return;
      }
   }   
   cxt = 20;  
}

//+------------------------------------------------------------------+
//| fonction d�cidant de prendre ou non une position                 |
//+------------------------------------------------------------------+
void take_pos ()  {
   //cxt = 30;
   //mess = "dans la fonction take_pos ()";
   //info (mess);
   if (compt == 0)   {
      cxt = 26;      // v�rfication volatibilit�
      return;
   }
   if (volt >= 20)   {
      mess = "March� trop volatile!";
      info (mess);
      cxt = 25;      // attendre nouveau chandelier
      return;
   }
   int v = 0;
   int a = 0;
   int magic, retour, num;
   if (tableau[3][1] < 0 && tableau[3][3] > 0)  {     // si pente MM10 cro�t et d�cro�t
      if (tableau[6][1] >= 30)   {                    // et si RSI >= � 30
         if (tableau[5][1] < 0)   {                   // et si signe chandelier n�gatif
            v = 1;                                    // => vendre
         }
      }
   }
   if (tableau[3][1] > 0 && tableau[3][3] < 0)  {     // si pente MM10 d�cro�t et cro�t
      if (tableau[6][1] <= 70)   {                    // et si RSI <= � 70
         if (tableau[5][1] > 0)   {                   // et si signe chandelier positif
            a = 1;                                    // => acheter
         }
      }
   }
   if (v != 1) {
      mess ="ne peut pas vendre techniquement";
      info (mess);
   }
   if (a != 1) {
      mess = "ne peut pas acheter techniquement";
      info (mess);
   }
   if (nbpos <= 1)   {                                // si moins de deux positions prises
      if (nbpos == 0)   {                             // si premier ordre
         magic = magic1;                              // attribution du num�ro magic au premier ordre
      }
      if (nbpos == 1)   {                             // si deuxi�me ordre
         magic = magic2;                              // attirbution du num�ro magic au deuxi�me ordre
      }
      if (v == 1 && vente == true)  {                  // passer ordre de vente
         retour = OrderSend(sym, OP_SELL, lot, Bid, 3, 0, 0, "Je vends!", magic, 0, Red); // prise d'ordre
         if (retour < 1) {
            error = GetLastError();
            Print ("erreur d\'ordre de vente: (",error,"): ", ErrorDescription(error));
            return (0);
         }
         OrderSelect(OrdersTotal() - 1, SELECT_BY_POS, MODE_TRADES);
         num = OrderTicket ();
         vmax = NormalizeDouble (Ask + trailing * Point, Digits);
         vnull = Bid;               // prix auquel l'ordre est � l'�quilibre
         vente = false;             // ne peut plus prendre d'ordre de vente
         PlaySound ("vente.wav");
         Comment ("pris une position de vente avec le num�ro: (",num,") et vmax = ", vmax, " sur ", sym);
         mess = StringConcatenate ("pris une position de vente avec le num�ro: ", num," et vmax = ", vmax);
         info (mess);            
         nbpos++;                           // incr�mentation de nbpos
         cxt = 40;                                    // vers calcul()
         return;
      }
      if (v == 1 && vente == false) {
         mess = "ne peut pas vendre car d�j� un ordre de vente!";
         info (mess);
      }
      if (a == 1 && achat == true)  {                  // passer ordre d'achat
         retour = OrderSend(sym, OP_BUY, lot, Ask, 3, 0, 0, "J\'ach�te!", magic, 0, Blue); // prise d'ordre
         if (retour < 1) {
            error = GetLastError();
            Print ("erreur d\'ordre d\'achat: (",error,"): ", ErrorDescription(error));
            return (0);
         }
         OrderSelect(OrdersTotal() - 1, SELECT_BY_POS, MODE_TRADES);
         num = OrderTicket ();
         amin = NormalizeDouble (Bid - trailing * Point, Digits);
         anull = Ask;               // prix auquel l'ordre est � l'�quilibre
         achat = false;             // ne peut plus prendre d'ordre d'achat
         PlaySound ("achat.wav");
         Comment ("pris une position d\'achat avec le num�ro: (",num,") et amin = ", amin, " sur ", sym);
         mess = StringConcatenate ("pris une position d\'achat avec le num�ro: ", num," et amin = ", amin);
         info (mess);
         nbpos++;                           // incr�mentation de nbpos
         cxt = 40;                                    // vers calcul()
         return;
      }
      if (a == 1 && achat == false) {
         mess = "ne peut pas acheter car d�j� un ordre d\'achat!";
         info (mess);
      }
   }
   if (nbpos == 1)   {        // si pas pris de position mais si d�j� une de prise
      cxt =40;                // vers calcul
      return;
   }  else if (nbpos == 0) {
      //cxt = 25;      // pas pris de position => attendre nouveau chandeleier
      cxt = 20;         // recalculer
      return;
   }
   mess = "erreur dans le programme!!!";
   Alert (mess);
}

//+------------------------------------------------------------------+
//| fonction d�cidant de garder ou non une position                  |
//+------------------------------------------------------------------+
void keep_pos ()  {
   cxt = 50;
   //mess = "dans la fonction keep_pos ()";
   //info (mess);
   for (int i = OrdersTotal () - 1; i>= 0; i--) {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if (nbpos == 1)   {                 // si une seule position a �t� prise
         if (OrderMagicNumber () == magic1)  {
            if (achat == false)  {           // si c'est un ordre d'achat
               ordre = 1;                    // ordre d'achat
               virtual (OrderOpenPrice ());  // v�rifier trailing stop virtuel de l'ordre
               if (clo == 1)  {              // => fermer l'ordre de magic1
                  fermer (magic1);
                  cxt = 10;                  // recommencer au d�but
                  return;
               }
               if (tableau[2][1] < 0)  {     // si diff�rences de MMs n�gatives
                  //cxt = 40;                  // ne rien faire
                  if (nbpos == 2)   {
                     cxt = 40;
                  }  else if (nbpos == 1) {
                     cxt = 20;
                  }
                  return;
               }
               if (tableau[2][1] <= 3)  {    // si diff�rence de MMs quasi nulles
                  virtual (OrderOpenPrice ());  // v�rifier trailing stop virtuel de l'ordre
                  if (clo == 1)  {           // => fermer l'ordre de magic1
                     fermer (magic1);
                     cxt = 10;               // recommencer au d�but
                     return;
                  }
                  //cxt = 40;                  // recalculer
                  if (nbpos == 2)   {
                     cxt = 40;
                  }  else if (nbpos == 1) {
                     cxt = 20;
                  }
                  return;
               }
            }                                // fin d'achat
            if (vente == false)  {           // si c'est un ordre de vente
               ordre = 2;                    // ordre de vente
               virtual (OrderOpenPrice ());  // v�rifier trailing stop virtuel de l'ordre
               if (clo == 1)  {              // => fermer l'ordre de magic1
                  fermer (magic1);
                  cxt= 10;                   // recommencer au d�but
                  return;
               }
               if (tableau[2][1] > 0)  {     // si diff�rences de MMs positives
                  //cxt = 40;                  // ne rien faire
                  if (nbpos == 2)   {
                     cxt = 40;
                  }  else if (nbpos == 1) {
                     cxt = 20;
                  }
                  return;
               }
               if (tableau[2][1] <= 3)  {    // si diff�rence de MMs quasi nulles
                  virtual (OrderOpenPrice ());  // v�rifier trailing stop virtuel de l'ordre
                  if (clo == 1)  {           // => fermer l'ordre de magic1
                     fermer (magic1);
                     cxt = 10;               // recommencer au d�but
                     return;
                  }
                  //cxt = 40;                  // recalculer
                  if (nbpos == 2)   {
                     cxt = 40;
                  }  else if (nbpos == 1) {
                     cxt = 20;
                  }
                  return;
               }
            }                                // fin de vente
         }                                   // fin de magic1 pour un seul ordre de pass�(redondant) 
      }                                   // fin de npos = 1
      if (nbpos == 2)   {              // si deux positions de prises
         
         if (OrderMagicNumber () == magic1)  {
            if (OrderType () == OP_BUY)   {  // si magic1 est en achat
               ordre = 1;                    // ordre d'achat
               virtual (OrderOpenPrice ());  // v�rifier trailing stop virtuel de l'ordre
               if (clo == 1)  {              // => fermer l'ordre de magic1
                  fermer (magic1);
                  cxt = 10;                  // recommencer au d�but
                  return;
               }
               if (tableau[2][1] < 0)  {     // si diff�rences de MMs n�gatives
                  //cxt = 40;                  // ne rien faire
                  if (nbpos == 2)   {
                     cxt = 40;
                  }  else if (nbpos == 1) {
                     cxt = 20;
                  }
                  return;
               }
               if (tableau[2][1] <= 3)  {    // si diff�rence de MMs quasi nulles
                  virtual (OrderOpenPrice ());  // v�rifier trailing stop virtuel de l'ordre  
                  if (clo == 1)  {           // => fermer l'ordre de magic1
                     fermer (magic1);
                     cxt = 10;               // recommencer au d�but
                     return;
                  }
               }
               //cxt = 40;                  // recalculer
               if (nbpos == 2)   {
                     cxt = 40;
                  }  else if (nbpos == 1) {
                     cxt = 20;
                  }
               return;              
            }                             // fin de achat magic1
            if (OrderType () == OP_SELL)   {    // si magic1 est en vente
               ordre = 2;                    // ordre de vente
               virtual (OrderOpenPrice ());  // v�rifier trailing stop virtuel de l'ordre
               if (clo == 1)  {              // => fermer l'ordre de magic1
                  fermer (magic1);
                  cxt = 10;                  // recommencer au d�but
                  return;
               }
               if (tableau[2][1] > 0)  {     // si diff�rences de MMs positives
                  //cxt = 40;                  // ne rien faire
                  if (nbpos == 2)   {
                     cxt = 40;
                  }  else if (nbpos == 1) {
                     cxt = 20;
                  }
                  return;
               }
               if (tableau[2][1] <= 3)  {    // si diff�rence de MMs quasi nulles
                  virtual (OrderOpenPrice ());  // v�rifier trailing stop virtuel de l'ordre
                  if (clo == 1)  {           // => fermer l'ordre de magic2
                     fermer (magic1);
                     cxt = 10;               // recommencer au d�but
                     return;
                  }
               }
               //cxt = 40;                  // recalculer
               if (nbpos == 2)   {
                     cxt = 40;
                  }  else if (nbpos == 1) {
                     cxt = 20;
                  }
               return;
            }                             // fin de vente magic1
         }                                // fin de magic1
         if (OrderMagicNumber () == magic2)  {
            if (OrderType () == OP_BUY)   {  // si magic2 est en achat
               ordre = 1;                    // ordre d'achat
               virtual (OrderOpenPrice ());  // v�rifier trailing stop virtuel de l'ordre
               if (clo == 1)  {              // => fermer l'ordre de magic1
                  fermer (magic2);
                  cxt = 10;                  // recommencer au d�but
                  return;
               }
               if (tableau[2][1] < 0)  {     // si diff�rences de MMs n�gatives
                  cxt = 40;                  // ne rien faire
                  return;
               }
               if (tableau[2][1] <= 3)  {    // si diff�rence de MMs quasi nulles
                  virtual (OrderOpenPrice ());  // v�rifier trailing stop virtuel de l'ordre
                  if (clo == 1)  {           // => fermer l'ordre de magic2
                     fermer (magic2);
                     cxt = 10;               // recommencer au d�but
                     return;
                  }
                  //cxt = 40;                  // recalculer
                  if (nbpos == 2)   {
                     cxt = 40;
                  }  else if (nbpos == 1) {
                     cxt = 20;
                  }
                  return;
               }
            }                                // fin de achat magic2
            if (OrderType () == OP_SELL)   {    // si magic2 est en vente
                  ordre = 2;                    // ordre de vente
                  virtual (OrderOpenPrice ());  // v�rifier trailing stop virtuel de l'ordre
                  if (clo == 1)  {              // => fermer l'ordre de magic2
                     fermer (magic2);
                     cxt = 10;                  // recommencer au d�but
                     return;
                  }
                  if (tableau[2][1] > 0)  {     // si diff�rences de MMs positives
                     //cxt = 40;                  // ne rien faire
                     if (nbpos == 2)   {
                        cxt = 40;
                     }  else if (nbpos == 1) {
                        cxt = 20;
                     }
                     return;
                  }
                  if (tableau[2][1] <= 3)  {    // si diff�rence de MMs quasi nulles
                     virtual (OrderOpenPrice ());  // v�rifier trailing stop virtuel de l'ordre
                     if (clo == 1)  {           // => fermer l'ordre de magic2
                        fermer (magic2);
                        cxt = 10;               // recommencer au d�but
                        return;
                     }
                     //cxt = 40;                  // recalculer
                     if (nbpos == 2)   {
                        cxt = 40;
                     }  else if (nbpos == 1) {
                        cxt = 20;
                     }
                     return;
                  }
            }                                   // fin de vente magic2
         }                                      // fin de magic2    
      }                                         // fin de deux positions
   }                                            // fin de for pour �num�rer les ordres 
   //if (nbpos != 2)   {
   //   cxt = 20;      // calculer pour voir si autre position possible
   //   return;
   //}
   //cxt = 40;      // garder position et recalculer
//   cxt = 60;      // cl�turer position
      
}

//+------------------------------------------------------------------+
//| fonction d�tectant les nouveaux chandeliers                      |
//+------------------------------------------------------------------+
void detectbar()                                      // Funct. detecting ..
         {                                            // .. a new bar
         //mess = "dans la fonction detectbar()";       // texte pour d�bogage
                                                      // contexte EA
         static datetime New_Time=0;                  // Time of the current bar
         New_Bar=false;                               // No new bar
         if(New_Time!=Time[0])                        // Compare time
           {
            New_Time=Time[0];                         // Now time is so
            New_Bar=true;                             // A new bar detected
           }
         }
//+------------------------------------------------------------------+
//| fonction attendant un nouveau chandelier                         |
//+------------------------------------------------------------------+
void wait_candle ()  {
   //mess = "dans la fonction wait_candle ()";
   cxt = 25;
   detectbar ();
   if (New_Bar == false)   {
      //mess = "attends un nouveau chandelier";
      //info (mess);
      detectbar ();
   }  else  {
      mess = "Un nouveau chandelier est apparu!";
      //info (mess);
      for (int i=0; i<=9; i++)   {
         Print ("La valeur max de[",i,"] est de: ", tableau[9][i]);
         Print ("La valeur min de[",i,"] est de: ", tableau[8][i]);
      }
      cxt = 20;      // recalculer tableau
   }
}   

//+------------------------------------------------------------------+
//| fonction d'analyse de la volatilit�                              |
//+------------------------------------------------------------------+
int volatile (string symbole, int nbbars, double moy) {
    volt = 0;
    for (int i = 0; i < nbbars; i++)   {
      if (iHigh(symbole,PERIOD_M5,i) > moy && iLow(symbole,PERIOD_M5,i) < moy)
      volt++;
    }
    compt++;            // test de volatilit� effectu�
    int heure = TimeHour(iTime(symbole,PERIOD_M1,nbbars));
    int minute = TimeMinute(iTime(symbole,PERIOD_M1,nbbars));
    mess = "Les chandeliers son pass�s "+volt+" fois par moy depuis "+heure+" : "+minute+" min";
    cxt = 30;           // vers take_pos()
    return (volt);     //  retourne le nombre de fois o� les chandeliers ont coup� la moyenne moy
                       //  sur nbbars chandeliers
}
    

//+------------------------------------------------------------------+
//| fonction de trailing stop virtuel/cach�                          |
//+------------------------------------------------------------------+
int virtual (double open)  {
   double suiv;
   if (ordre == 1)   {        // si ordre d'achat
      //suiv = NormalizeDouble (Bid - trailing * Point, Digits);
      suiv = Bid;
      mess = StringConcatenate("up = ", up, " et anull =", anull, " et ordre = ", ordre, " et amin = ", amin);
      info (mess);      
      if (suiv <= amin) {     // si ordre perdant de "trailing" pips =>
         clo = 1;             // cl�turer l'ordre d'achat
         mess = "fermeture d\'ordre d\'achat d�cid� par Virtual()";
         info (mess);
         return (clo);
      }
      if (up == false && (suiv - trailing * Point) > amin && suiv > anull)  {    // si le d�passement de "trailing" pips n'a pas eu lieu
         amin = (suiv - trailing * Point);  // => trailing stop de "trailing" pips
         mess = StringConcatenate("Le amin vaut: ", amin, " Euros");
         info (mess); 
      } 
      if (suiv >= (anull + trailing * Point) && (suiv - 10 * Point) > amin)   {  // si d�passement de "trailing" pips gagants =>
         amin = (suiv - 10 * Point);    // trailing stop ramen� � 10 pips
         up = true;                    // indique que le trailing stop � "trailing" pips a eu lieu
         mess = StringConcatenate("Le amin a �t� d�pass� et vaut maintenant: ", amin, " Euros");
         info (mess); 
      }
      clo = 0;                // garder position
   }  else if (ordre == 2) {  // si ordre de vente
      suiv = Ask;
      mess = StringConcatenate("down = ", down, " et vnull =", vnull, " et ordre = ", ordre, " et vmax = ", vmax);
      info (mess);      
      if (suiv >= vmax) {     // si ordre perdant de "trailing" pips =>
         clo = 1;             // cl�turer l'ordre d'achat
         mess = "fermeture d\'ordre de vente d�cid� par Virtual()";
         info (mess);
         return (clo);
      }
      if (down == false && (suiv + trailing * Point) < vmax && suiv < vnull)  {  // si le d�passement de "trailing" pips n'a pas eu lieu
         vmax = (suiv + trailing * Point);  // => trailing stop de "trailing" pips
         mess = StringConcatenate("Le vmax vaut: ", vmax, " Euros");
         info (mess);
      } 
      if (suiv <= (vnull - trailing * Point) && (suiv + 10 * Point) < vmax)   {  // si d�passement de "trailing" pips gagants =>
         vmax = (suiv + 10 * Point);    // trailing stop ramen� � 10 pips
         down = true;                  // indique que le trailing stop � "trailing" pips a eu lieu
         mess = StringConcatenate("Le vmax a �t� d�pass� et vaut maintenant: ", vmax, " Euros");
         info (mess); 
      }
      clo = 0;   
   }                          // fin de v�rification ordre de vente  
   return(clo);   
}                             // fin de la fonction
//+------------------------------------------------------------------+
//| fonction de cl�ture d'ordres                                     |
//+------------------------------------------------------------------+
void fermer (int magicnumber) {
   int retour;
   for (int i = OrdersTotal () - 1; i >=0; i--)  {
      if (OrderSelect (i, SELECT_BY_POS, MODE_TRADES) == true) {
         if (OrderMagicNumber () == magicnumber)   {
            double prix ;
            int ticket = OrderTicket ();
            if (ordre == 1)   {                   // si ordre d'achat
               prix = Bid;                        // cl�turer au Bid
               retour = OrderClose (ticket, lot, prix, 3, Orange);
               if (retour < 1) {
                  error = GetLastError();
                  Print ("erreur de fermeture d\'ordre d\'achat: (",error,"): ", ErrorDescription(error));
                  return (0);
               }
               achat = true;                    // ordre d'achat d�sormais autoris� de nouveau
               up = false;                      // remise � z�ro du d�passement de "trailing" pips
               mess = "fermeture d\'ordre d\'achat";
               info (mess);
               if (Bid <= OrderOpenPrice ()) {
                  PlaySound ("boo.wav");
               }  else  {
                  PlaySound ("caisse.wav");
               }
            }  else if (ordre == 2) {             // si ordre de vente
               prix = Ask;                        // cl�turer � l'Ask
               retour = OrderClose (ticket, lot, prix, 3, Orange);
               if (retour < 1) {
                  error = GetLastError();
                  Print ("erreur de fermeture d\'ordre de vente: (",error,"): ", ErrorDescription(error));
                  return (0);
               }
               vente = true;                    // ordre de vente d�sormais autoris� de nouveau
               down = false;                    // remise � z�ro du d�passement de "trailing" pips
               mess = "fermeture d\'ordre de vente";
               info (mess);
               if (Ask >= OrderOpenPrice ()) {
                  PlaySound ("boo.wav");
               }  else  {
                  PlaySound ("caisse.wav");
               }
            }                 // fin de ordre de vente
         }                    // fin de num�ro magic identifi�
      }                       // fin de s�lection d'ordre
   }                          // fin de for pour �num�rer tout les ordres
   nbpos--;                   // d�cr�mente le nombre d'ordres actifs pass�s par lui-m�me
   clo = 0;                   // ne plus fermer l'ordre
   cxt = 10;                  // recommencer au d�but
}                             // fin de la fonction
//+------------------------------------------------------------------+
//| fonction de v�rification d'ordres en cours                       |
//+------------------------------------------------------------------+
//void act_ordres ()   {
  
//}

//+------------------------------------------------------------------+