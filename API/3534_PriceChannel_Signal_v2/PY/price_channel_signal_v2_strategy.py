import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class price_channel_signal_v2_strategy(Strategy):
    def __init__(self):
        super(price_channel_signal_v2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._channel_period = self.Param("ChannelPeriod", 20)

        self._high_history = []
        self._low_history = []
        self._prev_trend = 0
        self._prev_close = 0.0
        self._has_prev_close = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def ChannelPeriod(self):
        return self._channel_period.Value

    @ChannelPeriod.setter
    def ChannelPeriod(self, value):
        self._channel_period.Value = value

    def OnReseted(self):
        super(price_channel_signal_v2_strategy, self).OnReseted()
        self._high_history = []
        self._low_history = []
        self._prev_trend = 0
        self._prev_close = 0.0
        self._has_prev_close = False

    def OnStarted(self, time):
        super(price_channel_signal_v2_strategy, self).OnStarted(time)
        self._high_history = []
        self._low_history = []
        self._prev_trend = 0
        self._prev_close = 0.0
        self._has_prev_close = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        period = self.ChannelPeriod

        if len(self._high_history) < period:
            self._high_history.append(high)
            self._low_history.append(low)
            self._prev_close = close
            self._has_prev_close = True
            return

        channel_high = max(self._high_history)
        channel_low = min(self._low_history)
        ch_range = channel_high - channel_low

        if ch_range <= 0:
            self._prev_close = close
            self._high_history.append(high)
            self._low_history.append(low)
            while len(self._high_history) > period:
                self._high_history.pop(0)
            while len(self._low_history) > period:
                self._low_history.pop(0)
            return

        mid = (channel_high + channel_low) / 2.0

        trend = self._prev_trend
        if close > channel_high + ch_range * 0.05:
            trend = 1
        elif close < channel_low - ch_range * 0.05:
            trend = -1

        changed_position = False

        if trend > 0 and self._prev_trend <= 0:
            if self.Position <= 0:
                self.BuyMarket()
                changed_position = True
        elif trend < 0 and self._prev_trend >= 0:
            if self.Position >= 0:
                self.SellMarket()
                changed_position = True

        # Exit on mid-line cross
        if not changed_position and self._has_prev_close:
            if self.Position > 0 and self._prev_close >= mid and close < mid:
                self.SellMarket()
            elif self.Position < 0 and self._prev_close <= mid and close > mid:
                self.BuyMarket()

        self._prev_trend = trend
        self._prev_close = close
        self._has_prev_close = True

        self._high_history.append(high)
        self._low_history.append(low)
        while len(self._high_history) > period:
            self._high_history.pop(0)
        while len(self._low_history) > period:
            self._low_history.pop(0)

    def CreateClone(self):
        return price_channel_signal_v2_strategy()
