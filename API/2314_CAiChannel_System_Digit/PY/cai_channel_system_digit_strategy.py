import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class cai_channel_system_digit_strategy(Strategy):
    def __init__(self):
        super(cai_channel_system_digit_strategy, self).__init__()
        self._length = self.Param("Length", 12) \
            .SetDisplay("Length", "BB period", "Indicator")
        self._width = self.Param("Width", 2.0) \
            .SetDisplay("Width", "BB width", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_up = False
        self._prev_down = False

    @property
    def length(self):
        return self._length.Value

    @property
    def width(self):
        return self._width.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cai_channel_system_digit_strategy, self).OnReseted()
        self._prev_up = False
        self._prev_down = False

    def OnStarted(self, time):
        super(cai_channel_system_digit_strategy, self).OnStarted(time)
        self._prev_up = False
        self._prev_down = False
        bb = BollingerBands()
        bb.Length = self.length
        bb.Width = self.width
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, bb_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        up = bb_val.UpBand
        down = bb_val.LowBand
        if up is None or down is None:
            return
        up = float(up)
        down = float(down)
        close_price = float(candle.ClosePrice)
        if self._prev_up and close_price <= up and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_down and close_price >= down and self.Position >= 0:
            self.SellMarket()
        self._prev_up = close_price > up
        self._prev_down = close_price < down

    def CreateClone(self):
        return cai_channel_system_digit_strategy()
