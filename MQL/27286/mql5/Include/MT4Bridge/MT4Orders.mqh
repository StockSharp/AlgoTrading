// MQL4&5-code

// Parallel use of the MetaTrader 4 and MetaTrader 5 order systems.
// https://www.mql5.com/en/code/16006

// This mqh file allows working with the orders in MQL5 (MT5-hedge) in the same way as in MQL4.
// That is, the order language system (OLS) becomes identical to MQL4. // At the same time, it is still possible to use
// the MQL5 order system in parallel. In particular, the standard MQL5 library will continue to fully operate.
// It is not necessary to choose between the order systems. Use them in parallel!

// When translating MQL4 -> MQL5, there is no need to touch the order system at all.
// You only need to add a single line at the beginning (if the source file can compile in MetaTrader 4 with #property strict):

// #include <MT4Orders.mqh> // if there is #include <Expert/Expert.mqh>, add this line AFTER that

// Similar actions (adding one line) in your MQL5 codes enable the addition of the MT4 OLS to the MT5 OLS, or its complete replacement.

// The author created this feature for himself, therefore, he deliberately had not applied the same idea of "one-line" transfer
// for timeseries, graphical objects, indicators, etc.

// This work covers only the order system.

// The author did not aim at creating a complete library for allowing the MQL4 code to work in MetaTrader 5 without changes.

// What is not implemented:
//   CloseBy operations - did not have time for that. Maybe it will be implemented in the future, when it is needed.
//   Detection of TP and SL of closed positions - as of now (build 1470), MQL5 is unable to do that.
//   Accounting of DEAL_ENTRY_INOUT and DEAL_ENTRY_OUT_BY deals.

// Features:
//   In MetaTrader 4, OrderSelect in the SELECT_BY_TICKET mode selects a ticket regardless of MODE_TRADES/MODE_HISTORY,
//   because "The ticket number is a unique order identifier".
//   In MetaTrader 5, the ticket number is NOT unique,
//   therefore, OrderSelect in the SELECT_BY_TICKET mode has the following selection priorities for matching tickets:
//     MODE_TRADES:  existing position > existing order > deal > canceled order
//     MODE_HISTORY: deal > canceled order > existing position > existing order
//
// Accordingly, OrderSelect in the SELECT_BY_TICKET mode in MetaTrader 5 may occasionally (in the tester) select not what was intended in MetaTrader 4.
//
// If OrdersTotal() is called with an input parameter, the returned value will not correspond to the MetaTrader 5 variant.

// List of changes:
// 03.08.2016:
//   Release - written and tested only on the offline tester.
// 29.09.2016:
//   Add: support for operation on exchanges (SYMBOL_TRADE_EXECUTION_EXCHANGE). Note that exchanges use Netting (not Hedging) mode.
//   Add: Requirement "if there is #include <Trade/Trade.mqh>, insert this line AFTER"
//        changed to "if there is #include <Expert/Expert.mqh>, insert this line AFTER".
//   Fix: OrderSend for market orders returns position ticket, not deal ticket.
// 13.11.2016:
//   Add: Complete synchronization of OrderSend, OrderModify, OrderClose, OrderDelete with the trading environment (real-time and history) - as in MetaTrader 4.
//        The maximum synchronization time can be set using MT4ORDERS::OrderSend_MaxPause in microseconds. The average synchronization time in MetaTrader 5 is ~1 microsecond.
//        By default, the maximum synchronization time is one second. MT4ORDERS::OrderSend_MaxPause = 0 - no synchronization.
//   Add: Since the parameter SlipPage (OrderSend, OrderClose) affects the execution of market orders only in the Instant mode,
//        it can now be used to set the type of execution by remainder, if necessary - ENUM_ORDER_TYPE_FILLING:
//        ORDER_FILLING_FOK, ORDER_FILLING_IOC or ORDER_FILLING_RETURN.
//        In case it is set by mistake, or if the symbol does not support the specified execution time, the working mode will be automatically selected.
//        Examples:
//          OrderSend(Symb, Type, Lots, Price, ORDER_FILLING_FOK, SL, TP) - send the corresponding order with the ORDER_FILLING_FOK execution type
//          OrderSend(Symb, Type, Lots, Price, ORDER_FILLING_IOC, SL, TP) - send the corresponding order with the ORDER_FILLING_IOC execution type
//          OrderClose(Ticket, Lots, Price, ORDER_FILLING_RETURN) - send the corresponding order with the ORDER_FILLING_RETURN execution type
//   Add: OrdersHistoryTotal() and OrderSelect(Pos, SELECT_BY_POS, MODE_HISTORY) are cached - they work as fast as possible.
//        There are no slow implementations left in the library.
// 08.02.2017:
//   Add: MT4ORDERS::LastTradeRequest and MT4ORDERS::LastTradeResult variables contain the corresponding MT5-OrderSend data.
// 14.06.2017:
//   Add: Implemented the initially built-in detection of SL/TP of positions closed using OrderClose.
//   Add: MagicNumber now has the long type - 8 bytes (while it was int - 4 bytes previously).
//   Add: If the last color input parameter in OrderSend, OrderClose or OrderModify is set to INT_MAX,
//        the appropriate MT5 trade request (MT4ORDERS::LastTradeRequest) is formed but NOT sent. MT5 check is carried out instead,
//        and its result becomes available in MT4ORDERS::LastTradeCheckResult.
//        In case of a successful check, OrderModify and OrderClose return true, otherwise - false.
//        OrderSend returns 0 if successful, otherwise it returns -1.
//
//        If the corresponding color input parameter is set to INT_MIN, it is sent ONLY in case of a successful MT5 check of the formed
//        trade request (as INT_MAX).
//   Add: Added asynchronous equivalents of MQL4 trade functions: OrderSendAsync, OrderModifyAsync, OrderCloseAsync, OrderDeleteAsync.
//        They return the appropriate Result.request_id if successful, otherwise - 0.
// 03.08.2017:
//   Add: Added OrderCloseBy.
//   Add: Accelerated the operation of OrderSelect in the MODE_TRADES mode. Now, the data of a selected order can be obtained via
//        relevant MT4-Order functions, even if the MT5 position/order (not in history) isn't selected via MT4Orders.
//        For example, via the MT5-PositionSelect* functions or MT5-OrderSelect.
//   Add: Added OrderOpenPriceRequest() and OrderClosePriceRequest() - return the trading request price at opening/closing a position.
//        Using the data of the functions, you can compute the relevant slippages of the orders.
// 26.08.2017:
//   Add: Added OrderOpenTimeMsc() and OrderCloseTimeMsc() - relevant time in milliseconds.
//   Fix: Previously, all trade tickets were of type int, like in MT4. Due to the occurrences of going beyond the int type in MT5, the type of tickets is changed for long.
//        Accordingly, OrderTicket and OrderSend return 'long' values. Mode of returning the same type as in MT4 (int), to be enabled via
//        Writing the next string before #include <MT4Orders.mqh>

//        #define MT4_TICKET_TYPE // Make OrderSend and OrderTicket return a value of the same type as in MT4 (int).
// 03.09.2017:
//   Add: Added OrderTicketOpen()  - the ticket of the MT5 transaction of opening a position
//                  OrderOpenReason()  - reason for performing the MT5 transaction of opening (reason for opening the position)
//                  OrderCloseReason() - reason for performing the MT5 transaction of closing (reason for closing the position)
// 14.09.2017:
//   Fix: Now the library does not see the current MT5 orders that do not have the state of ORDER_STATE_PLACED.
//        For the library to see all the open MT5 orders, you should write the following string BEFORE the library
//
//        #define MT4ORDERS_SELECTFILTER_OFF // Make MT4Orders.mqh see all the current MT5 orders
// 16.10.2017:
//   Fix: OrdersHistoryTotal() responds to changing the trading account number during execution.
// 13.02.2018
//   Add: Added logging of the wrong execution of MT5-OrderSend.
//   Fix: Now only closing MT5 orders, such as SL/TP/SO or partial/full close, are "invisible."
//   Fix: Mechanism of defining the SL/TP of closed positions after OrderClose having been corrected - it works if StopLevel allows this.
// 15.02.2018
//   Fix: Now MT5-OrderSend synchronization check considers potential special aspects of implementing ECN/STP.
// 06.03.2018
//   Add: Added TICKET_TYPE and MAGIC_TYPE to be able to write a unified cross-platform code
//        Without warnings of compilers, including the MQL4 strict mode.
// 30.05.2018
//   Add: Accelerated working with trading history; middle course was steered in implementations between performance and
//        memory consumption, which is important for VPS. Standard Generic library is used.
//        If you don't want to use the Generic library, the conventional mode is available to work with history.
//        For this purpose, write the following string BEFORE the MT4Orders library:
//
//        #define MT4ORDERS_FASTHISTORY_OFF // Disabling fast trading history implementation, don't use the Generic library.
// 02.11.2018
//   Fix: Now the MT4 position Open price cannot be zero before its triggering.
//   Fix: Some rare execution aspects of certain trading servers have been taken into account.
// 26.11.2018
//   Fix: Magic and comment of a closed MT4 position: Priority of the relevant fields of opening transactions is higher than that of closing ones.
//   Fix: Rare changes in MT5-OrdersTotal and MT5-PositionsTotal are taken into account while calculating MT4-OrdersTotal and MT4-OrderSelect.
//   Fix: Library does not take into account the orders which have opened a position, but have not been yet deleted from MT5.
// 17.01.2019
//   Fix: Fixed an unfortunate error in selecting pending orders.
// 08.02.2019
//   Add: Comment of a position is saved at partial closing via OrderClose.
//        If you need to modify the comment on an open position at partial closing, you can specify it in OrderClose.
// 20.02.2019
//   Fix: In case of no MT5 order, the library will expect the history synchronization from the existing MT5 transaction. In case of failure, it will inform about it.
// 13.03.2019
//   Add: Added OrderTicketID() - PositionID of an MT5 transaction or MT5 position, and the ticket of a pending MT4 order.
//   Add: SELECT_BY_TICKET works for all MT5 tickets (and MT5-PositionID).
// 02.11.2019
//   Fix: Corrected lot, commission, and Close price for CloseBy positions.
// 12.01.2020
//   Fix: OrderTicketID() for balance deals returns now a correct value.
//   Fix: Fixed SELECT_BY_TICKET - selection by OrderTicketID() (MT5-PositionID).
//   Fix: Changed name of the internal library method for greater compatibility with macros.
// 10.04.2020
//   Fix: Partially executed live pending order did not get into OrdersTotal().
// 09.06.2020
//   Add: StopLoss/TakeProfit/ClosePriceRequest for closed positions are now better defined.
// 10.06.2020
//   Add: Added milliseconds, removed price and order rounding in OrderPrint().
// 13.08.2020
//   Add: Added ability to check the performance of library parts via the MT4ORDERS_BENCHMARK_MINTIME macro.
// 20.08.2020
//   Fix: Taking into account the revealed features of partial order execution.
// 29.08.2020
//   Fix: Implemented faster working with the history of trades.
// 24.09.2020
//   Add: If you need to increase the priority of an MT5 order over an MT5 position when selecting a live MT4 order by SELECT_BY_TICKET (the same tickets),
//        this can be done by changing the ticket size to negative: OrderSelect(-Ticket, SELECT_BY_TICKET).
//   Add: If you need to specify the selection of only MT5 order when modifying a live MT4 order (the same tickets),
//        this can be done by changing the ticket size to negative: OrderModify(-Ticket, ...).
//   Add: OrderSelect(INT_MAX, SELECT_BY_POS) - switch to an MT5 position without checking the existence and updating.
//        OrderSelect(INT_MIN, SELECT_BY_POS) - switch to a live MT5 order without checking the existence and updating.
//   Fix: Implemented faster working with the history of trades.
// 30.09.2020
//   Fix: Implemented faster working with the history of trades.
// 08.10.2020
//   Fix: OrderSend of a market order could be executed longer due to a flaw in a fresh MT5 deal search.
// 21.10.2020
//   Add: To provide compatibility for MT4, added OrderTicketID() - returns OrderTicket().
// 11.11.2020
//   Fix: OrderTicketID() and OrderTicketOpen() return the value type specified in TICKET_TYPE.
// 06.12.2020
//   Fix: The cases of incorrect SL/TP execution records in the MT5 trading history are now taken into account.
//   Add: The MT4ORDERS_TESTER_SELECT_BY_TICKET market forces SELECT_BY_TICKET to work in the Tester only via OrderTicketID().
// 04.05.2021
//   Fix: The MT5 orders that open positions and fail to disappear are no longer added to the list of MT4 orders.
//   Fix: CloseBy MT5 orders no longer appear in the list of MT4 orders.
// 12.05.2021
//   Add: The MT4ORDERS_BYPASS_MAXTIME macro changes the constantly emerging cases of trading environment desynchronization in MT5.
// 13.05.2021
//   Fix: Fixed bug in OrderOpenReason().
// 14.05.2021
//   Fix: The BYPASS mechanism no longer affects OrderSelect(INT_MAX, SELECT_BY_POS) and OrderSelect(INT_MIN, SELECT_BY_POS).
// 01.06.2021
//   Fix: Compatibility with the compiler build 2449 and higher.
//   Fix: Improved synchronization. ByPass.mqh must be of the latest version.
//   Add: OrderLots(true) - synchronized size of the selected position, taking into account all orders which close this position.

#ifdef __MQL5__
#ifndef __MT4ORDERS__

// #define MT4ORDERS_BYPASS_MAXTIME 1000000 // Max time (in microseconds) to wait for the trading environment synchronization

#ifdef MT4ORDERS_BYPASS_MAXTIME
  #include <fxsaber\TradesID\ByPass.mqh> // https://www.mql5.com/en/code/34173
#endif // #ifdef MT4ORDERS_BYPASS_MAXTIME

// #define MT4ORDERS_BENCHMARK_MINTIME 1000 // Minimum time for the performance alert trigger.

#ifdef MT4ORDERS_BENCHMARK_MINTIME
  #include <fxsaber\Benchmark\Benchmark.mqh> // https://www.mql5.com/en/code/31279

  #define _B2(A) _B(A, MT4ORDERS_BENCHMARK_MINTIME)
  #define _B3(A) _B(A, 1)
  #define _BV2(A) _BV(A, MT4ORDERS_BENCHMARK_MINTIME)
#else // MT4ORDERS_BENCHMARK_MINTIME
  #define _B2(A) (A)
  #define _B3(A) (A)
  #define _BV2(A) { A; }
#endif // MT4ORDERS_BENCHMARK_MINTIME

#define __MT4ORDERS__ "2021.06.01"
#define MT4ORDERS_SLTP_OLD // Enabling the old mechanism of identifying the SL/TP of closed positions via OrderClose
// #define MT4ORDERS_TESTER_SELECT_BY_TICKET // Forces SELECT_BY_TICKET to work in the Tester only via OrderTicketID().

#ifdef MT4_TICKET_TYPE
  #define TICKET_TYPE int
  #define MAGIC_TYPE  int

  #undef MT4_TICKET_TYPE
#else // MT4_TICKET_TYPE
  #define TICKET_TYPE long // Negative values are also needed for OrderSelectByTicket.
  #define MAGIC_TYPE  long
#endif // MT4_TICKET_TYPE

struct MT4_ORDER
{
  long Ticket;
  int Type;

  long TicketOpen;
  long TicketID;

  double Lots;

  string Symbol;
  string Comment;

  double OpenPriceRequest;
  double OpenPrice;

  long OpenTimeMsc;
  datetime OpenTime;

  ENUM_DEAL_REASON OpenReason;

  double StopLoss;
  double TakeProfit;

  double ClosePriceRequest;
  double ClosePrice;

  long CloseTimeMsc;
  datetime CloseTime;

  ENUM_DEAL_REASON CloseReason;

  ENUM_ORDER_STATE State;

  datetime Expiration;

  long MagicNumber;

  double Profit;

  double Commission;
  double Swap;

#define POSITION_SELECT (-1)
#define ORDER_SELECT (-2)

  static int GetDigits( double Price )
  {
    int Res = 0;

    while ((bool)(Price = ::NormalizeDouble(Price - (int)Price, 8)))
    {
      Price *= 10;

      Res++;
    }

    return(Res);
  }

  static string DoubleToString( const double Num, const int digits )
  {
    return(::DoubleToString(Num, ::MathMax(digits, MT4_ORDER::GetDigits(Num))));
  }

  static string TimeToString( const long time )
  {
    return((string)(datetime)(time / 1000) + "." + ::IntegerToString(time % 1000, 3, '0'));
  }

