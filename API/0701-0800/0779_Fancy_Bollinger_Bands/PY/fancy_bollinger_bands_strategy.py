import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class fancy_bollinger_bands_strategy(Strategy):
    """
    Buy an upper Bollinger-band close crossover and sell a lower-band crossover.
    Exit when the close crosses back inside the broken band.
    """

    def __init__(self):
        super(fancy_bollinger_bands_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Number of completed closes in each band calculation.", "Bollinger")
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for both bands.", "Bollinger")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Candle timeframe.", "General")
        self._closes = []
        self._previous_close = None
        self._previous_upper = None
        self._previous_lower = None
        self._entry_requested = False
        self._exit_requested = False

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(fancy_bollinger_bands_strategy, self).OnReseted()
        self._closes = []
        self._previous_close = None
        self._previous_upper = None
        self._previous_lower = None
        self._entry_requested = False
        self._exit_requested = False

    def OnStarted2(self, time):
        super(fancy_bollinger_bands_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.Bind(self.on_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        period = int(self._bollinger_period.Value)
        close = float(candle.ClosePrice)
        self._closes.append(close)
        if len(self._closes) > period:
            self._closes.pop(0)
        if len(self._closes) < period:
            return

        mean = sum(self._closes) / period
        variance = sum((price - mean) ** 2 for price in self._closes) / period
        offset = math.sqrt(variance) * float(self._deviation_multiplier.Value)
        upper = mean + offset
        lower = mean - offset

        if self._previous_upper is not None and self._previous_lower is not None:
            previous = self._previous_close
            position = self.Position
            if position != 0:
                self._entry_requested = False
                if not self._exit_requested:
                    if position > 0 and previous >= self._previous_upper and close <= upper:
                        self._exit_requested = True
                        self.SellMarket(position)
                    elif position < 0 and previous <= self._previous_lower and close >= lower:
                        self._exit_requested = True
                        self.BuyMarket(Math.Abs(position))
            else:
                self._exit_requested = False
                if not self._entry_requested:
                    if previous <= self._previous_upper and close > upper:
                        self._entry_requested = True
                        self.BuyMarket(self.Volume)
                    elif previous >= self._previous_lower and close < lower:
                        self._entry_requested = True
                        self.SellMarket(self.Volume)

        self._previous_close = close
        self._previous_upper = upper
        self._previous_lower = lower

    def CreateClone(self):
        return fancy_bollinger_bands_strategy()
