import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class omar_mmr_strategy(Strategy):
    """Omar MMR Strategy.
    Uses RSI, triple EMA alignment, and EMA A/B crossover for entries.
    Buys when price > EMA C, EMA A > EMA B, EMA A crosses above EMA B, RSI in range.
    Sells when EMA alignment reverses or EMA A crosses below EMA B.
    """

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

        self._prev_ema_a = 0.0
        self._prev_ema_b = 0.0
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(omar_mmr_strategy, self).OnReseted()
        self._prev_ema_a = 0.0
        self._prev_ema_b = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(omar_mmr_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        ema_a = ExponentialMovingAverage()
        ema_a.Length = self._ema_a_length.Value
        ema_b = ExponentialMovingAverage()
        ema_b.Length = self._ema_b_length.Value
        ema_c = ExponentialMovingAverage()
        ema_c.Length = self._ema_c_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, ema_a, ema_b, ema_c, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_a)
            self.DrawIndicator(area, ema_b)
            self.DrawIndicator(area, ema_c)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val, ema_a_val, ema_b_val, ema_c_val):
        if candle.State != CandleStates.Finished:
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

        # EMA alignment
        bullish_alignment = ema_a > ema_b and close > ema_c
        bearish_alignment = ema_a < ema_b and close < ema_c

        # EMA A/B crossover
        ema_cross_up = ema_a > ema_b and self._prev_ema_a <= self._prev_ema_b
        ema_cross_down = ema_a < ema_b and self._prev_ema_a >= self._prev_ema_b

        # RSI filter
        rsi_in_range = rsi > 30 and rsi < 70

        # Buy: bullish EMA alignment + EMA cross up + RSI in range
        if bullish_alignment and ema_cross_up and rsi_in_range and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Sell: bearish EMA alignment + EMA cross down + RSI in range
        elif bearish_alignment and ema_cross_down and rsi_in_range and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit long: EMA A crosses below EMA B
        elif self.Position > 0 and ema_cross_down:
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit short: EMA A crosses above EMA B
        elif self.Position < 0 and ema_cross_up:
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value

        self._prev_ema_a = ema_a
        self._prev_ema_b = ema_b

    def CreateClone(self):
        return omar_mmr_strategy()
