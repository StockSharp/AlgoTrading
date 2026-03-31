import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class rsi_ema_strategy(Strategy):
    """RSI + EMA Strategy."""

    def __init__(self):
        super(rsi_ema_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI calculation length", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
        self._ma1_length = self.Param("Ma1Length", 20) \
            .SetDisplay("MA1 Length", "Fast EMA length", "Moving Averages")
        self._ma2_length = self.Param("Ma2Length", 50) \
            .SetDisplay("MA2 Length", "Slow EMA length", "Moving Averages")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._rsi = None
        self._ma1 = None
        self._ma2 = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_ema_strategy, self).OnReseted()
        self._rsi = None
        self._ma1 = None
        self._ma2 = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(rsi_ema_strategy, self).OnStarted2(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        self._ma1 = ExponentialMovingAverage()
        self._ma1.Length = int(self._ma1_length.Value)

        self._ma2 = ExponentialMovingAverage()
        self._ma2.Length = int(self._ma2_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._ma1, self._ma2, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma1)
            self.DrawIndicator(area, self._ma2)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_val, ma1_val, ma2_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed or not self._ma1.IsFormed or not self._ma2.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        rsi = float(rsi_val)
        ma1 = float(ma1_val)
        ma2 = float(ma2_val)
        rsi_ob = int(self._rsi_overbought.Value)
        rsi_os = int(self._rsi_oversold.Value)
        cooldown = int(self._cooldown_bars.Value)

        uptrend = ma1 > ma2
        downtrend = ma1 < ma2

        if rsi < rsi_os and uptrend and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif rsi > rsi_ob and downtrend and self.Position >= 0:
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

    def CreateClone(self):
        return rsi_ema_strategy()
