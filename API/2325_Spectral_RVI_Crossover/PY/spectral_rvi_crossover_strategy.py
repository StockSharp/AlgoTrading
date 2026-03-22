import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeVigorIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class spectral_rvi_crossover_strategy(Strategy):
    def __init__(self):
        super(spectral_rvi_crossover_strategy, self).__init__()
        self._rvi_length = self.Param("RviLength", 14) \
            .SetDisplay("RVI Length", "Length for RVI", "General")
        self._smooth_length = self.Param("SmoothLength", 10) \
            .SetDisplay("Smooth Length", "Smoothing length", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._smooth_rvi = None
        self._smooth_sig = None
        self._prev_sm_rvi = None
        self._prev_sm_sig = None

    @property
    def rvi_length(self):
        return self._rvi_length.Value

    @property
    def smooth_length(self):
        return self._smooth_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(spectral_rvi_crossover_strategy, self).OnReseted()
        self._smooth_rvi = None
        self._smooth_sig = None
        self._prev_sm_rvi = None
        self._prev_sm_sig = None

    def OnStarted(self, time):
        super(spectral_rvi_crossover_strategy, self).OnStarted(time)
        self._prev_sm_rvi = None
        self._prev_sm_sig = None
        self._smooth_rvi = SimpleMovingAverage()
        self._smooth_rvi.Length = self.smooth_length
        self._smooth_sig = SimpleMovingAverage()
        self._smooth_sig.Length = self.smooth_length
        rvi = RelativeVigorIndex()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rvi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rvi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rvi_val):
        if candle.State != CandleStates.Finished:
            return
        avg = rvi_val.Average
        sig = rvi_val.Signal
        if avg is None or sig is None:
            return
        avg = float(avg)
        sig = float(sig)
        t = candle.CloseTime
        sm_rvi_result = self._smooth_rvi.Process(avg, t, True)
        sm_sig_result = self._smooth_sig.Process(sig, t, True)
        if not self._smooth_rvi.IsFormed or not self._smooth_sig.IsFormed:
            return
        sm_rvi = float(sm_rvi_result)
        sm_sig = float(sm_sig_result)
        if self._prev_sm_rvi is not None and self._prev_sm_sig is not None:
            if self._prev_sm_rvi <= self._prev_sm_sig and sm_rvi > sm_sig and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_sm_rvi >= self._prev_sm_sig and sm_rvi < sm_sig and self.Position >= 0:
                self.SellMarket()
        self._prev_sm_rvi = sm_rvi
        self._prev_sm_sig = sm_sig

    def CreateClone(self):
        return spectral_rvi_crossover_strategy()
