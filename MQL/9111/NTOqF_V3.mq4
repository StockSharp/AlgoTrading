//+------------------------------------------------------------------+
//|                                           NTOqF V2.mq4 12/08/2009|
//|                                            Rafael Maia de Amorim |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Rafael Maia de Amorim"
#property link      "http://www.metaquotes.net"
extern string Autor = "Rafael Maia de Amorim";
//---- input parameters
extern double    Lots=0.01;
extern double    TakeProfit=80;
extern double    StopLoss=10;
extern string    Desc1 = "Always optimize on M1 timeframe, use the TimeFrame";
extern string    Desc2 = " variable to define the timeframe you want.";
extern string    Desc3 = "Use (5) for 5 minutes";
extern string    Desc4 = "Use (15) for 15 minutes";
extern string    Desc5 = "Use (30) for 30 minutes";
extern string    Desc6 = "Use (60) for 1 Hour";
extern string    Desc7 = "Use (240) for 4 Hour";
extern string    Desc8 = "Use (1440) for 1 Daily";
extern string    Desc9 = "Thats are the common values, always in minutes";
extern string    Desc10 = "You can set diferent timeframes to each signal";
extern int       RSITimeFrame = 0;
extern int       STOTimeFrame = 0;
extern int       ADXTimeFrame = 0;
extern int       ADXMainTimeFrame = 0;
extern int       SARTimeFrame = 0;
extern int       MATimeFrame = 0;


extern string    Desc11 = "Select true if do you want to use RSI to check the trend signal";
extern bool      useRSI=true;
extern int       RSI=4;
extern int       RSIPos=90;
extern int       RSINeg=10;

extern string    Desc12 = "Select true if do you want to use Stochastic to check the trend signal";
extern bool      useSTO = false;
extern int       STOK=5;
extern int       STOD=3;
extern int       STOL=3;

extern string    Desc13 = "Select true if do you want to use Stochastic";
extern string    Desc14 = "High / Low to determine the trend";
extern bool      useSTOHighLow = false;
extern int       STOHigh=80;
extern int       STOLow=20;

extern string    Desc15 = "Select true if do you want to use ADX to check the trend signal";
extern bool      useADX = false;
extern int       ADX=50;

extern string    Desc16 = "Select true if do you want to use ADX Main Signal to check the trend signal";
extern bool      useADXMain = false;
extern int       ADXMainValue = 25;


extern string    Desc17 = "Select true if do you want to use Parabolic SARS to check the trend signal";
extern bool      useSAR = false;
extern double    SARStep = 0.02;
extern double    SARMax = 0.2;

extern string    Desc18 = "Select true if do you want to use";
extern string    Desc19 = "Moving Average vs Close Price to trend signal";
extern bool      useMA = false;
extern double    MAPeriod = 50;
extern int       MAShift = 0;
extern string    Desc20 = "Select method to moving average";
extern string    Desc21 = "0 to Simple moving average";
extern string    Desc22 = "1 to Exponential moving average";
extern string    Desc23 = "2 to Smoothed moving average";
extern string    Desc24 = "3 to Linear weighted moving average";
extern int       MAMethod = 0;
extern string    Desc25 = "Select Applied Price to moving average";
extern string    Desc26 = "0 to Close Price";
extern string    Desc27 = "1 to Open Price";
extern string    Desc28 = "2 to High Price";
extern string    Desc29 = "3 to Low Price";
extern string    Desc30 = "4 to Median price, (high+low)/2";
extern string    Desc31 = "5 to Typical price, (high+low+close)/3";
extern string    Desc32 = "6 to Weighted close price, (high+low+close+close)/4";
extern int       MAPrice = 0;


extern string    Desc100 = "Select true if do you want to use TrailingStop";
extern bool      useTrailingStop = false;
extern int       TrailingStop = 6;

extern int       Shift = 1;
extern int       NumeroMagico = 1000; // Magic Number
extern string    Info1 = "If you make money with this EA, please help me, i don´t have money to start...";
extern string    Info2 = "alertpay or paypal: rdamorim@click21.com.br";
extern string    Info3 = "Thanks a lot";





