//+------------------------------------------------------------------+
//|                                              TuyulUncensored.mq5 |
//|                                  Copyright 2025, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "zvickyhac"
#property description "Tuyul Uncensored"
#property version   "1.00"

#include <Trade\Trade.mqh>
CTrade trade;

input string  section0            = "General Parameters";
input int     MagicNumber         = 9291; 
input double  InitialLot          = 0.03;
input double  TPFromSLMultiplier  = 1.2;
input string  section1            = "Zigzag Strategy";
input int     ZigZagDepth         = 12;
input int     ZigZagDeviation     = 5;
input int     ZigZagBackstep      = 3;
input int     ZigZagReadBarCount  = 100;
input int     WaitNewSignalZigZagBarCount = 12; 
input double  BreakoutDistancePips  = 10;

input string section2 = "Moving Average Strategy";
input int FastEMA = 9;
input ENUM_MA_METHOD FastEMAMethod = MODE_EMA;
input ENUM_APPLIED_PRICE FastEMAPrice = PRICE_CLOSE;
input int SlowEMA  = 21;
input ENUM_MA_METHOD SlowEMAMethod = MODE_EMA;
input ENUM_APPLIED_PRICE SlowEMAPrice = PRICE_CLOSE;
 
input string  section3       = "Time Strategy";    
input bool    AllowMonday    = true; 
input bool    AllowTuesday   = true;
input bool    AllowWednesday = true;
input bool    AllowThursday  = true;
input bool    AllowFriday    = true; 

//--- Global Parameters ---
double close_prices[];
int    LookbackBars    = 200;
static double s_currentLot = 0.0;
int    m_zigzag_handle = INVALID_HANDLE; // Variabel global untuk menyimpan handle ZigZag
int    handleFastEMA, handleSlowEMA;
double prevZigZagHigh = 0;
double prevZigZagLow = 0;

double lastZigZagHigh = 0;
double lastZigZagLow = 0;

