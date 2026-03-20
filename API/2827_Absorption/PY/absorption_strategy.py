import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class absorption_strategy(Strategy):
    def __init__(self):
        super(absorption_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._max_search = self.Param("MaxSearch", 10) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._take_profit_buy = self.Param("TakeProfitBuy", 10) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._take_profit_sell = self.Param("TakeProfitSell", 10) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._trailing_stop = self.Param("TrailingStop", 5) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._trailing_step = self.Param("TrailingStep", 5) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._indent = self.Param("Indent", 1) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._order_expiration_hours = self.Param("OrderExpirationHours", 8) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._breakeven = self.Param("Breakeven", 1) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._breakeven_profit = self.Param("BreakevenProfit", 10) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")

        self._highest = None
        self._lowest = None
        self._prev1 = None
        self._prev2 = None
        self._has_active_orders = False
        self._pending_high = 0.0
        self._pending_low = 0.0
        self._pending_buy_price = 0.0
        self._pending_sell_price = 0.0
        self._pending_buy_stop_loss = 0.0
        self._pending_sell_stop_loss = 0.0
        self._pending_buy_take_profit = 0.0
        self._pending_sell_take_profit = 0.0
        self._orders_expiry = None
        self._entry_price = 0.0
        self._stop_loss = 0.0
        self._take_profit = 0.0
        self._prev_position = 0.0
        self._exit_request_active = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(absorption_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._prev1 = None
        self._prev2 = None
        self._has_active_orders = False
        self._pending_high = 0.0
        self._pending_low = 0.0
        self._pending_buy_price = 0.0
        self._pending_sell_price = 0.0
        self._pending_buy_stop_loss = 0.0
        self._pending_sell_stop_loss = 0.0
        self._pending_buy_take_profit = 0.0
        self._pending_sell_take_profit = 0.0
        self._orders_expiry = None
        self._entry_price = 0.0
        self._stop_loss = 0.0
        self._take_profit = 0.0
        self._prev_position = 0.0
        self._exit_request_active = False

    def OnStarted(self, time):
        super(absorption_strategy, self).OnStarted(time)

        self.__highest = Highest()
        self.__highest.Length = self.max_search
        self.__lowest = Lowest()
        self.__lowest.Length = self.max_search

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return absorption_strategy()
