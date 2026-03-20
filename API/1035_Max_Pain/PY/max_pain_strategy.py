import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class max_pain_strategy(Strategy):
    def __init__(self):
        super(max_pain_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Volume average lookback", "General")
        self._hold_periods = self.Param("HoldPeriods", 8) \
            .SetDisplay("Hold Periods", "Bars to hold", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._bar_index = 0
        self._entry_bar = None
        self._prev_close = 0.0
        self._volumes = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(max_pain_strategy, self).OnReseted()
        self._bar_index = 0
        self._entry_bar = None
        self._prev_close = 0.0
        self._volumes = []

    def OnStarted(self, time):
        super(max_pain_strategy, self).OnStarted(time)
        self._bar_index = 0
        self._entry_bar = None
        self._prev_close = 0.0
        self._volumes = []
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._bar_index += 1
        vol = float(candle.TotalVolume)
        self._volumes.append(vol)
        lb = self._lookback_period.Value
        if len(self._volumes) > lb:
            self._volumes.pop(0)
        close = float(candle.ClosePrice)
        if len(self._volumes) < lb or self._prev_close == 0.0:
            self._prev_close = close
            return
        avg_vol = sum(self._volumes) / len(self._volumes)
        price_change = abs(close - self._prev_close)
        pain_zone = vol > avg_vol * 1.2 and price_change > self._prev_close * 0.003
        if pain_zone and self.Position <= 0:
            self.BuyMarket()
            self._entry_bar = self._bar_index
        hp = self._hold_periods.Value
        if self.Position > 0 and self._entry_bar is not None and self._bar_index >= self._entry_bar + hp:
            self.SellMarket()
            self._entry_bar = None
        self._prev_close = close

    def CreateClone(self):
        return max_pain_strategy()
