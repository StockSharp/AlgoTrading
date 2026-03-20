import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class rm_stochastic_band_strategy(Strategy):
    def __init__(self):
        super(rm_stochastic_band_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._stochastic_length = self.Param("StochasticLength", 5) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._stochastic_smoothing = self.Param("StochasticSmoothing", 3) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._stochastic_signal_length = self.Param("StochasticSignalLength", 3) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._stop_loss_multiplier = self.Param("StopLossMultiplier", 1.5) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._take_profit_multiplier = self.Param("TakeProfitMultiplier", 3) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._min_margin = self.Param("MinMargin", 100) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._max_spread_standard = self.Param("MaxSpreadStandard", 3) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._max_spread_cent = self.Param("MaxSpreadCent", 10) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._oversold_level = self.Param("OversoldLevel", 20) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._overbought_level = self.Param("OverboughtLevel", 80) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._base_candle_type = self.Param("BaseCandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._mid_candle_type = self.Param("MidCandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._high_candle_type = self.Param("HighCandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")

        self._stoch_m1 = None
        self._stoch_m5 = None
        self._stoch_m15 = None
        self._atr_value = None
        self._long_stop_price = None
        self._long_take_profit = None
        self._short_stop_price = None
        self._short_take_profit = None
        self._best_bid = None
        self._best_ask = None

    def OnReseted(self):
        super(rm_stochastic_band_strategy, self).OnReseted()
        self._stoch_m1 = None
        self._stoch_m5 = None
        self._stoch_m15 = None
        self._atr_value = None
        self._long_stop_price = None
        self._long_take_profit = None
        self._short_stop_price = None
        self._short_take_profit = None
        self._best_bid = None
        self._best_ask = None

    def OnStarted(self, time):
        super(rm_stochastic_band_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        base_subscription = self.SubscribeCandles(Baseself.candle_type)
        base_subscription.BindEx(baseStochastic, self._process_candle).Start()

        subscription = self.SubscribeCandles(Midself.candle_type)
        subscription.BindEx(midStochastic, self._process_candle_1).Start()

        subscription = self.SubscribeCandles(Highself.candle_type)
        subscription.BindEx(highStochastic, self._atr, self._process_candle_2).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return rm_stochastic_band_strategy()
