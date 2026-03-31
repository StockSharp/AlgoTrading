import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from System.Collections.Generic import Queue
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class back_to_the_future_strategy(Strategy):

    def __init__(self):
        super(back_to_the_future_strategy, self).__init__()

        self._bar_size = self.Param("BarSize", 1500.0) \
            .SetDisplay("Price Difference", "Threshold to trigger trades", "General")
        self._history_minutes = self.Param("HistoryMinutes", 240) \
            .SetDisplay("History Minutes", "Minutes back for price comparison", "General")
        self._take_profit = self.Param("TakeProfit", 1500.0) \
            .SetDisplay("Take Profit", "Distance from entry", "Risk")
        self._stop_loss = self.Param("StopLoss", 2000.0) \
            .SetDisplay("Stop Loss", "Distance from entry", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 2) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._history = []
        self._entry_price = 0.0
        self._bars_since_trade = 0

    @property
    def BarSize(self):
        return self._bar_size.Value

    @BarSize.setter
    def BarSize(self, value):
        self._bar_size.Value = value

    @property
    def HistoryMinutes(self):
        return self._history_minutes.Value

    @HistoryMinutes.setter
    def HistoryMinutes(self, value):
        self._history_minutes.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(back_to_the_future_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close_price = float(candle.ClosePrice)
        close_time = candle.CloseTime

        self._history.append((close_time, close_price))

        min_time = close_time - TimeSpan.FromMinutes(self.HistoryMinutes)
        while len(self._history) > 0 and self._history[0][0] < min_time:
            self._history.pop(0)

        if len(self._history) == 0:
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        oldest = self._history[0][1]
        diff = close_price - oldest

        pos = self.Position
        tp = float(self.TakeProfit)
        sl = float(self.StopLoss)
        bar_size = float(self.BarSize)

        if pos > 0:
            if close_price >= self._entry_price + tp or close_price <= self._entry_price - sl:
                self.SellMarket(pos)
                self._bars_since_trade = 0
        elif pos < 0:
            if close_price <= self._entry_price - tp or close_price >= self._entry_price + sl:
                self.BuyMarket(-pos)
                self._bars_since_trade = 0
        elif self._bars_since_trade >= self.CooldownBars:
            if diff > bar_size:
                vol = float(self.Volume) + (float(-pos) if pos < 0 else 0.0)
                self.BuyMarket(vol)
                self._entry_price = close_price
                self._bars_since_trade = 0
            elif diff < -bar_size:
                vol = float(self.Volume) + (float(pos) if pos > 0 else 0.0)
                self.SellMarket(vol)
                self._entry_price = close_price
                self._bars_since_trade = 0

    def OnReseted(self):
        super(back_to_the_future_strategy, self).OnReseted()
        self._history = []
        self._entry_price = 0.0
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return back_to_the_future_strategy()
