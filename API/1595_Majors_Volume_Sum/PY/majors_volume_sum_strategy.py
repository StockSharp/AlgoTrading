import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class majors_volume_sum_strategy(Strategy):
    def __init__(self):
        super(majors_volume_sum_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for processing", "General")
        self._ema_length = self.Param("EmaLength", 10) \
            .SetDisplay("EMA Length", "Smoothing period for volume", "General")
        self._prev_close = 0.0
        self._volume_ema = 0.0
        self._max_abs = 0.0
        self._is_ready = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def ema_length(self):
        return self._ema_length.Value

    def OnReseted(self):
        super(majors_volume_sum_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._volume_ema = 0.0
        self._max_abs = 0.0
        self._is_ready = False

    def OnStarted(self, time):
        super(majors_volume_sum_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._is_ready:
            self._prev_close = candle.ClosePrice
            self._is_ready = True
            return
        # Volume direction based on price change
        direction = (1 if candle.ClosePrice > self._prev_close else -1)
        signed_vol = direction * candle.TotalVolume
        # Simple EMA of signed volume
        k = 2.0 / (float(self.ema_length) + 1.0)
        self._volume_ema = self._volume_ema * (1 - k) + signed_vol * k
        abs_ema = abs(self._volume_ema)
        if abs_ema > self._max_abs:
            self._max_abs = abs_ema
        # Trade when volume momentum is strong relative to its history
        threshold = self._max_abs * 0.5
        if self._volume_ema > threshold and candle.ClosePrice > ema_val and self.Position <= 0:
            self.BuyMarket()
        elif self._volume_ema < -threshold and candle.ClosePrice < ema_val and self.Position >= 0:
            self.SellMarket()
        self._prev_close = candle.ClosePrice

    def CreateClone(self):
        return majors_volume_sum_strategy()
