import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class t3_ma_alarm_strategy(Strategy):
    """EMA direction change with SL/TP and cooldown."""
    def __init__(self):
        super(t3_ma_alarm_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 19).SetGreaterThanZero().SetDisplay("MA Period", "EMA length", "Indicator")
        self._ma_shift = self.Param("MaShift", 1).SetDisplay("MA Shift", "Bars shift for direction check", "Indicator")
        self._stop_loss = self.Param("StopLoss", 200.0).SetDisplay("Stop Loss", "Stop-loss distance in price", "Risk")
        self._take_profit = self.Param("TakeProfit", 400.0).SetDisplay("Take Profit", "Take-profit distance in price", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 4).SetDisplay("Cooldown Bars", "Completed candles to wait after position change", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Type of candles for calculation", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(t3_ma_alarm_strategy, self).OnReseted()
        self._ema_values = []
        self._prev_direction = 0
        self._entry_price = 0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(t3_ma_alarm_strategy, self).OnStarted(time)
        self._ema_values = []
        self._prev_direction = 0
        self._entry_price = 0
        self._cooldown_remaining = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ma_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        ema_val = float(ema_val)
        shift = self._ma_shift.Value
        required = shift + 2
        self._ema_values.append(ema_val)
        if len(self._ema_values) > required:
            self._ema_values.pop(0)
        if len(self._ema_values) < required:
            return

        val_shift = self._ema_values[-(1 + shift)]
        val_prev = self._ema_values[-(2 + shift)]
        if val_shift > val_prev:
            direction = 1
        elif val_shift < val_prev:
            direction = -1
        else:
            direction = self._prev_direction

        close = float(candle.ClosePrice)

        if self._cooldown_remaining == 0:
            if self._prev_direction == -1 and direction == 1:
                if self.Position < 0:
                    self.BuyMarket()
                if self.Position <= 0:
                    self._entry_price = close
                    self.BuyMarket()
                    self._cooldown_remaining = self._cooldown_bars.Value
            elif self._prev_direction == 1 and direction == -1:
                if self.Position > 0:
                    self.SellMarket()
                if self.Position >= 0:
                    self._entry_price = close
                    self.SellMarket()
                    self._cooldown_remaining = self._cooldown_bars.Value

        if self.Position != 0 and self._entry_price > 0:
            self._check_exit(close)

        self._prev_direction = direction

    def _check_exit(self, price):
        sl = self._stop_loss.Value
        tp = self._take_profit.Value
        if self.Position > 0:
            if sl > 0 and price <= self._entry_price - sl:
                self.SellMarket()
                self._cooldown_remaining = self._cooldown_bars.Value
            elif tp > 0 and price >= self._entry_price + tp:
                self.SellMarket()
                self._cooldown_remaining = self._cooldown_bars.Value
        elif self.Position < 0:
            if sl > 0 and price >= self._entry_price + sl:
                self.BuyMarket()
                self._cooldown_remaining = self._cooldown_bars.Value
            elif tp > 0 and price <= self._entry_price - tp:
                self.BuyMarket()
                self._cooldown_remaining = self._cooldown_bars.Value

    def CreateClone(self):
        return t3_ma_alarm_strategy()
