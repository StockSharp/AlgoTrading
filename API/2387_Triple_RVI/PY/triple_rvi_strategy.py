import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class triple_rvi_strategy(Strategy):
    def __init__(self):
        super(triple_rvi_strategy, self).__init__()
        self._rvi_period = self.Param("RviPeriod", 10)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._trend1 = 0
        self._trend2 = 0
        self._prev_signal = None

    @property
    def RviPeriod(self): return self._rvi_period.Value
    @RviPeriod.setter
    def RviPeriod(self, v): self._rvi_period.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnStarted(self, time):
        super(triple_rvi_strategy, self).OnStarted(time)
        self._trend1 = 0
        self._trend2 = 0
        self._prev_signal = None
        trend_rsi = RelativeStrengthIndex()
        trend_rsi.Length = self.RviPeriod * 3
        mid_rsi = RelativeStrengthIndex()
        mid_rsi.Length = self.RviPeriod * 2
        signal_rsi = RelativeStrengthIndex()
        signal_rsi.Length = self.RviPeriod
        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(trend_rsi, mid_rsi, signal_rsi, self.ProcessCandle).Start()
        self.StartProtection(Unit(2000, UnitTypes.Absolute), Unit(1000, UnitTypes.Absolute))

    def ProcessCandle(self, candle, trend_value, mid_value, signal_value):
        if candle.State != CandleStates.Finished: return
        if not self.IsFormedAndOnlineAndAllowTrading(): return
        tv = float(trend_value)
        mv = float(mid_value)
        sv = float(signal_value)
        self._trend1 = 1 if tv > 55.0 else (-1 if tv < 45.0 else 0)
        self._trend2 = 1 if mv > 55.0 else (-1 if mv < 45.0 else 0)
        if self._prev_signal is None:
            self._prev_signal = sv
            return
        cross_up = self._prev_signal <= 50.0 and sv > 50.0
        cross_down = self._prev_signal >= 50.0 and sv < 50.0
        if cross_up and self._trend1 > 0 and self._trend2 > 0 and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self._trend1 < 0 and self._trend2 < 0 and self.Position >= 0:
            self.SellMarket()
        if self.Position > 0 and (self._trend1 < 0 or self._trend2 < 0):
            self.SellMarket()
        elif self.Position < 0 and (self._trend1 > 0 or self._trend2 > 0):
            self.BuyMarket()
        self._prev_signal = sv

    def OnReseted(self):
        super(triple_rvi_strategy, self).OnReseted()
        self._trend1 = 0
        self._trend2 = 0
        self._prev_signal = None

    def CreateClone(self):
        return triple_rvi_strategy()
