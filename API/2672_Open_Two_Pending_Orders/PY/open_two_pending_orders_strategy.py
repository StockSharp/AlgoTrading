import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class open_two_pending_orders_strategy(Strategy):
    def __init__(self):
        super(open_two_pending_orders_strategy, self).__init__()
        self._sl_points = self.Param("StopLossPoints", 5000.0).SetDisplay("Stop Loss (steps)", "Stop loss distance in price steps", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 8000.0).SetDisplay("Take Profit (steps)", "Take profit distance in price steps", "Risk")
        self._trail_points = self.Param("TrailingStopPoints", 3000.0).SetDisplay("Trailing Stop (steps)", "Trailing stop distance in price steps", "Risk")
        self._entry_offset = self.Param("EntryOffsetPoints", 1000.0).SetDisplay("Entry Offset (steps)", "Offset from close for pending entries", "Execution")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(open_two_pending_orders_strategy, self).OnReseted()
        self._reset_state()

    def _reset_state(self):
        self._pending_buy = None
        self._pending_sell = None
        self._entry_price = None
        self._stop_level = None
        self._take_level = None
        self._highest = 0
        self._lowest = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(open_two_pending_orders_strategy, self).OnStarted(time)
        self._reset_state()

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
        if self._cooldown > 0:
            self._cooldown -= 1
            return

        step = self._get_step()

        # Manage existing position
        if self.Position != 0 and self._entry_price is not None:
            if self.Position > 0:
                self._highest = max(self._highest, candle.HighPrice)
                if self._stop_level is not None and candle.LowPrice <= self._stop_level:
                    self.SellMarket()
                    self._reset_state()
                    self._cooldown = 20
                    return
                if self._take_level is not None and candle.HighPrice >= self._take_level:
                    self.SellMarket()
                    self._reset_state()
                    self._cooldown = 20
                    return
                # trailing
                if self._trail_points.Value > 0:
                    trail_dist = self._trail_points.Value * step
                    if self._highest - self._entry_price >= trail_dist:
                        desired = self._highest - trail_dist
                        if self._stop_level is None or desired > self._stop_level:
                            self._stop_level = desired
            elif self.Position < 0:
                self._lowest = min(self._lowest, candle.LowPrice)
                if self._stop_level is not None and candle.HighPrice >= self._stop_level:
                    self.BuyMarket()
                    self._reset_state()
                    self._cooldown = 20
                    return
                if self._take_level is not None and candle.LowPrice <= self._take_level:
                    self.BuyMarket()
                    self._reset_state()
                    self._cooldown = 20
                    return
                if self._trail_points.Value > 0:
                    trail_dist = self._trail_points.Value * step
                    if self._entry_price - self._lowest >= trail_dist:
                        desired = self._lowest + trail_dist
                        if self._stop_level is None or desired < self._stop_level:
                            self._stop_level = desired

            if self.Position == 0:
                self._reset_state()
                self._cooldown = 20
            return

        # Check pending entries
        if self._pending_buy is not None and self._pending_sell is not None:
            if candle.HighPrice >= self._pending_buy:
                entry = self._pending_buy
                self._pending_buy = None
                self._pending_sell = None
                self.BuyMarket()
                self._entry_price = entry
                self._highest = entry
                self._lowest = entry
                self._stop_level = entry - self._sl_points.Value * step if self._sl_points.Value > 0 else None
                self._take_level = entry + self._tp_points.Value * step if self._tp_points.Value > 0 else None
                return
            if candle.LowPrice <= self._pending_sell:
                entry = self._pending_sell
                self._pending_buy = None
                self._pending_sell = None
                self.SellMarket()
                self._entry_price = entry
                self._highest = entry
                self._lowest = entry
                self._stop_level = entry + self._sl_points.Value * step if self._sl_points.Value > 0 else None
                self._take_level = entry - self._tp_points.Value * step if self._tp_points.Value > 0 else None
                return
        else:
            offset = self._entry_offset.Value * step
            self._pending_buy = candle.ClosePrice + offset
            self._pending_sell = candle.ClosePrice - offset

    def CreateClone(self):
        return open_two_pending_orders_strategy()