int placedBar     = -1;
bool orderSudahDipasang = false;
bool orderSudahDihapus = false;
//+------------------------------------------------------------------+
//| Fungsi inisialisasi Expert Advisor                               |
//+------------------------------------------------------------------+
int OnInit()
{          
    trade.SetExpertMagicNumber(MagicNumber);
    trade.SetMarginMode();      
    trade.SetDeviationInPoints(10);  
    // --- Inisialisasi Ema Handle di OnInit ---
     s_currentLot = InitialLot;
    handleFastEMA = iMA(Symbol(), PERIOD_CURRENT, FastEMA, 0, FastEMAMethod, FastEMAPrice);
    handleSlowEMA = iMA(Symbol(), PERIOD_CURRENT, SlowEMA, 0, SlowEMAMethod, SlowEMAPrice);
    if(handleFastEMA == INVALID_HANDLE || handleSlowEMA == INVALID_HANDLE)
    {
      Print("Failed to create EMA handles");
      return(INIT_FAILED);
    }
    // --- Inisialisasi ZigZag Handle di OnInit ---
    m_zigzag_handle = iCustom(_Symbol, _Period, "Examples\\ZigZag", ZigZagDepth, ZigZagDeviation, ZigZagBackstep);
    if(m_zigzag_handle == INVALID_HANDLE)
    {
      Print("Failed to create ZigZag handle in OnInit. Error: ", GetLastError());
      return INIT_FAILED; // Penting: Gagal inisialisasi jika handle tidak valid
    }
           
    Print("Tuyul Uncensored initialized!");
    return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{    
    // --- Lepaskan MA Handle di OnDeinit ---   
    if(handleFastEMA != INVALID_HANDLE || handleSlowEMA != INVALID_HANDLE)
    {
        IndicatorRelease(handleFastEMA);
        IndicatorRelease(handleSlowEMA);
        handleFastEMA = INVALID_HANDLE;
        handleSlowEMA = INVALID_HANDLE;
    }
    // --- Lepaskan ZigZag Handle di OnDeinit ---
    if(m_zigzag_handle != INVALID_HANDLE)
    {
        IndicatorRelease(m_zigzag_handle);
        m_zigzag_handle = INVALID_HANDLE;
    }
    Print("Tuyul Uncensored deinitialized!");
}

//+------------------------------------------------------------------+
//| Load Close Prices                                                |
//+------------------------------------------------------------------+
bool LoadClosePrices()
{
   ArrayResize(close_prices, LookbackBars);
   if(CopyClose(_Symbol, _Period, 0, LookbackBars, close_prices) <= 0)
   {
      Print("Failed to load Close prices");
      return false;
   }
   ArraySetAsSeries(close_prices, true);
   return true;
}

//+------------------------------------------------------------------+
//| Check if Not enough money                                        |
//+------------------------------------------------------------------+
bool CheckMargin(double lotSize)
{
    // Hitung margin yang diperlukan untuk 1 lot
    double marginRequired;
    if(!OrderCalcMargin(ORDER_TYPE_BUY, _Symbol, lotSize, SymbolInfoDouble(_Symbol, SYMBOL_ASK), marginRequired))
    {
        //Print("Failed to calculate margin!");
        return false;
    }
    
    double freeMargin = AccountInfoDouble(ACCOUNT_MARGIN_FREE);
    
    if(marginRequired > freeMargin)
    {
        //Print("Not enough margin! Required: ", marginRequired, ", Free: ", freeMargin);
        return false;
    }
    return true;
}

//+------------------------------------------------------------------+
//| Fungsi untuk mengecek apakah hari ini diperbolehkan trading      |
//+------------------------------------------------------------------+
bool IsTradingDayAllowed()
{
   MqlDateTime dt;
   TimeToStruct(TimeCurrent(), dt);  // Konversi waktu ke struktur

   int dayOfWeek = dt.day_of_week;  // 0 = Minggu, 1 = Senin, ..., 6 = Sabtu

   switch(dayOfWeek)
   {      
      case 1: return AllowMonday;
      case 2: return AllowTuesday;
      case 3: return AllowWednesday;
      case 4: return AllowThursday;
      case 5: return AllowFriday;      
   }
   return false;
}
//+------------------------------------------------------------------+
//| Fungsi untuk menggambar garis horizontal di chart                |
//+------------------------------------------------------------------+
void DrawTradeLines(double entryPrice, double stopLoss, double takeProfit)
{
   string tagEntry  = "FIBO_ENTRY";
   string tagSL     = "FIBO_SL";
   string tagTP     = "FIBO_TP";

   datetime timeNow = TimeCurrent();

   // Entry line (blue)
   ObjectCreate(0, tagEntry, OBJ_HLINE, 0, timeNow, entryPrice);
   ObjectSetInteger(0, tagEntry, OBJPROP_COLOR, clrBlue);
   ObjectSetInteger(0, tagEntry, OBJPROP_WIDTH, 1);
   ObjectSetInteger(0, tagEntry, OBJPROP_STYLE, STYLE_SOLID);

   // Stop Loss line (red)
   ObjectCreate(0, tagSL, OBJ_HLINE, 0, timeNow, stopLoss);
   ObjectSetInteger(0, tagSL, OBJPROP_COLOR, clrRed);
   ObjectSetInteger(0, tagSL, OBJPROP_WIDTH, 1);
   ObjectSetInteger(0, tagSL, OBJPROP_STYLE, STYLE_DOT);

   // Take Profit line (green)
   ObjectCreate(0, tagTP, OBJ_HLINE, 0, timeNow, takeProfit);
   ObjectSetInteger(0, tagTP, OBJPROP_COLOR, clrGreen);
   ObjectSetInteger(0, tagTP, OBJPROP_WIDTH, 1);
   ObjectSetInteger(0, tagTP, OBJPROP_STYLE, STYLE_DOT);
}

//+------------------------------------------------------------------+
//| Menghapus semua objek garis trading lama yang ada di chart       |
//+------------------------------------------------------------------+
void DeleteOldTradeLines()
{
   string tagsToDelete[] = {"FIBO_ENTRY", "FIBO_SL", "FIBO_TP"};

   for(int i = 0; i < ArraySize(tagsToDelete); i++)
   {
      if(ObjectFind(0, tagsToDelete[i]) != -1)
         ObjectDelete(0, tagsToDelete[i]);
   }
}

//+------------------------------------------------------------------+
//| Pending Order execution wrapper                                  |
//+------------------------------------------------------------------+
void PendingOrders(ENUM_ORDER_TYPE type, double price, double lotSize, double takeProfit, double stopLoss, string comment)
{
    // Validate lot size
    double minLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
    double lotStep = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);
    double maxLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
             
    if (s_currentLot > maxLot)
    {
        s_currentLot = maxLot;
        //Print("Warning: Volume exceeds broker's maximum limit. Adjusted to ", DoubleToString(s_currentLot, 2));
        return;
    }
    
    if (s_currentLot < minLot)
    {
        //Print("Error: Final volume is less than the minimum allowed volume: ", DoubleToString(s_currentLot, 2));
        return;
    }
    
    int ratio=(int)MathRound(s_currentLot/lotStep);
    if(MathAbs(ratio*lotStep-s_currentLot)>0.0000001)
    {     
     //Print("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",lotStep,ratio*lotStep);
     return;
    }  
    
    int lotPrecision = 0;
    double tempStep = lotStep;
    while(tempStep < 1.0 && lotPrecision < 5) 
    {
        tempStep *= 10;
        lotPrecision++;
    }
    
    lotSize = MathMax(minLot, MathMin(maxLot, lotSize)); 
    lotSize = MathRound(lotSize / lotStep) * lotStep;     
    lotSize = NormalizeDouble(lotSize, lotPrecision);
    
    // Get current market prices
    double currentBid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
    double currentAsk = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
    double minDistPoints = (double)SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL); 
    double minDistPrice = minDistPoints * _Point;
    
    if(!CheckMargin(lotSize)) return;
    
    // Validate price based on order type
    price = NormalizeDouble(price, _Digits);
    
    switch(type)
    {
        case ORDER_TYPE_BUY_STOP:
            // Buy Stop must be above current Ask + minimum distance
            price = MathMax(price, currentAsk + minDistPrice);
            break;
            
        case ORDER_TYPE_SELL_STOP:
            // Sell Stop must be below current Bid - minimum distance
            price = MathMin(price, currentBid - minDistPrice);
            break;
            
        case ORDER_TYPE_BUY_LIMIT:
            // Buy Limit must be below current Ask - minimum distance
            price = MathMin(price, currentAsk - minDistPrice);
            break;
            
        case ORDER_TYPE_SELL_LIMIT:
            // Sell Limit must be above current Bid + minimum distance
            price = MathMax(price, currentBid + minDistPrice);
            break;
            
        default:
            Print("Invalid order type for pending order");
            return;
    }
    
    // Calculate TP (you'll need to define TakeProfit somewhere)
    double sl = 0;
    double tp = 0;
    
    sl = NormalizeDouble(stopLoss, _Digits);
    tp = NormalizeDouble(takeProfit, _Digits);
    
    // Prepare trade request
    MqlTradeRequest request = {};
    MqlTradeResult result = {};
    
    request.action = TRADE_ACTION_PENDING;
    request.magic = MagicNumber;
    request.symbol = _Symbol;
    request.volume = lotSize;
    request.price = price;
    request.type = type;
    request.type_filling = ORDER_FILLING_RETURN;
    request.deviation = 10;
    request.sl = sl;
    request.tp = tp;
    request.comment = comment;
    
    // Send order
    if(!OrderSend(request, result))
    {
        Print("OrderSend failed: ", GetLastError(),  
              " Type: ", EnumToString(type),
              " Requested Price: ", price,
              " Current Ask: ", currentAsk,
              " Current Bid: ", currentBid,
              " Min Distance (points): ", minDistPoints,
              " Min Distance (price): ", DoubleToString(minDistPrice, _Digits + 2),
              " Lot: ", lotSize);
    }
    else
    {
        Print("Pending order placed: Ticket=", result.order, 
              " Type: ", EnumToString(type),
              " Price: ", DoubleToString(price, _Digits),
              " Lot: ", DoubleToString(lotSize, lotPrecision));
    }
}

