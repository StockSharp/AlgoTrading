import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class simple_forecast_keltner_worms_strategy(Strategy):
    def __init__(self):
        super(simple_forecast_keltner_worms_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles for processing", "General")
        self._length = self.Param("Length", 20) \
            .SetDisplay("Length", "Channel calculation period", "Indicators")
        self._multiplier = self.Param("Multiplier", 2) \
            .SetDisplay("Multiplier", "ATR multiplier for bands", "Indicators")
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def length(self):
        return self._length.Value

    @property
    def multiplier(self):
        return self._multiplier.Value

    def OnReseted(self):
        super(simple_forecast_keltner_worms_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(simple_forecast_keltner_worms_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.length
        atr = AverageTrueRange()
        atr.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        upper = ema_val + self.multiplier * atr_val
        lower = ema_val - self.multiplier * atr_val
        if not self._has_prev:
            self._prev_close = candle.ClosePrice
            self._prev_upper = upper
            self._prev_lower = lower
            self._has_prev = True
            return
        # Breakout above upper Keltner band
        if self._prev_close <= self._prev_upper and candle.ClosePrice > upper and self.Position <= 0:
            self.BuyMarket()
        # Breakdown below lower Keltner band
        elif self._prev_close >= self._prev_lower and candle.ClosePrice < lower and self.Position >= 0:
            self.SellMarket()
        self._prev_close = candle.ClosePrice
        self._prev_upper = upper
        self._prev_lower = lower

    def CreateClone(self):
        return simple_forecast_keltner_worms_strategy()
