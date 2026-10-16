import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Strategies import Strategy


class fibonacci_bollinger_bands_strategy(Strategy):
    """Long-only Bollinger/Fibonacci entry with a previous-swing-high exit."""

    def __init__(self):
        super(fibonacci_bollinger_bands_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Number of closes in Bollinger bands.", "Bollinger")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Population standard-deviation multiplier.", "Bollinger")
        self._fib_lookback = self.Param("FibLookback", 50).SetGreaterThanZero() \
            .SetDisplay("Fib Lookback", "Previous swing bars (current excluded).", "Fibonacci")
        self._fibonacci_level = self.Param("FibonacciLevel", 0.618).SetGreaterThanZero() \
            .SetDisplay("Fibonacci Level", "Fraction of swing range above the prior low.", "Fibonacci")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Candle timeframe.", "General")
        self._closes = []
        self._previous_highs = []
        self._previous_lows = []
        self._pending_order = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(fibonacci_bollinger_bands_strategy, self).OnReseted()
        self._closes = []
        self._previous_highs = []
        self._previous_lows = []
        self._pending_order = None

    def OnStarted2(self, time):
        super(fibonacci_bollinger_bands_strategy, self).OnStarted2(time)
        if float(self._fibonacci_level.Value) >= 1.0:
            raise ValueError("FibonacciLevel must be smaller than one")
        self.OrderRegistering += self._on_order_registering
        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.Bind(self.on_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_order_registering(self, order):
        self._pending_order = order

    def on_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        band_length = int(self._bollinger_period.Value)
        lookback = int(self._fib_lookback.Value)
        self._closes.append(close)
        if len(self._closes) > band_length:
            self._closes.pop(0)

        active = self._pending_order is not None and self._pending_order.State not in (
            OrderStates.Done, OrderStates.Failed)
        if len(self._closes) == band_length and len(self._previous_highs) == lookback and not active:
            mean = sum(self._closes) / band_length
            variance = sum((value - mean) ** 2 for value in self._closes) / band_length
            offset = math.sqrt(variance) * float(self._bollinger_deviation.Value)
            upper = mean + offset
            lower = mean - offset
            high = max(self._previous_highs)
            low = min(self._previous_lows)
            fib = low + float(self._fibonacci_level.Value) * (high - low)
            if self.Position > 0 and (close >= upper or close >= high):
                self.SellMarket(self.Position)
            elif self.Position == 0 and close < lower and close > fib:
                self.BuyMarket(self.Volume)

        self._previous_highs.append(float(candle.HighPrice))
        self._previous_lows.append(float(candle.LowPrice))
        if len(self._previous_highs) > lookback:
            self._previous_highs.pop(0)
        if len(self._previous_lows) > lookback:
            self._previous_lows.pop(0)

    def CreateClone(self):
        return fibonacci_bollinger_bands_strategy()
