//+------------------------------------------------------------------+
//|                                                         Rich.mq4 |
//|                                              © 2008, Christopher |
//|                               http://www.expert-profit.webs.com/ |
//+------------------------------------------------------------------+
//
// This EA uses MapPath file ("rl.txt" by default) from your 
// /tester/files/ (for strategy tester) or /experts/files/ (for actual use)
// to get the initial statistics. It also continues to gather its own 
// statistics and appends it to the initial file, saving it after
// deinitilizing.
//

#property copyright "© 2010, Christopher"
#property link      "http://www.expert-profit.webs.com/"

#define BUY_MAP 1
#define SELL_MAP 0
#define HOLD_MAP 2

//Map size for BUY and SELL
#define MAPBASE 10000
//Map size for HOLD
#define HOLDBASE 25000
//Number of map memory-cells for each bar
#define VBASE 7

int LastBars = 0;
double vector[VBASE]; //Vector with memory-cells for current bar
double vectorp[VBASE]; //Vector with memory-cells for previous bar

//3 Maps
double MapBuy[MAPBASE][VBASE];
double MapSell[MAPBASE][VBASE];
double MapHold[HOLDBASE][VBASE];

//Min and Max amounts of pips of profit to consider teaching BUY and SELL maps
extern int MinPips = 5;
extern int MaxPips = 43;

extern int TakeProfit = 150;
extern int StopLoss = 100;

extern double Lots = 1;
extern double Slippage = 3;

extern string MapPath = "rl.txt";
extern string EAName = "RowLearner";

int Magic;

double TLots;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
{
   Magic = Period()+1937004;

   InitKohonenMap();

   LoadKohonenMap();

   TLots = Lots;

   return(0);
}