//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
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
   double p = Point;
   int    OrdersPerSymbol=0;
   int    cnt=0;
   if(AccountFreeMargin()<(1000*Lots))        {Print("No money"); return(0);}
   if(Bars<100)                               {Print("-----NO BARS "); return(0);}
   if (useTrailingStop == true)
   {
    //Start of TrailingStop
    int Total = OrdersTotal(); // Numero Total de ordens no MetaTrader      
    int NumOrdensBuy  = 0;             // Numero de ordens buy neste consultor       
    int NumOrdensSell = 0;             // Numero de ordens sell neste consultor       
    int Ticket        = 0;             // Numero de ordem requisitada ao servidor       
    int Lucro         = 0;             // Lucro da ordem atual       
    int Atual         = 0;             // Diferenta atual entre SL e o preço 
      
    int TipoOrd       = -1;            // Tipo de ordem a a abri -1 = nenhuma       
    int i;                             // utilizado nos loops   
    double NovoSL  = 0; // Novo valor para o SL          
    double LocalTP = 0; // TP calculado para a ordem
 
         
    double LocalSL = 0; // SL calculado para a ordem          
    double Preco;       // preço para abrir a ordem
 
    // Conta as ordens abertas por este sistema   
    for(i=0;i<Total;i++) 
    { 
    // passa por todas as ordens abertas     
    // seleciono a ordem da lista de ordens      
    // pela localização da mesma na lista     
    OrderSelect(i,SELECT_BY_POS,MODE_TRADES);     // se a ordem pertence a este par e tem este número mágico     
    if ((OrderSymbol()==Symbol()) && (OrderMagicNumber()==NumeroMagico)) 
    {
           // Cacula o lucro da ordem em pips       
           Lucro = OrderProfit() / Point;         
           // Zera variavel para novo SL       
           NovoSL = 0;       
           if (OrderType()==OP_BUY) 
           {         
               NumOrdensBuy++; 
               // Conta se for Buy         
               //se tem trailing entao calculoa novo SL         
               if (TrailingStop>0) 
               {           
               // Calcula quantos pips do preço atual esta o SL           
               // lembre-se na ordem buy o SL esta sempre abaixo do preço           
               Atual = (OrderClosePrice()-OrderStopLoss())/Point;           
               // so usa o trailing se estiver com lucro maior que o proprio trailing           
                // e se o SL esta abaixo do Trailing esperado           
                if ((Lucro>TrailingStop) && (Atual>TrailingStop))             
                NovoSL = OrderClosePrice() - (TrailingStop*Point);         
                }        
            }        
            if (OrderType()==OP_SELL) 
            {
                     NumOrdensSell++;
                      // Conta se for Sell         
                      //se tem trailing entao calculoa novo SL 
                      if (TrailingStop>0) 
                      {           
                                 // Calcula quantos pips do preço atual esta o SL
                                 // lembre-se na ordem sell o SL esta sempre acima do preço
                                 Atual = (OrderStopLoss()-OrderClosePrice())/Point;
                                 // so usa o trailing se estiver com lucro maior que o proprio trailing            
                                 // e se o SL esta abaixo do Trailing esperado           
                                 if ((Lucro>TrailingStop) && (Atual>TrailingStop))              
                                 NovoSL = OrderClosePrice() + (TrailingStop*Point);         
                      }        
             }        
             // check New Stoploss
             if (NovoSL>0) 
             {
                      // Try modify order         
                      //bool Ok = 
                      OrderModify(OrderTicket(), OrderOpenPrice(),NovoSL,OrderTakeProfit(), 0,Red);
                      // Check error          
                      //if (!Ok) Print(" Error : ",ErrorDescription(GetLastError()));
              }      
         }    
    }
   
   
   
   //fim   

   }
   //check number of orders
   OrdersPerSymbol=0;
   for(cnt=OrdersTotal();cnt>=0;cnt--)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if( OrderSymbol()==Symbol() )
        {
         OrdersPerSymbol++;
        }
     }
   //only one order by Symbol
   if(OrdersPerSymbol<1)
     {
      if(Signal() == 1)
		  {
         OrderSend(Symbol(),OP_BUY,Lots,Ask,0,Ask-(StopLoss*p),Ask+(TakeProfit*p),"Compra  "+CurTime(),NumeroMagico,0,White);
         return(0);
        }
        
      if(Signal() == 2)
        {
         OrderSend(Symbol(),OP_SELL,Lots,Bid,0,Bid+(StopLoss*p),Bid-(TakeProfit*p),"Venda "+CurTime(),NumeroMagico,0,Red);
         return(0);
        }
     }

   return(0);
  }
  
  
