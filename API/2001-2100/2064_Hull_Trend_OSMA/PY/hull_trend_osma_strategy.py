import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates
from StockSharp.Algo.Indicators import HullMovingAverage, SimpleMovingAverage
from indicator_extensions import process_float
from StockSharp.Algo.Strategies import Strategy


class hull_trend_osma_strategy(Strategy):

    def __init__(self):
        super(hull_trend_osma_strategy, self).__init__()

        self._hull_period = self.Param("HullPeriod", 20) \
            .SetDisplay("Hull Period", "Period for Hull Moving Average", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 5) \
            .SetDisplay("Signal Period", "Period for signal SMA", "Indicators")
        self._take_profit = self.Param("TakeProfit", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev1 = None
        self._prev2 = None
        self._pending = None
        self._hma = None
        self._signal = None

    @property
    def HullPeriod(self):
        return self._hull_period.Value

    @HullPeriod.setter
    def HullPeriod(self, value):
        self._hull_period.Value = value

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    @SignalPeriod.setter
    def SignalPeriod(self, value):
        self._signal_period.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(hull_trend_osma_strategy, self).OnStarted2(time)

        self._prev1 = None
        self._prev2 = None

        self._hma = HullMovingAverage()
        self._hma.Length = self.HullPeriod
        self._signal = SimpleMovingAverage()
        self._signal.Length = self.SignalPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(self._hma, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(float(self.TakeProfit), UnitTypes.Absolute),
            stopLoss=Unit(float(self.StopLoss), UnitTypes.Absolute),
            useMarketOrders=True
        )

    def ProcessCandle(self, candle, hma_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._hma.IsFormed:
            return

        smooth = process_float(self._signal, hma_val, candle.OpenTime, True)
        if not smooth.IsFormed:
            return
        osma = float(hma_val) - float(smooth)

        if self._prev1 is None or self._prev2 is None:
            self._prev2 = self._prev1
            self._prev1 = osma
            return

        prev = self._prev1
        prev_prev = self._prev2

        is_rising = prev > prev_prev and osma >= prev
        is_falling = prev < prev_prev and osma <= prev

        if self.IsFormedAndOnlineAndAllowTrading() and (self._pending is None or self._pending.State in (OrderStates.Done, OrderStates.Failed)):
            if is_rising and self.Position <= 0:
                self._pending = self.BuyMarket(self.Volume - min(0, self.Position))
            elif is_falling and self.Position >= 0:
                self._pending = self.SellMarket(self.Volume + max(0, self.Position))

        self._prev2 = self._prev1
        self._prev1 = osma

    def OnReseted(self):
        super(hull_trend_osma_strategy, self).OnReseted()
        self._prev1 = None
        self._prev2 = None
        self._hma = None
        self._signal = None

    def CreateClone(self):
        return hull_trend_osma_strategy()
