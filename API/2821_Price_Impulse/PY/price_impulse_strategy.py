import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class price_impulse_strategy(Strategy):
    def __init__(self):
        super(price_impulse_strategy, self).__init__()
        self._sl_points = self.Param("StopLossPoints", 150).SetNotNegative().SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 50).SetNotNegative().SetDisplay("Take Profit Points", "Take profit distance in price steps", "Risk")
        self._impulse_points = self.Param("ImpulsePoints", 15).SetGreaterThanZero().SetDisplay("Impulse Points", "Minimum price impulse to trade", "Signals")
        self._history_gap = self.Param("HistoryGap", 15).SetNotNegative().SetDisplay("Gap Candles", "Candles between comparison points", "Signals")
        self._extra_history = self.Param("ExtraHistory", 15).SetNotNegative().SetDisplay("Extra History", "Additional buffer samples", "Signals")
        self._cooldown_seconds = self.Param("CooldownSeconds", 100).SetNotNegative().SetDisplay("Cooldown Seconds", "Min seconds between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle type for price tracking", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(price_impulse_strategy, self).OnReseted()
        self._price_history = []
        self._tick_size = 0
        self._last_trade_time = None
        self._entry_price = None
        self._stop_price = None
        self._tp_price = None

    def OnStarted(self, time):
        super(price_impulse_strategy, self).OnStarted(time)
        self._price_history = []
        self._tick_size = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._tick_size = float(self.Security.PriceStep)
        self._last_trade_time = None
        self._entry_price = None
        self._stop_price = None
        self._tp_price = None

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def _history_capacity(self):
        gap = self._history_gap.Value
        extra = self._extra_history.Value
        return max(gap + extra + 1, gap + 1)

    def _is_cooldown_passed(self, candle_time):
        if self._last_trade_time is None:
            return True
        cd = self._cooldown_seconds.Value
        if cd <= 0:
            return True
        return (candle_time - self._last_trade_time) >= TimeSpan.FromSeconds(cd)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        self._price_history.append(close)

        cap = self._history_capacity()
        while len(self._price_history) > cap:
            self._price_history.pop(0)

        candle_time = candle.CloseTime

        if self.Position > 0:
            if self._stop_price is not None and candle.LowPrice <= self._stop_price:
                self.SellMarket()
                self._entry_price = None
                self._stop_price = None
                self._tp_price = None
                return
            if self._tp_price is not None and candle.HighPrice >= self._tp_price:
                self.SellMarket()
                self._entry_price = None
                self._stop_price = None
                self._tp_price = None
                return
        elif self.Position < 0:
            if self._stop_price is not None and candle.HighPrice >= self._stop_price:
                self.BuyMarket()
                self._entry_price = None
                self._stop_price = None
                self._tp_price = None
                return
            if self._tp_price is not None and candle.LowPrice <= self._tp_price:
                self.BuyMarket()
                self._entry_price = None
                self._stop_price = None
                self._tp_price = None
                return

        gap = self._history_gap.Value
        if len(self._price_history) <= gap:
            return

        impulse_threshold = self._impulse_points.Value * self._tick_size
        last_idx = len(self._price_history) - 1
        compare_idx = last_idx - gap
        if compare_idx < 0:
            return

        comparison_price = self._price_history[compare_idx]
        up_impulse = close - comparison_price
        down_impulse = comparison_price - close

        if up_impulse > impulse_threshold and self.Position <= 0 and self._is_cooldown_passed(candle_time):
            self.BuyMarket()
            self._entry_price = close
            self._stop_price = close - self._sl_points.Value * self._tick_size if self._sl_points.Value > 0 else None
            self._tp_price = close + self._tp_points.Value * self._tick_size if self._tp_points.Value > 0 else None
            self._last_trade_time = candle_time
            return

        if down_impulse > impulse_threshold and self.Position >= 0 and self._is_cooldown_passed(candle_time):
            self.SellMarket()
            self._entry_price = close
            self._stop_price = close + self._sl_points.Value * self._tick_size if self._sl_points.Value > 0 else None
            self._tp_price = close - self._tp_points.Value * self._tick_size if self._tp_points.Value > 0 else None
            self._last_trade_time = candle_time

    def CreateClone(self):
        return price_impulse_strategy()
