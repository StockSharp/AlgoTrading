import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy

class donchian_channel_strategy(Strategy):
    """
    Strategy based on Donchian Channel.
    Enters long when the close breaks above the upper band of the previous candles, short below the lower band.
    Exits when the close returns to the channel midpoint.
    """

    def __init__(self):
        super(donchian_channel_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 20).SetGreaterThanZero().SetDisplay("Channel Period", "Period for Donchian Channel calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        # Channel of the candles before the current one.
        self._prev_upper_band = None
        self._prev_lower_band = None
        self._prev_middle = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchian_channel_strategy, self).OnReseted()
        # Channel of the candles before the current one.
        self._prev_upper_band = None
        self._prev_lower_band = None
        self._prev_middle = None

    def OnStarted2(self, time):
        super(donchian_channel_strategy, self).OnStarted2(time)

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

        if donchian_val.UpperBand is None or donchian_val.LowerBand is None or donchian_val.Middle is None:
            return

        # A close can only break out of the channel the candles before it formed.
        upper = self._prev_upper_band
        lower = self._prev_lower_band
        middle = self._prev_middle

        self._prev_upper_band = donchian_val.UpperBand
        self._prev_lower_band = donchian_val.LowerBand
        self._prev_middle = donchian_val.Middle

        if upper is None or lower is None or middle is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        position = self.Position

        if close > upper and position <= 0:
            self.BuyMarket(self.Volume + abs(position))
        elif close < lower and position >= 0:
            self.SellMarket(self.Volume + abs(position))
        elif position > 0 and close <= middle:
            self.SellMarket(position)
        elif position < 0 and close >= middle:
            self.BuyMarket(-position)

    def CreateClone(self):
        return donchian_channel_strategy()
