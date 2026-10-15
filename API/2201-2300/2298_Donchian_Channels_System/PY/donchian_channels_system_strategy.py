import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy


class donchian_channels_system_strategy(Strategy):
    def __init__(self):
        super(donchian_channels_system_strategy, self).__init__()
        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetDisplay("Donchian Period", "Lookback period for Donchian Channel", "Indicators")
        self._shift = self.Param("Shift", 2) \
            .SetDisplay("Shift", "Bars offset for breakout evaluation", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe for analysis", "General")
        self._upper_buffer = []
        self._lower_buffer = []
        self._prev_close = 0.0

    @property
    def donchian_period(self):
        return self._donchian_period.Value

    @property
    def shift(self):
        return self._shift.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchian_channels_system_strategy, self).OnReseted()
        self._upper_buffer = []
        self._lower_buffer = []
        self._prev_close = 0.0

    def OnStarted2(self, time):
        super(donchian_channels_system_strategy, self).OnStarted2(time)
        self._upper_buffer = []
        self._lower_buffer = []
        self._prev_close = 0.0
        donchian = DonchianChannels()
        donchian.Length = self.donchian_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, donchian_value):
        if candle.State != CandleStates.Finished:
            return
        upper = donchian_value.UpperBand
        lower = donchian_value.LowerBand
        if upper is None or lower is None:
            return
        upper = float(upper)
        lower = float(lower)
        shift = int(self.shift)
        self._upper_buffer.append(upper)
        self._lower_buffer.append(lower)
        if len(self._upper_buffer) > shift + 1:
            self._upper_buffer.pop(0)
            self._lower_buffer.pop(0)
        if len(self._upper_buffer) <= shift:
            self._prev_close = float(candle.ClosePrice)
            return
        shifted_upper = self._upper_buffer[0]
        shifted_lower = self._lower_buffer[0]
        close_price = float(candle.ClosePrice)
        up_break = close_price > shifted_upper and self._prev_close <= shifted_upper
        dn_break = close_price < shifted_lower and self._prev_close >= shifted_lower
        if up_break and self.Position <= 0:
            self.BuyMarket()
        elif dn_break and self.Position >= 0:
            self.SellMarket()
        self._prev_close = close_price

    def CreateClone(self):
        return donchian_channels_system_strategy()
