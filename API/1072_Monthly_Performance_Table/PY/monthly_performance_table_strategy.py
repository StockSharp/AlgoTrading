import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class monthly_performance_table_strategy(Strategy):
    def __init__(self):
        super(monthly_performance_table_strategy, self).__init__()
        self._length = self.Param("Length", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for DMI/ADX", "General")
        self._long_difference = self.Param("LongDifference", 10.0) \
            .SetDisplay("Long Difference", "Minimum diff for longs", "General")
        self._short_difference = self.Param("ShortDifference", 10.0) \
            .SetDisplay("Short Difference", "Minimum diff for shorts", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Working candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(monthly_performance_table_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(monthly_performance_table_strategy, self).OnStarted(time)
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self._length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._adx, self.OnProcess).Start()

    def OnProcess(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._adx.IsFormed:
            return
        adx_val = adx_value
        adx_mv = adx_val.MovingAverage
        di_plus = adx_val.Dx.Plus
        di_minus = adx_val.Dx.Minus
        if adx_mv is None or di_plus is None or di_minus is None:
            return
        adx = float(adx_mv)
        dip = float(di_plus)
        dim = float(di_minus)
        diff2 = abs(dip - adx)
        diff3 = abs(dim - adx)
        ld = float(self._long_difference.Value)
        sd = float(self._short_difference.Value)
        buy_cond = diff2 >= ld and diff3 >= ld and adx < dip and adx > dim
        sell_cond = diff2 >= sd and diff3 >= sd and adx > dip and adx < dim
        if buy_cond and self.Position <= 0:
            self.BuyMarket()
        elif sell_cond and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return monthly_performance_table_strategy()