  static const MT4_ORDER GetPositionData( void )
  {
    MT4_ORDER Res = {};

    Res.Ticket = ::PositionGetInteger(POSITION_TICKET);
    Res.Type = (int)::PositionGetInteger(POSITION_TYPE);

    Res.Lots = ::PositionGetDouble(POSITION_VOLUME);

    Res.Symbol = ::PositionGetString(POSITION_SYMBOL);
//    Res.Comment = NULL; // MT4ORDERS::CheckPositionCommissionComment();

    Res.OpenPrice = ::PositionGetDouble(POSITION_PRICE_OPEN);
    Res.OpenTimeMsc = (datetime)::PositionGetInteger(POSITION_TIME_MSC);

    Res.StopLoss = ::PositionGetDouble(POSITION_SL);
    Res.TakeProfit = ::PositionGetDouble(POSITION_TP);

    Res.ClosePrice = ::PositionGetDouble(POSITION_PRICE_CURRENT);
    Res.CloseTimeMsc = 0;

    Res.Expiration = 0;

    Res.MagicNumber = ::PositionGetInteger(POSITION_MAGIC);

    Res.Profit = ::PositionGetDouble(POSITION_PROFIT);

    Res.Swap = ::PositionGetDouble(POSITION_SWAP);

//    Res.Commission = UNKNOWN_COMMISSION; // MT4ORDERS::CheckPositionCommissionComment();

    return(Res);
  }

  static const MT4_ORDER GetOrderData( void )
  {
    MT4_ORDER Res = {};

    Res.Ticket = ::OrderGetInteger(ORDER_TICKET);
    Res.Type = (int)::OrderGetInteger(ORDER_TYPE);

    Res.Lots = ::OrderGetDouble(ORDER_VOLUME_CURRENT);

    Res.Symbol = ::OrderGetString(ORDER_SYMBOL);
    Res.Comment = ::OrderGetString(ORDER_COMMENT);

    Res.OpenPrice = ::OrderGetDouble(ORDER_PRICE_OPEN);
    Res.OpenTimeMsc = (datetime)::OrderGetInteger(ORDER_TIME_SETUP_MSC);

    Res.StopLoss = ::OrderGetDouble(ORDER_SL);
    Res.TakeProfit = ::OrderGetDouble(ORDER_TP);

    Res.ClosePrice = ::OrderGetDouble(ORDER_PRICE_CURRENT);
    Res.CloseTimeMsc = 0; // (datetime)::OrderGetInteger(ORDER_TIME_DONE)

    Res.Expiration = (datetime)::OrderGetInteger(ORDER_TIME_EXPIRATION);

    Res.MagicNumber = ::OrderGetInteger(ORDER_MAGIC);

    Res.Profit = 0;

    Res.Commission = 0;
    Res.Swap = 0;

    if (!Res.OpenPrice)
      Res.OpenPrice = Res.ClosePrice;

    return(Res);
  }

  string ToString( void ) const
  {
    static const string Types[] = {"buy", "sell", "buy limit", "sell limit", "buy stop", "sell stop", "balance"};
    const int digits = (int)::SymbolInfoInteger(this.Symbol, SYMBOL_DIGITS);

    MT4_ORDER TmpOrder = {};

    if (this.Ticket == POSITION_SELECT)
    {
      TmpOrder = MT4_ORDER::GetPositionData();

      TmpOrder.Comment = this.Comment;
      TmpOrder.Commission = this.Commission;
    }
    else if (this.Ticket == ORDER_SELECT)
      TmpOrder = MT4_ORDER::GetOrderData();

    return(((this.Ticket == POSITION_SELECT) || (this.Ticket == ORDER_SELECT)) ? TmpOrder.ToString() :
           ("#" + (string)this.Ticket + " " +
            MT4_ORDER::TimeToString(this.OpenTimeMsc) + " " +
            ((this.Type < ::ArraySize(Types)) ? Types[this.Type] : "unknown") + " " +
            MT4_ORDER::DoubleToString(this.Lots, 2) + " " +
            (::StringLen(this.Symbol) ? this.Symbol + " " : NULL) +
            MT4_ORDER::DoubleToString(this.OpenPrice, digits) + " " +
            MT4_ORDER::DoubleToString(this.StopLoss, digits) + " " +
            MT4_ORDER::DoubleToString(this.TakeProfit, digits) + " " +
            ((this.CloseTimeMsc > 0) ? (MT4_ORDER::TimeToString(this.CloseTimeMsc) + " ") : "") +
            MT4_ORDER::DoubleToString(this.ClosePrice, digits) + " " +
            MT4_ORDER::DoubleToString(::NormalizeDouble(this.Commission, 3), 2) + " " + // Больше трех цифр после запятой не выводим.
            MT4_ORDER::DoubleToString(this.Swap, 2) + " " +
            MT4_ORDER::DoubleToString(this.Profit, 2) + " " +
            ((this.Comment == "") ? "" : (this.Comment + " ")) +
            (string)this.MagicNumber +
            (((this.Expiration > 0) ? (" expiration " + (string)this.Expiration): ""))));
  }
};

