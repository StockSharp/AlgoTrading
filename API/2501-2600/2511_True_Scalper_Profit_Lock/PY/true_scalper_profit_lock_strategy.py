import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
class true_scalper_profit_lock_strategy(Strategy):
    def __init__(self):
        super(true_scalper_profit_lock_strategy, self).__init__()
        self._tp_points = self.Param("TakeProfitPoints", 44.0)
        self._sl_points = self.Param("StopLossPoints", 90.0)
        self._fast_period = self.Param("FastPeriod", 3)
        self._slow_period = self.Param("SlowPeriod", 7)
        self._rsi_length = self.Param("RsiLength", 2)
        self._rsi_threshold = self.Param("RsiThreshold", 50.0)
        self._abandon_bars = self.Param("AbandonBars", 101)
        self._be_trigger = self.Param("BreakEvenTrigger", 25.0)
        self._be_offset = self.Param("BreakEvenOffset", 3.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(true_scalper_profit_lock_strategy, self).OnReseted()
        self._entry_price = 0
        self._stop_price = 0
        self._take_price = 0
        self._prev_rsi = 0
        self._is_long = False
        self._bars_since_entry = 0
        self._be_applied = False

    def OnStarted2(self, time):
        super(true_scalper_profit_lock_strategy, self).OnStarted2(time)
        self._entry_price = 0
        self._stop_price = 0
        self._take_price = 0
        self._prev_rsi = 0
        self._is_long = False
        self._bars_since_entry = 0
        self._be_applied = False

        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_val)
        slow_val = float(slow_val)
        rsi_val = float(rsi_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        step = 1.0

        # Count bars since entry
        if self.Position != 0:
            self._bars_since_entry += 1
        else:
            self._bars_since_entry = 0

        # Abandon logic
        if self.Position != 0 and self._bars_since_entry >= self._abandon_bars.Value:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._reset_trade()
            self._prev_rsi = rsi_val
            return

        # Break-even
        if self.Position != 0 and self._entry_price > 0 and not self._be_applied:
            trigger = self._be_trigger.Value * step
            offset = self._be_offset.Value * step
            if self._is_long and self.Position > 0:
                if high >= self._entry_price + trigger:
                    new_stop = self._entry_price + offset
                    if new_stop > self._stop_price:
                        self._stop_price = new_stop
                    self._be_applied = True
            elif not self._is_long and self.Position < 0:
                if low <= self._entry_price - trigger:
                    new_stop = self._entry_price - offset
                    if self._stop_price == 0 or new_stop < self._stop_price:
                        self._stop_price = new_stop
                    self._be_applied = True

        # SL/TP exits
        if self._is_long and self.Position > 0:
            if self._take_price > 0 and high >= self._take_price:
                self.SellMarket()
                self._reset_trade()
                self._prev_rsi = rsi_val
                return
            if self._stop_price > 0 and low <= self._stop_price:
                self.SellMarket()
                self._reset_trade()
                self._prev_rsi = rsi_val
                return
        elif not self._is_long and self.Position < 0:
            if self._take_price > 0 and low <= self._take_price:
                self.BuyMarket()
                self._reset_trade()
                self._prev_rsi = rsi_val
                return
            if self._stop_price > 0 and high >= self._stop_price:
                self.BuyMarket()
                self._reset_trade()
                self._prev_rsi = rsi_val
                return

        # RSI signals
        rsi_positive = False
        rsi_negative = False
        if self._prev_rsi > 0:
            thr = self._rsi_threshold.Value
            if rsi_val > thr and self._prev_rsi < thr:
                rsi_positive = True
            elif rsi_val < thr and self._prev_rsi > thr:
                rsi_negative = True

        buy_signal = fast_val > slow_val + step and rsi_negative
        sell_signal = fast_val < slow_val - step and rsi_positive

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._is_long = True
            self._stop_price = close - step * self._sl_points.Value
            self._take_price = close + step * self._tp_points.Value
            self._be_applied = False
            self._bars_since_entry = 0
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._is_long = False
            self._stop_price = close + step * self._sl_points.Value
            self._take_price = close - step * self._tp_points.Value
            self._be_applied = False
            self._bars_since_entry = 0

        self._prev_rsi = rsi_val

    def _reset_trade(self):
        self._entry_price = 0
        self._stop_price = 0
        self._take_price = 0
        self._be_applied = False
        self._bars_since_entry = 0

    def CreateClone(self):
        return true_scalper_profit_lock_strategy()