//+------------------------------------------------------------------+
//| Delete Pending Order                                             |
//+------------------------------------------------------------------+
void DeletePendingOrders()
{
   for(int i = OrdersTotal() - 1; i >= 0; i--)
   {
      ulong ticket = OrderGetTicket(i);
      if(ticket > 0)
      {
         if(OrderSelect(ticket) && OrderGetString(ORDER_SYMBOL) == _Symbol && 
            OrderGetInteger(ORDER_MAGIC) == MagicNumber)
         {
            int type = (int)OrderGetInteger(ORDER_TYPE);            
            if(type == ORDER_TYPE_BUY_STOP || type == ORDER_TYPE_BUY_LIMIT || type == ORDER_TYPE_SELL_STOP || type == ORDER_TYPE_SELL_LIMIT )
            {
               if(!trade.OrderDelete(ticket))
               {
                  Print("Failed to delete order ", ticket, ". Error: ", GetLastError());
               }
               else
               {
                  Print("Pending order deleted (Ticket: ", ticket, ")");
               }
            }
         }
      }
   }
}
//+------------------------------------------------------------------+
//| Draw fibonanci reytracement                                       |
//+------------------------------------------------------------------+
void DrawFibonacciRetracement(double high, double low, datetime time1, datetime time2)
{
   string fibName = "AutoFibo";

   ObjectDelete(0, fibName);

   ObjectCreate(0, fibName, OBJ_FIBO, 0, time1, high, time2, low);
   ObjectSetInteger(0, fibName, OBJPROP_COLOR, clrYellow);
   ObjectSetInteger(0, fibName, OBJPROP_RAY_LEFT, false);

   ObjectSetDouble(0, fibName, OBJPROP_LEVELVALUE, 0, 0.0);
   ObjectSetDouble(0, fibName, OBJPROP_LEVELVALUE, 1, 0.236);
   ObjectSetDouble(0, fibName, OBJPROP_LEVELVALUE, 2, 0.382);
   ObjectSetDouble(0, fibName, OBJPROP_LEVELVALUE, 3, 0.570);
   ObjectSetDouble(0, fibName, OBJPROP_LEVELVALUE, 4, 0.618);
   ObjectSetDouble(0, fibName, OBJPROP_LEVELVALUE, 5, 1.0);

   ObjectSetString(0, fibName, OBJPROP_LEVELTEXT, 3, "57.0");
}



