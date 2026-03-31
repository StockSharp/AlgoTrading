import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest

class profit_hunter_hsi_with_fibonacci_strategy(Strategy):
    def __init__(self):
        super(profit_hunter_hsi_with_fibonacci_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Period for trend filter EMA", "Indicators")
        self._lookback_period = self.Param("LookbackPeriod", 50) \
            .SetDisplay("Lookback Period", "Bars to look back for range high/low", "Fibonacci")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")

        self._bar_count = 0

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(profit_hunter_hsi_with_fibonacci_strategy, self).OnStarted2(time)

        self._bar_count = 0

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaPeriod
        self._highest = Highest()
        self._highest.Length = self.LookbackPeriod
        self._lowest = Lowest()
        self._lowest.Length = self.LookbackPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ema, self._highest, self._lowest, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, ema_value, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_count += 1

        if self._bar_count < self.LookbackPeriod:
            return

        ema_val = float(ema_value)
        high_val = float(highest_value)
        low_val = float(lowest_value)
        rng = high_val - low_val
        if rng <= 0:
            return

        fib382 = high_val - rng * 0.382
        fib618 = high_val - rng * 0.618
        close = float(candle.ClosePrice)

        # Manage position
        if self.Position > 0:
            if close >= high_val or close < fib618:
                self.SellMarket()
        elif self.Position < 0:
            if close <= low_val or close > fib382:
                self.BuyMarket()

        # Entry logic
        if self.Position == 0:
            if close > ema_val and close <= fib382 and close > fib618:
                self.BuyMarket()
            elif close < ema_val and close >= fib618 and close < fib382:
                self.SellMarket()

    def OnReseted(self):
        super(profit_hunter_hsi_with_fibonacci_strategy, self).OnReseted()
        self._bar_count = 0

    def CreateClone(self):
        return profit_hunter_hsi_with_fibonacci_strategy()
