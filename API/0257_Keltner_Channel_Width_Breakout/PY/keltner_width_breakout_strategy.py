import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class keltner_width_breakout_strategy(Strategy):
    """
    Strategy that trades on Keltner Channel width breakouts.
    When Keltner Channel width increases significantly above its average,
    it enters position in the direction determined by price movement.
    """

    def __init__(self):
        super(keltner_width_breakout_strategy, self).__init__()

        self._emaPeriod = self.Param("EMAPeriod", 20) \
            .SetDisplay("EMA Period", "Period of EMA for Keltner Channel", "Indicators")

        self._atrPeriod = self.Param("ATRPeriod", 14) \
            .SetDisplay("ATR Period", "Period of ATR for Keltner Channel", "Indicators")

        self._atrMultiplier = self.Param("ATRMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channel", "Indicators")

        self._widthThreshold = self.Param("WidthThreshold", 1.2) \
            .SetDisplay("Width Threshold", "Threshold multiplier for width breakout detection", "Trading")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._widthAverage = None

    @property
    def EMAPeriod(self):
        return self._emaPeriod.Value

    @EMAPeriod.setter
    def EMAPeriod(self, value):
        self._emaPeriod.Value = value

    @property
    def ATRPeriod(self):
        return self._atrPeriod.Value

    @ATRPeriod.setter
    def ATRPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def ATRMultiplier(self):
        return self._atrMultiplier.Value

    @ATRMultiplier.setter
    def ATRMultiplier(self, value):
        self._atrMultiplier.Value = value

    @property
    def WidthThreshold(self):
        return self._widthThreshold.Value

    @WidthThreshold.setter
    def WidthThreshold(self, value):
        self._widthThreshold.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(keltner_width_breakout_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(keltner_width_breakout_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.EMAPeriod
        atr = AverageTrueRange()
        atr.Length = self.ATRPeriod
        ema_period = self.EMAPeriod
        self._widthAverage = SimpleMovingAverage()
        self._widthAverage.Length = max(5, ema_period // 2)

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished or atr_value <= 0:
            return

        # Keltner width = 2 * ATR * multiplier
        width = 2.0 * self.ATRMultiplier * float(atr_value)
        ema_val = float(ema_value)

        # Process width through average
        avg_result = process_float(self._widthAverage, width, candle.ServerTime, True)

        if not self._widthAverage.IsFormed:
            return

        avg_width = float(avg_result)
        if avg_width <= 0:
            return

        # Width breakout detection
        if width > avg_width * self.WidthThreshold and self.Position == 0:
            # Determine direction based on price relative to EMA
            if float(candle.ClosePrice) > ema_val:
                self.BuyMarket()
            elif float(candle.ClosePrice) < ema_val:
                self.SellMarket()

    def CreateClone(self):
        return keltner_width_breakout_strategy()
