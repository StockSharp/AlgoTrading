import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class icai_strategy(Strategy):
    def __init__(self):
        super(icai_strategy, self).__init__()
        self._length = self.Param("Length", 12) \
            .SetDisplay("Length", "Indicator smoothing length", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")
        self._ma = None
        self._std = None
        self._prev_icai = None
        self._prev_slope = None

    @property
    def length(self):
        return self._length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(icai_strategy, self).OnReseted()
        self._ma = None
        self._std = None
        self._prev_icai = None
        self._prev_slope = None

    def OnStarted2(self, time):
        super(icai_strategy, self).OnStarted2(time)
        self._prev_icai = None
        self._prev_slope = None
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.length
        self._std = StandardDeviation()
        self._std.Length = self.length
        self.Indicators.Add(self._ma)
        self.Indicators.Add(self._std)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        price = float(candle.ClosePrice)
        t = candle.OpenTime
        ma_result = process_float(self._ma, price, t, True)
        std_result = process_float(self._std, price, t, True)
        if not self._ma.IsFormed or not self._std.IsFormed:
            return
        ma_val = float(ma_result)
        std_val = float(std_result)
        prev = self._prev_icai if self._prev_icai is not None else ma_val
        diff = prev - ma_val
        pow_dxma = diff * diff
        pow_std = std_val * std_val
        koeff = 0.0
        if pow_dxma >= pow_std and pow_dxma != 0.0:
            koeff = 1.0 - pow_std / pow_dxma
        icai = prev + koeff * (ma_val - prev)
        self._prev_icai = icai
        if self._prev_slope is None:
            self._prev_slope = 0.0
            return
        slope = icai - prev
        if self._prev_slope <= 0 and slope > 0 and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_slope >= 0 and slope < 0 and self.Position >= 0:
            self.SellMarket()
        self._prev_slope = slope

    def CreateClone(self):
        return icai_strategy()
