import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeVigorIndex
from StockSharp.Algo.Strategies import Strategy

class rvi_histogram_reversal_strategy(Strategy):
    def __init__(self):
        super(rvi_histogram_reversal_strategy, self).__init__()
        self._rvi_period = self.Param("RviPeriod", 14).SetDisplay("RVI Period", "Period of RVI indicator", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_avg = None
        self._prev_sig = None
    @property
    def rvi_period(self): return self._rvi_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(rvi_histogram_reversal_strategy, self).OnReseted()
        self._prev_avg = None
        self._prev_sig = None
    def OnStarted(self, time):
        super(rvi_histogram_reversal_strategy, self).OnStarted(time)
        rvi = RelativeVigorIndex()
        rvi.Average.Length = self.rvi_period
        rvi.Signal.Length = self.rvi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rvi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rvi)
            self.DrawOwnTrades(area)
    def process_candle(self, candle, value):
        if candle.State != CandleStates.Finished: return
        avg = value.Average
        sig = value.Signal
        if avg is None or sig is None: return
        avg = float(avg)
        sig = float(sig)
        if self._prev_avg is not None and self._prev_sig is not None:
            if self._prev_avg <= self._prev_sig and avg > sig and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_avg >= self._prev_sig and avg < sig and self.Position >= 0:
                self.SellMarket()
        self._prev_avg = avg
        self._prev_sig = sig
    def CreateClone(self): return rvi_histogram_reversal_strategy()
