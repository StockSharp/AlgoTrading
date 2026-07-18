import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class omar_mmr_strategy(Strategy):
    """Omar MMR Strategy."""

    def __init__(self):
        super(omar_mmr_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._ema_a_length = self.Param("EmaALength", 20) \
            .SetDisplay("EMA A Length", "Fast EMA period", "Moving Averages")
        self._ema_b_length = self.Param("EmaBLength", 50) \
            .SetDisplay("EMA B Length", "Medium EMA period", "Moving Averages")
        self._ema_c_length = self.Param("EmaCLength", 200) \
            .SetDisplay("EMA C Length", "Slow EMA period", "Moving Averages")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._rsi = None
        self._ema_a = None
        self._ema_b = None
        self._ema_c = None
        self._prev_ema_a = 0.0
        self._prev_ema_b = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(omar_mmr_strategy, self).OnReseted()
        self._rsi = None
        self._ema_a = None
        self._ema_b = None
        self._ema_c = None
        self._prev_ema_a = 0.0
        self._prev_ema_b = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(omar_mmr_strategy, self).OnStarted2(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        self._ema_a = ExponentialMovingAverage()
        self._ema_a.Length = int(self._ema_a_length.Value)

        self._ema_b = ExponentialMovingAverage()
        self._ema_b.Length = int(self._ema_b_length.Value)

        self._ema_c = ExponentialMovingAverage()
        self._ema_c.Length = int(self._ema_c_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._ema_a, self._ema_b, self._ema_c, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema_a)
            self.DrawIndicator(area, self._ema_b)
            self.DrawIndicator(area, self._ema_c)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_val, ema_a_val, ema_b_val, ema_c_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed or not self._ema_a.IsFormed or not self._ema_b.IsFormed or not self._ema_c.IsFormed:
            self._prev_ema_a = float(ema_a_val)
            self._prev_ema_b = float(ema_b_val)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_ema_a = float(ema_a_val)
            self._prev_ema_b = float(ema_b_val)
            return

        ema_a = float(ema_a_val)
        ema_b = float(ema_b_val)
        ema_c = float(ema_c_val)
        rsi = float(rsi_val)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_ema_a = ema_a
            self._prev_ema_b = ema_b
            return

        if self._prev_ema_a == 0.0 or self._prev_ema_b == 0.0:
            self._prev_ema_a = ema_a
            self._prev_ema_b = ema_b
            return

        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        bullish_alignment = ema_a > ema_b and close > ema_c
        bearish_alignment = ema_a < ema_b and close < ema_c

        ema_cross_up = ema_a > ema_b and self._prev_ema_a <= self._prev_ema_b
        ema_cross_down = ema_a < ema_b and self._prev_ema_a >= self._prev_ema_b

        rsi_in_buy_range = rsi > 30 and rsi < 70
        rsi_in_sell_range = rsi > 30 and rsi < 70

        if bullish_alignment and ema_cross_up and rsi_in_buy_range and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif bearish_alignment and ema_cross_down and rsi_in_sell_range and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and ema_cross_down:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and ema_cross_up:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_ema_a = ema_a
        self._prev_ema_b = ema_b

    def CreateClone(self):
        return omar_mmr_strategy()
