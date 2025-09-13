//+------------------------------------------------------------------+
//|                             Prototypes.mqh (02/12/2014 revision) |
//|                           Copyright 2014, Vasiliy Sokolov (C-4). |
//|                              https://login.mql5.com/ru/users/c-4 |
//|    DESCRIPTION: This file contains prototypes exported functions |
//|  HedgeTerminalAPI. Copy this file to .\MQL5\Include directory on |
//|                                            your pc. For example: |
//|    "C:\Programm Files\MetaTrader 5\MQL5\Include\Prototypes.mqh". |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013, Vasiliy Sokolov"
#property link      "https://login.mql5.com/ru/users/c-4"
#define __HT__
///
/// Targets ID.
///
enum ENUM_TARGET_TYPE
{
   ///
   /// Not define target.
   ///
   TARGET_NDEF,
   ///
   /// Something that means, but I do not remember.
   ///
   TARGET_CREATE_TASK,
   ///
   /// Delete pending order.
   ///
   TARGET_DELETE_PENDING_ORDER,
   ///
   /// Set pending order.
   ///
   TARGET_SET_PENDING_ORDER,
   ///
   /// Change price of pending order.
   ///
   TARGET_MODIFY_PENDING_ORDER,
   ///
   /// Trade by market.
   ///
   TARGET_TRADE_BY_MARKET
};

///
/// Define type of parameter 'index' in function HedgePositionSelect().
///
enum ENUM_MODE_SELECT
{
   ///
   /// Parameter 'index' contains entry ticket of position. 
   ///
   SELECT_BY_POS,
   ///
   /// Parameter 'index' equal index of position in list positions. 
   ///
   SELECT_BY_TICKET
};

///
/// Define type of list wherein function HedgePositionSelect find position. 
///
enum ENUM_MODE_TRADES
{
   ///
   /// Transaction selected from trading pool.
   ///
   MODE_TRADES,
   ///
   /// Transaction selected from history pool (closed and canceled order).
   ///
   MODE_HISTORY
};

///
/// Type of elected order in selected position.
///
enum ENUM_HEDGE_ORDER_SELECTED_TYPE
{
   ORDER_SELECTED_INIT,
   ORDER_SELECTED_CLOSED,
   ORDER_SELECTED_SL
};

///
/// Type of state position.
///
enum ENUM_HEDGE_POSITION_STATE
{
   POSITION_STATE_ACTIVE,
   POSITION_STATE_FROZEN
};
///
/// Status of current position.
///
enum ENUM_HEDGE_POSITION_STATUS
{
   HEDGE_POSITION_ACTIVE,
   HEDGE_POSITION_HISTORY
};
///
/// Status of last task.
///
enum ENUM_TASK_STATUS
{
   ///
   /// No task ot task waiting.
   ///
   TASK_STATUS_WAITING,
   ///
   /// Position frozen for changes. Task executing.
   ///
   TASK_STATUS_EXECUTING,
   ///
   /// Last task was executed successfuly. 
   ///
   TASK_STATUS_COMPLETE,
   ///
   /// Last task failed. 
   ///
   TASK_STATUS_FAILED
};

///
/// Type of transaction.
///
enum ENUM_TRANS_TYPE
{
   ///
   /// Transaction not defined.
   ///
   TRANS_NOT_DEFINED,
   ///
   /// Transaction type is position.
   ///
   TRANS_HEDGE_POSITION,
   ///
   /// Transaction type is brokerage deal.
   ///
   TRANS_BROKERAGE_DEAL,
   ///
   /// Transaction type is order.
   ///
   TRANS_PENDING_ORDER,
   ///
   /// Transaction type is swap.
   ///
   TRANS_SWAP_POS
};

///
/// Direction of transaction.
///
enum ENUM_DIRECTION_TYPE
{
   ///
   /// Short transaction. Init Sell, Sell Limit, Sell Stop and Sell Stop Limit orders.
   ///
   DIRECTION_SHORT = -1,
   ///
   /// Direction of transaction not defined. For example brokerage deal is undefined direction.
   ///
   DIRECTION_UNDEFINED,
   ///
   /// Long transaction. Init Buy, Buy Limit, Buy Stop and Buy Stop Limit orders.
   ///
   DIRECTION_LONG
};

///
/// Direction of transaction.
///
/*enum ENUM_TRANS_DIRECTION
{
   ///
   /// Direction transaction not defined.
   ///
   TRANS_NDEF,
   ///
   /// Direction transaction is long.
   ///
   TRANS_LONG,
   ///
   /// Direction transaction is short.
   ///
   TRANS_SHORT
};*/

