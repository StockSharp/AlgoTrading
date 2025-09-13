int BarsTotal_OnInt; 
int BarsTotal_OnTick;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {  
   BarsTotal_OnInt = iBars(NULL,PERIOD_CURRENT); // Asign the total bars at initialization
   return(INIT_SUCCEEDED);
  }
  
void OnTick() // OnTick Function
  {   
   BarsTotal_OnTick = iBars(NULL,PERIOD_CURRENT); // Stores the latest amount
   
   if(BarsTotal_OnTick > BarsTotal_OnInt) // New bar has arrived
   {
    BarsTotal_OnInt = BarsTotal_OnTick; // Updates the history.
    Alert("New Bar has arrived");
    Comment("Bars Count in history -: ", BarsTotal_OnInt, "\n", "Bars Count in Live -: ", BarsTotal_OnTick);
   }
  }
