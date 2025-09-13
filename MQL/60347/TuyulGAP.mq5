//+------------------------------------------------------------------+
//|                                                     TuyulGAP.mq5 |
//|                                              Copyright zvickyhac |
//+------------------------------------------------------------------+
#property copyright "zvickyhac"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property description "Tuyul Gap Trading End of Week - Dynamic High/Low"
#include <Trade\Trade.mqh>
CTrade trade;

//--- Input Parameters ---
input string section0 = "#### General Parameters ####";
input int    MagicNumber    = 6062; 
input double LotSize        = 0.1;
input int    StopLoss       = 60;
input int    MaxOpenTrade   = 1;
input string section1 = "#### Search High & Low Price in Bars ####";
input int    LookbackBars   = 12;
input string section2 = "#### Day Filter ####";
input int    DayOfWeek = 5;
input int    Hours = 23;
input int    Minutes = 15;
input string section3 = "#### Secure Profit in USD ####";
input double SecureProfitTarget = 5.0; 

bool ordersPlacedForThisSession = false; // Global flag untuk menandakan apakah order sudah ditempatkan di sesi ini
 
//+------------------------------------------------------------------+
//| Fungsi inisialisasi Expert Advisor                               |
//+------------------------------------------------------------------+
int OnInit()
{          
    trade.SetExpertMagicNumber(MagicNumber);
    trade.SetMarginMode(); 
    ordersPlacedForThisSession = false; 
    Print("Tuyul Gap initialized!");
    return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
    DeletePendingOrders();
    Print("Tuyul Gap EA deinitialized!");
}

