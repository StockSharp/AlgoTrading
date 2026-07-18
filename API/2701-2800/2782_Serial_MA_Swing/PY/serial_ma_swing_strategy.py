import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class serial_ma_swing_strategy(Strategy):
    """Serial MA swing: custom serial moving average that resets on cross, with SL/TP."""
    def __init__(self):
        super(serial_ma_swing_strategy, self).__init__()
        self._sl_points = self.Param("StopLossPoints", 0.0).SetNotNegative().SetDisplay("Stop Loss (points)", "SL distance in price steps", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 0.0).SetNotNegative().SetDisplay("Take Profit (points)", "TP distance in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Data series", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(serial_ma_swing_strategy, self).OnReseted()
        self._ma_sum = 0
        self._ma_count = 0
        self._prev_diff = None
        self._history_count = 0
        self._prev_had_cross = False
        self._prev_ma = None
        self._prev_close = None
        self._entry_price = 0

    def OnStarted2(self, time):
        super(serial_ma_swing_strategy, self).OnStarted2(time)
        self._ma_sum = 0
        self._ma_count = 0
        self._prev_diff = None
        self._history_count = 0
        self._prev_had_cross = False
        self._prev_ma = None
        self._prev_close = None
        self._entry_price = 0
        self._step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._step = float(self.Security.PriceStep)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        self._history_count += 1

        if self._ma_count == 0:
            self._ma_sum = close
            self._ma_count = 1
            self._prev_diff = 0
            self._prev_close = close
            return

        self._ma_sum += close
        self._ma_count += 1
        ma = self._ma_sum / self._ma_count
        diff = ma - close
        is_cross = False
        signal = 0

        if self._prev_diff is not None and diff * self._prev_diff < 0:
            is_cross = True
            signal = 1 if diff < 0 else -1
            ma = close
            diff = 0
            self._ma_sum = close
            self._ma_count = 1

        self._prev_diff = diff

        if self._history_count <= 2:
            self._prev_had_cross = is_cross
            self._prev_ma = ma
            self._prev_close = close
            return

        # Manage SL/TP
        self._handle_protection(candle, close)

        if signal == 0:
            signal = self._get_pending_signal()

        if signal > 0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close
        elif signal < 0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
                self._entry_price = close

        self._prev_had_cross = is_cross
        self._prev_ma = ma
        self._prev_close = close

    def _get_pending_signal(self):
        if not self._prev_had_cross or self._prev_ma is None or self._prev_close is None:
            return 0
        if self._prev_close > self._prev_ma:
            return 1
        if self._prev_close < self._prev_ma:
            return -1
        return 0

    def _handle_protection(self, candle, close):
        step = self._step
        if self.Position > 0 and self._entry_price > 0:
            if self._sl_points.Value > 0:
                sl = self._entry_price - self._sl_points.Value * step
                if float(candle.LowPrice) <= sl:
                    self.SellMarket()
                    self._entry_price = 0
                    return
            if self._tp_points.Value > 0:
                tp = self._entry_price + self._tp_points.Value * step
                if float(candle.HighPrice) >= tp:
                    self.SellMarket()
                    self._entry_price = 0
        elif self.Position < 0 and self._entry_price > 0:
            if self._sl_points.Value > 0:
                sl = self._entry_price + self._sl_points.Value * step
                if float(candle.HighPrice) >= sl:
                    self.BuyMarket()
                    self._entry_price = 0
                    return
            if self._tp_points.Value > 0:
                tp = self._entry_price - self._tp_points.Value * step
                if float(candle.LowPrice) <= tp:
                    self.BuyMarket()
                    self._entry_price = 0

    def CreateClone(self):
        return serial_ma_swing_strategy()
