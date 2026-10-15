import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

class parabolic_sar_fibo_limits_strategy(Strategy):
    def __init__(self):
        super(parabolic_sar_fibo_limits_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(3))).SetDisplay("Candle Type", "Timeframe", "General")
        self._lookback = self.Param("Lookback", 20).SetDisplay("Lookback", "Period for Highest/Lowest range", "Indicators")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(parabolic_sar_fibo_limits_strategy, self).OnReseted()
        self._prev_sar = 0
        self._has_prev_sar = False
        self._entry_price = 0

    def OnStarted2(self, time):
        super(parabolic_sar_fibo_limits_strategy, self).OnStarted2(time)
        self._prev_sar = 0
        self._has_prev_sar = False
        self._entry_price = 0

        sar = ParabolicSar()
        highest = Highest()
        highest.Length = self._lookback.Value
        lowest = Lowest()
        lowest.Length = self._lookback.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sar, highest, lowest, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sar_value, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        rng = highest_value - lowest_value

        sar_below = sar_value < close
        prev_sar_below = self._has_prev_sar and self._prev_sar < close
        sar_above = sar_value > close
        prev_sar_above = self._has_prev_sar and self._prev_sar > close

        fib382 = lowest_value + rng * 0.382
        fib618 = lowest_value + rng * 0.618

        if self.Position > 0:
            if close >= fib618 or sar_above:
                self.SellMarket()
        elif self.Position < 0:
            if close <= fib382 or sar_below:
                self.BuyMarket()

        if self.Position == 0 and self._has_prev_sar and rng > 0:
            if sar_below and not prev_sar_below and close > fib382:
                self._entry_price = close
                self.BuyMarket()
            elif sar_above and not prev_sar_above and close < fib618:
                self._entry_price = close
                self.SellMarket()

        self._prev_sar = sar_value
        self._has_prev_sar = True

    def CreateClone(self):
        return parabolic_sar_fibo_limits_strategy()
