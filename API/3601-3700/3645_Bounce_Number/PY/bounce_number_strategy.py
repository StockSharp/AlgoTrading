import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class bounce_number_strategy(Strategy):
    def __init__(self):
        super(bounce_number_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._max_history_candles = self.Param("MaxHistoryCandles", 10000)
        self._channel_points = self.Param("ChannelPoints", 10)

        self._channel_center = None
        self._bounce_count = 0
        self._last_touch_direction = 0
        self._candles_in_cycle = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MaxHistoryCandles(self):
        return self._max_history_candles.Value

    @MaxHistoryCandles.setter
    def MaxHistoryCandles(self, value):
        self._max_history_candles.Value = value

    @property
    def ChannelPoints(self):
        return self._channel_points.Value

    @ChannelPoints.setter
    def ChannelPoints(self, value):
        self._channel_points.Value = value

    def OnReseted(self):
        super(bounce_number_strategy, self).OnReseted()
        self._channel_center = None
        self._bounce_count = 0
        self._last_touch_direction = 0
        self._candles_in_cycle = 0

    def OnStarted2(self, time):
        super(bounce_number_strategy, self).OnStarted2(time)
        self._channel_center = None
        self._bounce_count = 0
        self._last_touch_direction = 0
        self._candles_in_cycle = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _get_channel_half_width(self):
        return float(self.ChannelPoints)

    def _reset_channel(self, center):
        self._channel_center = center
        self._bounce_count = 0
        self._last_touch_direction = 0
        self._candles_in_cycle = 0

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        channel_half = self._get_channel_half_width()
        if channel_half <= 0:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self._channel_center is None:
            self._reset_channel(close)
            return

        self._candles_in_cycle += 1

        center = self._channel_center
        upper_band = center + channel_half
        lower_band = center - channel_half
        break_upper = center + channel_half * 2.0
        break_lower = center - channel_half * 2.0

        breakout_up = high >= break_upper
        breakout_down = low <= break_lower
        max_hist = self.MaxHistoryCandles

        if breakout_up or breakout_down or (self._candles_in_cycle >= max_hist and max_hist > 0):
            self._reset_channel(close)
            return

        touched_lower = low <= lower_band and high >= lower_band
        touched_upper = high >= upper_band and low <= upper_band

        if touched_lower and self._last_touch_direction >= 0:
            self._bounce_count += 1
            self._last_touch_direction = -1
            if self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
        elif touched_upper and self._last_touch_direction <= 0:
            self._bounce_count += 1
            self._last_touch_direction = 1
            if self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        if breakout_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif breakout_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return bounce_number_strategy()