///
/// Status of current order.
///
enum ENUM_HEDGE_ORDER_STATUS
{
   HEDGE_ORDER_PENDING,
   HEDGE_ORDER_HISTORY
};

///
/// Define type integer property of hedge position.
/// This enum is analog ENUM_POSITION_PROPERTY_INTEGER and used by
/// HedgePositionGetInteger function.
///
enum ENUM_HEDGE_POSITION_PROP_INTEGER
{
   HEDGE_POSITION_ENTRY_TIME_SETUP_MSC,
   HEDGE_POSITION_ENTRY_TIME_EXECUTED_MSC,
   HEDGE_POSITION_EXIT_TIME_SETUP_MSC,
   HEDGE_POSITION_EXIT_TIME_EXECUTED_MSC,
   HEDGE_POSITION_TYPE,
   HEDGE_POSITION_DIRECTION,
   HEDGE_POSITION_MAGIC,
   HEDGE_POSITION_CLOSE_TYPE,
   HEDGE_POSITION_ID,
   HEDGE_POSITION_ENTRY_ORDER_ID,
   HEDGE_POSITION_EXIT_ORDER_ID,
   HEDGE_POSITION_STATUS,
   HEDGE_POSITION_STATE,
   HEDGE_POSITION_USING_SL,
   HEDGE_POSITION_USING_TP,
   HEDGE_POSITION_TASK_STATUS,
   HEDGE_POSITION_ACTIONS_TOTAL
};

///
/// Define type double property of hedge position.
/// This enum is analog ENUM_POSITION_PROPERTY_DOUBLE and used by
/// HedgePositionGetDouble function.
///
enum ENUM_HEDGE_POSITION_PROP_DOUBLE
{
   HEDGE_POSITION_VOLUME,
   HEDGE_POSITION_PRICE_OPEN,
   HEDGE_POSITION_PRICE_CLOSED,
   HEDGE_POSITION_PRICE_CURRENT,
   HEDGE_POSITION_SL,
   HEDGE_POSITION_TP,
   HEDGE_POSITION_COMMISSION,
   HEDGE_POSITION_SLIPPAGE,
   HEDGE_POSITION_PROFIT_CURRENCY,
   HEDGE_POSITION_PROFIT_POINTS
};

///
/// Define type string property of hedge position.
/// This enum is analog ENUM_POSITION_PROPERTY_STRING and used by
/// HedgePositionGetString function.
///
enum ENUM_HEDGE_POSITION_PROP_STRING
{
   HEDGE_POSITION_SYMBOL,
   HEDGE_POSITION_ENTRY_COMMENT,
   HEDGE_POSITION_EXIT_COMMENT
};


///
/// Define type ulong property of order include in current position.
///
enum ENUM_HEDGE_ORDER_PROP_INTEGER
{
   HEDGE_ORDER_ID,
   HEDGE_ORDER_STATUS,
   HEDGE_ORDER_DEALS_TOTAL,
   HEDGE_ORDER_TIME_SETUP_MSC,
   HEDGE_ORDER_TIME_EXECUTED_MSC,
   HEDGE_ORDER_TIME_CANCELED_MSC,
};
///
/// Define type double property of order include in current position.
///
enum ENUM_HEDGE_ORDER_PROP_DOUBLE
{
   HEDGE_ORDER_VOLUME_SETUP,
   HEDGE_ORDER_VOLUME_EXECUTED,
   HEDGE_ORDER_VOLUME_REJECTED,
   HEDGE_ORDER_PRICE_SETUP,
   HEDGE_ORDER_PRICE_EXECUTED,
   HEDGE_ORDER_COMMISSION,
   HEDGE_ORDER_SLIPPAGE
};

///
/// Define type integer property of hedge notion deal.
/// This enum is analog ENUM_DEAL_PROPERTY_INTEGER and used by
/// HedgeDealGetInteger function.
///
enum ENUM_HEDGE_DEAL_PROP_INTEGER
{
   HEDGE_DEAL_ID,
   HEDGE_DEAL_TIME_EXECUTED_MSC
};

///
/// Define type double property of hedge notion deal.
/// This enum is analog ENUM_DEAL_PROPERTY_DOUBLE and used by
/// HedgeDealGetDouble function.
///
enum ENUM_HEDGE_DEAL_PROP_DOUBLE
{
   HEDGE_DEAL_VOLUME_EXECUTED,
   HEDGE_DEAL_PRICE_EXECUTED,
   HEDGE_DEAL_COMMISSION
};

