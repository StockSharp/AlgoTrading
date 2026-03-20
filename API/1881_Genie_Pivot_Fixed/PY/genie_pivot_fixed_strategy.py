import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class genie_pivot_fixed_strategy(Strategy):
    def __init__(self):
        super(genie_pivot_fixed_strategy, self).__init__()
        self._take_profit = self.Param("TakeProfit", 500.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit", "Target profit in price units", "Risk Management") \
            .SetOptimize(100.0, 1000.0, 100.0)
        self._trailing_stop = self.Param("TrailingStop", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trailing Stop", "Trailing stop distance in price units", "Risk Management") \
            .SetOptimize(50.0, 500.0, 50.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Risk Management")

        self._lows = [0.0] * 8
        self._highs = [0.0] * 8
        self._stored = 0
        self._cooldown_remaining = 0

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def trailing_stop(self):
        return self._trailing_stop.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(genie_pivot_fixed_strategy, self).OnReseted()
        self._stored = 0
        self._cooldown_remaining = 0
        self._lows = [0.0] * 8
        self._highs = [0.0] * 8

    def OnStarted(self, time):
        super(genie_pivot_fixed_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        self.StartProtection(
            Unit(float(self.take_profit), UnitTypes.Absolute),
            Unit(float(self.trailing_stop), UnitTypes.Absolute),
            True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        # Shift arrays
        for i in range(7, 0, -1):
            self._lows[i] = self._lows[i - 1]
            self._highs[i] = self._highs[i - 1]

        self._lows[0] = float(candle.LowPrice)
        self._highs[0] = float(candle.HighPrice)
        if self._stored < 8:
            self._stored += 1

        if self._stored < 5:
            return

        if self._cooldown_remaining > 0:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        # Buy sequence: falling lows [4] > [3] > [2] > [1]
        buy_seq = self._lows[4] > self._lows[3] and self._lows[3] > self._lows[2] and self._lows[2] > self._lows[1]

        if buy_seq and self._lows[1] < self._lows[0] and self._highs[1] < close and close > open_price:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
            return

        # Sell sequence: rising highs [4] < [3] < [2] < [1]
        sell_seq = self._highs[4] < self._highs[3] and self._highs[3] < self._highs[2] and self._highs[2] < self._highs[1]

        if sell_seq and self._highs[1] > self._highs[0] and self._lows[1] > close and close < open_price:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return genie_pivot_fixed_strategy()
