import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class advanced_multi_seasonality_strategy(Strategy):
    def __init__(self):
        super(advanced_multi_seasonality_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._holding_bars = self.Param("HoldingBars", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Holding Bars", "Bars to hold position", "General")
        self._cooldown_bars = self.Param("CooldownBars", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._bar_index = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(advanced_multi_seasonality_strategy, self).OnReseted()
        self._bar_index = 0

    def OnStarted2(self, time):
        super(advanced_multi_seasonality_strategy, self).OnStarted2(time)
        ema1 = ExponentialMovingAverage()
        ema1.Length = 10
        ema2 = ExponentialMovingAverage()
        ema2.Length = 30
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema1, ema2, self.OnProcess).Start()

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        self._bar_index += 1
        if self._bar_index > self._holding_bars.Value and self.Position > 0:
            self.SellMarket()
            return
        if self.Position == 0 and self._bar_index > self.cooldown_bars:
            self.BuyMarket()
            self._bar_index = 0

    def CreateClone(self):
        return advanced_multi_seasonality_strategy()