#define RESERVE_SIZE 1000
#define DAY (24 * 3600)
#define HISTORY_PAUSE (MT4HISTORY::IsTester ? 0 : 5)
#define END_TIME D'31.12.3000 23:59:59'
#define THOUSAND 1000
#define LASTTIME(A)                                          \
  if (Time##A >= LastTimeMsc)                                \
  {                                                          \
    const datetime TmpTime = (datetime)(Time##A / THOUSAND); \
                                                             \
    if (TmpTime > this.LastTime)                             \
    {                                                        \
      this.LastTotalOrders = 0;                              \
      this.LastTotalDeals = 0;                               \
                                                             \
      this.LastTime = TmpTime;                               \
      LastTimeMsc = this.LastTime * THOUSAND;                \
    }                                                        \
                                                             \
    this.LastTotal##A##s++;                                  \
  }

#ifndef MT4ORDERS_FASTHISTORY_OFF
  #include <Generic\HashMap.mqh>
#endif // MT4ORDERS_FASTHISTORY_OFF

class MT4HISTORY
{
private:
  static const bool MT4HISTORY::IsTester;
//  static long MT4HISTORY::AccountNumber;

#ifndef MT4ORDERS_FASTHISTORY_OFF
  CHashMap<ulong, ulong> DealsIn;  // By PositionID, it returns DealIn.
  CHashMap<ulong, ulong> DealsOut; // By PositionID, it returns DealOut.
#endif // MT4ORDERS_FASTHISTORY_OFF

  long Tickets[];
  uint Amount;

  int LastTotalDeals;
  int LastTotalOrders;

#ifdef MT4ORDERS_HISTORY_OLD

  datetime LastTime;
  datetime LastInitTime;

  int PrevDealsTotal;
  int PrevOrdersTotal;

  // https://www.mql5.com/ru/forum/93352/page50#comment_18040243
  bool IsChangeHistory( void )
  {
    bool Res = !_B2(::HistorySelect(0, INT_MAX));

    if (!Res)
    {
      const int iDealsTotal = ::HistoryDealsTotal();
      const int iOrdersTotal = ::HistoryOrdersTotal();

      if (Res = (iDealsTotal != this.PrevDealsTotal) || (iOrdersTotal != this.PrevOrdersTotal))
      {
        this.PrevDealsTotal = iDealsTotal;
        this.PrevOrdersTotal = iOrdersTotal;
      }
    }

    return(Res);
  }

  bool RefreshHistory( void )
  {
    bool Res = !MT4HISTORY::IsChangeHistory();

    if (!Res)
    {
      const datetime LastTimeCurrent = ::TimeCurrent();

      if (!MT4HISTORY::IsTester && ((LastTimeCurrent >= this.LastInitTime + DAY)/* || (MT4HISTORY::AccountNumber != ::AccountInfoInteger(ACCOUNT_LOGIN))*/))
      {
      //  MT4HISTORY::AccountNumber = ::AccountInfoInteger(ACCOUNT_LOGIN);

        this.LastTime = 0;

        this.LastTotalOrders = 0;
        this.LastTotalDeals = 0;

        this.Amount = 0;

        ::ArrayResize(this.Tickets, this.Amount, RESERVE_SIZE);

        this.LastInitTime = LastTimeCurrent;

      #ifndef MT4ORDERS_FASTHISTORY_OFF
        this.DealsIn.Clear();
        this.DealsOut.Clear();
      #endif // MT4ORDERS_FASTHISTORY_OFF
      }

      const datetime LastTimeCurrentLeft = LastTimeCurrent - HISTORY_PAUSE;

      // If LastTime is equal to zero, then HistorySelect has already been done in MT4HISTORY::IsChangeHistory().
      if (!this.LastTime || _B2(::HistorySelect(this.LastTime, END_TIME))) // https://www.mql5.com/ru/forum/285631/page79#comment_9884935
  //    if (_B2(::HistorySelect(this.LastTime, INT_MAX))) // Perhaps INT_MAX is faster than END_TIME
      {
        const int TotalOrders = ::HistoryOrdersTotal();
        const int TotalDeals = ::HistoryDealsTotal();

        Res = ((TotalOrders > this.LastTotalOrders) || (TotalDeals > this.LastTotalDeals));

        if (Res)
        {
          int iOrder = this.LastTotalOrders;
          int iDeal = this.LastTotalDeals;

          ulong TicketOrder = 0;
          ulong TicketDeal = 0;

          long TimeOrder = (iOrder < TotalOrders) ? ::HistoryOrderGetInteger((TicketOrder = ::HistoryOrderGetTicket(iOrder)), ORDER_TIME_DONE_MSC) : LONG_MAX;
          long TimeDeal = (iDeal < TotalDeals) ? ::HistoryDealGetInteger((TicketDeal = ::HistoryDealGetTicket(iDeal)), DEAL_TIME_MSC) : LONG_MAX;

          if (this.LastTime < LastTimeCurrentLeft)
          {
            this.LastTotalOrders = 0;
            this.LastTotalDeals = 0;

            this.LastTime = LastTimeCurrentLeft;
          }

          long LastTimeMsc = this.LastTime * THOUSAND;

          while ((iDeal < TotalDeals) || (iOrder < TotalOrders))
            if (TimeOrder < TimeDeal)
            {
              LASTTIME(Order)

              if (MT4HISTORY::IsMT4Order(TicketOrder))
              {
                this.Amount = ::ArrayResize(this.Tickets, this.Amount + 1, RESERVE_SIZE);

                this.Tickets[this.Amount - 1] = -(long)TicketOrder;
              }

              iOrder++;

              TimeOrder = (iOrder < TotalOrders) ? ::HistoryOrderGetInteger((TicketOrder = ::HistoryOrderGetTicket(iOrder)), ORDER_TIME_DONE_MSC) : LONG_MAX;
            }
            else
            {
              LASTTIME(Deal)

              if (MT4HISTORY::IsMT4Deal(TicketDeal))
              {
                this.Amount = ::ArrayResize(this.Tickets, this.Amount + 1, RESERVE_SIZE);

                this.Tickets[this.Amount - 1] = (long)TicketDeal;

              #ifndef MT4ORDERS_FASTHISTORY_OFF
                _B2(this.DealsOut.Add(::HistoryDealGetInteger(TicketDeal, DEAL_POSITION_ID), TicketDeal));
              #endif // MT4ORDERS_FASTHISTORY_OFF
              }
            #ifndef MT4ORDERS_FASTHISTORY_OFF
              else if ((ENUM_DEAL_ENTRY)::HistoryDealGetInteger(TicketDeal, DEAL_ENTRY) == DEAL_ENTRY_IN)
                _B2(this.DealsIn.Add(::HistoryDealGetInteger(TicketDeal, DEAL_POSITION_ID), TicketDeal));
            #endif // MT4ORDERS_FASTHISTORY_OFF

              iDeal++;

              TimeDeal = (iDeal < TotalDeals) ? ::HistoryDealGetInteger((TicketDeal = ::HistoryDealGetTicket(iDeal)), DEAL_TIME_MSC) : LONG_MAX;
            }
        }
        else if (LastTimeCurrentLeft > this.LastTime)
        {
          this.LastTime = LastTimeCurrentLeft;

          this.LastTotalOrders = 0;
          this.LastTotalDeals = 0;
        }
      }
    }

    return(Res);
  }

#else // #ifdef MT4ORDERS_HISTORY_OLD
  bool RefreshHistory( void )
  {
    if (_B2(::HistorySelect(0, INT_MAX)))
    {
      const int TotalOrders = ::HistoryOrdersTotal();
      const int TotalDeals = ::HistoryDealsTotal();

      if ((TotalOrders > this.LastTotalOrders) || (TotalDeals > this.LastTotalDeals))
      {
        ulong TicketOrder = 0;
        ulong TicketDeal = 0;

        long TimeOrder = (this.LastTotalOrders < TotalOrders) ?
                           ::HistoryOrderGetInteger((TicketOrder = ::HistoryOrderGetTicket(this.LastTotalOrders)), ORDER_TIME_DONE_MSC) : LONG_MAX;
        long TimeDeal = (this.LastTotalDeals < TotalDeals) ?
                          ::HistoryDealGetInteger((TicketDeal = ::HistoryDealGetTicket(this.LastTotalDeals)), DEAL_TIME_MSC) : LONG_MAX;

        while ((this.LastTotalDeals < TotalDeals) || (this.LastTotalOrders < TotalOrders))
          if (TimeOrder < TimeDeal)
          {
            if (MT4HISTORY::IsMT4Order(TicketOrder))
            {
              this.Amount = ::ArrayResize(this.Tickets, this.Amount + 1, RESERVE_SIZE);

              this.Tickets[this.Amount - 1] = -(long)TicketOrder;
            }

            this.LastTotalOrders++;

            TimeOrder = (this.LastTotalOrders < TotalOrders) ?
                          ::HistoryOrderGetInteger((TicketOrder = ::HistoryOrderGetTicket(this.LastTotalOrders)), ORDER_TIME_DONE_MSC) : LONG_MAX;
          }
          else
          {
            if (MT4HISTORY::IsMT4Deal(TicketDeal))
            {
              this.Amount = ::ArrayResize(this.Tickets, this.Amount + 1, RESERVE_SIZE);

              this.Tickets[this.Amount - 1] = (long)TicketDeal;

              _B2(this.DealsOut.Add(::HistoryDealGetInteger(TicketDeal, DEAL_POSITION_ID), TicketDeal));
            }
            else if ((ENUM_DEAL_ENTRY)::HistoryDealGetInteger(TicketDeal, DEAL_ENTRY) == DEAL_ENTRY_IN)
              _B2(this.DealsIn.Add(::HistoryDealGetInteger(TicketDeal, DEAL_POSITION_ID), TicketDeal));

            this.LastTotalDeals++;

            TimeDeal = (this.LastTotalDeals < TotalDeals) ?
                         ::HistoryDealGetInteger((TicketDeal = ::HistoryDealGetTicket(this.LastTotalDeals)), DEAL_TIME_MSC) : LONG_MAX;
          }
      }
    }

    return(true);
  }
#endif // #ifdef MT4ORDERS_HISTORY_OLD #else
public:
  static bool IsMT4Deal( const ulong &Ticket )
  {
    const ENUM_DEAL_TYPE DealType = (ENUM_DEAL_TYPE)::HistoryDealGetInteger(Ticket, DEAL_TYPE);
    const ENUM_DEAL_ENTRY DealEntry = (ENUM_DEAL_ENTRY)::HistoryDealGetInteger(Ticket, DEAL_ENTRY);

    return(((DealType != DEAL_TYPE_BUY) && (DealType != DEAL_TYPE_SELL)) ||      // non trading deal
           ((DealEntry == DEAL_ENTRY_OUT) || (DealEntry == DEAL_ENTRY_OUT_BY))); // trading
  }

  static bool IsMT4Order( const ulong &Ticket )
  {
    // If the pending order has been executed, its ORDER_POSITION_ID is filled out.
    // https://www.mql5.com/ru/forum/170952/page70#comment_6543162
    // https://www.mql5.com/ru/forum/93352/page19#comment_6646726
    // The second condition: when a limit order has been partially executed and then deleted.
    return(!::HistoryOrderGetInteger(Ticket, ORDER_POSITION_ID) || (::HistoryOrderGetDouble(Ticket, ORDER_VOLUME_CURRENT) &&
                                                                    ::HistoryOrderGetInteger(Ticket, ORDER_TYPE) > ORDER_TYPE_SELL));
  }

  MT4HISTORY( void ) : Amount(::ArrayResize(this.Tickets, 0, RESERVE_SIZE)),
                       LastTotalDeals(0), LastTotalOrders(0)
                     #ifdef MT4ORDERS_HISTORY_OLD
                       , LastTime(0), LastInitTime(0), PrevDealsTotal(0), PrevOrdersTotal(0)
                     #endif // #ifdef MT4ORDERS_HISTORY_OLD
  {
//    this.RefreshHistory(); // If history is not used, there is no point in wasting resources.
  }

  ulong GetPositionDealIn( const ulong PositionIdentifier = -1 ) // 0 is not available, since the tester's balance trade is zero
  {
    ulong Ticket = 0;

    if (PositionIdentifier == -1)
    {
      const ulong MyPositionIdentifier = ::PositionGetInteger(POSITION_IDENTIFIER);

    #ifndef MT4ORDERS_FASTHISTORY_OFF
      if (!_B2(this.DealsIn.TryGetValue(MyPositionIdentifier, Ticket))
          #ifndef MT4ORDERS_HISTORY_OLD
          && !_B2(this.RefreshHistory() && this.DealsIn.TryGetValue(MyPositionIdentifier, Ticket))
          #endif // #ifndef MT4ORDERS_HISTORY_OLD
          )
    #endif // MT4ORDERS_FASTHISTORY_OFF
      {
        const datetime PosTime = (datetime)::PositionGetInteger(POSITION_TIME);

        if (_B3(::HistorySelect(PosTime, PosTime)))
        {
          const int Total = ::HistoryDealsTotal();

          for (int i = 0; i < Total; i++)
          {
            const ulong TicketDeal = ::HistoryDealGetTicket(i);

            if ((::HistoryDealGetInteger(TicketDeal, DEAL_POSITION_ID) == MyPositionIdentifier) /*&&
                ((ENUM_DEAL_ENTRY)::HistoryDealGetInteger(TicketDeal, DEAL_ENTRY) == DEAL_ENTRY_IN) */) // First mention will be DEAL_ENTRY_IN anyway
            {
              Ticket = TicketDeal;

            #ifndef MT4ORDERS_FASTHISTORY_OFF
              _B2(this.DealsIn.Add(MyPositionIdentifier, Ticket));
            #endif // MT4ORDERS_FASTHISTORY_OFF

              break;
            }
          }
        }
      }
    }
    else if (PositionIdentifier && // PositionIdentifier of balance trades is zero
           #ifndef MT4ORDERS_FASTHISTORY_OFF
             !_B2(this.DealsIn.TryGetValue(PositionIdentifier, Ticket)) &&
             #ifndef MT4ORDERS_HISTORY_OLD
               !_B2(this.RefreshHistory() && this.DealsIn.TryGetValue(PositionIdentifier, Ticket)) &&
             #endif // #ifndef MT4ORDERS_HISTORY_OLD
           #endif // MT4ORDERS_FASTHISTORY_OFF
             _B3(::HistorySelectByPosition(PositionIdentifier)) && (::HistoryDealsTotal() > 1)) // Why > 1, not > 0 ?!
    {
      Ticket = _B2(::HistoryDealGetTicket(0)); // First mention will be DEAL_ENTRY_IN anyway

      /*
      const int Total = ::HistoryDealsTotal();

      for (int i = 0; i < Total; i++)
      {
        const ulong TicketDeal = ::HistoryDealGetTicket(i);

        if (TicketDeal > 0)
          if ((ENUM_DEAL_ENTRY)::HistoryDealGetInteger(TicketDeal, DEAL_ENTRY) == DEAL_ENTRY_IN)
          {
            Ticket = TicketDeal;

            break;
          }
      } */

    #ifndef MT4ORDERS_FASTHISTORY_OFF
      _B2(this.DealsIn.Add(PositionIdentifier, Ticket));
    #endif // MT4ORDERS_FASTHISTORY_OFF
    }

    return(Ticket);
  }

  ulong GetPositionDealOut( const ulong PositionIdentifier )
  {
    ulong Ticket = 0;

  #ifndef MT4ORDERS_FASTHISTORY_OFF
    if (!_B2(this.DealsOut.TryGetValue(PositionIdentifier, Ticket)) && _B2(this.RefreshHistory()))
      _B2(this.DealsOut.TryGetValue(PositionIdentifier, Ticket));
    #endif // MT4ORDERS_FASTHISTORY_OFF

    return(Ticket);
  }

  int GetAmount( void )
  {
    _B2(this.RefreshHistory());

    return((int)this.Amount);
  }

  long operator []( const uint &Pos )
  {
    long Res = 0;

    if ((Pos >= this.Amount)/* || (!MT4HISTORY::IsTester && (MT4HISTORY::AccountNumber != ::AccountInfoInteger(ACCOUNT_LOGIN)))*/)
    {
      _B2(this.RefreshHistory());

      if (Pos < this.Amount)
        Res = this.Tickets[Pos];
    }
    else
      Res = this.Tickets[Pos];

    return(Res);
  }
};

static const bool MT4HISTORY::IsTester = ::MQLInfoInteger(MQL_TESTER);
// static long MT4HISTORY::AccountNumber = ::AccountInfoInteger(ACCOUNT_LOGIN);

#undef LASTTIME
#undef THOUSAND
#undef END_TIME
#undef HISTORY_PAUSE
#undef DAY
#undef RESERVE_SIZE

#define OP_BUY ORDER_TYPE_BUY
#define OP_SELL ORDER_TYPE_SELL
#define OP_BUYLIMIT ORDER_TYPE_BUY_LIMIT
#define OP_SELLLIMIT ORDER_TYPE_SELL_LIMIT
#define OP_BUYSTOP ORDER_TYPE_BUY_STOP
#define OP_SELLSTOP ORDER_TYPE_SELL_STOP
#define OP_BALANCE 6

#define SELECT_BY_POS 0
#define SELECT_BY_TICKET 1

#define MODE_TRADES 0
#define MODE_HISTORY 1

class MT4ORDERS
{
private:
  static MT4_ORDER Order;
  static MT4HISTORY History;

  static const bool MT4ORDERS::IsTester;
  static const bool MT4ORDERS::IsHedging;

  static int OrderSendBug;

//  static bool HistorySelectOrder( const ulong &Ticket )
  static bool HistorySelectOrder( const ulong Ticket )
  {
    return(Ticket && ((::HistoryOrderGetInteger(Ticket, ORDER_TICKET) == Ticket) ||
                      (_B2(::HistorySelect(0, INT_MAX)) && (::HistoryOrderGetInteger(Ticket, ORDER_TICKET) == Ticket))));
  }

  static bool HistorySelectDeal( const ulong &Ticket )
  {
    return(Ticket && ((::HistoryDealGetInteger(Ticket, DEAL_TICKET) == Ticket) ||
                      (_B2(::HistorySelect(0, INT_MAX)) && (::HistoryDealGetInteger(Ticket, DEAL_TICKET) == Ticket))));
  }

#define UNKNOWN_COMMISSION DBL_MIN
#define UNKNOWN_REQUEST_PRICE DBL_MIN
#define UNKNOWN_TICKET 0
// #define UNKNOWN_REASON (-1)

  static bool CheckNewTicket( void )
  {
    return(false); // This functionality is useless - there is INT_MIN/INT_MAX with SELECT_BY_POS + MODE_TRADES

    static long PrevPosTimeUpdate = 0;
    static long PrevPosTicket = 0;

    const long PosTimeUpdate = ::PositionGetInteger(POSITION_TIME_UPDATE_MSC);
    const long PosTicket = ::PositionGetInteger(POSITION_TICKET);

    // In case that the user has not chosen a position via MT4Orders
    // There is no reason to overload MQL5-PositionSelect* and MQL5-OrderSelect.
    // This check is sufficient, since several changes of position + PositionSelect per millisecond are only possible in tester
    const bool Res = ((PosTimeUpdate != PrevPosTimeUpdate) || (PosTicket != PrevPosTicket));

    if (Res)
    {
      MT4ORDERS::GetPositionData();

      PrevPosTimeUpdate = PosTimeUpdate;
      PrevPosTicket = PosTicket;
    }

    return(Res);
  }

  static bool CheckPositionTicketOpen( void )
  {
    if ((MT4ORDERS::Order.TicketOpen == UNKNOWN_TICKET) || MT4ORDERS::CheckNewTicket())
      MT4ORDERS::Order.TicketOpen = (long)_B2(MT4ORDERS::History.GetPositionDealIn()); // All because of this very expensive function

    return(true);
  }

  static bool CheckPositionCommissionComment( void )
  {
    if ((MT4ORDERS::Order.Commission == UNKNOWN_COMMISSION) || MT4ORDERS::CheckNewTicket())
    {
      MT4ORDERS::Order.Commission = 0; // ::PositionGetDouble(POSITION_COMMISSION); // Always zero
      MT4ORDERS::Order.Comment = ::PositionGetString(POSITION_COMMENT);

      if (!MT4ORDERS::Order.Commission || (MT4ORDERS::Order.Comment == ""))
      {
        MT4ORDERS::CheckPositionTicketOpen();

        const ulong Ticket = MT4ORDERS::Order.TicketOpen;

        if ((Ticket > 0) && _B2(MT4ORDERS::HistorySelectDeal(Ticket)))
        {
          if (!MT4ORDERS::Order.Commission)
          {
            const double LotsIn = ::HistoryDealGetDouble(Ticket, DEAL_VOLUME);

            if (LotsIn > 0)
              MT4ORDERS::Order.Commission = ::HistoryDealGetDouble(Ticket, DEAL_COMMISSION) * ::PositionGetDouble(POSITION_VOLUME) / LotsIn;
          }

          if (MT4ORDERS::Order.Comment == "")
            MT4ORDERS::Order.Comment = ::HistoryDealGetString(Ticket, DEAL_COMMENT);
        }
      }
    }

    return(true);
  }
/*
  static bool CheckPositionOpenReason( void )
  {
    if ((MT4ORDERS::Order.OpenReason == UNKNOWN_REASON) || MT4ORDERS::CheckNewTicket())
    {
      MT4ORDERS::CheckPositionTicketOpen();

      const ulong Ticket = MT4ORDERS::Order.TicketOpen;

      if ((Ticket > 0) && (MT4ORDERS::IsTester || MT4ORDERS::HistorySelectDeal(Ticket)))
        MT4ORDERS::Order.OpenReason = (ENUM_DEAL_REASON)::HistoryDealGetInteger(Ticket, DEAL_REASON);
    }

    return(true);
  }
*/
  static bool CheckPositionOpenPriceRequest( void )
  {
    const long PosTicket = ::PositionGetInteger(POSITION_TICKET);

    if (((MT4ORDERS::Order.OpenPriceRequest == UNKNOWN_REQUEST_PRICE) || MT4ORDERS::CheckNewTicket()) &&
        !(MT4ORDERS::Order.OpenPriceRequest = (_B2(MT4ORDERS::HistorySelectOrder(PosTicket)) &&
                                              (MT4ORDERS::IsTester || (::PositionGetInteger(POSITION_TIME_MSC) ==
                                              ::HistoryOrderGetInteger(PosTicket, ORDER_TIME_DONE_MSC)))) // Is this check necessary?
                                            ? ::HistoryOrderGetDouble(PosTicket, ORDER_PRICE_OPEN)
                                            : ::PositionGetDouble(POSITION_PRICE_OPEN)))
      MT4ORDERS::Order.OpenPriceRequest = ::PositionGetDouble(POSITION_PRICE_OPEN); // In case the order price is zero

    return(true);
  }

  static void GetPositionData( void )
  {
    MT4ORDERS::Order.Ticket = POSITION_SELECT;

    MT4ORDERS::Order.Commission = UNKNOWN_COMMISSION; // MT4ORDERS::CheckPositionCommissionComment();
    MT4ORDERS::Order.OpenPriceRequest = UNKNOWN_REQUEST_PRICE; // MT4ORDERS::CheckPositionOpenPriceRequest()
    MT4ORDERS::Order.TicketOpen = UNKNOWN_TICKET;
//    MT4ORDERS::Order.OpenReason = UNKNOWN_REASON;

//    const bool AntoWarning = ::OrderSelect(0); // Resets data of the selected position to zero - may be needed for OrderModify

    return;
  }

// #undef UNKNOWN_REASON
#undef UNKNOWN_TICKET
#undef UNKNOWN_REQUEST_PRICE
#undef UNKNOWN_COMMISSION

  static void GetOrderData( void )
  {
    MT4ORDERS::Order.Ticket = ORDER_SELECT;

//    ::PositionSelectByTicket(0);  // Resets data of the selected position to zero - may be needed for OrderModify

    return;
  }

  static void GetHistoryOrderData( const ulong Ticket )
  {
    MT4ORDERS::Order.Ticket = ::HistoryOrderGetInteger(Ticket, ORDER_TICKET);
    MT4ORDERS::Order.Type = (int)::HistoryOrderGetInteger(Ticket, ORDER_TYPE);

    MT4ORDERS::Order.TicketOpen = MT4ORDERS::Order.Ticket;
    MT4ORDERS::Order.TicketID = MT4ORDERS::Order.Ticket; // A deleted pending order can have a non-zero POSITION_ID.

    MT4ORDERS::Order.Lots = ::HistoryOrderGetDouble(Ticket, ORDER_VOLUME_CURRENT);

    if (!MT4ORDERS::Order.Lots)
      MT4ORDERS::Order.Lots = ::HistoryOrderGetDouble(Ticket, ORDER_VOLUME_INITIAL);

    MT4ORDERS::Order.Symbol = ::HistoryOrderGetString(Ticket, ORDER_SYMBOL);
    MT4ORDERS::Order.Comment = ::HistoryOrderGetString(Ticket, ORDER_COMMENT);

    MT4ORDERS::Order.OpenTimeMsc = ::HistoryOrderGetInteger(Ticket, ORDER_TIME_SETUP_MSC);
    MT4ORDERS::Order.OpenTime = (datetime)(MT4ORDERS::Order.OpenTimeMsc / 1000);

    MT4ORDERS::Order.OpenPrice = ::HistoryOrderGetDouble(Ticket, ORDER_PRICE_OPEN);
    MT4ORDERS::Order.OpenPriceRequest = MT4ORDERS::Order.OpenPrice;

    MT4ORDERS::Order.OpenReason = (ENUM_DEAL_REASON)::HistoryOrderGetInteger(Ticket, ORDER_REASON);

    MT4ORDERS::Order.StopLoss = ::HistoryOrderGetDouble(Ticket, ORDER_SL);
    MT4ORDERS::Order.TakeProfit = ::HistoryOrderGetDouble(Ticket, ORDER_TP);

    MT4ORDERS::Order.CloseTimeMsc = ::HistoryOrderGetInteger(Ticket, ORDER_TIME_DONE_MSC);
    MT4ORDERS::Order.CloseTime = (datetime)(MT4ORDERS::Order.CloseTimeMsc / 1000);

    MT4ORDERS::Order.ClosePrice = ::HistoryOrderGetDouble(Ticket, ORDER_PRICE_CURRENT);
    MT4ORDERS::Order.ClosePriceRequest = MT4ORDERS::Order.ClosePrice;

    MT4ORDERS::Order.CloseReason = MT4ORDERS::Order.OpenReason;

    MT4ORDERS::Order.State = (ENUM_ORDER_STATE)::HistoryOrderGetInteger(Ticket, ORDER_STATE);

    MT4ORDERS::Order.Expiration = (datetime)::HistoryOrderGetInteger(Ticket, ORDER_TIME_EXPIRATION);

    MT4ORDERS::Order.MagicNumber = ::HistoryOrderGetInteger(Ticket, ORDER_MAGIC);

    MT4ORDERS::Order.Profit = 0;

    MT4ORDERS::Order.Commission = 0;
    MT4ORDERS::Order.Swap = 0;

    return;
  }

  static string GetTickFlag( uint tickflag )
  {
    string flag = " " + (string)tickflag;

  #define TICKFLAG_MACRO(A) flag += ((bool)(tickflag & TICK_FLAG_##A)) ? " TICK_FLAG_" + #A : ""; \
                            tickflag -= tickflag & TICK_FLAG_##A;
    TICKFLAG_MACRO(BID)
    TICKFLAG_MACRO(ASK)
    TICKFLAG_MACRO(LAST)
    TICKFLAG_MACRO(VOLUME)
    TICKFLAG_MACRO(BUY)
    TICKFLAG_MACRO(SELL)
  #undef TICKFLAG_MACRO

    if (tickflag)
      flag += " FLAG_UNKNOWN (" + (string)tickflag + ")";

    return(flag);
  }

#define TOSTR(A) " " + #A + " = " + (string)Tick.A
#define TOSTR2(A) " " + #A + " = " + ::DoubleToString(Tick.A, digits)
#define TOSTR3(A) " " + #A + " = " + (string)(A)

  static string TickToString( const string &Symb, const MqlTick &Tick )
  {
    const int digits = (int)::SymbolInfoInteger(Symb, SYMBOL_DIGITS);

    return(TOSTR3(Symb) + TOSTR(time) + "." + ::IntegerToString(Tick.time_msc % 1000, 3, '0') +
           TOSTR2(bid) + TOSTR2(ask) + TOSTR2(last)+ TOSTR(volume) + MT4ORDERS::GetTickFlag(Tick.flags));
  }

  static string TickToString( const string &Symb )
  {
    MqlTick Tick = {};

    return(TOSTR3(::SymbolInfoTick(Symb, Tick)) + MT4ORDERS::TickToString(Symb, Tick));
  }

#undef TOSTR3
#undef TOSTR2
#undef TOSTR

  static void AlertLog( void )
  {
    ::Alert("Please send the logs to the coauthor - https://www.mql5.com/en/users/fxsaber");

    string Str = ::TimeToString(::TimeLocal(), TIME_DATE);
    ::StringReplace(Str, ".", NULL);

    ::Alert(::TerminalInfoString(TERMINAL_PATH) + "\\MQL5\\Logs\\" + Str + ".log");

    return;
  }

  static long GetTimeCurrent( void )
  {
    long Res = 0;
    MqlTick Tick = {};

    for (int i = ::SymbolsTotal(true) - 1; i >= 0; i--)
    {
      const string SymbName = ::SymbolName(i, true);

      if (!::SymbolInfoInteger(SymbName, SYMBOL_CUSTOM) && ::SymbolInfoTick(SymbName, Tick) && (Tick.time_msc > Res))
        Res = Tick.time_msc;
    }

    return(Res);
  }

  static string TimeToString( const long time )
  {
    return((string)(datetime)(time / 1000) + "." + ::IntegerToString(time % 1000, 3, '0'));
  }

#define WHILE(A) while ((!(Res = (A))) && MT4ORDERS::Waiting())

#define TOSTR(A)  #A + " = " + (string)(A) + "\n"
#define TOSTR2(A) #A + " = " + ::EnumToString(A) + " (" + (string)(A) + ")\n"

  static void GetHistoryPositionData( const ulong Ticket )
  {
    MT4ORDERS::Order.Ticket = (long)Ticket;
    MT4ORDERS::Order.TicketID = ::HistoryDealGetInteger(MT4ORDERS::Order.Ticket, DEAL_POSITION_ID);
    MT4ORDERS::Order.Type = (int)::HistoryDealGetInteger(Ticket, DEAL_TYPE);

    if ((MT4ORDERS::Order.Type > OP_SELL))
      MT4ORDERS::Order.Type += (OP_BALANCE - OP_SELL - 1);
    else
      MT4ORDERS::Order.Type = 1 - MT4ORDERS::Order.Type;

    MT4ORDERS::Order.Lots = ::HistoryDealGetDouble(Ticket, DEAL_VOLUME);

    MT4ORDERS::Order.Symbol = ::HistoryDealGetString(Ticket, DEAL_SYMBOL);
    MT4ORDERS::Order.Comment = ::HistoryDealGetString(Ticket, DEAL_COMMENT);

    MT4ORDERS::Order.CloseTimeMsc = ::HistoryDealGetInteger(Ticket, DEAL_TIME_MSC);
    MT4ORDERS::Order.CloseTime = (datetime)(MT4ORDERS::Order.CloseTimeMsc / 1000); // (datetime)::HistoryDealGetInteger(Ticket, DEAL_TIME);

    MT4ORDERS::Order.ClosePrice = ::HistoryDealGetDouble(Ticket, DEAL_PRICE);

    MT4ORDERS::Order.CloseReason = (ENUM_DEAL_REASON)::HistoryDealGetInteger(Ticket, DEAL_REASON);

    MT4ORDERS::Order.Expiration = 0;

    MT4ORDERS::Order.MagicNumber = ::HistoryDealGetInteger(Ticket, DEAL_MAGIC);

    MT4ORDERS::Order.Profit = ::HistoryDealGetDouble(Ticket, DEAL_PROFIT);

    MT4ORDERS::Order.Commission = ::HistoryDealGetDouble(Ticket, DEAL_COMMISSION);
    MT4ORDERS::Order.Swap = ::HistoryDealGetDouble(Ticket, DEAL_SWAP);

#ifndef MT4ORDERS_SLTP_OLD
    MT4ORDERS::Order.StopLoss = ::HistoryDealGetDouble(Ticket, DEAL_SL);
    MT4ORDERS::Order.TakeProfit = ::HistoryDealGetDouble(Ticket, DEAL_TP);
#else // MT4ORDERS_SLTP_OLD
    MT4ORDERS::Order.StopLoss = 0;
    MT4ORDERS::Order.TakeProfit = 0;
#endif // MT4ORDERS_SLTP_OLD

    const ulong OrderTicket = (MT4ORDERS::Order.Type < OP_BALANCE) ? ::HistoryDealGetInteger(Ticket, DEAL_ORDER) : 0;
    const ulong PosTicket = MT4ORDERS::Order.TicketID;
    const ulong OpenTicket = (OrderTicket > 0) ? _B2(MT4ORDERS::History.GetPositionDealIn(PosTicket)) : 0;

    if (OpenTicket > 0)
    {
      const ENUM_DEAL_REASON Reason = (ENUM_DEAL_REASON)HistoryDealGetInteger(Ticket, DEAL_REASON);
      const ENUM_DEAL_ENTRY DealEntry = (ENUM_DEAL_ENTRY)::HistoryDealGetInteger(Ticket, DEAL_ENTRY);

    // History (OpenTicket and OrderTicket) is loaded due to GetPositionDealIn, - HistorySelectByPosition
    #ifdef MT4ORDERS_FASTHISTORY_OFF
      const bool Res = true;
    #else // MT4ORDERS_FASTHISTORY_OFF
      // Partial execution will generate the necessary order - https://www.mql5.com/ru/forum/227423/page2#comment_6543129
      bool Res = MT4ORDERS::IsTester ? MT4ORDERS::HistorySelectOrder(OrderTicket) : MT4ORDERS::Waiting(true);

      // Можно долго ждать в этой ситуации: https://www.mql5.com/ru/forum/170952/page184#comment_17913645
      if (!Res)
        WHILE(_B2(MT4ORDERS::HistorySelectOrder(OrderTicket))) // https://www.mql5.com/ru/forum/304239#comment_10710403
          ;

      if (_B2(MT4ORDERS::HistorySelectDeal(OpenTicket))) // It will surely work, since OpenTicket is reliably in history.
    #endif // MT4ORDERS_FASTHISTORY_OFF
      {
        MT4ORDERS::Order.TicketOpen = (long)OpenTicket;

        MT4ORDERS::Order.OpenReason = (ENUM_DEAL_REASON)HistoryDealGetInteger(OpenTicket, DEAL_REASON);

        MT4ORDERS::Order.OpenPrice = ::HistoryDealGetDouble(OpenTicket, DEAL_PRICE);

        MT4ORDERS::Order.OpenTimeMsc = ::HistoryDealGetInteger(OpenTicket, DEAL_TIME_MSC);
        MT4ORDERS::Order.OpenTime = (datetime)(MT4ORDERS::Order.OpenTimeMsc / 1000);

        const double OpenLots = ::HistoryDealGetDouble(OpenTicket, DEAL_VOLUME);

        if (OpenLots > 0)
          MT4ORDERS::Order.Commission += ::HistoryDealGetDouble(OpenTicket, DEAL_COMMISSION) * MT4ORDERS::Order.Lots / OpenLots;

//        if (!MT4ORDERS::Order.MagicNumber) // Magic number of a closed position must always be equal to that of the opening deal.
          const long Magic = ::HistoryDealGetInteger(OpenTicket, DEAL_MAGIC);

          if (Magic)
            MT4ORDERS::Order.MagicNumber = Magic;

//        if (MT4ORDERS::Order.Comment == "") // Comment on a closed position must always be equal to that on the opening deal.
          const string StrComment = ::HistoryDealGetString(OpenTicket, DEAL_COMMENT);

        if (Res) // OrderTicket may be absent in history, but it may be found among those still alive. It is probably reasonable to wheedle info out of there.
        {
          double OrderPriceOpen = ::HistoryOrderGetDouble(OrderTicket, ORDER_PRICE_OPEN);

      #ifdef MT4ORDERS_SLTP_OLD
          if (Reason == DEAL_REASON_TP)
          {
            if (!OrderPriceOpen)
              // https://www.mql5.com/ru/forum/1111/page2820#comment_17749873
              OrderPriceOpen = (double)::StringSubstr(MT4ORDERS::Order.Comment, MT4ORDERS::IsTester ? 3 : (::StringFind(MT4ORDERS::Order.Comment, "tp ") + 3));

            MT4ORDERS::Order.TakeProfit = OrderPriceOpen;
            MT4ORDERS::Order.StopLoss = ::HistoryOrderGetDouble(OrderTicket, ORDER_TP);
          }
          else if (Reason == DEAL_REASON_SL)
          {
            if (!OrderPriceOpen)
              // https://www.mql5.com/ru/forum/1111/page2820#comment_17749873
              OrderPriceOpen = (double)::StringSubstr(MT4ORDERS::Order.Comment, MT4ORDERS::IsTester ? 3 : (::StringFind(MT4ORDERS::Order.Comment, "sl ") + 3));

            MT4ORDERS::Order.StopLoss = OrderPriceOpen;
            MT4ORDERS::Order.TakeProfit = ::HistoryOrderGetDouble(OrderTicket, ORDER_SL);
          }
          else if (!MT4ORDERS::IsTester &&::StringLen(MT4ORDERS::Order.Comment) > 3)
          {
            const string PartComment = ::StringSubstr(MT4ORDERS::Order.Comment, 0, 3);

            if (PartComment == "[tp")
            {
              MT4ORDERS::Order.CloseReason = DEAL_REASON_TP;

              if (!OrderPriceOpen)
                // https://www.mql5.com/ru/forum/1111/page2820#comment_17749873
                OrderPriceOpen = (double)::StringSubstr(MT4ORDERS::Order.Comment, MT4ORDERS::IsTester ? 3 : (::StringFind(MT4ORDERS::Order.Comment, "tp ") + 3));

              MT4ORDERS::Order.TakeProfit = OrderPriceOpen;
              MT4ORDERS::Order.StopLoss = ::HistoryOrderGetDouble(OrderTicket, ORDER_TP);
            }
            else if (PartComment == "[sl")
            {
              MT4ORDERS::Order.CloseReason = DEAL_REASON_SL;

              if (!OrderPriceOpen)
                // https://www.mql5.com/ru/forum/1111/page2820#comment_17749873
                OrderPriceOpen = (double)::StringSubstr(MT4ORDERS::Order.Comment, MT4ORDERS::IsTester ? 3 : (::StringFind(MT4ORDERS::Order.Comment, "sl ") + 3));

              MT4ORDERS::Order.StopLoss = OrderPriceOpen;
              MT4ORDERS::Order.TakeProfit = ::HistoryOrderGetDouble(OrderTicket, ORDER_SL);
            }
            else
            {
              // Reversed - not an error: see OrderClose.
              MT4ORDERS::Order.StopLoss = ::HistoryOrderGetDouble(OrderTicket, ORDER_TP);
              MT4ORDERS::Order.TakeProfit = ::HistoryOrderGetDouble(OrderTicket, ORDER_SL);
            }
          }
          else
          {
            // Reversed - not an error: see OrderClose.
            MT4ORDERS::Order.StopLoss = ::HistoryOrderGetDouble(OrderTicket, ORDER_TP);
            MT4ORDERS::Order.TakeProfit = ::HistoryOrderGetDouble(OrderTicket, ORDER_SL);
          }
      #endif // MT4ORDERS_SLTP_OLD

          MT4ORDERS::Order.State = (ENUM_ORDER_STATE)::HistoryOrderGetInteger(OrderTicket, ORDER_STATE);

          if (!(MT4ORDERS::Order.ClosePriceRequest = (DealEntry == DEAL_ENTRY_OUT_BY) ? MT4ORDERS::Order.ClosePrice : OrderPriceOpen))
            MT4ORDERS::Order.ClosePriceRequest = MT4ORDERS::Order.ClosePrice;

          if (!(MT4ORDERS::Order.OpenPriceRequest = _B2(MT4ORDERS::HistorySelectOrder(PosTicket) &&
                                                    // During partial execution, only the last deal of a fully executed order has this condition for taking the request price.
                                                    (MT4ORDERS::IsTester || (::HistoryDealGetInteger(OpenTicket, DEAL_TIME_MSC) == ::HistoryOrderGetInteger(PosTicket, ORDER_TIME_DONE_MSC)))) ?
                                                   ::HistoryOrderGetDouble(PosTicket, ORDER_PRICE_OPEN) : MT4ORDERS::Order.OpenPrice))
            MT4ORDERS::Order.OpenPriceRequest = MT4ORDERS::Order.OpenPrice;
        }
        else
        {
          MT4ORDERS::Order.State = ORDER_STATE_FILLED;

          MT4ORDERS::Order.ClosePriceRequest = MT4ORDERS::Order.ClosePrice;
          MT4ORDERS::Order.OpenPriceRequest = MT4ORDERS::Order.OpenPrice;
        }

        // The comment above is used to find SL/TP.
        if (StrComment != "")
          MT4ORDERS::Order.Comment = StrComment;
      }

      if (!Res)
      {
        ::Alert("HistoryOrderSelect(" + (string)OrderTicket + ") - BUG! MT4ORDERS - not Sync with History!");
        MT4ORDERS::AlertLog();

        ::Print(__FILE__ + "\nVersion = " + __MT4ORDERS__ + "\nCompiler = " + (string)__MQLBUILD__ + "\n" + TOSTR(__DATE__) +
                TOSTR(::AccountInfoString(ACCOUNT_SERVER)) + TOSTR2((ENUM_ACCOUNT_TRADE_MODE)::AccountInfoInteger(ACCOUNT_TRADE_MODE)) +
                TOSTR((bool)::TerminalInfoInteger(TERMINAL_CONNECTED)) +
                TOSTR(::TerminalInfoInteger(TERMINAL_PING_LAST)) + TOSTR(::TerminalInfoDouble(TERMINAL_RETRANSMISSION)) +
                TOSTR(::TerminalInfoInteger(TERMINAL_BUILD)) + TOSTR((bool)::TerminalInfoInteger(TERMINAL_X64)) +
                TOSTR((bool)::TerminalInfoInteger(TERMINAL_VPS)) + TOSTR2((ENUM_PROGRAM_TYPE)::MQLInfoInteger(MQL_PROGRAM_TYPE)) +
                TOSTR(::TimeCurrent()) + TOSTR(::TimeTradeServer()) + TOSTR(MT4ORDERS::TimeToString(MT4ORDERS::GetTimeCurrent())) +
                TOSTR(::SymbolInfoString(MT4ORDERS::Order.Symbol, SYMBOL_PATH)) + TOSTR(::SymbolInfoString(MT4ORDERS::Order.Symbol, SYMBOL_DESCRIPTION)) +
                "CurrentTick =" + MT4ORDERS::TickToString(MT4ORDERS::Order.Symbol) + "\n" +
                TOSTR(::PositionsTotal()) + TOSTR(::OrdersTotal()) +
                TOSTR(::HistorySelect(0, INT_MAX)) + TOSTR(::HistoryDealsTotal()) + TOSTR(::HistoryOrdersTotal()) +
                TOSTR(::TerminalInfoInteger(TERMINAL_MEMORY_AVAILABLE)) + TOSTR(::TerminalInfoInteger(TERMINAL_MEMORY_PHYSICAL)) +
                TOSTR(::TerminalInfoInteger(TERMINAL_MEMORY_TOTAL)) + TOSTR(::TerminalInfoInteger(TERMINAL_MEMORY_USED)) +
                TOSTR(::MQLInfoInteger(MQL_MEMORY_LIMIT)) + TOSTR(::MQLInfoInteger(MQL_MEMORY_USED)) +
                TOSTR(Ticket) + TOSTR(OrderTicket) + TOSTR(OpenTicket) + TOSTR(PosTicket) +
                TOSTR(MT4ORDERS::TimeToString(MT4ORDERS::Order.CloseTimeMsc)) +
                TOSTR(MT4ORDERS::HistorySelectOrder(OrderTicket)) + TOSTR(::OrderSelect(OrderTicket)) +
                (::OrderSelect(OrderTicket) ? TOSTR2((ENUM_ORDER_STATE)::OrderGetInteger(ORDER_STATE)) : NULL) +
                (::HistoryDealsTotal() ? TOSTR(::HistoryDealGetTicket(::HistoryDealsTotal() - 1)) +
                   "DEAL_ORDER = " + (string)::HistoryDealGetInteger(::HistoryDealGetTicket(::HistoryDealsTotal() - 1), DEAL_ORDER) + "\n"
                   "DEAL_TIME_MSC = " + MT4ORDERS::TimeToString(::HistoryDealGetInteger(::HistoryDealGetTicket(::HistoryDealsTotal() - 1), DEAL_TIME_MSC)) + "\n"
                                       : NULL) +
                (::HistoryOrdersTotal() ? TOSTR(::HistoryOrderGetTicket(::HistoryOrdersTotal() - 1)) +
                   "ORDER_TIME_DONE_MSC = " + MT4ORDERS::TimeToString(::HistoryOrderGetInteger(::HistoryOrderGetTicket(::HistoryOrdersTotal() - 1), ORDER_TIME_DONE_MSC)) + "\n"
                                        : NULL) +
              #ifdef MT4ORDERS_BYPASS_MAXTIME
                "MT4ORDERS::ByPass: " + MT4ORDERS::ByPass.ToString() + "\n" +
              #endif // #ifdef MT4ORDERS_BYPASS_MAXTIME
                TOSTR(MT4ORDERS::OrderSend_MaxPause));
      }
    }
    else
    {
      MT4ORDERS::Order.TicketOpen = MT4ORDERS::Order.Ticket;

      if (!MT4ORDERS::Order.TicketID && (MT4ORDERS::Order.Type <= OP_SELL)) // ID of balance deals must remain zero.
        MT4ORDERS::Order.TicketID = MT4ORDERS::Order.Ticket;

      MT4ORDERS::Order.OpenPrice = MT4ORDERS::Order.ClosePrice; // ::HistoryDealGetDouble(Ticket, DEAL_PRICE);

      MT4ORDERS::Order.OpenTimeMsc = MT4ORDERS::Order.CloseTimeMsc;
      MT4ORDERS::Order.OpenTime = MT4ORDERS::Order.CloseTime;   // (datetime)::HistoryDealGetInteger(Ticket, DEAL_TIME);

      MT4ORDERS::Order.OpenReason = MT4ORDERS::Order.CloseReason;

      MT4ORDERS::Order.State = ORDER_STATE_FILLED;

      MT4ORDERS::Order.ClosePriceRequest = MT4ORDERS::Order.ClosePrice;
      MT4ORDERS::Order.OpenPriceRequest = MT4ORDERS::Order.OpenPrice;
    }

    if (OrderTicket)
    {
      bool Res = MT4ORDERS::IsTester ? MT4ORDERS::HistorySelectOrder(OrderTicket) : MT4ORDERS::Waiting(true);

      if (!Res)
        WHILE(_B2(MT4ORDERS::HistorySelectOrder(OrderTicket))) // https://www.mql5.com/ru/forum/304239#comment_10710403
          ;

      if ((ENUM_ORDER_TYPE)::HistoryOrderGetInteger(OrderTicket, ORDER_TYPE) == ORDER_TYPE_CLOSE_BY)
      {
        const ulong PosTicketBy = ::HistoryOrderGetInteger(OrderTicket, ORDER_POSITION_BY_ID);

        if (PosTicketBy == PosTicket) // CloseBy-Slave should not affect the total trade.
        {
          MT4ORDERS::Order.Lots = 0;
          MT4ORDERS::Order.Commission = 0;

          MT4ORDERS::Order.ClosePrice = MT4ORDERS::Order.OpenPrice;
          MT4ORDERS::Order.ClosePriceRequest = MT4ORDERS::Order.ClosePrice;
        }
        else // CloseBy-Master must receive a commission from CloseBy-Slave.
        {
          const ulong OpenTicketBy = (OrderTicket > 0) ? _B2(MT4ORDERS::History.GetPositionDealIn(PosTicketBy)) : 0;

          if ((OpenTicketBy > 0) && _B2(MT4ORDERS::HistorySelectDeal(OpenTicketBy)))
          {
            const double OpenLots = ::HistoryDealGetDouble(OpenTicketBy, DEAL_VOLUME);

            if (OpenLots > 0)
              MT4ORDERS::Order.Commission += ::HistoryDealGetDouble(OpenTicketBy, DEAL_COMMISSION) * MT4ORDERS::Order.Lots / OpenLots;
          }
        }
      }
    }

    return;
  }

  static bool Waiting( const bool FlagInit = false )
  {
    static ulong StartTime = 0;

    const bool Res = FlagInit ? false : (::GetMicrosecondCount() - StartTime < MT4ORDERS::OrderSend_MaxPause);

    if (FlagInit)
    {
      StartTime = ::GetMicrosecondCount();

      MT4ORDERS::OrderSendBug = 0;
    }
    else if (Res)
    {
//      ::Sleep(0); // https://www.mql5.com/ru/forum/170952/page100#comment_8750511

      MT4ORDERS::OrderSendBug++;
    }

    return(Res);
  }

  static bool EqualPrices( const double Price1, const double &Price2, const int &digits)
  {
    return(!::NormalizeDouble(Price1 - Price2, digits));
  }

  static bool HistoryDealSelect2( MqlTradeResult &Result ) // At the end of the name there is a digit for greater compatibility with macros.
  {
  #ifdef MT4ORDERS_HISTORY_OLD
    // Replace HistorySelectByPosition with HistorySelect(PosTime, PosTime)
    if (!Result.deal && Result.order && _B3(::HistorySelectByPosition(::HistoryOrderGetInteger(Result.order, ORDER_POSITION_ID))))
    {
  #else // #ifdef MT4ORDERS_HISTORY_OLD
    if (!Result.deal && Result.order && _B2(MT4ORDERS::HistorySelectOrder(Result.order)))
    {
      const long OrderTimeFill = ::HistoryOrderGetInteger(Result.order, ORDER_TIME_DONE_MSC);
  #endif // #ifdef MT4ORDERS_HISTORY_OLD #else
      if (::HistorySelect(0, INT_MAX)) // Without it, the deal detection may fail.
        for (int i = ::HistoryDealsTotal() - 1; i >= 0; i--)
        {
          const ulong DealTicket = ::HistoryDealGetTicket(i);

          if (Result.order == ::HistoryDealGetInteger(DealTicket, DEAL_ORDER))
          {
            Result.deal = DealTicket;
            Result.price = ::HistoryDealGetDouble(DealTicket, DEAL_PRICE);

            break;
          }
        #ifndef MT4ORDERS_HISTORY_OLD
          else if (::HistoryDealGetInteger(DealTicket, DEAL_TIME_MSC) < OrderTimeFill)
            break;
        #endif // #ifndef MT4ORDERS_HISTORY_OLD
        }
    }

    return(_B2(MT4ORDERS::HistorySelectDeal(Result.deal)));
  }

/*
#define MT4ORDERS_BENCHMARK Alert(MT4ORDERS::LastTradeRequest.symbol + " " +       \
                                  (string)MT4ORDERS::LastTradeResult.order + " " + \
                                  MT4ORDERS::LastTradeResult.comment);             \
                            Print(ToString(MT4ORDERS::LastTradeRequest) +          \
                                  ToString(MT4ORDERS::LastTradeResult));
*/

#define TMP_MT4ORDERS_BENCHMARK(A) \
  static ulong Max##A = 0;         \
                                   \
  if (Interval##A > Max##A)        \
  {                                \
    MT4ORDERS_BENCHMARK            \
                                   \
    Max##A = Interval##A;          \
  }

  static void OrderSend_Benchmark( const ulong &Interval1, const ulong &Interval2 )
  {
    #ifdef MT4ORDERS_BENCHMARK
      TMP_MT4ORDERS_BENCHMARK(1)
      TMP_MT4ORDERS_BENCHMARK(2)
    #endif // MT4ORDERS_BENCHMARK

    return;
  }

#undef TMP_MT4ORDERS_BENCHMARK

  static string ToString( const MqlTradeRequest &Request )
  {
    return(TOSTR2(Request.action) + TOSTR(Request.magic) + TOSTR(Request.order) +
           TOSTR(Request.symbol) + TOSTR(Request.volume) + TOSTR(Request.price) +
           TOSTR(Request.stoplimit) + TOSTR(Request.sl) +  TOSTR(Request.tp) +
           TOSTR(Request.deviation) + TOSTR2(Request.type) + TOSTR2(Request.type_filling) +
           TOSTR2(Request.type_time) + TOSTR(Request.expiration) + TOSTR(Request.comment) +
           TOSTR(Request.position) + TOSTR(Request.position_by));
  }

  static string ToString( const MqlTradeResult &Result )
  {
    return(TOSTR(Result.retcode) + TOSTR(Result.deal) + TOSTR(Result.order) +
           TOSTR(Result.volume) + TOSTR(Result.price) + TOSTR(Result.bid) +
           TOSTR(Result.ask) + TOSTR(Result.comment) + TOSTR(Result.request_id) +
           TOSTR(Result.retcode_external));
  }

  static bool OrderSend( const MqlTradeRequest &Request, MqlTradeResult &Result )
  {
    const bool FlagCalc = !MT4ORDERS::IsTester && MT4ORDERS::OrderSend_MaxPause;

    MqlTick PrevTick = {};

    if (FlagCalc)
      ::SymbolInfoTick(Request.symbol, PrevTick); // May slow down.

    const long PrevTimeCurrent = FlagCalc ? _B2(MT4ORDERS::GetTimeCurrent()) : 0;
    const ulong StartTime1 = FlagCalc ? ::GetMicrosecondCount() : 0;

    bool Res = ::OrderSend(Request, Result);

    const ulong StartTime2 = FlagCalc ? ::GetMicrosecondCount() : 0;

    const ulong Interval1 = StartTime2 - StartTime1;

    if (FlagCalc && Res && (Result.retcode < TRADE_RETCODE_ERROR))
    {
      Res = (Result.retcode == TRADE_RETCODE_DONE);
      MT4ORDERS::Waiting(true);

      // TRADE_ACTION_CLOSE_BY is not present in the list of checks
      if (Request.action == TRADE_ACTION_DEAL)
      {
        if (!Result.deal)
        {
          WHILE(_B2(::OrderSelect(Result.order)) || _B2(MT4ORDERS::HistorySelectOrder(Result.order)))
            ;

          if (!Res)
            ::Print("Line = " + (string)__LINE__ + "\n" + TOSTR(::OrderSelect(Result.order)) + TOSTR(MT4ORDERS::HistorySelectOrder(Result.order)));
          else if (::OrderSelect(Result.order) && !(Res = ((ENUM_ORDER_STATE)::OrderGetInteger(ORDER_STATE) == ORDER_STATE_PLACED) ||
                                                          ((ENUM_ORDER_STATE)::OrderGetInteger(ORDER_STATE) == ORDER_STATE_PARTIAL)))
            ::Print("Line = " + (string)__LINE__ + "\n" + TOSTR(::OrderSelect(Result.order)) + TOSTR2((ENUM_ORDER_STATE)::OrderGetInteger(ORDER_STATE)));
        }

        // If the remaining part is still hanging after the partial execution, false.
        if (Res)
        {
          const bool ResultDeal = (!Result.deal) && (!MT4ORDERS::OrderSendBug);

          if (MT4ORDERS::OrderSendBug && (!Result.deal))
            ::Print("Line = " + (string)__LINE__ + "\n" + "Before ::HistoryOrderSelect(Result.order):\n" + TOSTR(MT4ORDERS::OrderSendBug) + TOSTR(Result.deal));

          WHILE(_B2(MT4ORDERS::HistorySelectOrder(Result.order)))
            ;

          // If there was no OrderSend bug and there was Result.deal == 0
          if (ResultDeal)
            MT4ORDERS::OrderSendBug = 0;

          if (!Res)
            ::Print("Line = " + (string)__LINE__ + "\n" + TOSTR(MT4ORDERS::HistorySelectOrder(Result.order)) +
                    TOSTR(MT4ORDERS::HistorySelectDeal(Result.deal)) + TOSTR(::OrderSelect(Result.order)) + TOSTR(Result.deal));
          // If the historical order was not executed (due to rejection), false
          else if (!(Res = ((ENUM_ORDER_STATE)::HistoryOrderGetInteger(Result.order, ORDER_STATE) == ORDER_STATE_FILLED) ||
                           ((ENUM_ORDER_STATE)::HistoryOrderGetInteger(Result.order, ORDER_STATE) == ORDER_STATE_PARTIAL)))
            ::Print("Line = " + (string)__LINE__ + "\n" + TOSTR2((ENUM_ORDER_STATE)::HistoryOrderGetInteger(Result.order, ORDER_STATE)));
        }

        if (Res)
        {
          const bool ResultDeal = (!Result.deal) && (!MT4ORDERS::OrderSendBug);

          if (MT4ORDERS::OrderSendBug && (!Result.deal))
            ::Print("Line = " + (string)__LINE__ + "\n" + "Before MT4ORDERS::HistoryDealSelect(Result):\n" + TOSTR(MT4ORDERS::OrderSendBug) + TOSTR(Result.deal));

          WHILE(MT4ORDERS::HistoryDealSelect2(Result))
            ;

          // If there was no OrderSend bug and there was Result.deal == 0
          if (ResultDeal)
            MT4ORDERS::OrderSendBug = 0;

          if (!Res)
            ::Print("Line = " + (string)__LINE__ + "\n" + TOSTR(MT4ORDERS::HistoryDealSelect2(Result)));
        }
      }
      else if (Request.action == TRADE_ACTION_PENDING)
      {
        if (Res)
        {
          WHILE(_B2(::OrderSelect(Result.order)))
            ;

          if (!Res)
            ::Print("Line = " + (string)__LINE__ + "\n" + TOSTR(::OrderSelect(Result.order)));
          else if (!(Res = ((ENUM_ORDER_STATE)::OrderGetInteger(ORDER_STATE) == ORDER_STATE_PLACED) ||
                           ((ENUM_ORDER_STATE)::OrderGetInteger(ORDER_STATE) == ORDER_STATE_PARTIAL)))
            ::Print("Line = " + (string)__LINE__ + "\n" + TOSTR2((ENUM_ORDER_STATE)::OrderGetInteger(ORDER_STATE)));
        }
        else
        {
          WHILE(_B2(MT4ORDERS::HistorySelectOrder(Result.order)))
            ;

          ::Print("Line = " + (string)__LINE__ + "\n" + TOSTR(MT4ORDERS::HistorySelectOrder(Result.order)));

          Res = false;
        }
      }
      else if (Request.action == TRADE_ACTION_SLTP)
      {
        if (Res)
        {
          const int digits = (int)::SymbolInfoInteger(Request.symbol, SYMBOL_DIGITS);

          bool EqualSL = false;
          bool EqualTP = false;

          do
            if (Request.position ? _B2(::PositionSelectByTicket(Request.position)) : _B2(::PositionSelect(Request.symbol)))
            {
              EqualSL = MT4ORDERS::EqualPrices(::PositionGetDouble(POSITION_SL), Request.sl, digits);
              EqualTP = MT4ORDERS::EqualPrices(::PositionGetDouble(POSITION_TP), Request.tp, digits);
            }
          WHILE(EqualSL && EqualTP);

          if (!Res)
            ::Print("Line = " + (string)__LINE__ + "\n" + TOSTR(::PositionGetDouble(POSITION_SL)) + TOSTR(::PositionGetDouble(POSITION_TP)) +
                    TOSTR(EqualSL) + TOSTR(EqualTP) +
                    TOSTR(Request.position ? ::PositionSelectByTicket(Request.position) : ::PositionSelect(Request.symbol)));
        }
      }
      else if (Request.action == TRADE_ACTION_MODIFY)
      {
        if (Res)
        {
          const int digits = (int)::SymbolInfoInteger(Request.symbol, SYMBOL_DIGITS);

          bool EqualSL = false;
          bool EqualTP = false;
          bool EqualPrice = false;

          do
            // https://www.mql5.com/ru/forum/170952/page184#comment_17913645
            if (_B2(::OrderSelect(Result.order)) && ((ENUM_ORDER_STATE)::OrderGetInteger(ORDER_STATE) != ORDER_STATE_REQUEST_MODIFY))
            {
              EqualSL = MT4ORDERS::EqualPrices(::OrderGetDouble(ORDER_SL), Request.sl, digits);
              EqualTP = MT4ORDERS::EqualPrices(::OrderGetDouble(ORDER_TP), Request.tp, digits);
              EqualPrice = MT4ORDERS::EqualPrices(::OrderGetDouble(ORDER_PRICE_OPEN), Request.price, digits);
            }
          WHILE((EqualSL && EqualTP && EqualPrice));

          if (!Res)
            ::Print("Line = " + (string)__LINE__ + "\n" + TOSTR(::OrderGetDouble(ORDER_SL)) + TOSTR(Request.sl)+
                    TOSTR(::OrderGetDouble(ORDER_TP)) + TOSTR(Request.tp) +
                    TOSTR(::OrderGetDouble(ORDER_PRICE_OPEN)) + TOSTR(Request.price) +
                    TOSTR(EqualSL) + TOSTR(EqualTP) + TOSTR(EqualPrice) +
                    TOSTR(::OrderSelect(Result.order)) +
                    TOSTR2((ENUM_ORDER_STATE)::OrderGetInteger(ORDER_STATE)));
        }
      }
      else if (Request.action == TRADE_ACTION_REMOVE)
      {
        if (Res)
          WHILE(_B2(MT4ORDERS::HistorySelectOrder(Result.order)))
            ;

        if (!Res)
          ::Print("Line = " + (string)__LINE__ + "\n" + TOSTR(MT4ORDERS::HistorySelectOrder(Result.order)));
      }

      const ulong Interval2 = ::GetMicrosecondCount() - StartTime2;

      Result.comment += " " + ::DoubleToString(Interval1 / 1000.0, 3) + " + " +
                              ::DoubleToString(Interval2 / 1000.0, 3) + " (" + (string)MT4ORDERS::OrderSendBug + ") ms.";

      if (!Res || MT4ORDERS::OrderSendBug)
      {
        ::Alert(Res ? "OrderSend(" + (string)Result.order + ") - BUG!" : "MT4ORDERS - not Sync with History!");
        MT4ORDERS::AlertLog();

        ::Print(__FILE__ + "\nVersion = " + __MT4ORDERS__ + "\nCompiler = " + (string)__MQLBUILD__ + "\n" + TOSTR(__DATE__) +
                TOSTR(::AccountInfoString(ACCOUNT_SERVER)) + TOSTR2((ENUM_ACCOUNT_TRADE_MODE)::AccountInfoInteger(ACCOUNT_TRADE_MODE)) +
                TOSTR((bool)::TerminalInfoInteger(TERMINAL_CONNECTED)) +
                TOSTR(::TerminalInfoInteger(TERMINAL_PING_LAST)) + TOSTR(::TerminalInfoDouble(TERMINAL_RETRANSMISSION)) +
                TOSTR(::TerminalInfoInteger(TERMINAL_BUILD)) + TOSTR((bool)::TerminalInfoInteger(TERMINAL_X64)) +
                TOSTR((bool)::TerminalInfoInteger(TERMINAL_VPS)) + TOSTR2((ENUM_PROGRAM_TYPE)::MQLInfoInteger(MQL_PROGRAM_TYPE)) +
                TOSTR(::TimeCurrent()) + TOSTR(::TimeTradeServer()) +
                TOSTR(MT4ORDERS::TimeToString(MT4ORDERS::GetTimeCurrent())) + TOSTR(MT4ORDERS::TimeToString(PrevTimeCurrent)) +
                "PrevTick =" + MT4ORDERS::TickToString(Request.symbol, PrevTick) + "\n" +
                "CurrentTick =" + MT4ORDERS::TickToString(Request.symbol) + "\n" +
                TOSTR(::SymbolInfoString(Request.symbol, SYMBOL_PATH)) + TOSTR(::SymbolInfoString(Request.symbol, SYMBOL_DESCRIPTION)) +
                TOSTR(::PositionsTotal()) + TOSTR(::OrdersTotal()) +
                TOSTR(::HistorySelect(0, INT_MAX)) + TOSTR(::HistoryDealsTotal()) + TOSTR(::HistoryOrdersTotal()) +
                (::HistoryDealsTotal() ? TOSTR(::HistoryDealGetTicket(::HistoryDealsTotal() - 1)) +
                   "DEAL_ORDER = " + (string)::HistoryDealGetInteger(::HistoryDealGetTicket(::HistoryDealsTotal() - 1), DEAL_ORDER) + "\n"
                   "DEAL_TIME_MSC = " + MT4ORDERS::TimeToString(::HistoryDealGetInteger(::HistoryDealGetTicket(::HistoryDealsTotal() - 1), DEAL_TIME_MSC)) + "\n"
                                       : NULL) +
                (::HistoryOrdersTotal() ? TOSTR(::HistoryOrderGetTicket(::HistoryOrdersTotal() - 1)) +
                   "ORDER_TIME_DONE_MSC = " + MT4ORDERS::TimeToString(::HistoryOrderGetInteger(::HistoryOrderGetTicket(::HistoryOrdersTotal() - 1), ORDER_TIME_DONE_MSC)) + "\n"
                                        : NULL) +
                TOSTR(::TerminalInfoInteger(TERMINAL_MEMORY_AVAILABLE)) + TOSTR(::TerminalInfoInteger(TERMINAL_MEMORY_PHYSICAL)) +
                TOSTR(::TerminalInfoInteger(TERMINAL_MEMORY_TOTAL)) + TOSTR(::TerminalInfoInteger(TERMINAL_MEMORY_USED)) +
                TOSTR(::MQLInfoInteger(MQL_MEMORY_LIMIT)) + TOSTR(::MQLInfoInteger(MQL_MEMORY_USED)) +
                TOSTR(MT4ORDERS::IsHedging) + TOSTR(Res) + TOSTR(MT4ORDERS::OrderSendBug) +
                MT4ORDERS::ToString(Request) + MT4ORDERS::ToString(Result) +
              #ifdef MT4ORDERS_BYPASS_MAXTIME
                "MT4ORDERS::ByPass: " + MT4ORDERS::ByPass.ToString() + "\n" +
              #endif // #ifdef MT4ORDERS_BYPASS_MAXTIME
                TOSTR(MT4ORDERS::OrderSend_MaxPause));
      }
      else
        MT4ORDERS::OrderSend_Benchmark(Interval1, Interval2);
    }
    else if (FlagCalc)
    {
      Result.comment += " " + ::DoubleToString(Interval1 / 1000.0, 3) + " ms";

      ::Print(TOSTR(::TimeCurrent()) + TOSTR(::TimeTradeServer()) + TOSTR(MT4ORDERS::TimeToString(PrevTimeCurrent)) +
              MT4ORDERS::TickToString(Request.symbol, PrevTick) + "\n" + MT4ORDERS::TickToString(Request.symbol) + "\n" +
              MT4ORDERS::ToString(Request) + MT4ORDERS::ToString(Result));

//      ExpertRemove();
    }

    return(Res);
  }

#undef TOSTR2
#undef TOSTR
#undef WHILE

  static ENUM_DAY_OF_WEEK GetDayOfWeek( const datetime &time )
  {
    MqlDateTime sTime = {};

    ::TimeToStruct(time, sTime);

    return((ENUM_DAY_OF_WEEK)sTime.day_of_week);
  }

  static bool SessionTrade( const string &Symb )
  {
    datetime TimeNow = ::TimeCurrent();

    const ENUM_DAY_OF_WEEK DayOfWeek = MT4ORDERS::GetDayOfWeek(TimeNow);

    TimeNow %= 24 * 60 * 60;

    bool Res = false;
    datetime From, To;

    for (int i = 0; (!Res) && ::SymbolInfoSessionTrade(Symb, DayOfWeek, i, From, To); i++)
      Res = ((From <= TimeNow) && (TimeNow < To));

    return(Res);
  }

  static bool SymbolTrade( const string &Symb )
  {
    MqlTick Tick;

    return(::SymbolInfoTick(Symb, Tick) ? (Tick.bid && Tick.ask && MT4ORDERS::SessionTrade(Symb) /* &&
           ((ENUM_SYMBOL_TRADE_MODE)::SymbolInfoInteger(Symb, SYMBOL_TRADE_MODE) == SYMBOL_TRADE_MODE_FULL) */) : false);
  }

  static bool CorrectResult( void )
  {
    ::ZeroMemory(MT4ORDERS::LastTradeResult);

    MT4ORDERS::LastTradeResult.retcode = MT4ORDERS::LastTradeCheckResult.retcode;
    MT4ORDERS::LastTradeResult.comment = MT4ORDERS::LastTradeCheckResult.comment;

    return(false);
  }

  static bool NewOrderCheck( void )
  {
    return((::OrderCheck(MT4ORDERS::LastTradeRequest, MT4ORDERS::LastTradeCheckResult) &&
           (MT4ORDERS::IsTester || MT4ORDERS::SymbolTrade(MT4ORDERS::LastTradeRequest.symbol))) ||
           (!MT4ORDERS::IsTester && MT4ORDERS::CorrectResult()));
  }

  static bool NewOrderSend( const int &Check )
  {
    return((Check == INT_MAX) ? MT4ORDERS::NewOrderCheck() :
           (((Check != INT_MIN) || MT4ORDERS::NewOrderCheck()) && MT4ORDERS::OrderSend(MT4ORDERS::LastTradeRequest, MT4ORDERS::LastTradeResult)
              ? (MT4ORDERS::LastTradeResult.retcode < TRADE_RETCODE_ERROR)
              #ifdef MT4ORDERS_BYPASS_MAXTIME
                && _B2(MT4ORDERS::ByPass += MT4ORDERS::LastTradeResult.order)
              #endif // #ifdef MT4ORDERS_BYPASS_MAXTIME
              : false));
  }

  static bool ModifyPosition( const long &Ticket, MqlTradeRequest &Request )
  {
    const bool Res = _B2(::PositionSelectByTicket(Ticket));

    if (Res)
    {
      Request.action = TRADE_ACTION_SLTP;

      Request.position = Ticket;
      Request.symbol = ::PositionGetString(POSITION_SYMBOL); // specifying a single ticket is not sufficient!
    }

    return(Res);
  }

  static ENUM_ORDER_TYPE_FILLING GetFilling( const string &Symb, const uint Type = ORDER_FILLING_FOK )
  {
    static ENUM_ORDER_TYPE_FILLING Res = ORDER_FILLING_FOK;
    static string LastSymb = NULL;
    static uint LastType = ORDER_FILLING_FOK;

    const bool SymbFlag = (LastSymb != Symb);

    if (SymbFlag || (LastType != Type)) // It can be lightly accelerated by changing the sequence of checking the condition.
    {
      LastType = Type;

      if (SymbFlag)
        LastSymb = Symb;

      const ENUM_SYMBOL_TRADE_EXECUTION ExeMode = (ENUM_SYMBOL_TRADE_EXECUTION)::SymbolInfoInteger(Symb, SYMBOL_TRADE_EXEMODE);
      const int FillingMode = (int)::SymbolInfoInteger(Symb, SYMBOL_FILLING_MODE);

      Res = (!FillingMode || (Type >= ORDER_FILLING_RETURN) || ((FillingMode & (Type + 1)) != Type + 1)) ?
            (((ExeMode == SYMBOL_TRADE_EXECUTION_EXCHANGE) || (ExeMode == SYMBOL_TRADE_EXECUTION_INSTANT)) ?
             ORDER_FILLING_RETURN : ((FillingMode == SYMBOL_FILLING_IOC) ? ORDER_FILLING_IOC : ORDER_FILLING_FOK)) :
            (ENUM_ORDER_TYPE_FILLING)Type;
    }

    return(Res);
  }

  static ENUM_ORDER_TYPE_TIME GetExpirationType( const string &Symb, uint Expiration = ORDER_TIME_GTC )
  {
    static ENUM_ORDER_TYPE_TIME Res = ORDER_TIME_GTC;
    static string LastSymb = NULL;
    static uint LastExpiration = ORDER_TIME_GTC;

    const bool SymbFlag = (LastSymb != Symb);

    if ((LastExpiration != Expiration) || SymbFlag)
    {
      LastExpiration = Expiration;

      if (SymbFlag)
        LastSymb = Symb;

      const int ExpirationMode = (int)::SymbolInfoInteger(Symb, SYMBOL_EXPIRATION_MODE);

      if ((Expiration > ORDER_TIME_SPECIFIED_DAY) || (!((ExpirationMode >> Expiration) & 1)))
      {
        if ((Expiration < ORDER_TIME_SPECIFIED) || (ExpirationMode < SYMBOL_EXPIRATION_SPECIFIED))
          Expiration = ORDER_TIME_GTC;
        else if (Expiration > ORDER_TIME_DAY)
          Expiration = ORDER_TIME_SPECIFIED;

        uint i = 1 << Expiration;

        while ((Expiration <= ORDER_TIME_SPECIFIED_DAY) && ((ExpirationMode & i) != i))
        {
          i <<= 1;
          Expiration++;
        }
      }

      Res = (ENUM_ORDER_TYPE_TIME)Expiration;
    }

    return(Res);
  }

  static bool ModifyOrder( const long Ticket, const double &Price, const datetime &Expiration, MqlTradeRequest &Request )
  {
    const bool Res = _B2(::OrderSelect(Ticket));

    if (Res)
    {
      Request.action = TRADE_ACTION_MODIFY;
      Request.order = Ticket;

      Request.price = Price;

      Request.symbol = ::OrderGetString(ORDER_SYMBOL);

      // https://www.mql5.com/ru/forum/1111/page1817#comment_4087275
//      Request.type_filling = (ENUM_ORDER_TYPE_FILLING)::OrderGetInteger(ORDER_TYPE_FILLING);
      Request.type_filling = _B2(MT4ORDERS::GetFilling(Request.symbol));
      Request.type_time = _B2(MT4ORDERS::GetExpirationType(Request.symbol, (uint)Expiration));

      if (Expiration > ORDER_TIME_DAY)
        Request.expiration = Expiration;
    }

    return(Res);
  }

  static bool SelectByPosHistory( const int Index )
  {
    const long Ticket = MT4ORDERS::History[Index];
    const bool Res = (Ticket > 0) ? _B2(MT4ORDERS::HistorySelectDeal(Ticket)) : ((Ticket < 0) && _B2(MT4ORDERS::HistorySelectOrder(-Ticket)));

    if (Res)
    {
      if (Ticket > 0)
        _BV2(MT4ORDERS::GetHistoryPositionData(Ticket))
      else
        _BV2(MT4ORDERS::GetHistoryOrderData(-Ticket))
    }

    return(Res);
  }

  // https://www.mql5.com/ru/forum/227960#comment_6603506
  static bool OrderVisible( void )
  {
    // if the position has closed while there is still a live partially filled pending order from which the position originated,
    // and the remaining part of the pending order was fully filled then but has not disappeared yet,
    // then we will see both the new position (correct) and the non-disappeared pending order (wrong).

    const ulong PositionID = ::OrderGetInteger(ORDER_POSITION_ID);
    const ENUM_ORDER_TYPE Type = (ENUM_ORDER_TYPE)::OrderGetInteger(ORDER_TYPE);
    ulong Ticket = 0;

    return(!((Type == ORDER_TYPE_CLOSE_BY) ||
             (PositionID && // Partial pending order has a non-zero PositionID.
              (Type <= ORDER_TYPE_SELL) && // Ignore closing market orders 
              ((Ticket = ::OrderGetInteger(ORDER_TICKET)) != PositionID))) && // Do not ignor partially filled market orders.
           // Opening/position increasing order may not have time to disappear.
           (!::PositionsTotal() || !(::PositionSelectByTicket(Ticket ? Ticket : ::OrderGetInteger(ORDER_TICKET)) &&
//                                     (::PositionGetInteger(POSITION_TYPE) == (::OrderGetInteger(ORDER_TYPE) & 1)) &&
//                                     (::PositionGetInteger(POSITION_TIME_MSC) >= ::OrderGetInteger(ORDER_TIME_SETUP_MSC)) &&
                                     (::PositionGetDouble(POSITION_VOLUME) == ::OrderGetDouble(ORDER_VOLUME_INITIAL)))));
  }

  static ulong OrderGetTicket( const int Index )
  {
    ulong Res;
    int PrevTotal;
    const long PrevTicket = ::OrderGetInteger(ORDER_TICKET);
    const long PositionTicket = ::PositionGetInteger(POSITION_TICKET);

    do
    {
      Res = 0;
      PrevTotal = ::OrdersTotal();

      if ((Index >= 0) && (Index < PrevTotal))
      {
        int Count = 0;

        for (int i = 0; i < PrevTotal; i++)
        {
          const int Total = ::OrdersTotal();

          // Number of orders may change while searching for
          if (Total != PrevTotal)
          {
            PrevTotal = Total;

            Count = 0;
            i = -1;
          }
          else
          {
            const ulong Ticket = ::OrderGetTicket(i);

            if (Ticket && MT4ORDERS::OrderVisible())
            {
              if (Count == Index)
              {
                Res = Ticket;

                break;
              }

              Count++;
            }
          }
        }
      }
    } while (
/*           #ifdef MT4ORDERS_BYPASS_MAXTIME
             _B2(MT4ORDERS::ByPass.Waiting()) &&
           #endif // #ifdef MT4ORDERS_BYPASS_MAXTIME */
             (PrevTotal != ::OrdersTotal())); // Number of orders may change while searching

    // In case of a failure, select the order that have been chosen earlier.
    if (!Res && PrevTicket && (::OrderGetInteger(ORDER_TICKET) != PrevTicket))
      const bool AntiWarning = _B2(::OrderSelect(PrevTicket));

    // MT4ORDERS::OrderVisible() changes the position selection.
    if (PositionTicket && (::PositionGetInteger(POSITION_TICKET) != PositionTicket))
      ::PositionSelectByTicket(PositionTicket);

    return(Res);
  }

  // With the same tickets, the priority of position selection is higher than order selection
  static bool SelectByPos( const int Index )
  {
    bool Flag = (Index == INT_MAX);
    bool Res = Flag || (Index == INT_MIN);

    if (!Res)
    {
      const int Total = ::PositionsTotal();

      Flag = (Index < Total);
      Res = Flag ? _B2(::PositionGetTicket(Index)) :
                                                   #ifdef MT4ORDERS_SELECTFILTER_OFF
                                                     ::OrderGetTicket(Index - Total);
                                                   #else // MT4ORDERS_SELECTFILTER_OFF
                                                     (MT4ORDERS::IsTester ? ::OrderGetTicket(Index - Total) : _B2(MT4ORDERS::OrderGetTicket(Index - Total)));
                                                   #endif //MT4ORDERS_SELECTFILTER_OFF
    }

    if (Res)
    {
      if (Flag)
        MT4ORDERS::GetPositionData(); // (Index == INT_MAX) - switch to an MT5 position without checking the existence and updating.
      else
        MT4ORDERS::GetOrderData();    // (Index == INT_MIN) - switch to a live MT5 order without checking the existence and updating.
    }

    return(Res);
  }

  static bool SelectByHistoryTicket( const long &Ticket )
  {
    bool Res = false;

    if (!Ticket) // Selection by OrderTicketID (by zero value - balance operations).
    {
      const ulong TicketDealOut = MT4ORDERS::History.GetPositionDealOut(Ticket);

      if (Res = _B2(MT4ORDERS::HistorySelectDeal(TicketDealOut)))
        _BV2(MT4ORDERS::GetHistoryPositionData(TicketDealOut));
    }
    else if (_B2(MT4ORDERS::HistorySelectDeal(Ticket)))
    {
    #ifdef MT4ORDERS_TESTER_SELECT_BY_TICKET
      // In the tester, when searching for a closed position, we must first search by PositionID due to a close numbering of MT5 deals/orders.
      if (MT4ORDERS::IsTester)
      {
        const ulong TicketDealOut = MT4ORDERS::History.GetPositionDealOut(HistoryOrderGetInteger(Ticket, ORDER_POSITION_ID));

        if (Res = _B2(MT4ORDERS::HistorySelectDeal(TicketDealOut)))
          _BV2(MT4ORDERS::GetHistoryPositionData(TicketDealOut));
      }

      if (!Res)
    #endif // #ifdef MT4ORDERS_TESTER_SELECT_BY_TICKET
      {
        if (Res = MT4HISTORY::IsMT4Deal(Ticket))
          _BV2(MT4ORDERS::GetHistoryPositionData(Ticket))
        else// DealIn
        {
          const ulong TicketDealOut = MT4ORDERS::History.GetPositionDealOut(HistoryDealGetInteger(Ticket, DEAL_POSITION_ID)); // Select by DealIn

          if (Res = _B2(MT4ORDERS::HistorySelectDeal(TicketDealOut)))
            _BV2(MT4ORDERS::GetHistoryPositionData(TicketDealOut))
        }
      }
    }
    else if (_B2(MT4ORDERS::HistorySelectOrder(Ticket)))
    {
      if (Res = MT4HISTORY::IsMT4Order(Ticket))
        _BV2(MT4ORDERS::GetHistoryOrderData(Ticket))
      else
      {
        const ulong TicketDealOut = MT4ORDERS::History.GetPositionDealOut(HistoryOrderGetInteger(Ticket, ORDER_POSITION_ID));

        if (Res = _B2(MT4ORDERS::HistorySelectDeal(TicketDealOut)))
          _BV2(MT4ORDERS::GetHistoryPositionData(TicketDealOut));
      }
    }
    else
    {
      // Choosing by OrderTicketID or by the ticket of an executed pending order is relevant to Netting.
      const ulong TicketDealOut = MT4ORDERS::History.GetPositionDealOut(Ticket);

      if (Res = _B2(MT4ORDERS::HistorySelectDeal(TicketDealOut)))
        _BV2(MT4ORDERS::GetHistoryPositionData(TicketDealOut));
    }

    return(Res);
  }

  static bool SelectByExistingTicket( const long &Ticket )
  {
    bool Res = true;

    if (Ticket < 0)
    {
      if (_B2(::OrderSelect(-Ticket)))
        MT4ORDERS::GetOrderData();
      else if (_B2(::PositionSelectByTicket(-Ticket)))
        MT4ORDERS::GetPositionData();
      else
        Res = false;
    }
    else if (_B2(::PositionSelectByTicket(Ticket)))
      MT4ORDERS::GetPositionData();
    else if (_B2(::OrderSelect(Ticket)))
      MT4ORDERS::GetOrderData();
    else if (_B2(MT4ORDERS::HistorySelectDeal(Ticket)))
    {
    #ifdef MT4ORDERS_TESTER_SELECT_BY_TICKET
      // In the tester, when searching for a closed position, we must first search by PositionID due to a close numbering of MT5 deals/orders.
      if (Res = !MT4ORDERS::IsTester)
    #endif // #ifdef MT4ORDERS_TESTER_SELECT_BY_TICKET
      {
        if (MT4HISTORY::IsMT4Deal(Ticket)) // If the choice is made by DealOut.
          _BV2(MT4ORDERS::GetHistoryPositionData(Ticket))
        else if (_B2(::PositionSelectByTicket(::HistoryDealGetInteger(Ticket, DEAL_POSITION_ID)))) // Select by DealIn
          MT4ORDERS::GetPositionData();
        else
          Res = false;
      }
    }
    else if (_B2(MT4ORDERS::HistorySelectOrder(Ticket)) && _B2(::PositionSelectByTicket(::HistoryOrderGetInteger(Ticket, ORDER_POSITION_ID)))) // Select by MT5 order ticket
      MT4ORDERS::GetPositionData();
    else
      Res = false;

    return(Res);
  }

  // With the same ticket, selection priorities are:
  // MODE_TRADES:  existing position > existing order > deal > canceled order
  // MODE_HISTORY: deal > canceled order > existing position > existing order
  static bool SelectByTicket( const long &Ticket, const int &Pool )
  {
    return((Pool == MODE_TRADES) || (Ticket < 0) ?
           (_B2(MT4ORDERS::SelectByExistingTicket(Ticket)) || ((Ticket > 0) && _B2(MT4ORDERS::SelectByHistoryTicket(Ticket)))) :
           (_B2(MT4ORDERS::SelectByHistoryTicket(Ticket)) || _B2(MT4ORDERS::SelectByExistingTicket(Ticket))));
  }

#ifdef MT4ORDERS_SLTP_OLD
  static void CheckPrices( double &MinPrice, double &MaxPrice, const double Min, const double Max )
  {
    if (MinPrice && (MinPrice >= Min))
      MinPrice = 0;

    if (MaxPrice && (MaxPrice <= Max))
      MaxPrice = 0;

    return;
  }
#endif // MT4ORDERS_SLTP_OLD

  static int OrdersTotal( void )
  {
    int Res = 0;
    const long PrevTicket = ::OrderGetInteger(ORDER_TICKET);
    const long PositionTicket = ::PositionGetInteger(POSITION_TICKET);
    int PrevTotal;

    do
    {
      PrevTotal = ::OrdersTotal();

      for (int i = PrevTotal - 1; i >= 0; i--)
      {
        const int Total = ::OrdersTotal();

        // Number of orders may change while searching for
        if (Total != PrevTotal)
        {
          PrevTotal = Total;

          Res = 0;
          i = PrevTotal;
        }
        else if (::OrderGetTicket(i) && MT4ORDERS::OrderVisible())
          Res++;
      }
    } while (PrevTotal != ::OrdersTotal()); // Number of orders may change while searching for

    if (PrevTicket && (::OrderGetInteger(ORDER_TICKET) != PrevTicket))
      const bool AntiWarning = _B2(::OrderSelect(PrevTicket));

    // MT4ORDERS::OrderVisible() changes the position selection.
    if (PositionTicket && (::PositionGetInteger(POSITION_TICKET) != PositionTicket))
      ::PositionSelectByTicket(PositionTicket);

    return(Res);
  }

public:
  static uint OrderSend_MaxPause; // the maximum time for synchronization in microseconds.

#ifdef MT4ORDERS_BYPASS_MAXTIME
  static BYPASS ByPass;
#endif // #ifdef MT4ORDERS_BYPASS_MAXTIME

  static MqlTradeResult LastTradeResult;
  static MqlTradeRequest LastTradeRequest;
  static MqlTradeCheckResult LastTradeCheckResult;

  static bool MT4OrderSelect( const long &Index, const int &Select, const int &Pool )
  {
    return(
         #ifdef MT4ORDERS_BYPASS_MAXTIME
           (MT4ORDERS::IsTester || ((Select == SELECT_BY_POS) && ((Index == INT_MIN) || (Index == INT_MAX))) || _B2(MT4ORDERS::ByPass.Waiting())) &&
         #endif // #ifdef MT4ORDERS_BYPASS_MAXTIME
           ((Select == SELECT_BY_POS) ?
            ((Pool == MODE_TRADES) ? _B2(MT4ORDERS::SelectByPos((int)Index)) : _B2(MT4ORDERS::SelectByPosHistory((int)Index))) :
            _B2(MT4ORDERS::SelectByTicket(Index, Pool))));
  }

  static int MT4OrdersTotal( void )
  {
  #ifdef MT4ORDERS_SELECTFILTER_OFF
    return(::OrdersTotal() + ::PositionsTotal());
  #else // MT4ORDERS_SELECTFILTER_OFF
    int Res;

    if (MT4ORDERS::IsTester)
      return(::OrdersTotal() + ::PositionsTotal());
    else
    {
      int PrevTotal;

    #ifdef MT4ORDERS_BYPASS_MAXTIME
      _B2(MT4ORDERS::ByPass.Waiting());
    #endif // #ifdef MT4ORDERS_BYPASS_MAXTIME

      do
      {
        PrevTotal = ::PositionsTotal();

        Res = _B2(MT4ORDERS::OrdersTotal()) + PrevTotal;

      } while (
/*             #ifdef MT4ORDERS_BYPASS_MAXTIME
               _B2(MT4ORDERS::ByPass.Waiting()) &&
             #endif // #ifdef MT4ORDERS_BYPASS_MAXTIME  */
              (PrevTotal != ::PositionsTotal())); // Only position changes are tracked, since orders are tracked in MT4ORDERS::OrdersTotal()
    }

    return(Res); // https://www.mql5.com/ru/forum/290673#comment_9493241
  #endif //MT4ORDERS_SELECTFILTER_OFF
  }

  // This "overload" also allows using the MT5 version of OrdersTotal
  static int MT4OrdersTotal( const bool )
  {
    return(::OrdersTotal());
  }

  static int MT4OrdersHistoryTotal( void )
  {
    #ifdef MT4ORDERS_BYPASS_MAXTIME
      _B2(MT4ORDERS::ByPass.Waiting());
    #endif // #ifdef MT4ORDERS_BYPASS_MAXTIME

    return(MT4ORDERS::History.GetAmount());
  }

  static long MT4OrderSend( const string &Symb, const int &Type, const double &dVolume, const double &Price, const int &SlipPage, const double &SL, const double &TP,
                            const string &comment, const MAGIC_TYPE &magic, const datetime &dExpiration, const color &arrow_color )

  {
    ::ZeroMemory(MT4ORDERS::LastTradeRequest);

    MT4ORDERS::LastTradeRequest.action = (((Type == OP_BUY) || (Type == OP_SELL)) ? TRADE_ACTION_DEAL : TRADE_ACTION_PENDING);
    MT4ORDERS::LastTradeRequest.magic = magic;

    MT4ORDERS::LastTradeRequest.symbol = ((Symb == NULL) ? ::Symbol() : Symb);
    MT4ORDERS::LastTradeRequest.volume = dVolume;
    MT4ORDERS::LastTradeRequest.price = Price;

    MT4ORDERS::LastTradeRequest.tp = TP;
    MT4ORDERS::LastTradeRequest.sl = SL;
    MT4ORDERS::LastTradeRequest.deviation = SlipPage;
    MT4ORDERS::LastTradeRequest.type = (ENUM_ORDER_TYPE)Type;

    MT4ORDERS::LastTradeRequest.type_filling = _B2(MT4ORDERS::GetFilling(MT4ORDERS::LastTradeRequest.symbol, (uint)MT4ORDERS::LastTradeRequest.deviation));

    if (MT4ORDERS::LastTradeRequest.action == TRADE_ACTION_PENDING)
    {
      MT4ORDERS::LastTradeRequest.type_time = _B2(MT4ORDERS::GetExpirationType(MT4ORDERS::LastTradeRequest.symbol, (uint)dExpiration));

      if (dExpiration > ORDER_TIME_DAY)
        MT4ORDERS::LastTradeRequest.expiration = dExpiration;
    }

    if (comment != NULL)
      MT4ORDERS::LastTradeRequest.comment = comment;

    return((arrow_color == INT_MAX) ? (MT4ORDERS::NewOrderCheck() ? 0 : -1) :
           ((((int)arrow_color != INT_MIN) || MT4ORDERS::NewOrderCheck()) &&
            MT4ORDERS::OrderSend(MT4ORDERS::LastTradeRequest, MT4ORDERS::LastTradeResult)
          #ifdef MT4ORDERS_BYPASS_MAXTIME
            && (!MT4ORDERS::IsHedging || _B2(MT4ORDERS::ByPass += MT4ORDERS::LastTradeResult.order))
          #endif // #ifdef MT4ORDERS_BYPASS_MAXTIME
                                                                                          ?
            (MT4ORDERS::IsHedging ? (long)MT4ORDERS::LastTradeResult.order : // PositionID == Result.order - a feature of MT5-Hedge
             ((MT4ORDERS::LastTradeRequest.action == TRADE_ACTION_DEAL) ?
              (MT4ORDERS::IsTester ? (_B2(::PositionSelect(MT4ORDERS::LastTradeRequest.symbol)) ? PositionGetInteger(POSITION_TICKET) : 0) :
                                      // HistoryDealSelect в MT4ORDERS::OrderSend
                                      ::HistoryDealGetInteger(MT4ORDERS::LastTradeResult.deal, DEAL_POSITION_ID)) :
              (long)MT4ORDERS::LastTradeResult.order)) : -1));
  }

  static bool MT4OrderModify( const long &Ticket, const double &Price, const double &SL, const double &TP, const datetime &Expiration, const color &Arrow_Color )
  {
    ::ZeroMemory(MT4ORDERS::LastTradeRequest);

               // Considers the case when an order and a position with the same ticket are present
    bool Res = (Ticket < 0) ? MT4ORDERS::ModifyOrder(-Ticket, Price, Expiration, MT4ORDERS::LastTradeRequest) :
               ((MT4ORDERS::Order.Ticket != ORDER_SELECT) ?
                (MT4ORDERS::ModifyPosition(Ticket, MT4ORDERS::LastTradeRequest) || MT4ORDERS::ModifyOrder(Ticket, Price, Expiration, MT4ORDERS::LastTradeRequest)) :
                (MT4ORDERS::ModifyOrder(Ticket, Price, Expiration, MT4ORDERS::LastTradeRequest) || MT4ORDERS::ModifyPosition(Ticket, MT4ORDERS::LastTradeRequest)));

//    if (Res) // Ignore the check - OrderCheck is present
    {
      MT4ORDERS::LastTradeRequest.tp = TP;
      MT4ORDERS::LastTradeRequest.sl = SL;

      Res = MT4ORDERS::NewOrderSend(Arrow_Color);
    }

    return(Res);
  }

  static bool MT4OrderClose( const long &Ticket, const double &dLots, const double &Price, const int &SlipPage, const color &Arrow_Color, const string &comment )
  {
    // MT4ORDERS::LastTradeRequest and MT4ORDERS::LastTradeResult are present, therefore the result is not affected. However, it is necessary for PositionGetString below
    _B2(::PositionSelectByTicket(Ticket));

    ::ZeroMemory(MT4ORDERS::LastTradeRequest);

    MT4ORDERS::LastTradeRequest.action = TRADE_ACTION_DEAL;
    MT4ORDERS::LastTradeRequest.position = Ticket;

    MT4ORDERS::LastTradeRequest.symbol = ::PositionGetString(POSITION_SYMBOL);

    // Save the comment when partially closing the position
//    if (dLots < ::PositionGetDouble(POSITION_VOLUME))
      MT4ORDERS::LastTradeRequest.comment = (comment == NULL) ? ::PositionGetString(POSITION_COMMENT) : comment;

    // Is it correct not to define magic number when closing? -It is!
    MT4ORDERS::LastTradeRequest.volume = dLots;
    MT4ORDERS::LastTradeRequest.price = Price;

  #ifdef MT4ORDERS_SLTP_OLD
    // Needed to determine the SL/TP levels of the closed position. Inverted - not an error
    // SYMBOL_SESSION_PRICE_LIMIT_MIN and SYMBOL_SESSION_PRICE_LIMIT_MAX do not need any validation, since the initial SL/TP have already been set
    MT4ORDERS::LastTradeRequest.tp = ::PositionGetDouble(POSITION_SL);
    MT4ORDERS::LastTradeRequest.sl = ::PositionGetDouble(POSITION_TP);

    if (MT4ORDERS::LastTradeRequest.tp || MT4ORDERS::LastTradeRequest.sl)
    {
      const double StopLevel = ::SymbolInfoInteger(MT4ORDERS::LastTradeRequest.symbol, SYMBOL_TRADE_STOPS_LEVEL) *
                               ::SymbolInfoDouble(MT4ORDERS::LastTradeRequest.symbol, SYMBOL_POINT);

      const bool FlagBuy = (::PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY);
      const double CurrentPrice = SymbolInfoDouble(MT4ORDERS::LastTradeRequest.symbol, FlagBuy ? SYMBOL_ASK : SYMBOL_BID);

      if (CurrentPrice)
      {
        if (FlagBuy)
          MT4ORDERS::CheckPrices(MT4ORDERS::LastTradeRequest.tp, MT4ORDERS::LastTradeRequest.sl, CurrentPrice - StopLevel, CurrentPrice + StopLevel);
        else
          MT4ORDERS::CheckPrices(MT4ORDERS::LastTradeRequest.sl, MT4ORDERS::LastTradeRequest.tp, CurrentPrice - StopLevel, CurrentPrice + StopLevel);
      }
      else
      {
        MT4ORDERS::LastTradeRequest.tp = 0;
        MT4ORDERS::LastTradeRequest.sl = 0;
      }
    }
  #endif // MT4ORDERS_SLTP_OLD

    MT4ORDERS::LastTradeRequest.deviation = SlipPage;

    MT4ORDERS::LastTradeRequest.type = (ENUM_ORDER_TYPE)(1 - ::PositionGetInteger(POSITION_TYPE));

    MT4ORDERS::LastTradeRequest.type_filling = _B2(MT4ORDERS::GetFilling(MT4ORDERS::LastTradeRequest.symbol, (uint)MT4ORDERS::LastTradeRequest.deviation));

    return(MT4ORDERS::NewOrderSend(Arrow_Color));
  }

  static bool MT4OrderCloseBy( const long &Ticket, const long &Opposite, const color &Arrow_Color )
  {
    ::ZeroMemory(MT4ORDERS::LastTradeRequest);

    MT4ORDERS::LastTradeRequest.action = TRADE_ACTION_CLOSE_BY;
    MT4ORDERS::LastTradeRequest.position = Ticket;
    MT4ORDERS::LastTradeRequest.position_by = Opposite;

    if ((!MT4ORDERS::IsTester) && _B2(::PositionSelectByTicket(Ticket))) // needed for MT4ORDERS::SymbolTrade()
      MT4ORDERS::LastTradeRequest.symbol = ::PositionGetString(POSITION_SYMBOL);

    return(MT4ORDERS::NewOrderSend(Arrow_Color));
  }

  static bool MT4OrderDelete( const long &Ticket, const color &Arrow_Color )
  {
//    bool Res = ::OrderSelect(Ticket); // Is it necessary, when MT4ORDERS::LastTradeRequest and MT4ORDERS::LastTradeResult are needed?

    ::ZeroMemory(MT4ORDERS::LastTradeRequest);

    MT4ORDERS::LastTradeRequest.action = TRADE_ACTION_REMOVE;
    MT4ORDERS::LastTradeRequest.order = Ticket;

    if ((!MT4ORDERS::IsTester) && _B2(::OrderSelect(Ticket))) // needed for MT4ORDERS::SymbolTrade()
      MT4ORDERS::LastTradeRequest.symbol = ::OrderGetString(ORDER_SYMBOL);

    return(MT4ORDERS::NewOrderSend(Arrow_Color));
  }

#define MT4_ORDERFUNCTION(NAME,T,A,B,C)                               \
  static T MT4Order##NAME( void )                                     \
  {                                                                   \
    return(POSITION_ORDER((T)(A), (T)(B), MT4ORDERS::Order.NAME, C)); \
  }

#define POSITION_ORDER(A,B,C,D) (((MT4ORDERS::Order.Ticket == POSITION_SELECT) && (D)) ? (A) : ((MT4ORDERS::Order.Ticket == ORDER_SELECT) ? (B) : (C)))

  MT4_ORDERFUNCTION(Ticket, long, ::PositionGetInteger(POSITION_TICKET), ::OrderGetInteger(ORDER_TICKET), true)
  MT4_ORDERFUNCTION(Type, int, ::PositionGetInteger(POSITION_TYPE), ::OrderGetInteger(ORDER_TYPE), true)
  MT4_ORDERFUNCTION(Lots, double, ::PositionGetDouble(POSITION_VOLUME), ::OrderGetDouble(ORDER_VOLUME_CURRENT), true)
  MT4_ORDERFUNCTION(OpenPrice, double, ::PositionGetDouble(POSITION_PRICE_OPEN), (::OrderGetDouble(ORDER_PRICE_OPEN) ? ::OrderGetDouble(ORDER_PRICE_OPEN) : ::OrderGetDouble(ORDER_PRICE_CURRENT)), true)
  MT4_ORDERFUNCTION(OpenTimeMsc, long, ::PositionGetInteger(POSITION_TIME_MSC), ::OrderGetInteger(ORDER_TIME_SETUP_MSC), true)
  MT4_ORDERFUNCTION(OpenTime, datetime, ::PositionGetInteger(POSITION_TIME), ::OrderGetInteger(ORDER_TIME_SETUP), true)
  MT4_ORDERFUNCTION(StopLoss, double, ::PositionGetDouble(POSITION_SL), ::OrderGetDouble(ORDER_SL), true)
  MT4_ORDERFUNCTION(TakeProfit, double, ::PositionGetDouble(POSITION_TP), ::OrderGetDouble(ORDER_TP), true)
  MT4_ORDERFUNCTION(ClosePrice, double, ::PositionGetDouble(POSITION_PRICE_CURRENT), ::OrderGetDouble(ORDER_PRICE_CURRENT), true)
  MT4_ORDERFUNCTION(CloseTimeMsc, long, 0, 0, true)
  MT4_ORDERFUNCTION(CloseTime, datetime, 0, 0, true)
  MT4_ORDERFUNCTION(Expiration, datetime, 0, ::OrderGetInteger(ORDER_TIME_EXPIRATION), true)
  MT4_ORDERFUNCTION(MagicNumber, long, ::PositionGetInteger(POSITION_MAGIC), ::OrderGetInteger(ORDER_MAGIC), true)
  MT4_ORDERFUNCTION(Profit, double, ::PositionGetDouble(POSITION_PROFIT), 0, true)
  MT4_ORDERFUNCTION(Swap, double, ::PositionGetDouble(POSITION_SWAP), 0, true)
  MT4_ORDERFUNCTION(Symbol, string, ::PositionGetString(POSITION_SYMBOL), ::OrderGetString(ORDER_SYMBOL), true)
  MT4_ORDERFUNCTION(Comment, string, MT4ORDERS::Order.Comment, ::OrderGetString(ORDER_COMMENT), MT4ORDERS::CheckPositionCommissionComment())
  MT4_ORDERFUNCTION(Commission, double, MT4ORDERS::Order.Commission, 0, MT4ORDERS::CheckPositionCommissionComment())

  MT4_ORDERFUNCTION(OpenPriceRequest, double, MT4ORDERS::Order.OpenPriceRequest, ::OrderGetDouble(ORDER_PRICE_OPEN), MT4ORDERS::CheckPositionOpenPriceRequest())
  MT4_ORDERFUNCTION(ClosePriceRequest, double, ::PositionGetDouble(POSITION_PRICE_CURRENT), ::OrderGetDouble(ORDER_PRICE_CURRENT), true)

  MT4_ORDERFUNCTION(TicketOpen, long, MT4ORDERS::Order.TicketOpen, ::OrderGetInteger(ORDER_TICKET), MT4ORDERS::CheckPositionTicketOpen())
//  MT4_ORDERFUNCTION(OpenReason, ENUM_DEAL_REASON, MT4ORDERS::Order.OpenReason, ::OrderGetInteger(ORDER_REASON), MT4ORDERS::CheckPositionOpenReason())
  MT4_ORDERFUNCTION(OpenReason, ENUM_DEAL_REASON, ::PositionGetInteger(POSITION_REASON), ::OrderGetInteger(ORDER_REASON), true)
  MT4_ORDERFUNCTION(CloseReason, ENUM_DEAL_REASON, 0, ::OrderGetInteger(ORDER_REASON), true)
  MT4_ORDERFUNCTION(TicketID, long, ::PositionGetInteger(POSITION_IDENTIFIER), ::OrderGetInteger(ORDER_TICKET), true)

#undef POSITION_ORDER
#undef MT4_ORDERFUNCTION

  static void MT4OrderPrint( void )
  {
    if (MT4ORDERS::Order.Ticket == POSITION_SELECT)
      MT4ORDERS::CheckPositionCommissionComment();

    ::Print(MT4ORDERS::Order.ToString());

    return;
  }

  static double MT4OrderLots( const bool& )
  {
    double Res = MT4ORDERS::MT4OrderLots();

    if (Res && (MT4ORDERS::Order.Ticket == POSITION_SELECT))
    {
      const ulong PositionID = ::PositionGetInteger(POSITION_IDENTIFIER);

      if (::PositionSelectByTicket(PositionID))
      {
        const int Type = 1 - (int)::PositionGetInteger(POSITION_TYPE);

        double PrevVolume = Res;
        double NewVolume = 0;

        while (Res && (NewVolume != PrevVolume))
        {
        #ifdef MT4ORDERS_BYPASS_MAXTIME
          _B2(MT4ORDERS::ByPass.Waiting());
        #endif // #ifdef MT4ORDERS_BYPASS_MAXTIME

          if (::PositionSelectByTicket(PositionID))
          {
            Res = ::PositionGetDouble(POSITION_VOLUME);
            PrevVolume = Res;

            for (int i = ::OrdersTotal() - 1; i >= 0; i--)
              if (!::OrderGetTicket(i)) // Happens if i == ::OrdersTotal() - 1.
              {
                PrevVolume = -1;

                break;
              }
              else if ((::OrderGetInteger(ORDER_POSITION_ID) == PositionID) &&
                       (::OrderGetInteger(ORDER_TYPE) == Type))
                Res -= ::OrderGetDouble(ORDER_VOLUME_CURRENT);


            if (::PositionSelectByTicket(PositionID))
              NewVolume = ::PositionGetDouble(POSITION_VOLUME);
            else
              Res = 0;
          }
        }
      }
      else
        Res = 0;
    }

    return(Res);
  }

#undef ORDER_SELECT
#undef POSITION_SELECT
};

// #define OrderToString MT4ORDERS::MT4OrderToString

static MT4_ORDER MT4ORDERS::Order = {};

static MT4HISTORY MT4ORDERS::History;

static const bool MT4ORDERS::IsTester = ::MQLInfoInteger(MQL_TESTER);

// If you switch the account, this value will still be recalculated for EAs
// https://www.mql5.com/ru/forum/170952/page61#comment_6132824
static const bool MT4ORDERS::IsHedging = ((ENUM_ACCOUNT_MARGIN_MODE)::AccountInfoInteger(ACCOUNT_MARGIN_MODE) ==
                                          ACCOUNT_MARGIN_MODE_RETAIL_HEDGING);

static int MT4ORDERS::OrderSendBug = 0;

static uint MT4ORDERS::OrderSend_MaxPause = 1000000; // the maximum time for synchronization in microseconds.

#ifdef MT4ORDERS_BYPASS_MAXTIME
  static BYPASS MT4ORDERS::ByPass(MT4ORDERS_BYPASS_MAXTIME);
#endif // #ifdef MT4ORDERS_BYPASS_MAXTIME

static MqlTradeResult MT4ORDERS::LastTradeResult = {};
static MqlTradeRequest MT4ORDERS::LastTradeRequest = {};
static MqlTradeCheckResult MT4ORDERS::LastTradeCheckResult = {};

bool OrderClose( const long Ticket, const double dLots, const double Price, const int SlipPage, const color Arrow_Color = clrNONE, const string comment = NULL )
{
  return(MT4ORDERS::MT4OrderClose(Ticket, dLots, Price, SlipPage, Arrow_Color, comment));
}

bool OrderModify( const long Ticket, const double Price, const double SL, const double TP, const datetime Expiration, const color Arrow_Color = clrNONE )
{
  return(MT4ORDERS::MT4OrderModify(Ticket, Price, SL, TP, Expiration, Arrow_Color));
}

bool OrderCloseBy( const long Ticket, const long Opposite, const color Arrow_Color = clrNONE )
{
  return(MT4ORDERS::MT4OrderCloseBy(Ticket, Opposite, Arrow_Color));
}

bool OrderDelete( const long Ticket, const color Arrow_Color = clrNONE )
{
  return(MT4ORDERS::MT4OrderDelete(Ticket, Arrow_Color));
}

void OrderPrint( void )
{
  MT4ORDERS::MT4OrderPrint();

  return;
}

#define MT4_ORDERGLOBALFUNCTION(NAME,T)     \
  T Order##NAME( void )                     \
  {                                         \
    return((T)MT4ORDERS::MT4Order##NAME()); \
  }

MT4_ORDERGLOBALFUNCTION(sHistoryTotal, int)
MT4_ORDERGLOBALFUNCTION(Ticket, TICKET_TYPE)
MT4_ORDERGLOBALFUNCTION(Type, int)
MT4_ORDERGLOBALFUNCTION(Lots, double)
MT4_ORDERGLOBALFUNCTION(OpenPrice, double)
MT4_ORDERGLOBALFUNCTION(OpenTimeMsc, long)
MT4_ORDERGLOBALFUNCTION(OpenTime, datetime)
MT4_ORDERGLOBALFUNCTION(StopLoss, double)
MT4_ORDERGLOBALFUNCTION(TakeProfit, double)
MT4_ORDERGLOBALFUNCTION(ClosePrice, double)
MT4_ORDERGLOBALFUNCTION(CloseTimeMsc, long)
MT4_ORDERGLOBALFUNCTION(CloseTime, datetime)
MT4_ORDERGLOBALFUNCTION(Expiration, datetime)
MT4_ORDERGLOBALFUNCTION(MagicNumber, MAGIC_TYPE)
MT4_ORDERGLOBALFUNCTION(Profit, double)
MT4_ORDERGLOBALFUNCTION(Commission, double)
MT4_ORDERGLOBALFUNCTION(Swap, double)
MT4_ORDERGLOBALFUNCTION(Symbol, string)
MT4_ORDERGLOBALFUNCTION(Comment, string)

MT4_ORDERGLOBALFUNCTION(OpenPriceRequest, double)
MT4_ORDERGLOBALFUNCTION(ClosePriceRequest, double)

MT4_ORDERGLOBALFUNCTION(TicketOpen, TICKET_TYPE)
MT4_ORDERGLOBALFUNCTION(OpenReason, ENUM_DEAL_REASON)
MT4_ORDERGLOBALFUNCTION(CloseReason, ENUM_DEAL_REASON)
MT4_ORDERGLOBALFUNCTION(TicketID, TICKET_TYPE)

#undef MT4_ORDERGLOBALFUNCTION

double OrderLots( const bool Value )
{
  return(MT4ORDERS::MT4OrderLots(Value));
}

// Overloaded standard functions
#define OrdersTotal MT4ORDERS::MT4OrdersTotal // AFTER Expert/Expert.mqh - there is a call to MT5-OrdersTotal()

bool OrderSelect( const long Index, const int Select, const int Pool = MODE_TRADES )
{
  return(_B2(MT4ORDERS::MT4OrderSelect(Index, Select, Pool)));
}

TICKET_TYPE OrderSend( const string Symb, const int Type, const double dVolume, const double Price, const int SlipPage, const double SL, const double TP,
                       const string comment = NULL, const MAGIC_TYPE magic = 0, const datetime dExpiration = 0, color arrow_color = clrNONE )
{
  return((TICKET_TYPE)MT4ORDERS::MT4OrderSend(Symb, Type, dVolume, Price, SlipPage, SL, TP, comment, magic, dExpiration, arrow_color));
}

#define RETURN_ASYNC(A) return((A) && ::OrderSendAsync(MT4ORDERS::LastTradeRequest, MT4ORDERS::LastTradeResult) &&                        \
                               (MT4ORDERS::LastTradeResult.retcode == TRADE_RETCODE_PLACED) ? MT4ORDERS::LastTradeResult.request_id : 0);

uint OrderCloseAsync( const long Ticket, const double dLots, const double Price, const int SlipPage, const color Arrow_Color = clrNONE )
{
  RETURN_ASYNC(OrderClose(Ticket, dLots, Price, SlipPage, INT_MAX))
}

uint OrderModifyAsync( const long Ticket, const double Price, const double SL, const double TP, const datetime Expiration, const color Arrow_Color = clrNONE )
{
  RETURN_ASYNC(OrderModify(Ticket, Price, SL, TP, Expiration, INT_MAX))
}

uint OrderDeleteAsync( const long Ticket, const color Arrow_Color = clrNONE )
{
  RETURN_ASYNC(OrderDelete(Ticket, INT_MAX))
}

uint OrderSendAsync( const string Symb, const int Type, const double dVolume, const double Price, const int SlipPage, const double SL, const double TP,
                    const string comment = NULL, const MAGIC_TYPE magic = 0, const datetime dExpiration = 0, color arrow_color = clrNONE )
{
  RETURN_ASYNC(!OrderSend(Symb, Type, dVolume, Price, SlipPage, SL, TP, comment, magic, dExpiration, INT_MAX))
}

#undef RETURN_ASYNC

#undef MT4ORDERS_SLTP_OLD

#undef _BV2
#undef _B3
#undef _B2

#ifdef MT4ORDERS_BENCHMARK_MINTIME
  #undef MT4ORDERS_BENCHMARK_MINTIME
#endif // MT4ORDERS_BENCHMARK_MINTIME

// #undef TICKET_TYPE
#endif // __MT4ORDERS__
#else  // __MQL5__
  #define TICKET_TYPE int
  #define MAGIC_TYPE  int

  TICKET_TYPE OrderTicketID( void ) { return(::OrderTicket()); }
#endif // __MQL5__