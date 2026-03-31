import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ema50_crossover_monthly_dca_strategy(Strategy):
    """50 EMA Crossover Strategy."""

    def __init__(self):
        super(ema50_crossover_monthly_dca_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._ema = None
        self._rsi = None
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema50_crossover_monthly_dca_strategy, self).OnReseted()
        self._ema = None
        self._rsi = None
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(ema50_crossover_monthly_dca_strategy, self).OnStarted2(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed or not self._rsi.IsFormed:
            self._prev_close = float(candle.ClosePrice)
            self._prev_ema = float(ema_value)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = float(candle.ClosePrice)
            self._prev_ema = float(ema_value)
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = float(candle.ClosePrice)
            self._prev_ema = float(ema_value)
            return

        close = float(candle.ClosePrice)
        ema_v = float(ema_value)
        rsi_v = float(rsi_value)
        cooldown = int(self._cooldown_bars.Value)

        bull_cross = self._prev_close > 0 and self._prev_close <= self._prev_ema and close > ema_v
        if bull_cross and rsi_v < 70 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self._prev_close > 0 and self._prev_close >= self._prev_ema and close < ema_v and rsi_v > 30 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and close < ema_v * 0.98:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and close > ema_v * 1.02:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_close = close
        self._prev_ema = ema_v

    def CreateClone(self):
        return ema50_crossover_monthly_dca_strategy()
