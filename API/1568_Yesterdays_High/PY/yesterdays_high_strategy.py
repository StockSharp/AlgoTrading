import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class yesterdays_high_strategy(Strategy):
    def __init__(self):
        super(yesterdays_high_strategy, self).__init__()
        self._gap = self.Param("Gap", 0.5) \
            .SetDisplay("Gap%", "Entry gap percent above prev high", "Entry")
        self._stop_loss = self.Param("StopLoss", 2) \
            .SetDisplay("Stop-loss", "Stop-loss percent", "Risk")
        self._take_profit = self.Param("TakeProfit", 5) \
            .SetDisplay("Take-profit", "Take-profit percent", "Risk")
        self._trail_offset = self.Param("TrailOffset", 1) \
            .SetDisplay("Trail Offset", "Trailing stop offset percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_high = 0.0
        self._current_high = 0.0
        self._session_date = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._trail_highest = 0.0
        self._trail_active = False

    @property
    def gap(self):
        return self._gap.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def trail_offset(self):
        return self._trail_offset.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(yesterdays_high_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._current_high = 0.0
        self._session_date = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._trail_highest = 0.0
        self._trail_active = False

    def OnStarted(self, time):
        super(yesterdays_high_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        # Track daily highs
        date = candle.OpenTime.Date
        if self._session_date is None:
            self._session_date = date
            self._current_high = candle.HighPrice
        elif date > self._session_date:
            self._prev_high = self._current_high
            self._current_high = candle.HighPrice
            self._session_date = date
        else:
            if candle.HighPrice > self._current_high:
                self._current_high = candle.HighPrice
        price = candle.ClosePrice
        # Exit management
        if self.Position > 0 and self._entry_price > 0:
            if price > self._trail_highest:
                self._trail_highest = price
            # Trailing stop activation
            if not self._trail_active and price >= self._entry_price * (1 + self.trail_offset / 100):
                self._trail_active = True
            if self._trail_active:
                trail_stop = self._trail_highest * (1 - self.trail_offset / 100)
                if price <= trail_stop:
                    self.SellMarket()
                    self._entry_price = 0
                    return
            # Fixed SL/TP
            if price <= self._stop_price or price >= self._take_price:
                self.SellMarket()
                self._entry_price = 0
                return
        # Entry: price breaks above yesterday's high
        if self.Position == 0 and self._prev_high > 0:
            breakout_level = self._prev_high * (1 + self.gap / 100)
            if price > breakout_level and price > ema_val:
                self.BuyMarket()
                self._entry_price = price
                self._stop_price = self._entry_price * (1 - self.stop_loss / 100)
                self._take_price = self._entry_price * (1 + self.take_profit / 100)
                self._trail_highest = price
                self._trail_active = False

    def CreateClone(self):
        return yesterdays_high_strategy()
