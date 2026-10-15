import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class bollinger_bands_fibonacci_strategy(Strategy):
    """
    Bollinger Bands and Fibonacci strategy.
    The Fibonacci levels lie at FibonacciLevel0 and FibonacciLevel100 of the range between the lowest low and the highest high of
    the last FibonacciLength candles. A close crossing above the upper Bollinger band with the low above the Fibonacci low goes long,
    a close crossing below the lower band with the high below the Fibonacci high goes short, reversing an opposite position.
    A long closes when the close crosses below the middle band and a short when it crosses above it.
    """

    def __init__(self):
        super(bollinger_bands_fibonacci_strategy, self).__init__()
        self._bollinger_length = self.Param("BollingerLength", 20).SetGreaterThanZero().SetDisplay("Bollinger Length", "Bollinger period", "Bollinger")
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Bollinger Multiplier", "Bollinger standard deviation multiplier", "Bollinger")
        self._fibonacci_length = self.Param("FibonacciLength", 50).SetGreaterThanZero().SetDisplay("Fibonacci Length", "Candles of the Fibonacci range", "Fibonacci")
        self._fibonacci_level0 = self.Param("FibonacciLevel0", 0.0).SetDisplay("Fibonacci Level 0", "Fraction of the range that gives the Fibonacci low", "Fibonacci")
        self._fibonacci_level100 = self.Param("FibonacciLevel100", 1.0).SetDisplay("Fibonacci Level 100", "Fraction of the range that gives the Fibonacci high", "Fibonacci")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_upper = None
        self._prev_lower = None
        self._prev_middle = None

    def OnReseted(self):
        super(bollinger_bands_fibonacci_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(bollinger_bands_fibonacci_strategy, self).OnStarted2(time)

        self._reset_state()

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_length.Value
        bollinger.Width = Decimal(self._bollinger_multiplier.Value)
        highest = Highest()
        highest.Length = self._fibonacci_length.Value
        lowest = Lowest()
        lowest.Length = self._fibonacci_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, highest, lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bollinger_value, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed or not highest_value.IsFormed or not lowest_value.IsFormed:
            return

        upper = bollinger_value.UpBand
        lower = bollinger_value.LowBand
        middle = bollinger_value.MovingAverage
        if upper is None or lower is None or middle is None:
            return

        close = candle.ClosePrice
        pc = self._prev_close
        pu = self._prev_upper
        pl = self._prev_lower
        pm = self._prev_middle

        self._prev_close = close
        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_middle = middle

        if pc is None or pu is None or pl is None or pm is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        highest = highest_value.GetValue[Decimal](None)
        lowest = lowest_value.GetValue[Decimal](None)
        price_range = highest - lowest
        fib_low = lowest + price_range * Decimal(self._fibonacci_level0.Value)
        fib_high = lowest + price_range * Decimal(self._fibonacci_level100.Value)

        cross_above_upper = pc <= pu and close > upper
        cross_below_lower = pc >= pl and close < lower

        if cross_above_upper and candle.LowPrice > fib_low and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_below_lower and candle.HighPrice < fib_high and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and pc >= pm and close < middle:
            self.SellMarket(self.Position)
        elif self.Position < 0 and pc <= pm and close > middle:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return bollinger_bands_fibonacci_strategy()
