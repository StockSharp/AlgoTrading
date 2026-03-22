import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class rsi_plus_1200_strategy(Strategy):
    """RSI + 1200 Strategy."""

    def __init__(self):
        super(rsi_plus_1200_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI calculation length", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
        self._ema_length = self.Param("EmaLength", 100) \
            .SetDisplay("EMA Length", "EMA period for trend filter", "Moving Average")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._rsi = None
        self._ema = None
        self._prev_rsi = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_plus_1200_strategy, self).OnReseted()
        self._rsi = None
        self._ema = None
        self._prev_rsi = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(rsi_plus_1200_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed or not self._ema.IsFormed:
            self._prev_rsi = float(rsi_val)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = float(rsi_val)
            return

        rsi = float(rsi_val)
        ema = float(ema_val)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_rsi = rsi
            return

        if self._prev_rsi == 0.0:
            self._prev_rsi = rsi
            return

        rsi_ob = int(self._rsi_overbought.Value)
        rsi_os = int(self._rsi_oversold.Value)
        cooldown = int(self._cooldown_bars.Value)
        close = float(candle.ClosePrice)

        rsi_cross_up_oversold = rsi > rsi_os and self._prev_rsi <= rsi_os
        rsi_cross_down_overbought = rsi < rsi_ob and self._prev_rsi >= rsi_ob

        if rsi_cross_up_oversold and close > ema and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif rsi_cross_down_overbought and close < ema and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and rsi > rsi_ob:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and rsi < rsi_os:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_rsi = rsi

    def CreateClone(self):
        return rsi_plus_1200_strategy()
