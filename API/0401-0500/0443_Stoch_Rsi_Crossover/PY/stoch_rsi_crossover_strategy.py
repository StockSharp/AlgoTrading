import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class stoch_rsi_crossover_strategy(Strategy):
    """Stochastic RSI Crossover Strategy."""

    def __init__(self):
        super(stoch_rsi_crossover_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 40) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 60) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
        self._ema1_length = self.Param("Ema1Length", 8) \
            .SetDisplay("EMA 1 Length", "Fast EMA length", "Moving Averages")
        self._ema2_length = self.Param("Ema2Length", 14) \
            .SetDisplay("EMA 2 Length", "Medium EMA length", "Moving Averages")
        self._ema3_length = self.Param("Ema3Length", 50) \
            .SetDisplay("EMA 3 Length", "Slow EMA length", "Moving Averages")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._rsi = None
        self._ema1 = None
        self._ema2 = None
        self._ema3 = None
        self._prev_rsi = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stoch_rsi_crossover_strategy, self).OnReseted()
        self._rsi = None
        self._ema1 = None
        self._ema2 = None
        self._ema3 = None
        self._prev_rsi = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(stoch_rsi_crossover_strategy, self).OnStarted2(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        self._ema1 = ExponentialMovingAverage()
        self._ema1.Length = int(self._ema1_length.Value)

        self._ema2 = ExponentialMovingAverage()
        self._ema2.Length = int(self._ema2_length.Value)

        self._ema3 = ExponentialMovingAverage()
        self._ema3.Length = int(self._ema3_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._ema1, self._ema2, self._ema3, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema1)
            self.DrawIndicator(area, self._ema2)
            self.DrawIndicator(area, self._ema3)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_val, ema1_val, ema2_val, ema3_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed or not self._ema1.IsFormed or not self._ema2.IsFormed or not self._ema3.IsFormed:
            self._prev_rsi = float(rsi_val)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = float(rsi_val)
            return

        rsi = float(rsi_val)
        ema1 = float(ema1_val)
        ema2 = float(ema2_val)
        ema3 = float(ema3_val)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_rsi = rsi
            return

        if self._prev_rsi == 0.0:
            self._prev_rsi = rsi
            return

        rsi_os = int(self._rsi_oversold.Value)
        rsi_ob = int(self._rsi_overbought.Value)
        cooldown = int(self._cooldown_bars.Value)

        bullish_ema = ema1 > ema3
        bearish_ema = ema1 < ema3

        rsi_cross_up_oversold = rsi > rsi_os and self._prev_rsi <= rsi_os
        rsi_cross_down_overbought = rsi < rsi_ob and self._prev_rsi >= rsi_ob

        if rsi_cross_up_oversold and bullish_ema and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif rsi_cross_down_overbought and bearish_ema and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and (rsi > rsi_ob or ema1 < ema2):
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and (rsi < rsi_os or ema1 > ema2):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_rsi = rsi

    def CreateClone(self):
        return stoch_rsi_crossover_strategy()
