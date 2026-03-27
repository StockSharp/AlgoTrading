import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class pending_order_grid_strategy(Strategy):
    def __init__(self):
        super(pending_order_grid_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._sl_points = self.Param("StopLossPoints", 200).SetNotNegative().SetDisplay("Stop Loss (steps)", "Stop loss distance", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 200).SetNotNegative().SetDisplay("Take Profit (steps)", "Take profit distance", "Risk")
        self._cooldown = self.Param("CooldownBars", 50).SetDisplay("Cooldown", "Bars between trades", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(pending_order_grid_strategy, self).OnReseted()
        self._entry_price = 0
        self._cooldown_remaining = 0
        self._prev_close = 0
        self._has_prev = False

    def OnStarted(self, time):
        super(pending_order_grid_strategy, self).OnStarted(time)
        self._entry_price = 0
        self._cooldown_remaining = 0
        self._prev_close = 0
        self._has_prev = False

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def _get_step(self):
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            return float(self.Security.PriceStep)
        return 0.01

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        step = self._get_step()

        if not self._has_prev:
            self._prev_close = close
            self._has_prev = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = close
            return

        # SL/TP check
        if self.Position > 0 and self._entry_price > 0:
            if self._sl_points.Value > 0 and close <= self._entry_price - self._sl_points.Value * step:
                self.SellMarket()
                self._entry_price = 0
                self._cooldown_remaining = self._cooldown.Value
                self._prev_close = close
                return
            if self._tp_points.Value > 0 and close >= self._entry_price + self._tp_points.Value * step:
                self.SellMarket()
                self._entry_price = 0
                self._cooldown_remaining = self._cooldown.Value
                self._prev_close = close
                return
        elif self.Position < 0 and self._entry_price > 0:
            if self._sl_points.Value > 0 and close >= self._entry_price + self._sl_points.Value * step:
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown_remaining = self._cooldown.Value
                self._prev_close = close
                return
            if self._tp_points.Value > 0 and close <= self._entry_price - self._tp_points.Value * step:
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown_remaining = self._cooldown.Value
                self._prev_close = close
                return

        # Simple momentum entry
        if self.Position == 0:
            if close > self._prev_close:
                self.BuyMarket()
                self._entry_price = close
                self._cooldown_remaining = self._cooldown.Value
            elif close < self._prev_close:
                self.SellMarket()
                self._entry_price = close
                self._cooldown_remaining = self._cooldown.Value

        self._prev_close = close

    def CreateClone(self):
        return pending_order_grid_strategy()
