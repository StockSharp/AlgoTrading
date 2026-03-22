import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, SuperTrend, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class stoch_rsi_supertrend_strategy(Strategy):
    """Stochastic RSI + Supertrend Strategy."""

    def __init__(self):
        super(stoch_rsi_supertrend_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "Trend EMA period", "Moving Average")
        self._supertrend_length = self.Param("SupertrendLength", 11) \
            .SetDisplay("SuperTrend Length", "SuperTrend ATR period", "SuperTrend")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 2.0) \
            .SetDisplay("SuperTrend Multiplier", "SuperTrend ATR multiplier", "SuperTrend")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._rsi = None
        self._ema = None
        self._supertrend = None
        self._prev_rsi = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stoch_rsi_supertrend_strategy, self).OnReseted()
        self._rsi = None
        self._ema = None
        self._supertrend = None
        self._prev_rsi = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(stoch_rsi_supertrend_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        self._supertrend = SuperTrend()
        self._supertrend.Length = int(self._supertrend_length.Value)
        self._supertrend.Multiplier = float(self._supertrend_multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._rsi, self._ema, self._supertrend, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawIndicator(area, self._supertrend)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_value, ema_value, st_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed or not self._ema.IsFormed or not self._supertrend.IsFormed:
            return

        if rsi_value.IsEmpty or ema_value.IsEmpty or st_value.IsEmpty:
            return

        rsi_val = float(IndicatorHelper.ToDecimal(rsi_value))
        ema_val = float(IndicatorHelper.ToDecimal(ema_value))
        is_up_trend = st_value.IsUpTrend

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rsi_val
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_rsi = rsi_val
            return

        if self._prev_rsi == 0.0:
            self._prev_rsi = rsi_val
            return

        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        rsi_cross_up_oversold = rsi_val > 40 and self._prev_rsi <= 40
        rsi_cross_down_overbought = rsi_val < 60 and self._prev_rsi >= 60

        if rsi_cross_up_oversold and is_up_trend and close > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif rsi_cross_down_overbought and not is_up_trend and close < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and not is_up_trend:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and is_up_trend:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_rsi = rsi_val

    def CreateClone(self):
        return stoch_rsi_supertrend_strategy()
