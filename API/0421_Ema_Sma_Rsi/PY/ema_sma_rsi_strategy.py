import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ema_sma_rsi_strategy(Strategy):
    """EMA/SMA + RSI Strategy.
    Uses three EMAs for trend and crossover, with RSI for exit signals."""

    def __init__(self):
        super(ema_sma_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._ema_a_length = self.Param("EmaALength", 10) \
            .SetDisplay("EMA A Length", "Fast EMA period", "Moving Averages")
        self._ema_b_length = self.Param("EmaBLength", 20) \
            .SetDisplay("EMA B Length", "Medium EMA period", "Moving Averages")
        self._ema_c_length = self.Param("EmaCLength", 50) \
            .SetDisplay("EMA C Length", "Slow EMA period", "Moving Averages")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._ema_a = None
        self._ema_b = None
        self._ema_c = None
        self._rsi = None
        self._prev_ema_a = 0.0
        self._prev_ema_b = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_sma_rsi_strategy, self).OnReseted()
        self._ema_a = None
        self._ema_b = None
        self._ema_c = None
        self._rsi = None
        self._prev_ema_a = 0.0
        self._prev_ema_b = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(ema_sma_rsi_strategy, self).OnStarted(time)

        self._ema_a = ExponentialMovingAverage()
        self._ema_a.Length = int(self._ema_a_length.Value)

        self._ema_b = ExponentialMovingAverage()
        self._ema_b.Length = int(self._ema_b_length.Value)

        self._ema_c = ExponentialMovingAverage()
        self._ema_c.Length = int(self._ema_c_length.Value)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema_a, self._ema_b, self._ema_c, self._rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema_a)
            self.DrawIndicator(area, self._ema_b)
            self.DrawIndicator(area, self._ema_c)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_a_val, ema_b_val, ema_c_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema_a.IsFormed or not self._ema_b.IsFormed or not self._ema_c.IsFormed or not self._rsi.IsFormed:
            self._prev_ema_a = float(ema_a_val)
            self._prev_ema_b = float(ema_b_val)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_ema_a = float(ema_a_val)
            self._prev_ema_b = float(ema_b_val)
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_ema_a = float(ema_a_val)
            self._prev_ema_b = float(ema_b_val)
            return

        ea = float(ema_a_val)
        eb = float(ema_b_val)
        ec = float(ema_c_val)
        rsi = float(rsi_val)
        cooldown = int(self._cooldown_bars.Value)

        bullish_cross = ea > eb and self._prev_ema_a <= self._prev_ema_b and self._prev_ema_a > 0
        bearish_cross = ea < eb and self._prev_ema_a >= self._prev_ema_b and self._prev_ema_a > 0

        if self.Position > 0 and rsi > 70:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and rsi < 30:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif bullish_cross and ea > ec and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif bearish_cross and ea < ec and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        self._prev_ema_a = ea
        self._prev_ema_b = eb

    def CreateClone(self):
        return ema_sma_rsi_strategy()
