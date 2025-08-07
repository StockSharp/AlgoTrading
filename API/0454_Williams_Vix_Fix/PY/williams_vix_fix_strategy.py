import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math
from StockSharp.Messages import CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import BollingerBands, Highest, Lowest, SimpleMovingAverage, StandardDeviation
from datatype_extensions import *


class williams_vix_fix_strategy(Strategy):
    """Williams VIX Fix strategy.

    Adapts the Williams VIX Fix indicator to spot volatility extremes. When the
    calculated VIX Fix or its inverse rises above a dynamic Bollinger Band the
    strategy enters in anticipation of mean reversion. Bollinger Bands on price are
    used as additional confirmation.
    """

    def __init__(self):
        super(williams_vix_fix_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_length = self.Param("BbLength", 20) \
            .SetDisplay("BB Length", "Bollinger Bands length", "Bollinger Bands")
        self._bb_multiplier = self.Param("BbMultiplier", 2.0) \
            .SetDisplay("BB Multiplier", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
        self._wvf_period = self.Param("WvfPeriod", 20) \
            .SetDisplay("WVF Period", "Williams VIX Fix lookback period for StdDev", "Williams VIX Fix")
        self._wvf_lookback = self.Param("WvfLookback", 50) \
            .SetDisplay("WVF Lookback", "Williams VIX Fix lookback period for percentile", "Williams VIX Fix")
        self._highest_percentile = self.Param("HighestPercentile", 0.85) \
            .SetDisplay("Highest Percentile", "Highest percentile threshold", "Williams VIX Fix")
        self._lowest_percentile = self.Param("LowestPercentile", 0.99) \
            .SetDisplay("Lowest Percentile", "Lowest percentile threshold", "Williams VIX Fix")

        self._bollinger = None
        self._highest_close = None
        self._lowest_close = None
        self._wvf_sma = None
        self._wvf_std = None
        self._wvf_inv_sma = None
        self._wvf_inv_std = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def bb_length(self):
        return self._bb_length.Value

    @bb_length.setter
    def bb_length(self, value):
        self._bb_length.Value = value

    @property
    def bb_multiplier(self):
        return self._bb_multiplier.Value

    @bb_multiplier.setter
    def bb_multiplier(self, value):
        self._bb_multiplier.Value = value

    @property
    def wvf_period(self):
        return self._wvf_period.Value

    @wvf_period.setter
    def wvf_period(self, value):
        self._wvf_period.Value = value

    @property
    def wvf_lookback(self):
        return self._wvf_lookback.Value

    @wvf_lookback.setter
    def wvf_lookback(self, value):
        self._wvf_lookback.Value = value

    @property
    def highest_percentile(self):
        return self._highest_percentile.Value

    @highest_percentile.setter
    def highest_percentile(self, value):
        self._highest_percentile.Value = value

    @property
    def lowest_percentile(self):
        return self._lowest_percentile.Value

    @lowest_percentile.setter
    def lowest_percentile(self, value):
        self._lowest_percentile.Value = value

    def OnStarted(self, time):
        super(williams_vix_fix_strategy, self).OnStarted(time)

        self._bollinger = BollingerBands()
        self._bollinger.Length = self.bb_length
        self._bollinger.Width = self.bb_multiplier

        self._highest_close = Highest()
        self._highest_close.Length = self.wvf_period
        self._lowest_close = Lowest()
        self._lowest_close.Length = self.wvf_period

        self._wvf_sma = SimpleMovingAverage(); self._wvf_sma.Length = self.bb_length
        self._wvf_std = StandardDeviation(); self._wvf_std.Length = self.bb_length
        self._wvf_inv_sma = SimpleMovingAverage(); self._wvf_inv_sma.Length = self.bb_length
        self._wvf_inv_std = StandardDeviation(); self._wvf_inv_std.Length = self.bb_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bollinger, self._highest_close, self._lowest_close, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bb_value, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._bollinger.IsFormed or not self._highest_close.IsFormed or not self._lowest_close.IsFormed:
            return

        price = candle.ClosePrice
        low_price = candle.LowPrice
        high_price = candle.HighPrice

        wvf = ((float(highest_value) - low_price) / float(highest_value)) * 100
        wvf_sma = self._wvf_sma.Process(wvf, candle.ServerTime, True)
        wvf_std = self._wvf_std.Process(wvf, candle.ServerTime, True)
        if not wvf_sma.IsFormed or not wvf_std.IsFormed:
            return
        wvf_upper = float(wvf_sma) + (self.bb_multiplier * float(wvf_std))

        wvf_inv = ((high_price - float(lowest_value)) / float(lowest_value)) * 100
        wvf_inv_sma = self._wvf_inv_sma.Process(wvf_inv, candle.ServerTime, True)
        wvf_inv_std = self._wvf_inv_std.Process(wvf_inv, candle.ServerTime, True)
        if not wvf_inv_sma.IsFormed or not wvf_inv_std.IsFormed:
            return
        wvf_inv_upper = float(wvf_inv_sma) + (self.bb_multiplier * float(wvf_inv_std))

        self._check_conditions(candle, wvf, wvf_upper, wvf_inv, wvf_inv_upper)

    def _check_conditions(self, candle, wvf, wvf_upper, wvf_inv, wvf_inv_upper):
        price = candle.ClosePrice
        bb_lower = self._bollinger.LowBand.GetValue(0)
        bb_upper = self._bollinger.UpBand.GetValue(0)

        range_high = wvf * self.highest_percentile
        range_high_inv = wvf_inv * self.lowest_percentile

        buy = (wvf >= wvf_upper or wvf >= range_high) and price < bb_lower
        sell = (wvf_inv <= wvf_inv_upper or wvf_inv <= range_high_inv) and price > bb_upper

        if buy and self.Position == 0:
            self.RegisterOrder(self.CreateOrder(Sides.Buy, price, self.Volume))
        if self.Position > 0 and sell:
            self.RegisterOrder(self.CreateOrder(Sides.Sell, price, Math.Abs(self.Position)))

    def CreateClone(self):
        return williams_vix_fix_strategy()
