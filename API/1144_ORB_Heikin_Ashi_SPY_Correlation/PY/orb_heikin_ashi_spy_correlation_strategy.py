import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class orb_heikin_ashi_spy_correlation_strategy(Strategy):
    def __init__(self):
        super(orb_heikin_ashi_spy_correlation_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._or_high = 0.0
        self._or_low = 0.0
        self._trade_taken_today = False
        self._was_in_or = False
        self._current_day = None
        self._or_established = False
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(orb_heikin_ashi_spy_correlation_strategy, self).OnReseted()
        self._or_high = 0.0
        self._or_low = 0.0
        self._trade_taken_today = False
        self._was_in_or = False
        self._current_day = None
        self._or_established = False
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0

    def OnStarted(self, time):
        super(orb_heikin_ashi_spy_correlation_strategy, self).OnStarted(time)
        self._or_high = 0.0
        self._or_low = 0.0
        self._trade_taken_today = False
        self._was_in_or = False
        self._current_day = None
        self._or_established = False
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._sma = SimpleMovingAverage()
        self._sma.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self.OnProcess).Start()

    def OnProcess(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._sma.IsFormed:
            return
        sv = float(sma_val)
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        day = candle.OpenTime.Date
        if self._current_day is None or self._current_day != day:
            self._current_day = day
            self._or_high = 0.0
            self._or_low = 0.0
            self._trade_taken_today = False
            self._or_established = False
        if self._prev_ha_open == 0.0:
            ha_open = (o + c) / 2.0
            ha_close = (o + h + l + c) / 4.0
        else:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0
            ha_close = (o + h + l + c) / 4.0
        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close
        bullish_ha = ha_close > ha_open
        hour = candle.OpenTime.TimeOfDay.TotalHours
        in_or = hour < 1
        if in_or:
            self._or_high = max(self._or_high, h) if self._or_high > 0 else h
            self._or_low = min(self._or_low, l) if self._or_low > 0 else l
        if self._was_in_or and not in_or and self._or_high > 0 and self._or_low > 0 and self._or_high - self._or_low > 0:
            self._or_established = True
        if not self._trade_taken_today and self._or_established and not in_or:
            if c > self._or_high and bullish_ha and c > sv and self.Position <= 0:
                self.BuyMarket()
                self._trade_taken_today = True
            elif c < self._or_low and not bullish_ha and c < sv and self.Position >= 0:
                self.SellMarket()
                self._trade_taken_today = True
        self._was_in_or = in_or

    def CreateClone(self):
        return orb_heikin_ashi_spy_correlation_strategy()
