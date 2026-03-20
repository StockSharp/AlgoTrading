import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class multicurrency_overlay_hedge_strategy(Strategy):
    def __init__(self):
        super(multicurrency_overlay_hedge_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(1) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")
        self._range_length = self.Param("RangeLength", 400) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")
        self._correlation_lookback = self.Param("CorrelationLookback", 500) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")
        self._atr_lookback = self.Param("AtrLookback", 200) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")
        self._correlation_threshold = self.Param("CorrelationThreshold", 0.9) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")
        self._overlay_threshold = self.Param("OverlayThreshold", 100) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")
        self._take_profit_by_points = self.Param("TakeProfitByPoints", True) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 10) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")
        self._take_profit_by_currency = self.Param("TakeProfitByCurrency", False) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")
        self._take_profit_currency = self.Param("TakeProfitCurrency", 10) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")
        self._max_open_pairs = self.Param("MaxOpenPairs", 10) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")
        self._base_volume = self.Param("BaseVolume", 1) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")
        self._recalc_hour = self.Param("RecalculationHour", 1) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")
        self._max_spread = self.Param("MaxSpread", 10) \
            .SetDisplay("Universe", "Collection of forex symbols", "General")

        self._contexts = new()
        self._pairs = new()
        self._universe_list = new()
        self._last_recalc_day = DateTime.MinValue
        self._closes = None
        self._highs = None
        self._lows = None
        self._true_ranges = None
        self._previous_close = 0.0
        self._has_previous_close = False
        self._start = 0.0
        self._count = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multicurrency_overlay_hedge_strategy, self).OnReseted()
        self._contexts = new()
        self._pairs = new()
        self._universe_list = new()
        self._last_recalc_day = DateTime.MinValue
        self._closes = None
        self._highs = None
        self._lows = None
        self._true_ranges = None
        self._previous_close = 0.0
        self._has_previous_close = False
        self._start = 0.0
        self._count = 0.0

    def OnStarted(self, time):
        super(multicurrency_overlay_hedge_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


        subscription = self.SubscribeCandles(self.candle_type, true, security)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return multicurrency_overlay_hedge_strategy()