//+------------------------------------------------------------------+
//| Get Last ZigZag Points                                           |
//+------------------------------------------------------------------+
void GetLastZigZagPoints()
{

   // Cek apakah handle valid
   if(m_zigzag_handle == INVALID_HANDLE)
   {
      Print("Error: ZigZag handle is invalid. Cannot get ZigZag points.");
      return;
   }

   double zzHigh[], zzLow[];
   
   // Jumlah bar yang ingin Anda salin   
   if(CopyBuffer(m_zigzag_handle, 1, 0, ZigZagReadBarCount, zzHigh) <= 0 || CopyBuffer(m_zigzag_handle, 2, 0, ZigZagReadBarCount, zzLow) <= 0)
   {
      Print("Failed to copy ZigZag buffers. Error: ", GetLastError());
      return;
   }

   ArraySetAsSeries(zzHigh, true);
   ArraySetAsSeries(zzLow, true);
   
   lastZigZagHigh = 0;
   lastZigZagLow = 0;
   
   int highFound = 0;
   int lowFound = 0;

   // Lewati titik terakhir yang masih belum fix
   for(int i = 5; i < ArraySize(zzHigh); i++) // '5' adalah IgnoreLastZZ default
   {
        if (zzHigh[i] != 0 && highFound == 0) // Temukan high pertama yang bukan 0
        {
            lastZigZagHigh = zzHigh[i];
            highFound = 1;
        }
        if (zzLow[i] != 0 && lowFound == 0) // Temukan low pertama yang bukan 0
        {
            lastZigZagLow = zzLow[i];
            lowFound = 1;
        }

        if (highFound == 1 && lowFound == 1) break; // Jika keduanya ditemukan, keluar
   }
   
   int count = 0;
   for(int i = 0; i < ArraySize(zzHigh); i++)
   {
        if (zzHigh[i] != 0) { // Jika ada high zigzag di bar ini
            if (lastZigZagHigh == 0) lastZigZagHigh = zzHigh[i]; // Ini adalah high zigzag terakhir
            else { 
                // Ini adalah high zigzag sebelumnya
                //prevZigZagHigh = lastZigZagHigh; // Ini jika Anda ingin menyimpan dua high terakhir
                //lastZigZagHigh = zzHigh[i];
            }
        }
        if (zzLow[i] != 0) { // Jika ada low zigzag di bar ini
            if (lastZigZagLow == 0) lastZigZagLow = zzLow[i]; // Ini adalah low zigzag terakhir
            else { 
                // Ini adalah low zigzag sebelumnya
                //prevZigZagLow = lastZigZagLow; // Ini jika Anda ingin menyimpan dua low terakhir
                //lastZigZagLow = zzLow[i];
            }
        }
   }   
}