enum ENUM_HEDGE_PROP_INTEGER
{
   HEDGE_PROP_TIMEOUT
};
///
/// Define type of action in struct HedgeTradeRequest.
/// This enum is analog ENUM_TRADE_REQUEST_ACTIONS and used
/// by OrderSendFunction.
///
enum ENUM_HEDGE_REQUEST_ACTIONS
{
   HEDGE_ACTION_DEAL,
   HEDGE_ACTION_PENDING,
   HEDGE_ACTION_SLTP,
   HEDGE_ACTION_MODIFY,
   HEDGE_ACTION_REMOVE,
   HEDGE_ACTION_CLOSE
};

///
/// Type of error genered of HedgeTerminal.
///
enum ENUM_HEDGE_ERR
{
   ///
   /// No error.
   ///
   HEDGE_ERR_NOT_ERROR,
   ///
   /// Task was failed.
   ///
   HEDGE_ERR_TASK_FAILED,
   ///
   /// Transaction not find or missing.
   ///
   HEDGE_ERR_TRANS_NOTFIND,
   ///
   /// Index of transaction missing or wrong.
   ///
   HEDGE_ERR_WRONG_INDEX,
   ///
   /// Set volume for position is wrong.
   ///
   HEDGE_ERR_WRONG_VOLUME,
   ///
   /// Position not select.
   ///
   HEDGE_ERR_TRANS_NOTSELECTED,
   ///
   /// Parameter of tranastion not supporting or wrong.
   ///
   HEDGE_ERR_WRONG_PARAMETER,
   ///
   /// Selected position in the change process, and can not be read or modified.
   ///
   HEDGE_ERR_POS_FROZEN,
   ///
   /// No changes in trade request.
   ///
   HEDGE_ERR_POS_NO_CHANGES
};

///
/// This enum mark closing order as special order type.
///
enum ENUM_CLOSE_TYPE
{
   ///
   /// Mark closing position as market.
   ///
   CLOSE_AS_MARKET,
   ///
   /// Mark closing position as stop-loss.
   ///
   CLOSE_AS_STOP_LOSS,
   ///
   /// Mark closing position as take-profit.
   ///
   CLOSE_AS_TAKE_PROFIT,
};

///
/// Type of request.
///
enum ENUM_REQUEST_TYPE
{
   ///
   /// Close selected position.
   ///
   REQUEST_CLOSE_POSITION,
   ///
   /// Modify stop-loss and/or take-profit levels.
   ///
   REQUEST_MODIFY_SLTP,
   ///
   /// Modify exit comment.
   ///
   REQUEST_MODIFY_COMMENT
};

///
/// This structure used by HedgePositionClose function.
/// This structure define params which need for closing hedge position.
///
struct HedgeTradeRequest //HedgeTradeRequest
{
   ///
   /// Type of action.
   ///
   ENUM_REQUEST_TYPE action;
   ///
   /// Volume of position to be closed. May be less or equal than executed volume position.
   /// If equal 0.0 closing all executed volume position.
   ///
   double volume;
   ///
   /// Marker of closing order. See ENUM_CLOSE_TYPE description.
   ///
   ENUM_CLOSE_TYPE close_type;
   ///
   /// Stop-Loss level.
   ///
   double sl;
   ///
   /// Take-Profit level.
   ///
   double tp;
   ///
   /// Outgoing comment.
   ///
   string exit_comment;
   ///
   /// Last retcode in executed operation.
   ///
   uint retcode;
   ///
   /// True if the closure is performed asynchronously, otherwise false.
   ///
   bool asynch_mode;
   ///
   /// Deviation in step price.
   ///
   ulong deviation;
   ///
   /// Constructor.
   ///
   HedgeTradeRequest()
   {
      action = REQUEST_CLOSE_POSITION;
      asynch_mode = false;
      volume = 0.0;
      sl = 0.0;
      tp = 0.0;
   }
};

