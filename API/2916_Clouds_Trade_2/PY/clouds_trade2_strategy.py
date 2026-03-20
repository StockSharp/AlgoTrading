import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class clouds_trade2_strategy(Strategy):
    def __init__(self):
        super(clouds_trade2_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1) \
            .SetDisplay("Order Volume", "Default order volume", "General")
        self._stop_loss_offset = self.Param("StopLossOffset", 0.005) \
            .SetDisplay("Order Volume", "Default order volume", "General")
        self._take_profit_offset = self.Param("TakeProfitOffset", 0.005) \
            .SetDisplay("Order Volume", "Default order volume", "General")
        self._trailing_stop_offset = self.Param("TrailingStopOffset", 0) \
            .SetDisplay("Order Volume", "Default order volume", "General")
        self._trailing_step_offset = self.Param("TrailingStepOffset", 0.0005) \
            .SetDisplay("Order Volume", "Default order volume", "General")
        self._min_profit_currency = self.Param("MinProfitCurrency", 10) \
            .SetDisplay("Order Volume", "Default order volume", "General")
        self._min_profit_points = self.Param("MinProfitPoints", 0.001) \
            .SetDisplay("Order Volume", "Default order volume", "General")
        self._use_fractals = self.Param("UseFractals", True) \
            .SetDisplay("Order Volume", "Default order volume", "General")
        self._use_stochastic = self.Param("UseStochastic", False) \
            .SetDisplay("Order Volume", "Default order volume", "General")
        self._one_trade_per_day = self.Param("OneTradePerDay", True) \
            .SetDisplay("Order Volume", "Default order volume", "General")
        self._k_period = self.Param("KPeriod", 5) \
            .SetDisplay("Order Volume", "Default order volume", "General")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("Order Volume", "Default order volume", "General")
        self._slowing_period = self.Param("SlowingPeriod", 3) \
            .SetDisplay("Order Volume", "Default order volume", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Order Volume", "Default order volume", "General")

        self._stochastic = None
        self._prior_k = 0.0
        self._prior_d = 0.0
        self._last_k = 0.0
        self._last_d = 0.0
        self._has_prior_stoch = False
        self._has_last_stoch = False
        self._h1 = 0.0
        self._h2 = 0.0
        self._h3 = 0.0
        self._h4 = 0.0
        self._h5 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0
        self._l4 = 0.0
        self._l5 = 0.0
        self._latest_fractal = None
        self._previous_fractal = None
        self._fractal_buffer_count = 0.0
        self._stop_price = None
        self._take_profit_price = None
        self._entry_price = 0.0
        self._last_entry_date = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(clouds_trade2_strategy, self).OnReseted()
        self._stochastic = None
        self._prior_k = 0.0
        self._prior_d = 0.0
        self._last_k = 0.0
        self._last_d = 0.0
        self._has_prior_stoch = False
        self._has_last_stoch = False
        self._h1 = 0.0
        self._h2 = 0.0
        self._h3 = 0.0
        self._h4 = 0.0
        self._h5 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0
        self._l4 = 0.0
        self._l5 = 0.0
        self._latest_fractal = None
        self._previous_fractal = None
        self._fractal_buffer_count = 0.0
        self._stop_price = None
        self._take_profit_price = None
        self._entry_price = 0.0
        self._last_entry_date = None

    def OnStarted(self, time):
        super(clouds_trade2_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(_stochastic, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return clouds_trade2_strategy()
