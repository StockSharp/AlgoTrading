import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bollinger_heikin_ashi_entry_strategy(Strategy):
    def __init__(self):
        super(bollinger_heikin_ashi_entry_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Bollinger Bands length", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 1.5) \
            .SetDisplay("Bollinger Deviation", "Bollinger Bands standard deviation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._entry_price = 0.0
        self._initial_stop = 0.0
        self._first_target = 0.0
        self._first_target_reached = False
        self._trail_stop = 0.0
        self._is_ha_initialized = False
        self._ha_open1 = 0.0
        self._ha_close1 = 0.0
        self._ha_high1 = 0.0
        self._ha_low1 = 0.0
        self._ha_open2 = 0.0
        self._ha_close2 = 0.0
        self._ha_high2 = 0.0
        self._ha_low2 = 0.0
        self._upper_bb1 = 0.0
        self._lower_bb1 = 0.0
        self._upper_bb2 = 0.0
        self._lower_bb2 = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._cooldown = 0

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value
    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_heikin_ashi_entry_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._initial_stop = 0.0
        self._first_target = 0.0
        self._first_target_reached = False
        self._trail_stop = 0.0
        self._is_ha_initialized = False
        self._ha_open1 = 0.0
        self._ha_close1 = 0.0
        self._ha_high1 = 0.0
        self._ha_low1 = 0.0
        self._ha_open2 = 0.0
        self._ha_close2 = 0.0
        self._ha_high2 = 0.0
        self._ha_low2 = 0.0
        self._upper_bb1 = 0.0
        self._lower_bb1 = 0.0
        self._upper_bb2 = 0.0
        self._lower_bb2 = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(bollinger_heikin_ashi_entry_strategy, self).OnStarted2(time)
        bb = BollingerBands()
        bb.Length = self.bollinger_period
        bb.Width = self.bollinger_deviation
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if bb_value.UpBand is None or bb_value.LowBand is None:
            return
        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)

        ha_close = (float(candle.OpenPrice) + float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 4.0
        if not self._is_ha_initialized:
            ha_open = (float(candle.OpenPrice) + float(candle.ClosePrice)) / 2.0
            self._is_ha_initialized = True
        else:
            ha_open = (self._ha_open1 + self._ha_close1) / 2.0

        ha_high = max(float(candle.HighPrice), ha_open, ha_close)
        ha_low = min(float(candle.LowPrice), ha_open, ha_close)

        red1 = self._ha_close1 < self._ha_open1 and (self._ha_low1 <= self._lower_bb1 or self._ha_close1 <= self._lower_bb1)
        green1 = self._ha_close1 > self._ha_open1 and (self._ha_high1 >= self._upper_bb1 or self._ha_close1 >= self._upper_bb1)

        buy_signal = red1 and ha_close > ha_open and ha_close > lower
        sell_signal = green1 and ha_close < ha_open and ha_close < upper

        if self._cooldown > 0:
            self._cooldown -= 1

        if buy_signal and self.Position <= 0 and self._cooldown == 0:
            self._entry_price = float(candle.ClosePrice)
            self._initial_stop = self._prev_low if self._prev_low > 0 else float(candle.LowPrice)
            self._first_target = self._entry_price + (self._entry_price - self._initial_stop)
            self._first_target_reached = False
            self._trail_stop = 0.0
            self._cooldown = 20
            self.BuyMarket()
        elif sell_signal and self.Position >= 0 and self._cooldown == 0:
            self._entry_price = float(candle.ClosePrice)
            self._initial_stop = self._prev_high if self._prev_high > 0 else float(candle.HighPrice)
            self._first_target = self._entry_price - (self._initial_stop - self._entry_price)
            self._first_target_reached = False
            self._trail_stop = 0.0
            self._cooldown = 20
            self.SellMarket()

        if self.Position > 0:
            if float(candle.HighPrice) >= self._first_target:
                self._first_target_reached = True
                self._trail_stop = max(self._entry_price, self._prev_low)
            if self._first_target_reached:
                self._trail_stop = max(self._trail_stop, self._prev_low)
            current_stop = self._trail_stop if self._first_target_reached else self._initial_stop
            if current_stop > 0 and float(candle.LowPrice) <= current_stop:
                self.SellMarket()
        elif self.Position < 0:
            if float(candle.LowPrice) <= self._first_target:
                self._first_target_reached = True
                self._trail_stop = min(self._entry_price, self._prev_high)
            if self._first_target_reached and self._trail_stop > 0:
                self._trail_stop = min(self._trail_stop, self._prev_high)
            current_stop = self._trail_stop if self._first_target_reached else self._initial_stop
            if current_stop > 0 and float(candle.HighPrice) >= current_stop:
                self.BuyMarket()

        self._ha_open2 = self._ha_open1
        self._ha_close2 = self._ha_close1
        self._ha_high2 = self._ha_high1
        self._ha_low2 = self._ha_low1
        self._upper_bb2 = self._upper_bb1
        self._lower_bb2 = self._lower_bb1
        self._ha_open1 = ha_open
        self._ha_close1 = ha_close
        self._ha_high1 = ha_high
        self._ha_low1 = ha_low
        self._upper_bb1 = upper
        self._lower_bb1 = lower
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)

    def CreateClone(self):
        return bollinger_heikin_ashi_entry_strategy()
