import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class geedo_strategy(Strategy):
    def __init__(self):
        super(geedo_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 6) \
            .SetDisplay("Lookback", "Open price lookback bars", "Indicators")
        self._atr_period = self.Param("AtrPeriod", TimeSpan.FromHours(4)) \
            .SetDisplay("ATR Period", "ATR period for stops", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0

    @property
    def lookback(self):
        return self._lookback.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(geedo_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(geedo_strategy, self).OnStarted(time)
        atr = StandardDeviation()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return
        self._open_history.append(candle.OpenPrice)
        if len(self._open_history) > self.lookback + 1:
            self._open_history.pop(0)
        if len(self._open_history) <= self.lookback:
            if atr_val <= 0:
            close = candle.ClosePrice
        # Exit check
        if self.Position > 0 and self._entry_price > 0:
            if close <= self._entry_price - atr_val * 2 or close >= self._entry_price + atr_val * 1.5:
                self.SellMarket()
                self._entry_price = 0
                return
        elif self.Position < 0 and self._entry_price > 0:
            if close >= self._entry_price + atr_val * 2 or close <= self._entry_price - atr_val * 1.5:
                self.BuyMarket()
                self._entry_price = 0
                return
        past_open = self._open_history[0]
        current_open = self._open_history[^1]
        diff = current_open - past_open
        # Price rising => long
        if diff > atr_val * 0.5 and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
            self._entry_price = close
        # Price falling => short
        elif diff < -atr_val * 0.5 and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()
            self._entry_price = close

    def CreateClone(self):
        return geedo_strategy()
