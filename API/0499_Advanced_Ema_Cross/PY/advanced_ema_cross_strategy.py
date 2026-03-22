import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageDirectionalIndex, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class advanced_ema_cross_strategy(Strategy):
    def __init__(self):
        super(advanced_ema_cross_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_short_length = self.Param("EmaShortLength", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Short Length", "Short EMA period", "EMA")
        self._ema_long_length = self.Param("EmaLongLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Long Length", "Long EMA period", "EMA")
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "ADX calculation period", "ADX")
        self._adx_high_level = self.Param("AdxHighLevel", 20.0) \
            .SetDisplay("ADX Level", "ADX threshold for trending market", "ADX")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_ema_short = 0.0
        self._prev_ema_long = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(advanced_ema_cross_strategy, self).OnReseted()
        self._prev_ema_short = 0.0
        self._prev_ema_long = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(advanced_ema_cross_strategy, self).OnStarted(time)
        ema_short = ExponentialMovingAverage()
        ema_short.Length = int(self._ema_short_length.Value)
        ema_long = ExponentialMovingAverage()
        ema_long.Length = int(self._ema_long_length.Value)
        adx = AverageDirectionalIndex()
        adx.Length = int(self._adx_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema_short, ema_long, adx, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_short)
            self.DrawIndicator(area, ema_long)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_short_value, ema_long_value, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema_s = float(IndicatorHelper.ToDecimal(ema_short_value))
        ema_l = float(IndicatorHelper.ToDecimal(ema_long_value))

        adx_ma = adx_value.MovingAverage
        if adx_ma is None:
            self._prev_ema_short = ema_s
            self._prev_ema_long = ema_l
            return

        adx_v = float(adx_ma)

        if self._prev_ema_short == 0:
            self._prev_ema_short = ema_s
            self._prev_ema_long = ema_l
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_ema_short = ema_s
            self._prev_ema_long = ema_l
            return

        cooldown = int(self._cooldown_bars.Value)
        crossover = self._prev_ema_short <= self._prev_ema_long and ema_s > ema_l
        crossunder = self._prev_ema_short >= self._prev_ema_long and ema_s < ema_l
        trending = adx_v > float(self._adx_high_level.Value)

        if crossover and trending and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif crossunder and trending and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        self._prev_ema_short = ema_s
        self._prev_ema_long = ema_l

    def CreateClone(self):
        return advanced_ema_cross_strategy()
