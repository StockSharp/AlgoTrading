import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

class cidomo_v1_strategy(Strategy):
    """
    Daily breakout strategy based on previous range with trailing stop and break-even.
    """

    def __init__(self):
        super(cidomo_v1_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 32) \
            .SetDisplay("Lookback", "Number of candles to look back", "General")
        self._delta = self.Param("Delta", 0.0) \
            .SetDisplay("Delta", "Price offset added to breakout levels", "General")
        self._stop_loss = self.Param("StopLoss", 60.0) \
            .SetDisplay("Stop Loss", "Stop loss in points", "Risk")
        self._take_profit = self.Param("TakeProfit", 70.0) \
            .SetDisplay("Take Profit", "Take profit in points", "Risk")
        self._no_loss = self.Param("NoLoss", 35.0) \
            .SetDisplay("Break-even", "Move stop to entry after profit", "Risk")
        self._trailing = self.Param("Trailing", 5.0) \
            .SetDisplay("Trailing", "Trailing distance in points", "Risk")
        self._use_time_filter = self.Param("UseTimeFilter", True) \
            .SetDisplay("Use Time Filter", "Trade only after specified time", "General")
        self._trade_time = self.Param("TradeTime", TimeSpan(9, 0, 0)) \
            .SetDisplay("Trade Time", "Time to calculate breakout", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type for analysis", "General")

        self._long_level = 0.0
        self._short_level = 0.0
        self._last_trade_day = None
        self._entry_price = 0.0
        self._stop_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cidomo_v1_strategy, self).OnReseted()
        self._long_level = 0.0
        self._short_level = 0.0
        self._last_trade_day = None
        self._entry_price = 0.0
        self._stop_price = 0.0

    def OnStarted2(self, time):
        super(cidomo_v1_strategy, self).OnStarted2(time)

        highest = Highest()
        highest.Length = self._lookback.Value
        lowest = Lowest()
        lowest.Length = self._lookback.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def on_process(self, candle, highest_val, lowest_val):
        if candle.State != CandleStates.Finished:
            return

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and float(self.Security.PriceStep) > 0:
            step = float(self.Security.PriceStep)

        t = candle.OpenTime.TimeOfDay
        current_date = candle.OpenTime.Date

        if (not self._use_time_filter.Value or t >= self._trade_time.Value) and self._last_trade_day != current_date:
            self._long_level = float(highest_val) + self._delta.Value * step
            self._short_level = float(lowest_val) - self._delta.Value * step
            self._last_trade_day = current_date

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self.Position == 0:
            if high >= self._long_level:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = self._entry_price - self._stop_loss.Value * step
            elif low <= self._short_level:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = self._entry_price + self._stop_loss.Value * step
        elif self.Position > 0:
            if low <= self._stop_price:
                self.SellMarket()
                return

            if self._take_profit.Value > 0 and high >= self._entry_price + self._take_profit.Value * step:
                self.SellMarket()
                return

            if self._no_loss.Value > 0 and self._stop_price < self._entry_price and high >= self._entry_price + self._no_loss.Value * step:
                self._stop_price = self._entry_price

            if self._trailing.Value > 0 and high >= self._entry_price + self._trailing.Value * step:
                new_stop = close - self._trailing.Value * step
                if new_stop > self._stop_price:
                    self._stop_price = new_stop
        elif self.Position < 0:
            if high >= self._stop_price:
                self.BuyMarket()
                return

            if self._take_profit.Value > 0 and low <= self._entry_price - self._take_profit.Value * step:
                self.BuyMarket()
                return

            if self._no_loss.Value > 0 and self._stop_price > self._entry_price and low <= self._entry_price - self._no_loss.Value * step:
                self._stop_price = self._entry_price

            if self._trailing.Value > 0 and low <= self._entry_price - self._trailing.Value * step:
                new_stop = close + self._trailing.Value * step
                if new_stop < self._stop_price:
                    self._stop_price = new_stop

    def CreateClone(self):
        return cidomo_v1_strategy()
