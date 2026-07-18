import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy


class darvas_boxes_system_strategy(Strategy):
    def __init__(self):
        super(darvas_boxes_system_strategy, self).__init__()
        self._box_period = self.Param("BoxPeriod", 20) \
            .SetDisplay("Box Period", "Period for box calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_close = 0.0

    @property
    def box_period(self):
        return self._box_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(darvas_boxes_system_strategy, self).OnReseted()
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_close = 0.0

    def OnStarted2(self, time):
        super(darvas_boxes_system_strategy, self).OnStarted2(time)
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_close = 0.0
        donchian = DonchianChannels()
        donchian.Length = self.box_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        upper = value.UpperBand
        lower = value.LowerBand
        if upper is None or lower is None:
            return
        upper = float(upper)
        lower = float(lower)
        close_price = float(candle.ClosePrice)
        if self._prev_upper == 0.0:
            self._prev_upper = upper
            self._prev_lower = lower
            self._prev_close = close_price
            return
        is_up_breakout = close_price > self._prev_upper and self._prev_close <= self._prev_upper
        is_down_breakout = close_price < self._prev_lower and self._prev_close >= self._prev_lower
        if is_up_breakout and self.Position <= 0:
            self.BuyMarket()
        elif is_down_breakout and self.Position >= 0:
            self.SellMarket()
        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_close = close_price

    def CreateClone(self):
        return darvas_boxes_system_strategy()