int Signal()
  {
   int config = 0;
   int Trend = 0; //1 = buy, 2 = sell
   
   //set RSI Trend
   if (useRSI == true)
   {
      double RSIValue = 0;
      int RSITrend = 0;//1 = buy, 2 = sell
   
      RSIValue = iRSI(Symbol(),RSITimeFrame,RSI,PRICE_CLOSE, Shift);
      if (RSIValue > RSIPos)
      {
         RSITrend = 2;
      }
      else if (RSIValue < RSINeg)
      {
         RSITrend = 1;
      }
      else
      {
         RSITrend = 0;
      }
   }
   //end RSI
   
   //set STO Trend
   if (useSTO == true)
   {
      double STOValue = 0;
      double STOSignal = 0;
      int STOTrend = 0; //1 = buy, 2 = sell
   
      STOValue = iStochastic(Symbol(),STOTimeFrame,STOK,STOD,STOL,0,0,MODE_MAIN,Shift);
      STOSignal = iStochastic(Symbol(),STOTimeFrame,STOK,STOD,STOL,0,0,MODE_SIGNAL,Shift);
      if (useSTOHighLow == true)
      {
         if (STOValue > STOSignal && STOValue > STOHigh)
         {
            STOTrend = 1;
         }
         if (STOValue < STOSignal && STOValue < STOLow)
         {
            STOTrend = 2;
         }
      }
      else
      {
         if (STOValue > STOSignal)
         {
            STOTrend = 1;
         }
         else
         {
            STOTrend = 2;
         }
      }
   }

   //end STO
   
   //set ADX Trend
   if (useADX == true)
   {
      double ADXMinus = 0;
      double ADXPlus = 0;
      int ADXTrend = 0;  //1 = buy, 2 = sell

      ADXMinus = iADX(Symbol(),ADXTimeFrame,ADX,PRICE_LOW,MODE_MINUSDI,Shift);
      ADXPlus = iADX(Symbol(),ADXTimeFrame,ADX,PRICE_LOW,MODE_PLUSDI,Shift);
   
  
      if (ADXMinus < ADXPlus)
      {
         ADXTrend = 1;
      }
      else
      {
         ADXTrend = 2;
      }
   }
   //end ADX
   //ADX Main Value
   if (useADXMain == true)
   {
      double ADXMain = 0;
      ADXMain = iADX(Symbol(),ADXMainTimeFrame,ADX,PRICE_LOW,MODE_MAIN,Shift);
   }
   //end ADX Main Value


   //SAR Trend signal
   if (useSAR == true)
   {
      int SARTrend = 0;   //1 = buy, 2 = sell
      double SARSignal = 0;
   
      SARSignal = iSAR(Symbol(),SARTimeFrame,SARStep,SARMax, Shift);
      if (SARSignal > Close[Shift])
      {
         SARTrend = 1;
      }
      else
      {
         SARTrend = 2;
      }
   }
   //end SAR
   
   //MA Trend signal
   int MATrend = 0;   //1 = buy, 2 = sell
   double MASignal = 0;
   if (useMA == true)
   {
      MASignal = iMA(Symbol(),MATimeFrame,MAPeriod, MAShift, MAMethod, MAPrice, Shift);
      if (MASignal < Close[Shift])
         {
            MATrend = 1;
         }
         else
         {
            MATrend = 2;
         }
   }
   //end MA
   
   //check Indicators, set config
   if (useRSI == false && useSTO == false && useADX == true && useADXMain == false && useSAR == false && useMA == false) {config = 1;}
   if (useRSI == false && useSTO == true && useADX == false && useADXMain == false && useSAR == false && useMA == false) {config = 2;}
   if (useRSI == false && useSTO == true && useADX == true && useADXMain == false && useSAR == false && useMA == false) {config = 3;}
   if (useRSI == true && useSTO == false && useADX == false && useADXMain == false && useSAR == false && useMA == false) {config = 4;}
   if (useRSI == true && useSTO == false && useADX == true && useADXMain == false && useSAR == false && useMA == false) {config = 5;}
   if (useRSI == true && useSTO == true && useADX == false && useADXMain == false && useSAR == false && useMA == false) {config = 6;}
   if (useRSI == true && useSTO == true && useADX == true && useADXMain == false && useSAR == false && useMA == false) {config = 7;}
   if (useRSI == false && useSTO == false && useADX == true && useADXMain == true && useSAR == false && useMA == false) {config = 8;}
   if (useRSI == false && useSTO == true && useADX == false && useADXMain == true && useSAR == false && useMA == false) {config = 9;}
   if (useRSI == false && useSTO == true && useADX == true && useADXMain == true && useSAR == false && useMA == false) {config = 10;}
   if (useRSI == true && useSTO == false && useADX == false && useADXMain == true && useSAR == false && useMA == false) {config = 11;}
   if (useRSI == true && useSTO == false && useADX == true && useADXMain == true && useSAR == false && useMA == false) {config = 12;}
   if (useRSI == true && useSTO == true && useADX == false && useADXMain == true && useSAR == false && useMA == false) {config = 13;}
   if (useRSI == true && useSTO == true && useADX == true && useADXMain == true && useSAR == false && useMA == false) {config = 14;}
   if (useRSI == false && useSTO == false && useADX == true && useADXMain == false && useSAR == true && useMA == false) {config = 15;}
   if (useRSI == false && useSTO == true && useADX == false && useADXMain == false && useSAR == true && useMA == false) {config = 16;}
   if (useRSI == false && useSTO == true && useADX == true && useADXMain == false && useSAR == true && useMA == false) {config = 17;}
   if (useRSI == true && useSTO == false && useADX == false && useADXMain == false && useSAR == true && useMA == false) {config = 18;}
   if (useRSI == true && useSTO == false && useADX == true && useADXMain == false && useSAR == true && useMA == false) {config = 19;}
   if (useRSI == true && useSTO == true && useADX == false && useADXMain == false && useSAR == true && useMA == false) {config = 20;}
   if (useRSI == true && useSTO == true && useADX == true && useADXMain == false && useSAR == true && useMA == false) {config = 21;}
   if (useRSI == false && useSTO == false && useADX == true && useADXMain == true && useSAR == true && useMA == false) {config = 22;}
   if (useRSI == false && useSTO == true && useADX == false && useADXMain == true && useSAR == true && useMA == false) {config = 23;}
   if (useRSI == false && useSTO == true && useADX == true && useADXMain == true && useSAR == true && useMA == false) {config = 24;}
   if (useRSI == true && useSTO == false && useADX == false && useADXMain == true && useSAR == true && useMA == false) {config = 25;}
   if (useRSI == true && useSTO == false && useADX == true && useADXMain == true && useSAR == true && useMA == false) {config = 26;}
   if (useRSI == true && useSTO == true && useADX == false && useADXMain == true && useSAR == true && useMA == false) {config = 27;}
   if (useRSI == true && useSTO == true && useADX == true && useADXMain == true && useSAR == true && useMA == false) {config = 28;}   
   if (useRSI == false && useSTO == false && useADX == true && useADXMain == false && useSAR == false && useMA == true) {config = 29;}
   if (useRSI == false && useSTO == true && useADX == false && useADXMain == false && useSAR == false && useMA == true) {config = 30;}
   if (useRSI == false && useSTO == true && useADX == true && useADXMain == false && useSAR == false && useMA == true) {config = 31;}
   if (useRSI == true && useSTO == false && useADX == false && useADXMain == false && useSAR == false && useMA == true) {config = 32;}
   if (useRSI == true && useSTO == false && useADX == true && useADXMain == false && useSAR == false && useMA == true) {config = 33;}
   if (useRSI == true && useSTO == true && useADX == false && useADXMain == false && useSAR == false && useMA == true) {config = 34;}
   if (useRSI == true && useSTO == true && useADX == true && useADXMain == false && useSAR == false && useMA == true) {config = 35;}
   if (useRSI == false && useSTO == false && useADX == true && useADXMain == true && useSAR == false && useMA == true) {config = 36;}
   if (useRSI == false && useSTO == true && useADX == false && useADXMain == true && useSAR == false && useMA == true) {config = 37;}
   if (useRSI == false && useSTO == true && useADX == true && useADXMain == true && useSAR == false && useMA == true) {config = 38;}
   if (useRSI == true && useSTO == false && useADX == false && useADXMain == true && useSAR == false && useMA == true) {config = 39;}
   if (useRSI == true && useSTO == false && useADX == true && useADXMain == true && useSAR == false && useMA == true) {config = 40;}
   if (useRSI == true && useSTO == true && useADX == false && useADXMain == true && useSAR == false && useMA == true) {config = 41;}
   if (useRSI == true && useSTO == true && useADX == true && useADXMain == true && useSAR == false && useMA == true) {config = 42;}
   if (useRSI == false && useSTO == false && useADX == true && useADXMain == false && useSAR == true && useMA == true) {config = 43;}
   if (useRSI == false && useSTO == true && useADX == false && useADXMain == false && useSAR == true && useMA == true) {config = 44;}
   if (useRSI == false && useSTO == true && useADX == true && useADXMain == false && useSAR == true && useMA == true) {config = 45;}
   if (useRSI == true && useSTO == false && useADX == false && useADXMain == false && useSAR == true && useMA == true) {config = 46;}
   if (useRSI == true && useSTO == false && useADX == true && useADXMain == false && useSAR == true && useMA == true) {config = 47;}
   if (useRSI == true && useSTO == true && useADX == false && useADXMain == false && useSAR == true && useMA == true) {config = 48;}
   if (useRSI == true && useSTO == true && useADX == true && useADXMain == false && useSAR == true && useMA == true) {config = 49;}
   if (useRSI == false && useSTO == false && useADX == true && useADXMain == true && useSAR == true && useMA == true) {config = 50;}
   if (useRSI == false && useSTO == true && useADX == false && useADXMain == true && useSAR == true && useMA == true) {config = 51;}
   if (useRSI == false && useSTO == true && useADX == true && useADXMain == true && useSAR == true && useMA == true) {config = 52;}
   if (useRSI == true && useSTO == false && useADX == false && useADXMain == true && useSAR == true && useMA == true) {config = 53;}
   if (useRSI == true && useSTO == false && useADX == true && useADXMain == true && useSAR == true && useMA == true) {config = 54;}
   if (useRSI == true && useSTO == true && useADX == false && useADXMain == true && useSAR == true && useMA == true) {config = 55;}
   if (useRSI == true && useSTO == true && useADX == true && useADXMain == true && useSAR == true && useMA == true) {config = 56;}      
   
   
   //end indicators with config
   
   //return signal by config
   switch(config)
   {
   case 1:
      if (ADXTrend == 1)
      {
         Trend = 1;
      }
      else if(ADXTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 2:
      if (STOTrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 3:
      if (STOTrend == 1 && ADXTrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && ADXTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 4:
      if (RSITrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 5:
      if (RSITrend == 1 && ADXTrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && ADXTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 6:
      if (RSITrend == 1 && STOTrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && STOTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 7:
      if (RSITrend == 1 && STOTrend == 1 && ADXTrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 1 && STOTrend == 1 && ADXTrend == 1)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 8:
      if (ADXTrend == 1 && ADXMain > ADXMainValue)
      {
         Trend = 1;
      }
      else if(ADXTrend == 2 && ADXMain > ADXMainValue)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 9:
      if (STOTrend == 1 && ADXMain > ADXMainValue)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && ADXMain > ADXMainValue)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 10:
      if (STOTrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && ADXTrend == 2 && ADXMain > ADXMainValue)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 11:
      if (RSITrend == 1 && ADXMain > ADXMainValue)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && ADXMain > ADXMainValue)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 12:
      if (RSITrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && ADXTrend == 2 && ADXMain > ADXMainValue)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 13:
      if (RSITrend == 1 && STOTrend == 1 && ADXMain > ADXMainValue)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && STOTrend == 2 && ADXMain > ADXMainValue)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 14:
      if (RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue)
      {
         Trend = 1;
      }
      else if(RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 15:
      if (ADXTrend == 1 && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(ADXTrend == 2 && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 16:
      if (STOTrend == 1 && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 17:
      if (STOTrend == 1 && ADXTrend == 1 && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && ADXTrend == 2 && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 18:
      if (RSITrend == 1 && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 19:
      if (RSITrend == 1 && ADXTrend == 1 && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && ADXTrend == 2 && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 20:
      if (RSITrend == 1 && STOTrend == 1 && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && STOTrend == 2 && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 21:
      if (RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 22:
      if (ADXTrend == 1 && ADXMain > ADXMainValue && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(ADXTrend == 2 && ADXMain > ADXMainValue && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 23:
      if (STOTrend == 1 && ADXMain > ADXMainValue && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && ADXMain > ADXMainValue && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 24:
      if (STOTrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && ADXTrend == 2 && ADXMain > ADXMainValue && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 25:
      if (RSITrend == 1 && ADXMain > ADXMainValue && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && ADXMain > ADXMainValue && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 26:
      if (RSITrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && ADXTrend == 2 && ADXMain > ADXMainValue && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 27:
      if (RSITrend == 1 && STOTrend == 1 && ADXMain > ADXMainValue && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && STOTrend == 2 && ADXMain > ADXMainValue && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 28:
      if (RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue && SARTrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue && SARTrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 29:
      if (ADXTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(ADXTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 30:
      if (STOTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 31:
      if (STOTrend == 1 && ADXTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && ADXTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 32:
      if (RSITrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 33:
      if (RSITrend == 1 && ADXTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && ADXTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 34:
      if (RSITrend == 1 && STOTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && STOTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 35:
      if (RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 36:
      if (ADXTrend == 1 && ADXMain > ADXMainValue && MATrend == 1)
      {
         Trend = 1;
      }
      else if(ADXTrend == 2 && ADXMain > ADXMainValue && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 37:
      if (STOTrend == 1 && ADXMain > ADXMainValue && MATrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && ADXMain > ADXMainValue && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 38:
      if (STOTrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue && MATrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && ADXTrend == 2 && ADXMain > ADXMainValue && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 39:
      if (RSITrend == 1 && ADXMain > ADXMainValue && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && ADXMain > ADXMainValue && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 40:
      if (RSITrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && ADXTrend == 2 && ADXMain > ADXMainValue && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 41:
      if (RSITrend == 1 && STOTrend == 1 && ADXMain > ADXMainValue && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && STOTrend == 2 && ADXMain > ADXMainValue && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 42:
      if (RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 43:
      if (ADXTrend == 1 && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(ADXTrend == 2 && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 44:
      if (STOTrend == 1 && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 45:
      if (STOTrend == 1 && ADXTrend == 1 && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && ADXTrend == 2 && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 46:
      if (RSITrend == 1 && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 47:
      if (RSITrend == 1 && ADXTrend == 1 && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && ADXTrend == 2 && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 48:
      if (RSITrend == 1 && STOTrend == 1 && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && STOTrend == 2 && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 49:
      if (RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 50:
      if (ADXTrend == 1 && ADXMain > ADXMainValue && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(ADXTrend == 2 && ADXMain > ADXMainValue && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 51:
      if (STOTrend == 1 && ADXMain > ADXMainValue && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && ADXMain > ADXMainValue && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 52:
      if (STOTrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(STOTrend == 2 && ADXTrend == 2 && ADXMain > ADXMainValue && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 53:
      if (RSITrend == 1 && ADXMain > ADXMainValue && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && ADXMain > ADXMainValue && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 54:
      if (RSITrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && ADXTrend == 2 && ADXMain > ADXMainValue && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 55:
      if (RSITrend == 1 && STOTrend == 1 && ADXMain > ADXMainValue && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 2 && STOTrend == 2 && ADXMain > ADXMainValue && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   case 56:
      if (RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue && SARTrend == 1 && MATrend == 1)
      {
         Trend = 1;
      }
      else if(RSITrend == 1 && STOTrend == 1 && ADXTrend == 1 && ADXMain > ADXMainValue && SARTrend == 2 && MATrend == 2)
      {
         Trend = 2;
      }
      else
      {
         Trend = 0;
      }
      return (Trend);
      break;
   default:
      Trend = 0;
      return (Trend);
      break;
   
   }
   //end return signal
   
  }
//+------------------------------------------------------------------+