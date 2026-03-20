import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class candle245_breakout_strategy(Strategy):
    def __init__(self):
        super(candle245_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ref_period = self.Param("RefPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Ref Period", "Every N bars capture reference candle", "Trading")
        self._look_forward_bars = self.Param("LookForwardBars", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Look Forward Bars", "Bars to watch for breakout", "Trading")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA period for trend filter", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._ref_high = 0.0
        self._ref_low = 0.0
        self._bars_left = 0
        self._bar_count = 0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def ref_period(self):
        return self._ref_period.Value
    @property
    def look_forward_bars(self):
        return self._look_forward_bars.Value
    @property
    def ema_length(self):
        return self._ema_length.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(candle245_breakout_strategy, self).OnReseted()
        self._ref_high = 0.0
        self._ref_low = 0.0
        self._bars_left = 0
        self._bar_count = 0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(candle245_breakout_strategy, self).OnStarted(time)
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .Bind(self._ema, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._ema.IsFormed:
            return

        self._bar_count += 1

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            if self._bars_left > 0:
                self._bars_left -= 1
            return

        if self._bar_count % self.ref_period == 0:
            self._ref_high = float(candle.HighPrice)
            self._ref_low = float(candle.LowPrice)
            self._bars_left = self.look_forward_bars
            return

        if self._bars_left <= 0:
            return

        self._bars_left -= 1

        price = float(candle.ClosePrice)
        ema_v = float(ema_val)

        if price > self._ref_high and price > ema_v and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif price < self._ref_low and price < ema_v and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars

        if self._bars_left == 0 and self.Position != 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            else:
                self.BuyMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return candle245_breakout_strategy()
