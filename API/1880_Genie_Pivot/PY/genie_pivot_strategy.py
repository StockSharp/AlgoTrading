import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class genie_pivot_strategy(Strategy):
    """
    Pivot point reversal scalping strategy.
    Buys on 3 declining lows followed by higher low reversal.
    Sells on 3 rising highs followed by lower high reversal.
    Includes trailing stop and take profit management.
    """

    def __init__(self):
        super(genie_pivot_strategy, self).__init__()
        self._take_profit = self.Param("TakeProfit", 500.0) \
            .SetDisplay("Take Profit", "Profit target in points", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 200.0) \
            .SetDisplay("Trailing Stop", "Trailing distance in points", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after closing", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._lows = [0.0] * 8
        self._highs = [0.0] * 8
        self._filled = 0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._target_price = 0.0
        self._loss_count = 0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(genie_pivot_strategy, self).OnReseted()
        self._lows = [0.0] * 8
        self._highs = [0.0] * 8
        self._filled = 0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._target_price = 0.0
        self._loss_count = 0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(genie_pivot_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        for i in range(len(self._lows) - 1, 0, -1):
            self._lows[i] = self._lows[i - 1]
            self._highs[i] = self._highs[i - 1]
        self._lows[0] = low
        self._highs[0] = high

        if self._filled < 4:
            self._filled += 1
            return

        if self.Position == 0:
            if self._cooldown_remaining > 0:
                return

            buy_cond = (self._lows[4] > self._lows[3] and self._lows[3] > self._lows[2]
                        and self._lows[2] > self._lows[1] and self._lows[1] < self._lows[0]
                        and close > self._highs[1] and close > open_p)

            sell_cond = (self._highs[4] < self._highs[3] and self._highs[3] < self._highs[2]
                         and self._highs[2] < self._highs[1] and self._highs[1] > self._highs[0]
                         and close < self._lows[1] and close < open_p)

            if buy_cond:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = self._entry_price - self._trailing_stop.Value * step
                self._target_price = self._entry_price + self._take_profit.Value * step
            elif sell_cond:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = self._entry_price + self._trailing_stop.Value * step
                self._target_price = self._entry_price - self._take_profit.Value * step

        elif self.Position > 0:
            if close >= self._target_price:
                self.SellMarket()
                self._loss_count = 0
                self._cooldown_remaining = self._cooldown_bars.Value
            else:
                trailing = self._trailing_stop.Value * step
                if close - self._entry_price > trailing:
                    new_stop = close - trailing
                    if new_stop > self._stop_price:
                        self._stop_price = new_stop

                if low <= self._stop_price:
                    self.SellMarket()
                    self._loss_count += 1
                    self._cooldown_remaining = self._cooldown_bars.Value

        elif self.Position < 0:
            if close <= self._target_price:
                self.BuyMarket()
                self._loss_count = 0
                self._cooldown_remaining = self._cooldown_bars.Value
            else:
                trailing = self._trailing_stop.Value * step
                if self._entry_price - close > trailing:
                    new_stop = close + trailing
                    if new_stop < self._stop_price:
                        self._stop_price = new_stop

                if high >= self._stop_price:
                    self.BuyMarket()
                    self._loss_count += 1
                    self._cooldown_remaining = self._cooldown_bars.Value

    def CreateClone(self):
        return genie_pivot_strategy()
