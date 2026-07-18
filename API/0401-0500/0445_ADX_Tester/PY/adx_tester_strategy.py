import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, ExponentialMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class adx_tester_strategy(Strategy):
    """ADX Tester Strategy."""

    def __init__(self):
        super(adx_tester_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._adx_length = self.Param("AdxLength", 14) \
            .SetDisplay("ADX Length", "ADX/DI period", "ADX")
        self._adx_key_level = self.Param("AdxKeyLevel", 20) \
            .SetDisplay("ADX Key Level", "Minimum ADX level for trending", "ADX")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "Momentum EMA period", "Momentum")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._adx = None
        self._ema = None
        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_tester_strategy, self).OnReseted()
        self._adx = None
        self._ema = None
        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(adx_tester_strategy, self).OnStarted2(time)

        self._adx = AverageDirectionalIndex()
        self._adx.Length = int(self._adx_length.Value)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._adx, self._ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, adx_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._adx.IsFormed or not self._ema.IsFormed:
            return

        if adx_value.IsEmpty or ema_value.IsEmpty:
            return

        ema_val = float(IndicatorHelper.ToDecimal(ema_value))

        if adx_value.MovingAverage is None:
            self._prev_prev_ema = self._prev_ema
            self._prev_ema = ema_val
            return

        adx_val = float(adx_value.MovingAverage)

        dx_value = adx_value.Dx
        di_plus = None
        di_minus = None
        if dx_value is not None:
            di_plus = dx_value.Plus
            di_minus = dx_value.Minus

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_prev_ema = self._prev_ema
            self._prev_ema = ema_val
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_prev_ema = self._prev_ema
            self._prev_ema = ema_val
            return

        if self._prev_ema == 0.0 or self._prev_prev_ema == 0.0:
            self._prev_prev_ema = self._prev_ema
            self._prev_ema = ema_val
            return

        cooldown = int(self._cooldown_bars.Value)
        key_level = int(self._adx_key_level.Value)

        momentum_rising = ema_val > self._prev_ema and self._prev_ema > self._prev_prev_ema
        momentum_falling = ema_val < self._prev_ema and self._prev_ema < self._prev_prev_ema

        strong_trend = adx_val > key_level

        bullish = strong_trend and momentum_rising
        if di_plus is not None and di_minus is not None:
            bullish = bullish and float(di_plus) > float(di_minus)

        bearish = strong_trend and momentum_falling
        if di_plus is not None and di_minus is not None:
            bearish = bearish and float(di_minus) > float(di_plus)

        if bullish and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif bearish and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and ema_val < self._prev_ema:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and ema_val > self._prev_ema:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_prev_ema = self._prev_ema
        self._prev_ema = ema_val

    def CreateClone(self):
        return adx_tester_strategy()
