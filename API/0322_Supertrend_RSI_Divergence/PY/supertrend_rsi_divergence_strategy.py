import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SuperTrend, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class supertrend_rsi_divergence_strategy(Strategy):
    """
    Strategy that uses Supertrend indicator along with RSI to identify trading opportunities.
    """

    def __init__(self):
        super(supertrend_rsi_divergence_strategy, self).__init__()

        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetDisplay("Supertrend Period", "Supertrend ATR period", "Supertrend") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Supertrend") \
            .SetCanOptimize(True) \
            .SetOptimize(2.0, 5.0, 0.5)

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period for divergence detection", "RSI") \
            .SetCanOptimize(True) \
            .SetOptimize(8, 20, 2)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._supertrend = None
        self._rsi = None
        self._supertrend_value = 0.0

    @property
    def SupertrendPeriod(self):
        return self._supertrend_period.Value

    @SupertrendPeriod.setter
    def SupertrendPeriod(self, value):
        self._supertrend_period.Value = value

    @property
    def SupertrendMultiplier(self):
        return self._supertrend_multiplier.Value

    @SupertrendMultiplier.setter
    def SupertrendMultiplier(self, value):
        self._supertrend_multiplier.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(supertrend_rsi_divergence_strategy, self).OnReseted()
        self._supertrend = None
        self._rsi = None
        self._supertrend_value = 0.0

    def OnStarted(self, time):
        super(supertrend_rsi_divergence_strategy, self).OnStarted(time)

        self._supertrend = SuperTrend()
        self._supertrend.Length = self.SupertrendPeriod
        self._supertrend.Multiplier = self.SupertrendMultiplier

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._supertrend, self._rsi, self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._supertrend)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, supertrend_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        self._supertrend_value = float(supertrend_value)
        rsi = float(rsi_value)

        if self.Position != 0:
            return

        if float(candle.ClosePrice) > self._supertrend_value and rsi < 60.0:
            self.BuyMarket()
        elif float(candle.ClosePrice) < self._supertrend_value and rsi > 40.0:
            self.SellMarket()

    def CreateClone(self):
        return supertrend_rsi_divergence_strategy()