//+------------------------------------------------------------------+
//| Check if Not enough money                                        |
//+------------------------------------------------------------------+
bool CheckMargin(double lotSize)
{
    double marginRequired;
    if(!OrderCalcMargin(ORDER_TYPE_BUY, _Symbol, lotSize, SymbolInfoDouble(_Symbol, SYMBOL_ASK), marginRequired))
    {
        Print("Failed to calculate margin!");
        return false;
    }
    
    double freeMargin = AccountInfoDouble(ACCOUNT_MARGIN_FREE);
    
    if(marginRequired > freeMargin)
    {
        Print("Not enough margin! Required: ", marginRequired, ", Free: ", freeMargin);
        return false;
    }
    return true;
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
            if(type == ORDER_TYPE_BUY_STOP || type == ORDER_TYPE_SELL_STOP)
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
//| Pending Order execution wrapper                                  |
//+------------------------------------------------------------------+
void PendingOrders(ENUM_ORDER_TYPE type, double price, double lotSize, string comment)
{
    double minLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
    double lotStep = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);
    double maxLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
    
    int lotPrecision = 0;
    double tempStep = lotStep;
    while (tempStep < 1.0 && lotPrecision < 5) 
    {
        tempStep *= 10;
        lotPrecision++;
    }
    
    lotSize = MathMax(minLot, MathMin(maxLot, lotSize)); 
    lotSize = MathRound(lotSize / lotStep) * lotStep;     
    lotSize = NormalizeDouble(lotSize, lotPrecision);
    
    double currentBid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
    double currentAsk = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
    double minDistPoints = (double)SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL); 
    double minDistPrice = minDistPoints * _Point;   
    
    if(!CheckMargin(lotSize)) return;
    
    // Pastikan order stop ditempatkan di luar harga pasar saat ini dan di luar minDistPrice
    if(type == ORDER_TYPE_BUY_STOP)
    {      
        // Buy Stop harus di atas harga Ask saat ini dan di atas minDistPrice dari Ask
        price = MathMax(price, currentAsk + minDistPrice);
    }
    else if(type == ORDER_TYPE_SELL_STOP)
    {          
        // Sell Stop harus di bawah harga Bid saat ini dan di bawah minDistPrice dari Bid
        price = MathMin(price, currentBid - minDistPrice);
    }
    
    price = NormalizeDouble(price, _Digits);
    
    MqlTradeRequest request = {};
    MqlTradeResult result = {};
    
    double sl = 0;
    // Hitung SL dan TP berdasarkan jenis order
    if(type == ORDER_TYPE_BUY_LIMIT || type == ORDER_TYPE_BUY_STOP)
    {
      sl = NormalizeDouble(price - StopLoss * _Point, _Digits);      
    }        
    
    request.action = TRADE_ACTION_PENDING;
    request.magic = MagicNumber;
    request.symbol = _Symbol;
    request.volume = lotSize;
    request.price = price;
    request.type = type;
    request.type_filling = ORDER_FILLING_RETURN;
    request.deviation = 10;
    request.sl = sl;
    request.tp = 0;
    request.comment = comment;
            
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
//| Expert Tick Function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
    static datetime lastCheck = 0;
    if(TimeCurrent() == lastCheck) return;
    lastCheck = TimeCurrent();
    
    string tradeComment = "Tuyul Gap Trade";
    
    MqlDateTime dt;
    TimeToStruct(TimeCurrent(), dt);  
    int dayOfWeek = dt.day_of_week;   
    int currentHour = dt.hour;
    int currentMinute = dt.min;    
    
      // Iterasi terbalik untuk Menutup trade Profit
      for (int i = PositionsTotal() - 1; i >= 0; i--)
      {
          ulong position_ticket = PositionGetTicket(i);
          if (PositionSelectByTicket(position_ticket))
          {
              // Pastikan posisi ini dibuka oleh EA kita (menggunakan MagicNumber) dan untuk simbol saat ini
              if (PositionGetInteger(POSITION_MAGIC) == MagicNumber && PositionGetString(POSITION_SYMBOL) == _Symbol)
              {
                  // Ambil profit posisi saat ini
                  double currentProfit = PositionGetDouble(POSITION_PROFIT);
      
                  // Cek apakah profit posisi saat ini sudah mencapai target profit yang ditentukan
                  // Atau Anda bisa langsung membandingkan dengan 0 jika hanya ingin menutup yang profit (di atas 0)
                  if (currentProfit >= SecureProfitTarget) // Ubah SecureProfitTarget sesuai target profit Anda
                  {
                      // Coba tutup posisi
                      if (trade.PositionClose(position_ticket))
                      {
                          Print("Successfully closed profitable position with ticket: ", position_ticket, " Profit: ", currentProfit);
                      }
                      else
                      {
                          Print("Failed to close position with ticket: ", position_ticket, " Error: ", GetLastError());
                      }
                  }
              }
          }
      }
    
    // Logic untuk penempatan order Jumat malam
    if (dayOfWeek == DayOfWeek && currentHour == Hours && currentMinute >= 0 && currentMinute <= Minutes) // Dari 23:00 sampai 23:30
    {           
      // Hanya tempatkan order jika belum ditempatkan di sesi ini
      if (!ordersPlacedForThisSession) 
      {
            // --- Pencarian High Tertinggi dan Low Terendah ---
            double high_prices[];
            double low_prices[];

            // Salin data High dan Low dari LookbackBars
            if(CopyHigh(_Symbol, _Period, 0, LookbackBars, high_prices) <= 0)
            {
                Print("Failed to load High prices");
                return;
            }
            if(CopyLow(_Symbol, _Period, 0, LookbackBars, low_prices) <= 0)
            {
                Print("Failed to load Low prices");
                return;
            }
            
            ArraySetAsSeries(high_prices, true); // Pastikan index 0 adalah bar paling baru
            ArraySetAsSeries(low_prices, true);
            
            double highest_high = 0.0;
            double lowest_low = 999999.9; // Inisialisasi dengan nilai yang sangat tinggi

            // Cari high tertinggi dan low terendah dari LookbackBars
            for (int i = 1; i < LookbackBars; i++) // Mulai dari index 1 (bar sebelumnya, index 0 adalah bar saat ini yang masih bergerak)
            {
                if (high_prices[i] > highest_high)
                {
                    highest_high = high_prices[i];
                }
                if (low_prices[i] < lowest_low)
                {
                    lowest_low = low_prices[i];
                }
            }

            // Jika LookbackBars hanya melihat bar masa lalu yang sudah terbentuk sempurna,
            // maka kita harus memeriksa bar 0 (current bar) secara terpisah atau dimulai dari i=0.
            // Untuk strategi gap, kita mungkin ingin melihat bar yang sudah selesai.
            // Jika Anda ingin memasukkan bar saat ini (yang sedang terbentuk), maka mulai loop dari i=0
            // atau tambahkan pemeriksaan terpisah untuk bar 0.
            // Untuk keamanan, saya akan mengambil HighestHigh dan LowestLow dari bar yang sudah ditutup.

            // --- Penentuan Harga Pending Order ---
            // Harga Buy Stop akan ditempatkan di atas Highest High
            double priceBuyStop = NormalizeDouble(highest_high + _Point, _Digits); // Tambahkan sedikit buffer (_Point)
            
            // Harga Sell Stop akan ditempatkan di bawah Lowest Low
            double priceSellStop = NormalizeDouble(lowest_low - _Point, _Digits); // Kurangkan sedikit buffer (_Point)
            
            // Debugging
            Print("Highest High (last ", LookbackBars, " bars): ", DoubleToString(highest_high, _Digits));
            Print("Lowest Low (last ", LookbackBars, " bars): ", DoubleToString(lowest_low, _Digits));
            Print("Calculated Buy Stop Price: ", DoubleToString(priceBuyStop, _Digits));
            Print("Calculated Sell Stop Price: ", DoubleToString(priceSellStop, _Digits));
            // --- End Debugging ---

            // Periksa apakah ada pending order yang sudah ada dengan MagicNumber ini
            bool hasBuyStop = false;
            bool hasSellStop = false;
            for(int i = OrdersTotal() - 1; i >= 0; i--) {
                ulong ticket = OrderGetTicket(i);
                if(ticket > 0 && OrderSelect(ticket) && OrderGetString(ORDER_SYMBOL) == _Symbol && OrderGetInteger(ORDER_MAGIC) == MagicNumber) {
                    if (OrderGetInteger(ORDER_TYPE) == ORDER_TYPE_BUY_STOP) hasBuyStop = true;
                    if (OrderGetInteger(ORDER_TYPE) == ORDER_TYPE_SELL_STOP) hasSellStop = true;
                }
            }

            if (!hasSellStop) {
                PendingOrders(ORDER_TYPE_SELL_STOP, priceSellStop, LotSize, tradeComment);
            } else {
                Print("Sell Stop order already exists for this session.");
            }

            if (!hasBuyStop) {
                PendingOrders(ORDER_TYPE_BUY_STOP, priceBuyStop, LotSize, tradeComment);
            } else {
                Print("Buy Stop order already exists for this session.");
            }
            
            ordersPlacedForThisSession = true; // Set flag setelah menempatkan order
        }
    }   
    else if(dayOfWeek == 1) // Hari Senin
    {
        // Reset flag pada hari Senin
        ordersPlacedForThisSession = false;
        DeletePendingOrders();        
    }
    //---
}