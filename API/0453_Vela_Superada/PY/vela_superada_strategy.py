import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class vela_superada_strategy(Strategy):
    """Vela Superada Strategy."""

    def __init__(self):
        super(vela_superada_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_length = self.Param("EmaLength", 10) \
            .SetDisplay("EMA Length", "EMA period", "Moving Averages")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._ema = None
        self._rsi = None
        self._macd = None
        self._prev_close = 0.0
        self._prev_open = 0.0
        self._prev_macd = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vela_superada_strategy, self).OnReseted()
        self._ema = None
        self._rsi = None
        self._macd = None
        self._prev_close = 0.0
        self._prev_open = 0.0
        self._prev_macd = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(vela_superada_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        self._macd = MovingAverageConvergenceDivergence()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._rsi, self._macd, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_val, rsi_val, macd_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed or not self._rsi.IsFormed or not self._macd.IsFormed:
            self._prev_close = float(candle.ClosePrice)
            self._prev_open = float(candle.OpenPrice)
            self._prev_macd = float(macd_val)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = float(candle.ClosePrice)
            self._prev_open = float(candle.OpenPrice)
            self._prev_macd = float(macd_val)
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        ema = float(ema_val)
        rsi = float(rsi_val)
        macd = float(macd_val)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = close
            self._prev_open = open_price
            self._prev_macd = macd
            return

        if self._prev_close == 0.0:
            self._prev_close = close
            self._prev_open = open_price
            self._prev_macd = macd
            return

        cooldown = int(self._cooldown_bars.Value)

        bullish_reversal = self._prev_close < self._prev_open and close > open_price
        bearish_reversal = self._prev_close > self._prev_open and close < open_price

        macd_rising = macd > self._prev_macd
        macd_falling = macd < self._prev_macd

        if bullish_reversal and close > ema and rsi < 65 and macd_rising and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif bearish_reversal and close < ema and rsi > 35 and macd_falling and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and bearish_reversal and close < ema:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and bullish_reversal and close > ema:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_close = close
        self._prev_open = open_price
        self._prev_macd = macd

    def CreateClone(self):
        return vela_superada_strategy()
