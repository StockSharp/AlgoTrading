import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class keltner_macd_strategy(Strategy):
    """
    Strategy based on Keltner Channels and MACD.
    Enters long when price breaks above upper Keltner Channel with MACD cross above Signal.
    Enters short when price breaks below lower Keltner Channel with MACD cross below Signal.
    Exits when MACD crosses its signal line in the opposite direction.
    """

    def __init__(self):
        super(keltner_macd_strategy, self).__init__()

        self._emaPeriod = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Period for EMA calculation in Keltner Channel", "Indicators")

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for Keltner Channel bands", "Indicators")

        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation in Keltner Channel", "Indicators")

        self._macdFastPeriod = self.Param("MacdFastPeriod", 12) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD calculation", "Indicators")

        self._macdSlowPeriod = self.Param("MacdSlowPeriod", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD calculation", "Indicators")

        self._macdSignalPeriod = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("MACD Signal Period", "Signal line period for MACD calculation", "Indicators")

        self._cooldownBars = self.Param("CooldownBars", 20) \
            .SetRange(1, 200) \
            .SetDisplay("Cooldown Bars", "Bars between entries", "General")

        self._atrMultiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("Stop Loss ATR Multiplier", "ATR multiplier for stop loss calculation", "Risk Management")

        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Timeframe of data for strategy", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

        self._prevMacd = 0.0
        self._prevSignal = 0.0
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candleType.Value

    def OnReseted(self):
        super(keltner_macd_strategy, self).OnReseted()
        self._prevMacd = 0.0
        self._prevSignal = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(keltner_macd_strategy, self).OnStarted2(time)
        self._prevMacd = 0.0
        self._prevSignal = 0.0
        self._cooldown = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._emaPeriod.Value

        atr = AverageTrueRange()
        atr.Length = self._atrPeriod.Value

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macdFastPeriod.Value
        macd.Macd.LongMa.Length = self._macdSlowPeriod.Value
        macd.SignalMa.Length = self._macdSignalPeriod.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ema, atr, macd, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)

            macdArea = self.CreateChartArea()
            if macdArea is not None:
                self.DrawIndicator(macdArea, macd)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_value, atr_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        ema = float(ema_value)
        atr = float(atr_value)

        multiplier = float(self._multiplier.Value)
        upperBand = ema + multiplier * atr
        lowerBand = ema - multiplier * atr

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        macd = float(macd_value.Macd)
        signal = float(macd_value.Signal)

        macdCrossedAboveSignal = self._prevMacd <= self._prevSignal and macd > signal
        macdCrossedBelowSignal = self._prevMacd >= self._prevSignal and macd < signal

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prevMacd = macd
            self._prevSignal = signal
            return

        if self._cooldown > 0:
            self._cooldown -= 1

        cooldown_val = int(self._cooldownBars.Value)

        if self._cooldown == 0 and float(candle.ClosePrice) > upperBand * 1.001 and macdCrossedAboveSignal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._cooldown = cooldown_val
        elif self._cooldown == 0 and float(candle.ClosePrice) < lowerBand * 0.999 and macdCrossedBelowSignal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._cooldown = cooldown_val

        if self.Position > 0 and macdCrossedBelowSignal:
            self.ClosePosition()
            self._cooldown = cooldown_val
        elif self.Position < 0 and macdCrossedAboveSignal:
            self.ClosePosition()
            self._cooldown = cooldown_val

        self._prevMacd = macd
        self._prevSignal = signal

    def CreateClone(self):
        return keltner_macd_strategy()
