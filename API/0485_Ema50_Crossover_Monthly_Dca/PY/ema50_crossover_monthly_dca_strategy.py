import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ema50_crossover_monthly_dca_strategy(Strategy):
    def __init__(self):
        super(ema50_crossover_monthly_dca_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(ema50_crossover_monthly_dca_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(ema50_crossover_monthly_dca_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, rsi, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        ema_v = float(ema_value)
        rsi_v = float(rsi_value)
        close = float(candle.ClosePrice)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = close
            self._prev_ema = ema_v
            return
        bull_cross = self._prev_close > 0 and self._prev_close <= self._prev_ema and close > ema_v
        if bull_cross and rsi_v < 70 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self._prev_close > 0 and self._prev_close >= self._prev_ema and close < ema_v and rsi_v > 30 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and close < ema_v * 0.98:
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and close > ema_v * 1.02:
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        self._prev_close = close
        self._prev_ema = ema_v

    def CreateClone(self):
        return ema50_crossover_monthly_dca_strategy()
