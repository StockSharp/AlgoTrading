//+------------------------------------------------------------------+
//|                                                  allanmaug.mq5   |
//|                                  Copyright 2024, allanmaug         |
//|                                             https://www.allanmaug.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2024, allanmaug"
#property link      "https://www.allanmaug.com"
#property version   "1.00"
#property strict

#define FILE_NAME MQLInfoString(MQL_PROGRAM_NAME)+".bin"

#include <Trade/Trade.mqh>
#include <Arrays/ArrayLong.mqh>

//--- Input Parameters
enum ENUM_MODE {
    MODE_MASTER, // Master Mode
    MODE_SLAVE   // Slave Mode
};

input ENUM_MODE Mode = MODE_SLAVE;               // EA Mode
input string MasterSymbol1 = "XAUUSD.ecn";      // Master Symbol 1
input string SlaveSymbol1 = "GOLD";           // Slave Symbol 1
input string MasterSymbol2 = "USDJPY.ecn";      // Master Symbol 2
input string SlaveSymbol2 = "USDJPY";           // Slave Symbol 2
input int MagicNumber = 12345;                  // Magic Number (Slave Only)

//--- Global Variables
CTrade trade;
CArrayLong copiedTickets;

//+------------------------------------------------------------------+
//| Expert initialization function                                     |
//+------------------------------------------------------------------+
int OnInit() {
    if(Mode == MODE_SLAVE) {
        trade.SetExpertMagicNumber(MagicNumber);
    }
    
    copiedTickets.Clear();
    EventSetMillisecondTimer(50);
    Print("EA Initialized - Mode: ", Mode == MODE_MASTER ? "Master" : "Slave");
    return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                   |
//+------------------------------------------------------------------+
void OnDeinit(const int reason) {
    EventKillTimer();
    copiedTickets.Clear();
}

//+------------------------------------------------------------------+
//| Get corresponding slave symbol                                     |
//+------------------------------------------------------------------+
string GetSlaveSymbol(string masterSymbol) {
    if(masterSymbol == MasterSymbol1) return SlaveSymbol1;
    if(masterSymbol == MasterSymbol2) return SlaveSymbol2;
    return "";
}

//+------------------------------------------------------------------+
//| Timer function                                                     |
//+------------------------------------------------------------------+
void OnTimer() {
    if(Mode == MODE_MASTER) {
        int file = FileOpen(FILE_NAME, FILE_WRITE|FILE_BIN|FILE_COMMON);
        if(file != INVALID_HANDLE) {
            for(int i = 0; i < PositionsTotal(); i++) {
                ulong ticket = PositionGetTicket(i);
                if(PositionSelectByTicket(ticket)) {
                    string symbol = PositionGetString(POSITION_SYMBOL);
                    // Write positions for both master symbols
                    if(symbol == MasterSymbol1 || symbol == MasterSymbol2) {
                        FileWriteLong(file, ticket);
                        int length = StringLen(symbol);
                        FileWriteInteger(file, length);
                        FileWriteString(file, symbol);
                        FileWriteDouble(file, PositionGetDouble(POSITION_VOLUME));
                        FileWriteInteger(file, (int)PositionGetInteger(POSITION_TYPE));
                        FileWriteDouble(file, PositionGetDouble(POSITION_PRICE_OPEN));
                        FileWriteDouble(file, PositionGetDouble(POSITION_SL));
                        FileWriteDouble(file, PositionGetDouble(POSITION_TP));
                    }
                }
            }
            FileClose(file);
        }
    }
    else if(Mode == MODE_SLAVE) {
        CArrayLong currentMasterTickets;
        currentMasterTickets.Clear();
        
        int file = FileOpen(FILE_NAME, FILE_READ|FILE_BIN|FILE_COMMON);
        if(file != INVALID_HANDLE) {
            while(!FileIsEnding(file)) {
                long masterTicket = FileReadLong(file);
                int length = FileReadInteger(file);
                string masterSymbol = FileReadString(file, length);
                double volume = FileReadDouble(file);
                ENUM_POSITION_TYPE posType = (ENUM_POSITION_TYPE)FileReadInteger(file);
                double openPrice = FileReadDouble(file);
                double sl = FileReadDouble(file);
                double tp = FileReadDouble(file);
                
                string slaveSymbol = GetSlaveSymbol(masterSymbol);
                if(slaveSymbol == "") continue;  // Skip if no matching slave symbol
                
                currentMasterTickets.Add(masterTicket);
                
                // Check if ticket already copied
                if(copiedTickets.SearchLinear(masterTicket) == -1) {
                    Print("New position detected: ", masterSymbol, " -> ", slaveSymbol);
                    // Open new position
                    if(posType == POSITION_TYPE_BUY) {
                        if(trade.Buy(volume, slaveSymbol, 0, sl, tp, IntegerToString(masterTicket))) {
                            copiedTickets.Add(masterTicket);
                            Print("Opened Buy position for ticket: ", masterTicket, " on ", slaveSymbol);
                        }
                    }
                    else if(posType == POSITION_TYPE_SELL) {
                        if(trade.Sell(volume, slaveSymbol, 0, sl, tp, IntegerToString(masterTicket))) {
                            copiedTickets.Add(masterTicket);
                            Print("Opened Sell position for ticket: ", masterTicket, " on ", slaveSymbol);
                        }
                    }
                }
                else {
                    // Update existing position if needed
                    for(int i = 0; i < PositionsTotal(); i++) {
                        ulong ticket = PositionGetTicket(i);
                        if(PositionSelectByTicket(ticket)) {
                            if(PositionGetString(POSITION_SYMBOL) == slaveSymbol && 
                               PositionGetInteger(POSITION_MAGIC) == MagicNumber &&
                               StringToInteger(PositionGetString(POSITION_COMMENT)) == masterTicket) {
                                
                                if(PositionGetDouble(POSITION_SL) != sl || 
                                   PositionGetDouble(POSITION_TP) != tp) {
                                    trade.PositionModify(ticket, sl, tp);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            FileClose(file);
            
            // Close positions that don't exist in master anymore
            for(int i = 0; i < PositionsTotal(); i++) {
                ulong ticket = PositionGetTicket(i);
                if(PositionSelectByTicket(ticket)) {
                    string symbol = PositionGetString(POSITION_SYMBOL);
                    if((symbol == SlaveSymbol1 || symbol == SlaveSymbol2) && 
                       PositionGetInteger(POSITION_MAGIC) == MagicNumber) {
                        
                        long masterTicket = StringToInteger(PositionGetString(POSITION_COMMENT));
                        if(currentMasterTickets.SearchLinear(masterTicket) == -1) {
                            if(trade.PositionClose(ticket)) {
                                copiedTickets.Delete(copiedTickets.SearchLinear(masterTicket));
                                Print("Closed position for ticket: ", masterTicket, " on ", symbol);
                            }
                        }
                    }
                }
            }
        }
    }
}

//+------------------------------------------------------------------+
//| Trade event handler                                               |
//+------------------------------------------------------------------+
void OnTrade() {
}

//+------------------------------------------------------------------+
//| ChartEvent handler                                                |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long& lparam,
                  const double& dparam,
                  const string& sparam) {
}