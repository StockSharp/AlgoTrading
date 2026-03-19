import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class stochastic_three_periods_strategy(Strategy):
    """Fast/slow RSI alignment with StartProtection."""
    def __init__(self):
        super(stochastic_three_periods_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 5).SetGreaterThanZero().SetDisplay("Fast K", "Fast RSI period", "Parameters")
        self._slow_period = self.Param("SlowPeriod", 14).SetGreaterThanZero().SetDisplay("Slow K", "Slow RSI period", "Parameters")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Working timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(stochastic_three_periods_strategy, self).OnReseted()
        self._prev_slow = 0
        self._last_signal = 0

    def OnStarted(self, time):
        super(stochastic_three_periods_strategy, self).OnStarted(time)
        self._prev_slow = 0
        self._last_signal = 0

        self._fast_rsi = RelativeStrengthIndex()
        self._fast_rsi.Length = self._fast_period.Value
        self._slow_rsi = RelativeStrengthIndex()
        self._slow_rsi.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._fast_rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._fast_rsi)
            self.DrawOwnTrades(area)

        self.StartProtection(self.CreateProtection(2000, 1000))

    def OnProcess(self, candle, fast_value):
        if candle.State != CandleStates.Finished:
            return

        inp = DecimalIndicatorValue(self._slow_rsi, candle.ClosePrice)
        inp.IsFinal = True
        slow_result = self._slow_rsi.Process(inp)
        if not self._slow_rsi.IsFormed or slow_result.IsEmpty:
            return
        slow_value = float(slow_result.ToDecimal())
        fast_value = float(fast_value)

        if fast_value > slow_value and fast_value > 55 and slow_value > 50 and slow_value > self._prev_slow and self._last_signal != 1 and self.Position <= 0:
            self.BuyMarket()
            self._last_signal = 1
        elif fast_value < slow_value and fast_value < 45 and slow_value < 50 and slow_value < self._prev_slow and self._last_signal != -1 and self.Position >= 0:
            self.SellMarket()
            self._last_signal = -1

        self._prev_slow = slow_value

    def CreateClone(self):
        return stochastic_three_periods_strategy()