//+------------------------------------------------------------------+
//| main expert function                                             |
//+------------------------------------------------------------------+
int start()
{
   if (Bars < 7) return(0);

   //Wait for the new Bar in a chart.
	if (LastBars == Bars) return(0);
	else LastBars = Bars;

   CloseAllOrders();
   
   double bmu[3] = {0, 0, 0};
   
   //Calculating the Tom Demark's pivot points over last 5 bars
   double hi[5] = {0,0,0,0,0};
   double lo[5] = {0,0,0,0,0};

   hi[0] = High[2];
   hi[1] = High[3];
   hi[2] = High[4];
   hi[3] = High[5];
   hi[4] = High[6];

   lo[0] = Low[2];
   lo[1] = Low[3];
   lo[2] = Low[4];
   lo[3] = Low[5];
   lo[4] = Low[6];

   double H = hi[ArrayMaximum(hi)];
   double L = lo[ArrayMinimum(lo)];
   
   vectorp[0] = (H + L + Close[2]) / 3;
   //The difference between pivots and Open price is used to normalize statistics
   vectorp[1] = (2 * vectorp[0] - L) - Open[1];
   vectorp[2] = (vectorp[0] + H - L) - Open[1];
   vectorp[3] = (H + 2 * (vectorp[0] - L)) - Open[1];
   vectorp[4] = (2 * vectorp[0] - H) - Open[1];
   vectorp[5] = (vectorp[0] - H + L) - Open[1];
   vectorp[6] = (L - 2 * (H - vectorp[0])) - Open[1];
   vectorp[0] = vectorp[0] - Open[1];

   hi[0] = High[1];
   hi[1] = High[2];
   hi[2] = High[3];
   hi[3] = High[4];
   hi[4] = High[5];

   lo[0] = Low[1];
   lo[1] = Low[2];
   lo[2] = Low[3];
   lo[3] = Low[4];
   lo[4] = Low[5];

   H = hi[ArrayMaximum(hi)];
   L = lo[ArrayMinimum(lo)];

   vector[0] = (H + L + Close[1]) / 3;
   vector[1] = (2 * vector[0] - L) - Open[0];
   vector[2] = (vector[0] + H - L) - Open[0];
   vector[3] = (H + 2 * (vector[0] - L)) - Open[0];
   vector[4] = (2 * vector[0] - H) - Open[0];
   vector[5] = (vector[0] - H + L) - Open[0];
   vector[6] = (L - 2 * (H - vector[0])) - Open[0];
   vector[0] = vector[0] - Open[0];

   MapLookup(vector, bmu);
	
	TLots = (MathFloor(AccountBalance()/50))/10; 
	
   if ((bmu[0] < bmu[1]) && (bmu[0] < bmu[2])) Buy();
   else if ((bmu[1] < bmu[0]) && (bmu[1] < bmu[2])) Sell();
   
   Print("BMU Buy: ", bmu[0], " BMU Sell: ", bmu[1], " BMU Hold: ", bmu[2]);

   if ((NormalizeDouble((Open[0] - Open[1]), Digits) >= NormalizeDouble((MinPips*Point), Digits)) && (NormalizeDouble((Open[0] - Open[1]), Digits) <= NormalizeDouble((MaxPips*Point), Digits))) TeachMap(BUY_MAP, vectorp);
   else if ((NormalizeDouble((Open[0] - Open[1]), Digits) <= -NormalizeDouble((MinPips*Point), Digits)) && (NormalizeDouble((Open[0] - Open[1]), Digits) >= -NormalizeDouble((MaxPips*Point), Digits)))  TeachMap(SELL_MAP, vectorp);
   else TeachMap(HOLD_MAP, vectorp);

   return(0);
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
{
   SaveKohonenMap();
}

//+------------------------------------------------------------------+
//| Close all open orders                                            |
//+------------------------------------------------------------------+
void CloseAllOrders()
{
   int total = OrdersTotal();
	for(int pos = 0;pos < total; pos++)
	{
		if(OrderSelect(pos, SELECT_BY_POS) == true)
		{
			if (OrderMagicNumber() == Magic)
    		{
	        	if (OrderSelect(pos, SELECT_BY_POS, MODE_TRADES) == true)
	        	{
    	    		if (OrderSymbol() == Symbol())  //Check for symbol
					{
						int err = 0;
						int count = 0;
						while ((err != 1) && (count < 10))
						{
						   if (OrdersTotal() == 0) return(0);
						   count++;
							RefreshRates();
							if (OrderType() == OP_SELL) err = OrderClose(OrderTicket(),OrderLots(),Ask,10,Violet); //Close position
							else if (OrderType() == OP_BUY) err = OrderClose(OrderTicket(),OrderLots(),Bid,10,Violet); //Close position
						}
					}
				}
			}
		}
	}
}

void Buy()
{
	  int result = -1;
	  int count = 0;
	  while ((result == -1) && (count < 10))
	  {
        RefreshRates();
        count++;
        double SL = 0;
        double TP = 0;
        if (StopLoss == 0) SL = 0;
	     else SL = Ask-StopLoss*Point;
        if (TakeProfit == 0) TP = 0;
	     else TP = Ask+TakeProfit*Point;
	     if (IsTradeAllowed()) result = OrderSend(Symbol(),OP_BUY,TLots,Ask,Slippage,SL,TP,EAName,Magic,0); 
        if (result == -1)
        {
           int e = GetLastError();
           Print(e);
        }
     }
}

void Sell()
{
	int result = -1;
	int count = 0;
	while ((result == -1) && (count < 10))
	{
      RefreshRates();
      count++;
      double SL = 0;
      double TP = 0;
      if (StopLoss == 0) SL = 0;
	   else SL = Bid+StopLoss*Point;
      if (TakeProfit == 0) TP = 0;
	   else TP = Bid-TakeProfit*Point;
      if (IsTradeAllowed()) result = OrderSend(Symbol(),OP_SELL,TLots,Bid,Slippage,SL,TP,EAName,Magic,0);
      if (result == -1)
      {
         int e = GetLastError();
         Print(e);
      }
   }
}

void InitKohonenMap()
{
   for (int i = 0; i < MAPBASE; i++)
   {
      for (int v = 0; i < VBASE; i++)
      {
         MapSell[i][v] = 0;
         MapBuy[i][v] = 0;
      }
   }
   for (i = 0; i < HOLDBASE; i++)
   {
      for (v = 0; i < VBASE; i++)
      {
         MapHold[i][v] = 0;
      }
   }
}

void MapLookup(double vector[], double& BMU[])
{
   BMU[0] = FindBMU(BUY_MAP, vector);

	BMU[1] = FindBMU(SELL_MAP, vector);

	BMU[2] = FindBMU(HOLD_MAP, vector);
	
	int i = 0;

	//BUY
	for (i = 0; i < MAPBASE; i++)
	{
      int z  = 0;
      double vec[VBASE];
      for (int v = 0; v < VBASE; v++)
	   {
	     if (MapBuy[i][v] == 0) z++;
	     vec[v] = MapBuy[i][v];
	   }
	   if (z == VBASE) break;
	  
	   double E = EuclidDistance(vec, vector);
	}

	//SELL
	for (i = 0; i < MAPBASE; i++)
	{
      z  = 0;
      for (v = 0; v < VBASE; v++)
	   {
	     if (MapSell[i][v] == 0) z++;
	     vec[v] = MapSell[i][v];
	   }
	   if (z == VBASE) break;
	  
	   E = EuclidDistance(vec, vector);
	}

	//HOLD
	for (i = 0; i < HOLDBASE; i++)
	{
      z  = 0;
      for (v = 0; v < VBASE; v++)
	   {
	     if (MapHold[i][v] == 0) z++;
	     vec[v] = MapHold[i][v];
	   }
	   if (z == VBASE) break;
	  
	   E = EuclidDistance(vec, vector);
	}
}

double FindBMU(int Buy, double vector[])
{
	int N = 0;

	if (Buy == 1) N = MAPBASE;
	else if (Buy == 0) N = MAPBASE;
	else N = HOLDBASE;

	double BestEuclidDistance = 9999999;
	double vec[VBASE];
	for (int i = 0; i < N; i++)
	{
		double E;
      for (int v = 0; v < VBASE; v++)
	   {
	     if (Buy == 1) vec[v] = MapBuy[i][v];
	     else if (Buy == 0) vec[v] = MapSell[i][v];
	     else vec[v] = MapHold[i][v];
		}
		E = EuclidDistance(vec, vector);
		if (E < BestEuclidDistance)
		{
			BestEuclidDistance = E;
		}
	}

	return(BestEuclidDistance);
}

double EuclidDistance(double VectorFromMap[], double vector[])
{
	double E = 0;
	
	for (int v = 0; v < VBASE; v++)
	{
		E += MathPow((VectorFromMap[v]*10000 - vector[v]*10000), 2);
	}

	E = MathSqrt(E);

	return(E);
}

void TeachMap(int Buy, double vector[])
{
	int BMUx = -1;

	int N;

	int x;

	if (Buy == 1) N = MAPBASE;
	else if (Buy == 0) N = MAPBASE;
	else N = HOLDBASE;

	for (x = 0; x < N; x++)
	{
		bool flag = false;
		for (int v = 0; v < VBASE; v++)
		{
			if (Buy == 1) {if (MapBuy[x][v] != 0) flag = true;}
			else if (Buy == 0) {if (MapSell[x][v] != 0) flag = true;}
			else {if (MapHold[x][v] != 0) flag = true;}
		}
		if (flag == false) break;
	}

	for (v = 0; v < VBASE; v++)
	{
		if (Buy == 1) MapBuy[x][v] = vector[v];
		else if (Buy == 0) MapSell[x][v] = vector[v];
		else MapHold[x][v] = vector[v];
	}

}

void LoadKohonenMap()
{
   int handle = FileOpen(MapPath, FILE_BIN|FILE_WRITE|FILE_READ);
   if (handle < 1)
   {
      Print("File couldn't be opened; the last error is ", GetLastError());
      return(0);
   }
   FileReadArray(handle, MapBuy, 0, MAPBASE*VBASE);
   FileReadArray(handle, MapSell, 0, MAPBASE*VBASE);
   FileReadArray(handle, MapHold, 0, HOLDBASE*VBASE);
   FileClose(handle);
}

void SaveKohonenMap()
{
   int handle = FileOpen(MapPath, FILE_BIN|FILE_WRITE|FILE_READ);
   if (handle < 1)
   {
      Print("File couldn't be opened; the last error is ", GetLastError());
      return(0);
   }
   FileWriteArray(handle, MapBuy, 0, MAPBASE*VBASE);
   FileWriteArray(handle, MapSell, 0, MAPBASE*VBASE);
   FileWriteArray(handle, MapHold, 0, HOLDBASE*VBASE);
}  