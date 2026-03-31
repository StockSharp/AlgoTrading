import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, ExponentialMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class supertrend_ema_rebound_strategy(Strategy):
    """Supertrend + EMA Rebound Strategy."""

    def __init__(self):
        super(supertrend_ema_rebound_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._atr_period = self.Param("AtrPeriod", 10) \
            .SetDisplay("ATR Period", "ATR period for Supertrend", "Supertrend")
        self._atr_factor = self.Param("AtrFactor", 3.0) \
            .SetDisplay("ATR Factor", "ATR factor for Supertrend", "Supertrend")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period", "Moving Average")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._supertrend = None
        self._ema = None
        self._prev_is_up_trend = False
        self._prev_is_up_trend_set = False
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(supertrend_ema_rebound_strategy, self).OnReseted()
        self._supertrend = None
        self._ema = None
        self._prev_is_up_trend = False
        self._prev_is_up_trend_set = False
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(supertrend_ema_rebound_strategy, self).OnStarted2(time)

        self._supertrend = SuperTrend()
        self._supertrend.Length = int(self._atr_period.Value)
        self._supertrend.Multiplier = float(self._atr_factor.Value)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._supertrend, self._ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._supertrend)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, st_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._supertrend.IsFormed or not self._ema.IsFormed:
            return

        if st_value.IsEmpty or ema_value.IsEmpty:
            return

        is_up_trend = st_value.IsUpTrend
        ema_val = float(IndicatorHelper.ToDecimal(ema_value))

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_is_up_trend = is_up_trend
            self._prev_is_up_trend_set = True
            self._prev_close = float(candle.ClosePrice)
            self._prev_ema = ema_val
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_is_up_trend = is_up_trend
            self._prev_is_up_trend_set = True
            self._prev_close = float(candle.ClosePrice)
            self._prev_ema = ema_val
            return

        if not self._prev_is_up_trend_set or self._prev_close == 0.0:
            self._prev_is_up_trend = is_up_trend
            self._prev_is_up_trend_set = True
            self._prev_close = float(candle.ClosePrice)
            self._prev_ema = ema_val
            return

        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        trend_turned_up = is_up_trend and not self._prev_is_up_trend
        trend_turned_down = not is_up_trend and self._prev_is_up_trend

        ema_rebound_up = is_up_trend and self._prev_close < self._prev_ema and close > ema_val
        ema_rebound_down = not is_up_trend and self._prev_close > self._prev_ema and close < ema_val

        if (trend_turned_up or ema_rebound_up) and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif (trend_turned_down or ema_rebound_down) and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and trend_turned_down:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and trend_turned_up:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_is_up_trend = is_up_trend
        self._prev_is_up_trend_set = True
        self._prev_close = close
        self._prev_ema = ema_val

    def CreateClone(self):
        return supertrend_ema_rebound_strategy()
