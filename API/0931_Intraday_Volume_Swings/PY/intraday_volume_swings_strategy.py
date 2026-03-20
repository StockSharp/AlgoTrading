import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class intraday_volume_swings_strategy(Strategy):
    def __init__(self):
        super(intraday_volume_swings_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._high1 = 0.0
        self._high2 = 0.0
        self._low1 = 0.0
        self._low2 = 0.0
        self._volume1 = 0.0
        self._low_bar1 = False
        self._low_bar2 = False
        self._high_bar1 = False
        self._high_bar2 = False
        self._prev_swing_low = False
        self._prev_swing_high = False
        self._daily_swing_low_top = None
        self._daily_swing_high_bottom = None
        self._current_day = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(intraday_volume_swings_strategy, self).OnReseted()
        self._high1 = 0.0
        self._high2 = 0.0
        self._low1 = 0.0
        self._low2 = 0.0
        self._volume1 = 0.0
        self._low_bar1 = False
        self._low_bar2 = False
        self._high_bar1 = False
        self._high_bar2 = False
        self._prev_swing_low = False
        self._prev_swing_high = False
        self._daily_swing_low_top = None
        self._daily_swing_high_bottom = None
        self._current_day = None

    def OnStarted(self, time):
        super(intraday_volume_swings_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        vol = float(candle.TotalVolume)
        day = candle.OpenTime.Date
        if self._current_day is None or self._current_day != day:
            self._current_day = day
            self._daily_swing_low_top = None
            self._daily_swing_high_bottom = None
        inc_vol = vol > self._volume1
        lower_low = low < self._low1
        higher_high = high > self._high1
        low_bar = inc_vol and lower_low
        high_bar = inc_vol and higher_high
        swing_low = low_bar and self._low_bar1 and self._low_bar2
        swing_high = high_bar and self._high_bar1 and self._high_bar2
        hh3 = max(high, max(self._high1, self._high2))
        ll3 = min(low, min(self._low1, self._low2))
        if swing_low and not self._prev_swing_low:
            self._current_sl_top = hh3
            self._current_sl_bottom = ll3
        elif swing_low and self._prev_swing_low:
            self._current_sl_top = max(getattr(self, '_current_sl_top', hh3), high)
            self._current_sl_bottom = min(getattr(self, '_current_sl_bottom', ll3), low)
        if swing_high and not self._prev_swing_high:
            self._current_sh_top = hh3
            self._current_sh_bottom = ll3
        elif swing_high and self._prev_swing_high:
            self._current_sh_top = max(getattr(self, '_current_sh_top', hh3), high)
            self._current_sh_bottom = min(getattr(self, '_current_sh_bottom', ll3), low)
        if self._prev_swing_low and not swing_low:
            sl_bottom = getattr(self, '_current_sl_bottom', None)
            sl_top = getattr(self, '_current_sl_top', None)
            if sl_bottom is not None:
                if self._daily_swing_low_top is None or sl_bottom < (self._daily_swing_low_top or 0):
                    self._daily_swing_low_top = sl_top
        if self._prev_swing_high and not swing_high:
            sh_top = getattr(self, '_current_sh_top', None)
            sh_bottom = getattr(self, '_current_sh_bottom', None)
            if sh_top is not None:
                if self._daily_swing_high_bottom is None or sh_top > 0:
                    self._daily_swing_high_bottom = sh_bottom
        if self._daily_swing_high_bottom is not None:
            level = self._daily_swing_high_bottom
            if close > level and self.Position <= 0:
                self.BuyMarket()
        if self._daily_swing_low_top is not None:
            level = self._daily_swing_low_top
            if close < level and self.Position >= 0:
                self.SellMarket()
        self._volume1 = vol
        self._high2 = self._high1
        self._high1 = high
        self._low2 = self._low1
        self._low1 = low
        self._low_bar2 = self._low_bar1
        self._low_bar1 = low_bar
        self._high_bar2 = self._high_bar1
        self._high_bar1 = high_bar
        self._prev_swing_low = swing_low
        self._prev_swing_high = swing_high

    def CreateClone(self):
        return intraday_volume_swings_strategy()
