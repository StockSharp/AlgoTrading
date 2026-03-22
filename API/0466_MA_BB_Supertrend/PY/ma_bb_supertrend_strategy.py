import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, BollingerBands, SuperTrend, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class ma_bb_supertrend_strategy(Strategy):
    """Moving Average Crossover confirmed by SuperTrend with Bollinger Bands exits."""

    def __init__(self):
        super(ma_bb_supertrend_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_ma_length = self.Param("FastMaLength", 20) \
            .SetDisplay("Fast MA Length", "Fast SMA period", "MA")
        self._slow_ma_length = self.Param("SlowMaLength", 50) \
            .SetDisplay("Slow MA Length", "Slow SMA period", "MA")
        self._bb_length = self.Param("BbLength", 20) \
            .SetDisplay("BB Length", "Bollinger Bands period", "Bollinger")
        self._supertrend_period = self.Param("SupertrendPeriod", 20) \
            .SetDisplay("SuperTrend Period", "ATR period for SuperTrend", "SuperTrend")
        self._supertrend_factor = self.Param("SupertrendFactor", 4.0) \
            .SetDisplay("SuperTrend Factor", "ATR multiplier for SuperTrend", "SuperTrend")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._fast_ma = None
        self._slow_ma = None
        self._bb = None
        self._supertrend = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_bb_supertrend_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._bb = None
        self._supertrend = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(ma_bb_supertrend_strategy, self).OnStarted(time)

        self._fast_ma = SimpleMovingAverage()
        self._fast_ma.Length = int(self._fast_ma_length.Value)

        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = int(self._slow_ma_length.Value)

        self._bb = BollingerBands()
        self._bb.Length = int(self._bb_length.Value)
        self._bb.Width = 2.0

        self._supertrend = SuperTrend()
        self._supertrend.Length = int(self._supertrend_period.Value)
        self._supertrend.Multiplier = float(self._supertrend_factor.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._fast_ma, self._slow_ma, self._bb, self._supertrend, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ma)
            self.DrawIndicator(area, self._slow_ma)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_val, slow_val, bb_val, st_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed or not self._bb.IsFormed or not self._supertrend.IsFormed:
            return

        if fast_val.IsEmpty or slow_val.IsEmpty or bb_val.IsEmpty or st_val.IsEmpty:
            return

        fast = float(IndicatorHelper.ToDecimal(fast_val))
        slow = float(IndicatorHelper.ToDecimal(slow_val))

        if bb_val.UpBand is None or bb_val.LowBand is None:
            return

        upper = float(bb_val.UpBand)
        lower = float(bb_val.LowBand)
        uptrend = st_val.IsUpTrend

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return

        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return

        cooldown = int(self._cooldown_bars.Value)
        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow

        if cross_up and uptrend and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif cross_down and not uptrend and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and (float(candle.ClosePrice) >= upper or not uptrend):
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and (float(candle.ClosePrice) <= lower or uptrend):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return ma_bb_supertrend_strategy()