//+------------------------------------------------------------------+
//| Expert Tick Function                                            |
//+------------------------------------------------------------------+
void OnTick()
{
   // Mencegah eksekusi berulang pada tick yang sama (good practice)
   static datetime lastCheck = 0;
   if(TimeCurrent() == lastCheck) return;
   lastCheck = TimeCurrent();
   
   // Ini adalah awal bar baru, lakukan perhitungan yang intensif di sini
   if(!LoadClosePrices()) return;
   //menghapus pending order di hari dilarang
   if (!IsTradingDayAllowed())
   {         
      DeletePendingOrders();
   } 
   
   // ZigZag Panggil di sini
   GetLastZigZagPoints();
   // Hitung EMA
   double slowMA[2], fastMA[2];
   
   if(CopyBuffer(handleSlowEMA, 0, 0, 2, slowMA) <= 0 || CopyBuffer(handleFastEMA, 0, 0, 2, fastMA) <= 0)
   {
      Print("Failed to copy EMA buffer");
      return;
   }
   
   double slowMA_now = slowMA[0];
   double slowMA_prev = slowMA[1];
   double fastMA_now = fastMA[0];
   double fastMA_prev = fastMA[1];
   //------------------
   bool newZZHigh = (lastZigZagHigh != prevZigZagHigh);
   bool newZZLow  = (lastZigZagLow  != prevZigZagLow);
   
   int currentBar = Bars(_Symbol, PERIOD_CURRENT);
   // 1. Deteksi sinyal dan pasang order sekali saja
   if ((newZZHigh || newZZLow) && !PositionSelect(_Symbol) && !orderSudahDipasang)
   {
      datetime timeArray[];
      
      if(CopyTime(_Symbol, PERIOD_CURRENT, 0, 21, timeArray) > 0)
      {
         datetime time1 = timeArray[0];     // Bar sekarang
         datetime time2 = timeArray[20];    // Bar 20 candle lalu
      
         if (lastZigZagHigh > lastZigZagLow)
            DrawFibonacciRetracement(lastZigZagHigh, lastZigZagLow, time1, time2);
         else
            DrawFibonacciRetracement(lastZigZagLow, lastZigZagHigh, time1, time2);
      }
      //-----------
      double fibLevel = 0.57;  // Fibonacci retracement level
      double fibPrice = -1; // inisialisasi agar tidak uninitialized
      ENUM_ORDER_TYPE orderType = WRONG_VALUE; // nilai default tidak valid

      string tradeComment = "";      
   
      if (fastMA_prev > slowMA_prev && lastZigZagLow > 0 && lastZigZagHigh > lastZigZagLow)
      {
         // Trend naik (entry Buy di Fibonacci retracement level)
         fibPrice = lastZigZagLow + (lastZigZagHigh - lastZigZagLow) * fibLevel;
         orderType = ORDER_TYPE_BUY_LIMIT;
         tradeComment = "Buy Limit at Fibo 57";
      }
      else if (fastMA_prev < slowMA_prev && lastZigZagHigh > lastZigZagLow && lastZigZagHigh > 0)
      {
         // Trend turun (entry Sell di Fibonacci retracement level)
         fibPrice = lastZigZagHigh - (lastZigZagHigh - lastZigZagLow) * fibLevel;
         orderType = ORDER_TYPE_SELL_LIMIT;
         tradeComment = "Sell Limit at Fibo 57";
      }
   
      if (fibPrice > 0)
      {
         double stopLoss = 0, takeProfit = 0;
         double sl_distance = 0;
      
         if(orderType == ORDER_TYPE_SELL_LIMIT)
         {
            stopLoss = lastZigZagHigh;
            sl_distance = MathAbs(stopLoss - fibPrice);
            takeProfit = fibPrice - (sl_distance * TPFromSLMultiplier);
         }
         else if(orderType == ORDER_TYPE_BUY_LIMIT)
         {
            stopLoss = lastZigZagLow;
            sl_distance = MathAbs(fibPrice - stopLoss);
            takeProfit = fibPrice + (sl_distance * TPFromSLMultiplier);
         }
      
         // Validasi dan eksekusi
         long stopsLevelLong = SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL);
         double stopLevelPoints = (double)stopsLevelLong; 
         double stopLevelPrice  = stopLevelPoints * _Point;
         
         if (sl_distance > stopLevelPrice) // Pastikan SL cukup jauh
         {
            if (fabs(takeProfit - fibPrice) >= stopLevelPrice) // Pastikan TP juga cukup jauh
            {
               stopLoss = NormalizeDouble(stopLoss, _Digits);
               takeProfit = NormalizeDouble(takeProfit, _Digits);
               fibPrice = NormalizeDouble(fibPrice, _Digits);
         
               PendingOrders(orderType, fibPrice, InitialLot, takeProfit, stopLoss, tradeComment);
               DeleteOldTradeLines();
               DrawTradeLines(fibPrice, stopLoss, takeProfit);
         
               placedBar = Bars(_Symbol, PERIOD_CURRENT);
               orderSudahDipasang = true;
               orderSudahDihapus = false;
         
               Print("Order at Fibo 57: ", fibPrice,
                     " SL: ", stopLoss,
                     " TP: ", takeProfit,
                     " Jarak SL: ", sl_distance,
                     " TP Multiplier: 3.2x");
            }
            else
            {
               Print("TP terlalu dekat dengan harga entry. Jarak TP: ", fabs(takeProfit - fibPrice),
                     ", minimal: ", stopLevelPrice);
            }
         }
         else
         {
            Print("SL terlalu dekat dengan harga entry. Jarak SL: ", sl_distance,
                  ", minimal: ", stopLevelPrice);
         }
      }
   }
   
   // 2. Setelah jeda, hapus order
   if (orderSudahDipasang && !orderSudahDihapus && (currentBar - placedBar) >= WaitNewSignalZigZagBarCount)
   {
      DeletePendingOrders();
      orderSudahDihapus = true;
      Print("Orders are deleted after a break.");
   }
   
   // Siap menerima sinyal baru jika order sudah dihapus
   if (orderSudahDipasang && orderSudahDihapus)
   {
     // Reset untuk sinyal berikutnya
     orderSudahDipasang = false;
     orderSudahDihapus = false;
     placedBar = -1;
   }
   
   prevZigZagHigh = lastZigZagHigh;
   prevZigZagLow  = lastZigZagLow;
   
//----------
}