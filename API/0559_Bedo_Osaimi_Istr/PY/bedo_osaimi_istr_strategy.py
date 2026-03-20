import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bedo_osaimi_istr_strategy(Strategy):
    def __init__(self):
        super(bedo_osaimi_istr_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ma_length = self.Param("MaLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Moving average length", "Parameters")
        self._prev_close_ma = None
        self._prev_open_ma = None
        self._open_sum = 0.0
        self._open_values = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(bedo_osaimi_istr_strategy, self).OnReseted()
        self._prev_close_ma = None
        self._prev_open_ma = None
        self._open_sum = 0.0
        self._open_values = []

    def OnStarted(self, time):
        super(bedo_osaimi_istr_strategy, self).OnStarted(time)
        close_ma = SimpleMovingAverage()
        close_ma.Length = self._ma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(close_ma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, close_ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, close_ma_val):
        if candle.State != CandleStates.Finished:
            return
        close_ma_v = float(close_ma_val)
        open_p = float(candle.OpenPrice)
        length = self._ma_length.Value
        self._open_values.append(open_p)
        self._open_sum += open_p
        if len(self._open_values) > length:
            self._open_sum -= self._open_values[0]
            self._open_values = self._open_values[1:]
        if len(self._open_values) < length:
            self._prev_close_ma = close_ma_v
            return
        open_ma_v = self._open_sum / length
        if self._prev_close_ma is None or self._prev_open_ma is None:
            self._prev_close_ma = close_ma_v
            self._prev_open_ma = open_ma_v
            if self.Position == 0:
                self.BuyMarket()
            return
        if close_ma_v > open_ma_v and self._prev_close_ma <= self._prev_open_ma and self.Position == 0:
            self.BuyMarket()
        elif close_ma_v < open_ma_v and self._prev_close_ma >= self._prev_open_ma and self.Position == 0:
            self.SellMarket()
        self._prev_close_ma = close_ma_v
        self._prev_open_ma = open_ma_v

    def CreateClone(self):
        return bedo_osaimi_istr_strategy()
