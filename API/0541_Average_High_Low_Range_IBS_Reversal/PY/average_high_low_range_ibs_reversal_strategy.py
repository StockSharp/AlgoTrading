import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class average_high_low_range_ibs_reversal_strategy(Strategy):
    def __init__(self):
        super(average_high_low_range_ibs_reversal_strategy, self).__init__()
        self._channel_length = self.Param("ChannelLength", 20) \
            .SetDisplay("Channel Length", "Lookback for Highest/Lowest", "Indicators")
        self._ema_length = self.Param("EmaLength", 40) \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def channel_length(self):
        return self._channel_length.Value
    @channel_length.setter
    def channel_length(self, value):
        self._channel_length.Value = value

    @property
    def ema_length(self):
        return self._ema_length.Value
    @ema_length.setter
    def ema_length(self, value):
        self._ema_length.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value
    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    @property
    def candle_type(self):
        return self._candle_type.Value
    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(average_high_low_range_ibs_reversal_strategy, self).OnReseted()
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted(self, time):
        super(average_high_low_range_ibs_reversal_strategy, self).OnStarted(time)
        highest = Highest()
        highest.Length = self.channel_length
        lowest = Lowest()
        lowest.Length = self.channel_length
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, highest_value, lowest_value, ema_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self._bar_index += 1
        cooldown_ok = self._bar_index - self._last_trade_bar > self.cooldown_bars

        break_up = self._prev_highest > 0 and candle.ClosePrice > self._prev_highest and candle.ClosePrice > ema_value
        break_down = self._prev_lowest > 0 and candle.ClosePrice < self._prev_lowest and candle.ClosePrice < ema_value

        if break_up and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif break_down and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_highest = float(highest_value)
        self._prev_lowest = float(lowest_value)

    def CreateClone(self):
        return average_high_low_range_ibs_reversal_strategy()
