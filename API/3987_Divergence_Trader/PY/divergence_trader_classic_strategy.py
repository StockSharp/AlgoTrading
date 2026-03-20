import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, SimpleMovingAverage as SMA
from StockSharp.Algo.Strategies import Strategy


class divergence_trader_classic_strategy(Strategy):
    def __init__(self):
        super(divergence_trader_classic_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._fast_period = self.Param("FastPeriod", 7) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._slow_period = self.Param("SlowPeriod", 88) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._applied_price = self.Param("AppliedPrice", CandlePrices.Open) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._buy_threshold = self.Param("BuyThreshold", 10) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._stay_out_threshold = self.Param("StayOutThreshold", 1000) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._take_profit_pips = self.Param("TakeProfitPips", 0) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._stop_loss_pips = self.Param("StopLossPips", 0) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 9999) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._break_even_pips = self.Param("BreakEvenPips", 9999) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._break_even_buffer_pips = self.Param("BreakEvenBufferPips", 2) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._basket_profit_currency = self.Param("BasketProfitCurrency", 75) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._basket_loss_currency = self.Param("BasketLossCurrency", 9999) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._stop_hour = self.Param("StopHour", 24) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Order Volume", "Volume used when opening a new position.", "Trading")

        self._fast_sma = None
        self._slow_sma = None
        self._previous_spread = None
        self._pip_size = 0.0
        self._max_basket_pn_l = 0.0
        self._min_basket_pn_l = 0.0
        self._break_even_price = None
        self._trailing_stop_price = None
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(divergence_trader_classic_strategy, self).OnReseted()
        self._fast_sma = None
        self._slow_sma = None
        self._previous_spread = None
        self._pip_size = 0.0
        self._max_basket_pn_l = 0.0
        self._min_basket_pn_l = 0.0
        self._break_even_price = None
        self._trailing_stop_price = None
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(divergence_trader_classic_strategy, self).OnStarted(time)

        self.__fast_sma = SMA()
        self.__fast_sma.Length = self.fast_period
        self.__slow_sma = SMA()
        self.__slow_sma.Length = self.slow_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__fast_sma, self.__slow_sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return divergence_trader_classic_strategy()
