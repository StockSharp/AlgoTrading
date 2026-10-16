import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class bykov_trend_strategy(Strategy):
    """Two-threshold Williams %R transitions with signal arming and full reversals."""
    def __init__(self):
        super(bykov_trend_strategy, self).__init__()
        self._risk = self.Param("Risk", 3).SetDisplay("Risk", "K=33-Risk", "Indicator")
        self._ssp = self.Param("SSP", 9).SetDisplay("SSP", "Williams %R look-back", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay(
            "Candle Type", "Timeframe for the oscillator", "General")
        self._reset_signals()

    @property
    def risk(self): return self._risk.Value
    @property
    def SSP(self): return self._ssp.Value
    @SSP.setter
    def SSP(self, value): self._ssp.Value = value
    @property
    def ssp(self): return self.SSP
    @property
    def candle_type(self): return self._candle_type.Value

    def _reset_signals(self):
        self._previous_wpr = None
        self._long_armed = False
        self._short_armed = False
        self._pending_order = None

    def OnReseted(self):
        super(bykov_trend_strategy, self).OnReseted()
        self._reset_signals()

    def OnStarted2(self, time):
        super(bykov_trend_strategy, self).OnStarted2(time)
        if not 1 <= self.risk < 33:
            raise ValueError("Bykov Trend Risk must be from 1 to 32")
        self._reset_signals()
        wpr = WilliamsR()
        wpr.Length = self.SSP
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wpr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wpr)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, wpr_value):
        if candle.State != CandleStates.Finished:
            return
        wpr = float(wpr_value)
        k = 33 - float(self.risk)
        lower = -100 + k
        upper = -k
        if wpr <= lower:
            self._long_armed = True
        if wpr >= upper:
            self._short_armed = True
        old = self._previous_wpr
        long_signal = old is not None and self._long_armed and old <= upper and wpr > upper
        short_signal = old is not None and self._short_armed and old >= lower and wpr < lower
        self._previous_wpr = wpr
        if long_signal: self._long_armed = False
        if short_signal: self._short_armed = False
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if long_signal and self.Position <= 0:
            self._pending_order = self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self._pending_order = self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return bykov_trend_strategy()
