import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy


class delta_mfi_strategy(Strategy):
    def __init__(self):
        super(delta_mfi_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 14) \
            .SetDisplay("Fast MFI Period", "Period for fast Money Flow Index", "Parameters")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Slow MFI Period", "Period for slow Money Flow Index", "Parameters")
        self._level = self.Param("Level", 50) \
            .SetDisplay("Signal Level", "MFI level to confirm signals", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles used for analysis", "General")

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def level(self):
        return self._level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(delta_mfi_strategy, self).OnStarted2(time)
        fast_mfi = MoneyFlowIndex()
        fast_mfi.Length = int(self.fast_period)
        slow_mfi = MoneyFlowIndex()
        slow_mfi.Length = int(self.slow_period)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_mfi, slow_mfi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_mfi)
            self.DrawIndicator(area, slow_mfi)

    def process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        fast_value = float(fast_value)
        slow_value = float(slow_value)
        lvl = float(self.level)
        if slow_value > lvl and fast_value > slow_value and self.Position <= 0:
            self.BuyMarket()
        elif slow_value < (100 - lvl) and fast_value < slow_value and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return delta_mfi_strategy()