#import "..\Experts\Market\hedgeterminalapi.ex5"
   ///
   /// Return last error of Hedge terminal API.
   ///
   ENUM_HEDGE_ERR GetHedgeError(void);
   ///
   /// Reset last hedge error.
   ///
   void ResetHedgeError(void);
   ///
   /// Return count actions of last task, if task was executed. Current position should be selected.
   /// This function is used for the analysis of trade and the possible errors.
   /// \return Count of last task;
   ///
   uint TotalActionsTask(void);
   ///
   /// Get result of action by it's index 'index'. The values returned by reference.
   /// Current position should be selected. This function is used for the analysis of trade and the possible errors.
   /// \param index - Index of target in last task.
   /// \param target_type - Type of target.
   /// \param retcode - Result of executed target. This value return codes of the Trade Server in MetaTrader 5.
   /// You can see the constant code in documentation 'http://www.mql5.com/en/docs/constants/errorswarnings/enum_trade_return_codes'
   ///
   void GetActionResult(uint index, ENUM_TARGET_TYPE &target_type, uint& retcode);
   ///
   /// Returns the number of active or history transactions.
   /// \param pool - Selecting flags. It can be any of the following values:
   /// MODE_TRADES(default) - transaction selected from trading pool(opened positions and pending orders).
   /// MODE_HISTORY - transaction selected from history pool (closing positions, cancel orders, brokerage deals, swaps and etc.)
   ///
   int TransactionsTotal(ENUM_MODE_TRADES pool = MODE_TRADES);
   ///
   /// The function selects an transaction for further processing.
   /// \param index - index or order ticket.
   /// \param select - Selecting flags. It can be any of the following values:
   /// SELECT_BY_POS - index in the transaction pool,
   /// SELECT_BY_TICKET - index is transaction ticket. Used only when the pool parameter is MODE_TRADES.
   /// \param pool - Optional transaction pool index. It can be any of the following values:
   /// MODE_TRADES(default) - transaction selected from trading pool(opened positions and pending orders).
   /// MODE_HISTORY - transaction selected from history pool (closing positions, cancel orders, deals, swaps and etc.)
   ///
   bool TransactionSelect(ulong index, ENUM_MODE_SELECT select = SELECT_BY_POS, ENUM_MODE_TRADES pool=MODE_TRADES);
   ///
   /// Returns type of selected transaction. Before using this function, transaction must be selected by TransactionSelect.
   /// \return Type of selected transaction. 
   /// 
   ENUM_TRANS_TYPE TransactionType(void);
   ///
   /// Select order in current position by it type. Current position should be selected by TransactionSelect.
   /// \return True - if order was selected, otherwise false.
   ///
   bool HedgeOrderSelect(ENUM_HEDGE_ORDER_SELECTED_TYPE type);
   ///
   /// Select deal by index in current order. Current order should be selected by HedgeOrderSelect.
   /// \return True - if deal was selected, otherwise false.
   ///
   bool HedgeDealSelect(int index);
   ///
   /// Get ulong caption of selected position. Type of caption define 'property'.
   /// \return Value of caption.
   ///
   ulong HedgePositionGetInteger(ENUM_HEDGE_POSITION_PROP_INTEGER property);
   ///
   /// Get double caption of selected position. Type of caption define 'property'. 
   /// \return Value of caption.
   ///
   double HedgePositionGetDouble(ENUM_HEDGE_POSITION_PROP_DOUBLE property);
   ///
   /// Get string caption of selected position. Type of caption define 'property'. 
   /// \return Value of caption.
   ///
   string HedgePositionGetString(ENUM_HEDGE_POSITION_PROP_STRING property);
   ///
   /// Get ulong caption of selected order. Type of caption define 'property'. 
   /// \return Value of caption.
   ///
   ulong HedgeOrderGetInteger(ENUM_HEDGE_ORDER_PROP_INTEGER property);
   ///
   /// Get double caption of selected order. Type of caption define 'property'. 
   /// \return Value of caption.
   ///
   double HedgeOrderGetDouble(ENUM_HEDGE_ORDER_PROP_DOUBLE property);
   ///
   /// Get ulong caption of selected deal. Type of caption define 'property'. 
   /// \return Value of caption.
   ///
   ulong HedgeDealGetInteger(ENUM_HEDGE_DEAL_PROP_INTEGER property);
   ///
   /// Get double caption of selected deal. Type of caption define 'property'. 
   /// \return Value of caption.
   ///
   double HedgeDealGetDouble(ENUM_HEDGE_DEAL_PROP_DOUBLE property);
   ///
   /// Send trade request.
   /// \param request - define params which need for closing active hedge position.
   ///
   bool SendTradeRequest(HedgeTradeRequest& request);
   ///
   /// Set integer property of HedgeTerminal.
   /// \param property - Type of property.
   /// \param value - value of property.
   /// \return True if property set successfully, otherwise false.
   ///
   bool HedgePropertySetInteger(ENUM_HEDGE_PROP_INTEGER property, long value);
   ///
   /// Get integer property of HedgeTerminal.
   /// \param property - Type of property.
   /// \return value of property.
   ///
   long HedgePropertyGetInteger(ENUM_HEDGE_PROP_INTEGER property, long value);
#import

/*
                                 WILDCARD MACROSS
*/
#define FOREACH_POSITION for(int i = TransactionsTotal()-1; i >= 0; i--)
#define IF_LONG if(HedgePositionGetInteger(HEDGE_POSITION_DIRECTION) == DIRECTION_LONG)
#define IF_SHORT if(HedgePositionGetInteger(HEDGE_POSITION_DIRECTION) == DIRECTION_SHORT)
#define IF_FROZEN if(HedgePositionGetInteger(HEDGE_POSITION_STATE) == POSITION_STATE_FROZEN)