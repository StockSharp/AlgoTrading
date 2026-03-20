import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy

class donchian_channel_strategy(Strategy):
    """
    Strategy based on Donchian Channel.
    Enters long when price breaks above upper band, short when price breaks below lower band.
    """

    def __init__(self):
        super(donchian_channel_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 1000).SetDisplay("Channel Period", "Period for Donchian Channel calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_close_price = 0.0
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchian_channel_strategy, self).OnReseted()
        self._prev_close_price = 0.0
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0

    def OnStarted(self, time):
        super(donchian_channel_strategy, self).OnStarted(time)

        donchian = DonchianChannels()
        donchian.Length = self._channel_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, donchian_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if donchian_val.UpperBand is None or donchian_val.LowerBand is None or donchian_val.Middle is None:
            return

        upper = float(donchian_val.UpperBand)
        lower = float(donchian_val.LowerBand)

        if self._prev_upper_band == 0:
            self._prev_close_price = float(candle.ClosePrice)
            self._prev_upper_band = upper
            self._prev_lower_band = lower
            return

        close = float(candle.ClosePrice)
        is_upper_breakout = close > self._prev_upper_band and self._prev_close_price <= self._prev_upper_band
        is_lower_breakout = close < self._prev_lower_band and self._prev_close_price >= self._prev_lower_band

        if is_upper_breakout and self.Position <= 0:
            self.BuyMarket()
        elif is_lower_breakout and self.Position >= 0:
            self.SellMarket()

        self._prev_close_price = close
        self._prev_upper_band = upper
        self._prev_lower_band = lower

    def CreateClone(self):
        return donchian_channel_strategy()
