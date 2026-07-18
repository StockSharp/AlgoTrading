import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class hull_trend_osma_strategy(Strategy):

    def __init__(self):
        super(hull_trend_osma_strategy, self).__init__()

        self._hull_period = self.Param("HullPeriod", 20) \
            .SetDisplay("Hull Period", "Period for Hull Moving Average", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 5) \
            .SetDisplay("Signal Period", "Period for signal SMA", "Indicators")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev1 = None
        self._prev2 = None

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
    def TakeProfitPct(self):
        return self._take_profit_pct.Value

    @TakeProfitPct.setter
    def TakeProfitPct(self, value):
        self._take_profit_pct.Value = value

    @property
    def StopLossPct(self):
        return self._stop_loss_pct.Value

    @StopLossPct.setter
    def StopLossPct(self, value):
        self._stop_loss_pct.Value = value

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

        hma = HullMovingAverage()
        hma.Length = self.HullPeriod
        signal = ExponentialMovingAverage()
        signal.Length = self.SignalPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(hma, signal, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(float(self.TakeProfitPct), UnitTypes.Percent),
            stopLoss=Unit(float(self.StopLossPct), UnitTypes.Percent),
            useMarketOrders=True
        )

    def ProcessCandle(self, candle, hma_val, signal_val):
        if candle.State != CandleStates.Finished:
            return

        hma_f = float(hma_val)
        signal_f = float(signal_val)
        osma = hma_f - signal_f

        if self._prev1 is None or self._prev2 is None:
            self._prev2 = self._prev1
            self._prev1 = osma
            return

        prev = self._prev1
        prev_prev = self._prev2

        is_rising = prev > prev_prev and osma >= prev
        is_falling = prev < prev_prev and osma <= prev

        if is_rising and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif is_falling and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev2 = self._prev1
        self._prev1 = osma

    def OnReseted(self):
        super(hull_trend_osma_strategy, self).OnReseted()
        self._prev1 = None
        self._prev2 = None

    def CreateClone(self):
        return hull_trend_osma_strategy()
