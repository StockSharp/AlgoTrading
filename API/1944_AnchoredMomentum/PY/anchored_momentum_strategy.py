import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class anchored_momentum_strategy(Strategy):
    """
    Strategy based on the Anchored Momentum indicator.
    Calculates the ratio between EMA and SMA to detect trend strength.
    Opens long when momentum rises above upper level, short when below lower level.
    """

    def __init__(self):
        super(anchored_momentum_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Period", "Period of the simple moving average", "Indicator")
        self._ema_period = self.Param("EmaPeriod", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period of the exponential moving average", "Indicator")
        self._up_level = self.Param("UpLevel", 0.025) \
            .SetDisplay("Upper Level", "Upper threshold for momentum", "Indicator")
        self._down_level = self.Param("DownLevel", -0.025) \
            .SetDisplay("Lower Level", "Lower threshold for momentum", "Indicator")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles used by the strategy", "General")
        self._buy_enabled = self.Param("BuyEnabled", True) \
            .SetDisplay("Enable Buy", "Allow opening long positions", "Trading")
        self._sell_enabled = self.Param("SellEnabled", True) \
            .SetDisplay("Enable Sell", "Allow opening short positions", "Trading")

        self._previous_momentum = 0.0
        self._is_first_value = True

    @property
    def SmaPeriod(self): return self._sma_period.Value
    @SmaPeriod.setter
    def SmaPeriod(self, v): self._sma_period.Value = v
    @property
    def EmaPeriod(self): return self._ema_period.Value
    @EmaPeriod.setter
    def EmaPeriod(self, v): self._ema_period.Value = v
    @property
    def UpLevel(self): return self._up_level.Value
    @UpLevel.setter
    def UpLevel(self, v): self._up_level.Value = v
    @property
    def DownLevel(self): return self._down_level.Value
    @DownLevel.setter
    def DownLevel(self, v): self._down_level.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def BuyEnabled(self): return self._buy_enabled.Value
    @BuyEnabled.setter
    def BuyEnabled(self, v): self._buy_enabled.Value = v
    @property
    def SellEnabled(self): return self._sell_enabled.Value
    @SellEnabled.setter
    def SellEnabled(self, v): self._sell_enabled.Value = v

    def OnReseted(self):
        super(anchored_momentum_strategy, self).OnReseted()
        self._previous_momentum = 0.0
        self._is_first_value = True

    def OnStarted(self, time):
        super(anchored_momentum_strategy, self).OnStarted(time)

        sma = SimpleMovingAverage()
        sma.Length = self.SmaPeriod
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, sma_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        momentum = 0.0 if sma_value == 0 else 100.0 * (ema_value / sma_value - 1.0)

        if self._is_first_value:
            self._previous_momentum = momentum
            self._is_first_value = False
            return

        if self._previous_momentum <= self.UpLevel and momentum > self.UpLevel:
            if self.SellEnabled and self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            if self.BuyEnabled and self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif self._previous_momentum >= self.DownLevel and momentum < self.DownLevel:
            if self.BuyEnabled and self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            if self.SellEnabled and self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))

        self._previous_momentum = momentum

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return anchored_momentum_strategy()
