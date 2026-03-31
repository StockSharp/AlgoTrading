import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ict_bread_and_butter_sell_setup_strategy(Strategy):
    def __init__(self):
        super(ict_bread_and_butter_sell_setup_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._london_high = 0.0
        self._london_low = 0.0
        self._ny_high = 0.0
        self._ny_low = 0.0
        self._asia_high = 0.0
        self._asia_low = 0.0
        self._in_london = False
        self._in_ny = False
        self._in_asia = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ict_bread_and_butter_sell_setup_strategy, self).OnReseted()
        self._in_london = False
        self._in_ny = False
        self._in_asia = False
        self._london_high = 0.0
        self._london_low = 0.0
        self._ny_high = 0.0
        self._ny_low = 0.0
        self._asia_high = 0.0
        self._asia_low = 0.0

    def OnStarted2(self, time):
        super(ict_bread_and_butter_sell_setup_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        t = candle.OpenTime
        hour = t.Hour
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        if 2 <= hour < 8:
            if not self._in_london:
                self._london_high = high
                self._london_low = low
                self._in_london = True
            else:
                if high > self._london_high:
                    self._london_high = high
                if low < self._london_low:
                    self._london_low = low
        else:
            self._in_london = False
        if 8 <= hour < 16:
            if not self._in_ny:
                self._ny_high = high
                self._ny_low = low
                self._in_ny = True
            else:
                if high > self._ny_high:
                    self._ny_high = high
                if low < self._ny_low:
                    self._ny_low = low
        else:
            self._in_ny = False
        if hour >= 19 or hour < 2:
            if not self._in_asia:
                self._asia_high = high
                self._asia_low = low
                self._in_asia = True
            else:
                if high > self._asia_high:
                    self._asia_high = high
                if low < self._asia_low:
                    self._asia_low = low
        else:
            self._in_asia = False
        judas_swing = high >= self._london_high and 8 <= hour < 16
        short_entry = judas_swing and close < open_p
        if short_entry and self.Position >= 0:
            self.SellMarket()
        london_close_buy = 10 <= hour <= 13 and close < self._london_low
        if london_close_buy and self.Position <= 0:
            self.BuyMarket()
        asia_sell = (hour >= 19 or hour < 2) and close > self._asia_high
        if asia_sell and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return ict_bread_and_butter_sell_setup_strategy()
