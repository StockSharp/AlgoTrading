import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class millenium_code_strategy(Strategy):
    def __init__(self):
        super(millenium_code_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast MA", "Fast moving average length", "Indicators")
        self._slow_length = self.Param("SlowLength", 30) \
            .SetDisplay("Slow MA", "Slow moving average length", "Indicators")
        self._high_low_bars = self.Param("HighLowBars", 10) \
            .SetDisplay("HighLow Bars", "Bars count for high/low search", "Indicators")
        self._reverse_signal = self.Param("ReverseSignal", True) \
            .SetDisplay("Reverse", "Reverse buy/sell logic", "General")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._highest = None
        self._lowest = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def fast_length(self):
        return self._fast_length.Value
    @property
    def slow_length(self):
        return self._slow_length.Value
    @property
    def high_low_bars(self):
        return self._high_low_bars.Value
    @property
    def reverse_signal(self):
        return self._reverse_signal.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value

    def OnReseted(self):
        super(millenium_code_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def OnStarted(self, time):
        super(millenium_code_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_length
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_length
        self._highest = Highest()
        self._highest.Length = self.high_low_bars
        self._lowest = Lowest()
        self._lowest.Length = self.high_low_bars
        self.Indicators.Add(self._highest)
        self.Indicators.Add(self._lowest)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.on_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def on_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast_val = float(fast_val)
        slow_val = float(slow_val)
        high_result = self._highest.Process(candle)
        low_result = self._lowest.Process(candle)
        if not high_result.IsFormed or not low_result.IsFormed:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return
        if self._prev_fast == 0.0 or self._prev_slow == 0.0:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return
        cross_up = self._prev_fast < self._prev_slow and fast_val > slow_val
        cross_down = self._prev_fast > self._prev_slow and fast_val < slow_val
        direction = 0
        if cross_up:
            direction = -1 if self.reverse_signal else 1
        elif cross_down:
            direction = 1 if self.reverse_signal else -1
        if direction == 1 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif direction == -1 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return millenium_code_strategy()
