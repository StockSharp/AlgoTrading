import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class advanced_position_management_strategy(Strategy):
    def __init__(self):
        super(advanced_position_management_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA Length", "Period of the fast EMA", "General")
        self._slow_length = self.Param("SlowLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA Length", "Period of the slow EMA", "General")
        self._take_profit_percent = self.Param("TakeProfitPercent", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0

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
        super(advanced_position_management_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(advanced_position_management_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast_val)
        slow = float(slow_val)
        close = float(candle.ClosePrice)
        sl_pct = float(self._stop_loss_percent.Value)
        tp_pct = float(self._take_profit_percent.Value)
        if self.Position > 0 and self._entry_price > 0:
            sl = self._entry_price * (1.0 - sl_pct / 100.0)
            tp = self._entry_price * (1.0 + tp_pct / 100.0)
            if close <= sl or close >= tp:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown_remaining = self.cooldown_bars
                self._prev_fast = fast
                self._prev_slow = slow
                return
        elif self.Position < 0 and self._entry_price > 0:
            sl = self._entry_price * (1.0 + sl_pct / 100.0)
            tp = self._entry_price * (1.0 - tp_pct / 100.0)
            if close >= sl or close <= tp:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown_remaining = self.cooldown_bars
                self._prev_fast = fast
                self._prev_slow = slow
                return
        if self._prev_fast == 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return
        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow
        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown_remaining = self.cooldown_bars
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown_remaining = self.cooldown_bars
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return advanced_position_management_strategy()
