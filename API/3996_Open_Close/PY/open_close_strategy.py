import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class open_close_strategy(Strategy):
    def __init__(self):
        super(open_close_strategy, self).__init__()
        self._sl_points = self.Param("StopLossPoints", 500.0).SetNotNegative().SetDisplay("Stop Loss", "Stop loss in absolute points", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 500.0).SetNotNegative().SetDisplay("Take Profit", "Take profit in absolute points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Time-frame for open/close pattern.", "Data")
        self.Volume = 1

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(open_close_strategy, self).OnReseted()
        self._has_prev = False
        self._prev_open = 0
        self._prev_close = 0

    def OnStarted2(self, time):
        super(open_close_strategy, self).OnStarted2(time)
        self._has_prev = False
        self._prev_open = 0
        self._prev_close = 0

        ema = ExponentialMovingAverage()
        ema.Length = 20

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

        tp = Unit(self._tp_points.Value, UnitTypes.Absolute) if self._tp_points.Value > 0 else None
        sl = Unit(self._sl_points.Value, UnitTypes.Absolute) if self._sl_points.Value > 0 else None
        if tp is not None or sl is not None:
            self.StartProtection(tp, sl)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        open_price = candle.OpenPrice
        close = candle.ClosePrice

        if not self._has_prev:
            self._prev_open = open_price
            self._prev_close = close
            self._has_prev = True
            return

        # Exit logic
        if self.Position > 0:
            if open_price < self._prev_open and close < self._prev_close:
                self.SellMarket(self.Position)
        elif self.Position < 0:
            if open_price > self._prev_open and close > self._prev_close:
                self.BuyMarket(Math.Abs(self.Position))

        # Entry logic
        if self.Position == 0:
            if open_price > self._prev_open and close < self._prev_close:
                self.BuyMarket(self.Volume)
            elif open_price < self._prev_open and close > self._prev_close:
                self.SellMarket(self.Volume)

        self._prev_open = open_price
        self._prev_close = close

    def CreateClone(self):
        return open_close_strategy()
