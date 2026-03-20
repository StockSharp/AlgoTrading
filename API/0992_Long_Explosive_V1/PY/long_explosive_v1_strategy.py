import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class long_explosive_v1_strategy(Strategy):
    def __init__(self):
        super(long_explosive_v1_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._price_increase_percent = self.Param("PriceIncreasePercent", 0.5) \
            .SetDisplay("Price increase (%)", "Percentage increase to go long", "General")
        self._price_decrease_percent = self.Param("PriceDecreasePercent", 0.5) \
            .SetDisplay("Price decrease (%)", "Percentage decrease to go short", "General")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Min bars between signals", "General")
        self._previous_close = 0.0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(long_explosive_v1_strategy, self).OnReseted()
        self._previous_close = 0.0
        self._bars_since_signal = 0

    def OnStarted(self, time):
        super(long_explosive_v1_strategy, self).OnStarted(time)
        self._previous_close = 0.0
        self._bars_since_signal = 0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        close = float(candle.ClosePrice)
        if self._previous_close == 0.0:
            self._previous_close = close
            return
        change = (close - self._previous_close) / self._previous_close * 100.0
        self._previous_close = close
        if self._bars_since_signal < self._cooldown_bars.Value:
            return
        inc = float(self._price_increase_percent.Value)
        dec = float(self._price_decrease_percent.Value)
        if change > inc and self.Position <= 0:
            self.BuyMarket()
            self._bars_since_signal = 0
        elif change < -dec and self.Position >= 0:
            self.SellMarket()
            self._bars_since_signal = 0

    def CreateClone(self):
        return long_explosive_v1_strategy()
