import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage,
    ExponentialMovingAverage,
    SmoothedMovingAverage,
    WeightedMovingAverage,
    DonchianChannels,
)

class three_ma_cross_channel_strategy(Strategy):
    def __init__(self):
        super(three_ma_cross_channel_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 2) \
            .SetDisplay("Fast MA", "Length of the fast moving average", "Moving Averages")
        self._medium_length = self.Param("MediumLength", 4) \
            .SetDisplay("Medium MA", "Length of the medium moving average", "Moving Averages")
        self._slow_length = self.Param("SlowLength", 30) \
            .SetDisplay("Slow MA", "Length of the slow moving average", "Moving Averages")
        self._channel_length = self.Param("ChannelLength", 15) \
            .SetDisplay("Channel", "Donchian channel lookback period", "Risk Management")
        self._take_profit = self.Param("TakeProfit", 0.0) \
            .SetDisplay("Take Profit", "Distance to close profitable trades", "Risk Management")
        self._stop_loss = self.Param("StopLoss", 0.0) \
            .SetDisplay("Stop Loss", "Distance to limit losses", "Risk Management")
        self._use_channel_stop = self.Param("UseChannelStop", True) \
            .SetDisplay("Channel Exit", "Use Donchian channel boundaries for exits", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles used by the strategy", "General")

        self._prev_fast_above_slow = None
        self._prev_medium_above_slow = None
        self._entry_price = None

    @property
    def FastLength(self):
        return self._fast_length.Value

    @property
    def MediumLength(self):
        return self._medium_length.Value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @property
    def ChannelLength(self):
        return self._channel_length.Value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @property
    def UseChannelStop(self):
        return self._use_channel_stop.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(three_ma_cross_channel_strategy, self).OnStarted2(time)

        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self.FastLength
        self._medium_ma = ExponentialMovingAverage()
        self._medium_ma.Length = self.MediumLength
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self.SlowLength
        self._channel = DonchianChannels()
        self._channel.Length = self.ChannelLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .BindEx(self._fast_ma, self._medium_ma, self._slow_ma, self._channel, self.ProcessCandle) \
            .Start()

        self.StartProtection(None, None)

    def ProcessCandle(self, candle, fast_val, medium_val, slow_val, channel_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast_ma.IsFormed or not self._medium_ma.IsFormed or not self._slow_ma.IsFormed:
            return

        fast_value = float(fast_val)
        medium_value = float(medium_val)
        slow_value = float(slow_val)

        upper_band = 0.0
        lower_band = 0.0
        if hasattr(channel_val, 'UpperBand') and channel_val.UpperBand is not None:
            upper_band = float(channel_val.UpperBand)
        if hasattr(channel_val, 'LowerBand') and channel_val.LowerBand is not None:
            lower_band = float(channel_val.LowerBand)

        fast_above = fast_value > slow_value
        medium_above = medium_value > slow_value

        fast_cross_up = self._prev_fast_above_slow is not None and not self._prev_fast_above_slow and fast_above
        fast_cross_down = self._prev_fast_above_slow is not None and self._prev_fast_above_slow and not fast_above
        medium_cross_up = self._prev_medium_above_slow is not None and not self._prev_medium_above_slow and medium_above
        medium_cross_down = self._prev_medium_above_slow is not None and self._prev_medium_above_slow and not medium_above

        self._prev_fast_above_slow = fast_above
        self._prev_medium_above_slow = medium_above

        buy_signal = fast_above and medium_above and (fast_cross_up or medium_cross_up)
        sell_signal = not fast_above and not medium_above and (fast_cross_down or medium_cross_down)

        close = float(candle.ClosePrice)
        tp = float(self.TakeProfit)
        sl = float(self.StopLoss)

        if self.Position > 0:
            should_exit = sell_signal

            if not should_exit and self._entry_price is not None:
                if tp > 0 and close - self._entry_price >= tp:
                    should_exit = True
                if sl > 0 and self._entry_price - close >= sl:
                    should_exit = True

            if not should_exit and self.UseChannelStop and close <= lower_band:
                should_exit = True

            if should_exit:
                self.SellMarket(self.Position)
                self._entry_price = None
                return

        elif self.Position < 0:
            should_exit = buy_signal

            if not should_exit and self._entry_price is not None:
                if tp > 0 and self._entry_price - close >= tp:
                    should_exit = True
                if sl > 0 and close - self._entry_price >= sl:
                    should_exit = True

            if not should_exit and self.UseChannelStop and close >= upper_band:
                should_exit = True

            if should_exit:
                self.BuyMarket(abs(self.Position))
                self._entry_price = None
                return

        if buy_signal and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._entry_price = close
        elif sell_signal and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._entry_price = close

    def OnReseted(self):
        super(three_ma_cross_channel_strategy, self).OnReseted()
        self._prev_fast_above_slow = None
        self._prev_medium_above_slow = None
        self._entry_price = None

    def CreateClone(self):
        return three_ma_cross_channel_strategy()
