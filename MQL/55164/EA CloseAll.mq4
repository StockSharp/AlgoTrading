//+------------------------------------------------------------------+
//|                            CloseAllOrdersEA.mq4                  |
//|                                  Copyright 2025, getbos          |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "2025, getbos"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

//+------------------------------------------------------------------+
//| Fungsi inisialisasi EA                                           |
//+------------------------------------------------------------------+
int OnInit()
  {
   Print("CloseAllOrdersEA diinisialisasi");
   CloseAllOrders();
   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//| Fungsi deinisialisasi EA                                         |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   Print("CloseAllOrdersEA dideinisialisasi");
  }

//+------------------------------------------------------------------+
//| Fungsi tick                                                      |
//+------------------------------------------------------------------+
void OnTick()
  {
// EA ini tidak perlu melakukan apa-apa pada setiap tick karena menutup semua order di OnInit
  }

//+------------------------------------------------------------------+
//| Fungsi untuk menutup semua order                                 |
//+------------------------------------------------------------------+
void CloseAllOrders()
  {
   int retryCount = 3; // Jumlah percobaan ulang untuk menutup order
   for(int i=OrdersTotal()-1; i>=0; i--) // Loop dari akhir ke awal untuk menghindari masalah indeks saat penghapusan
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         bool closed = false;
         for(int retry = 0; retry < retryCount && !closed; retry++)
           {
            RefreshRates(); // Pastikan data harga terkini sudah diperbarui
            if(OrderType() <= OP_SELL) // Order pasar
              {
               double closePrice = (OrderType() == OP_BUY) ? Bid : Ask;
               if(OrderClose(OrderTicket(), OrderLots(), closePrice, 2)) // 2 adalah Slippage
                 {
                  Print("Order ", OrderTicket(), " berhasil ditutup pada percobaan ke-", retry+1);
                  closed = true;
                 }
              }
            else // Order pending
              {
               if(OrderDelete(OrderTicket()))
                 {
                  Print("Order pending ", OrderTicket(), " berhasil dihapus pada percobaan ke-", retry+1);
                  closed = true;
                 }
              }

            if(!closed)
              {
               int lastError = GetLastError();
               Print("Gagal menutup/menghapus order ", OrderTicket(), " pada percobaan ke-", retry+1, ": Error ", lastError);
               if(lastError == ERR_NO_RESULT || lastError == ERR_INVALID_TICKET || lastError == ERR_TRADE_CONTEXT_BUSY)
                 {
                  Sleep(1000); // Tunggu 1 detik sebelum mencoba lagi untuk error spesifik ini
                 }
               else
                 {
                  break; // Untuk error lain, tidak ada percobaan ulang
                 }
              }
           }
         if(!closed)
           {
            Print("Gagal menutup/menghapus order ", OrderTicket(), " setelah ", retryCount, " percobaan");
           }
        }
     }
  }
//+------------------------------------------------------------------+