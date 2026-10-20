import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

import math
from collections import deque
from System import TimeSpan, DateTime, DateTimeKind, Decimal, Nullable
from StockSharp.Messages import DataType, CandleStates, Sides, OrderStates, Unit, UnitTypes
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

from System import Array
from StockSharp.Algo.Indicators import EMA, IIndicator

class bearish_wick_reversal_strategy(Strategy):
    def __init__(self):
        super(bearish_wick_reversal_strategy, self).__init__()
        self._threshold = self.Param("Threshold", -1.0).SetRange(-100.0, 0.0).SetDisplay(
            "Threshold", "Negative lower wick percentage threshold", "Parameters")
        self._use_ema_filter = self.Param("UseEmaFilter", False).SetDisplay(
            "UseEmaFilter", "Require close above the formed EMA", "Parameters")
        self._ema_period = self.Param("EmaPeriod", 200).SetGreaterThanZero().SetDisplay(
            "EmaPeriod", "Optional EMA length", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay(
            "CandleType", "Finished signal candle interval", "Parameters")
        self._start_time = self.Param("StartTime", DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc)).SetDisplay(
            "StartTime", "First eligible UTC entry time", "Parameters")
        self._end_time = self.Param("EndTime", DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc)).SetDisplay(
            "EndTime", "Last eligible UTC entry time", "Parameters")
        self._reset_state()
        self.OrderRegistering += self._remember_order

    @property
    def Threshold(self):
        return self._threshold.Value

    @Threshold.setter
    def Threshold(self, value):
        self._threshold.Value = value

    @property
    def UseEmaFilter(self):
        return self._use_ema_filter.Value

    @UseEmaFilter.setter
    def UseEmaFilter(self, value):
        self._use_ema_filter.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StartTime(self):
        return self._start_time.Value

    @StartTime.setter
    def StartTime(self, value):
        self._start_time.Value = value

    @property
    def EndTime(self):
        return self._end_time.Value

    @EndTime.setter
    def EndTime(self, value):
        self._end_time.Value = value

    def _reset_state(self):
        self._ema = None
        self._previous_high = Decimal(0)
        self._pending = None
        self._last_time = DateTime.MinValue

    def _remember_order(self, order):
        self._pending = order

    def OnReseted(self):
        super(bearish_wick_reversal_strategy, self).OnReseted()
        self._reset_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        if self.StartTime.Kind != DateTimeKind.Utc or self.EndTime.Kind != DateTimeKind.Utc or self.StartTime > self.EndTime:
            raise ValueError("StartTime and EndTime must form an ordered UTC entry window.")
        super(bearish_wick_reversal_strategy, self).OnStarted2(time)
        candles = self.SubscribeCandles(self.CandleType)
        if self.UseEmaFilter:
            self._ema = EMA()
            self._ema.Length = self.EmaPeriod
            candles.BindEx(Array[IIndicator]([self._ema]), self._process_filtered).Start()
        else:
            candles.Bind(self._process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            if self._ema is not None:
                self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _process_filtered(self, candle, values):
        if values[0].IsFormed:
            self._process(candle, values[0].GetValue[Decimal](None))

    def _process(self, candle, average=None):
        if candle.State != CandleStates.Finished or candle.ClosePrice <= 0 or candle.OpenTime <= self._last_time:
            return
        previous_high = self._previous_high
        self._previous_high = candle.HighPrice
        self._last_time = candle.OpenTime
        if not self.IsFormedAndOnlineAndAllowTrading() or (self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)):
            return
        if self.Position > 0:
            if previous_high > 0 and candle.ClosePrice > previous_high:
                self.SellMarket(self.Position)
            return
        if self.Position != 0 or candle.OpenTime < self.StartTime or candle.OpenTime > self.EndTime:
            return
        if candle.ClosePrice >= candle.OpenPrice or 100.0 * (float(candle.LowPrice) - float(candle.ClosePrice)) / float(candle.ClosePrice) > float(self.Threshold):
            return
        if self._ema is not None and (not self._ema.IsFormed or average is None or candle.ClosePrice <= average):
            return
        self.BuyMarket(self.Volume)

    def CreateClone(self):
        return bearish_wick_reversal_strategy()
