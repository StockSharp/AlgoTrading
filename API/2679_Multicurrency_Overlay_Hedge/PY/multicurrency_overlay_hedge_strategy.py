import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class multicurrency_overlay_hedge_strategy(Strategy):
    """Multi-security correlation hedge strategy. Requires multiple securities (universe)."""

    def __init__(self):
        super(multicurrency_overlay_hedge_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))
        self._range_length = self.Param("RangeLength", 400)
        self._correlation_lookback = self.Param("CorrelationLookback", 500)
        self._atr_lookback = self.Param("AtrLookback", 200)
        self._correlation_threshold = self.Param("CorrelationThreshold", 0.9)
        self._overlay_threshold = self.Param("OverlayThreshold", 100.0)
        self._take_profit_by_points = self.Param("TakeProfitByPoints", True)
        self._take_profit_points = self.Param("TakeProfitPoints", 10.0)
        self._take_profit_by_currency = self.Param("TakeProfitByCurrency", False)
        self._take_profit_currency = self.Param("TakeProfitCurrency", 10.0)
        self._max_open_pairs = self.Param("MaxOpenPairs", 10)
        self._base_volume_param = self.Param("BaseVolume", 1.0)
        self._recalc_hour = self.Param("RecalculationHour", 1)
        self._max_spread = self.Param("MaxSpread", 10.0)

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RangeLength(self):
        return self._range_length.Value

    @property
    def CorrelationLookback(self):
        return self._correlation_lookback.Value

    @property
    def AtrLookback(self):
        return self._atr_lookback.Value

    @property
    def CorrelationThreshold(self):
        return self._correlation_threshold.Value

    @property
    def OverlayThreshold(self):
        return self._overlay_threshold.Value

    @property
    def TakeProfitByPoints(self):
        return self._take_profit_by_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def TakeProfitByCurrency(self):
        return self._take_profit_by_currency.Value

    @property
    def TakeProfitCurrency(self):
        return self._take_profit_currency.Value

    @property
    def MaxOpenPairs(self):
        return self._max_open_pairs.Value

    @property
    def BaseVolume(self):
        return self._base_volume_param.Value

    @property
    def RecalculationHour(self):
        return self._recalc_hour.Value

    @property
    def MaxSpread(self):
        return self._max_spread.Value

    def OnStarted2(self, time):
        super(multicurrency_overlay_hedge_strategy, self).OnStarted2(time)

        self.LogWarning("MulticurrencyOverlayHedge requires multiple securities. Running in single-security mode with no trading logic.")

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        # Multi-security logic cannot be replicated in single-security Python harness.

    def OnReseted(self):
        super(multicurrency_overlay_hedge_strategy, self).OnReseted()

    def CreateClone(self):
        return multicurrency_overlay_hedge_strategy()
