import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class optimized_heikin_ashi_buy_sell_strategy(Strategy):
    def __init__(self):
        super(optimized_heikin_ashi_buy_sell_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._ema_length = self.Param("EmaLength", 50) \
            .SetGreaterThanZero()
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._ha_init = False
        self._prev_bullish = False
        self._prev_bearish = False
        self._last_signal_ticks = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(optimized_heikin_ashi_buy_sell_strategy, self).OnReseted()
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._ha_init = False
        self._prev_bullish = False
        self._prev_bearish = False
        self._last_signal_ticks = 0

    def OnStarted2(self, time):
        super(optimized_heikin_ashi_buy_sell_strategy, self).OnStarted2(time)
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._ha_init = False
        self._prev_bullish = False
        self._prev_bearish = False
        self._last_signal_ticks = 0
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self.OnProcess).Start()

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._ema.IsFormed:
            return
        ev = float(ema_val)
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        if not self._ha_init:
            ha_open = (o + c) / 2.0
            ha_close = (o + h + l + c) / 4.0
            self._prev_ha_open = ha_open
            self._prev_ha_close = ha_close
            self._ha_init = True
            self._prev_bullish = ha_close > ha_open
            self._prev_bearish = ha_close < ha_open
            return
        ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0
        ha_close = (o + h + l + c) / 4.0
        is_bullish = ha_close > ha_open
        is_bearish = ha_close < ha_open
        cooldown_ticks = TimeSpan.FromMinutes(600).Ticks
        current_ticks = candle.OpenTime.Ticks
        if current_ticks - self._last_signal_ticks >= cooldown_ticks:
            if not self._prev_bullish and is_bullish and c > ev and self.Position <= 0:
                self.BuyMarket()
                self._last_signal_ticks = current_ticks
            elif not self._prev_bearish and is_bearish and c < ev and self.Position >= 0:
                self.SellMarket()
                self._last_signal_ticks = current_ticks
        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close
        self._prev_bullish = is_bullish
        self._prev_bearish = is_bearish

    def CreateClone(self):
        return optimized_heikin_ashi_buy_sell_strategy()
