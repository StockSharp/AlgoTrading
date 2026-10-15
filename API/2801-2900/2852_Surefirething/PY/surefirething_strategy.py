import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class surefirething_strategy(Strategy):
    def __init__(self):
        super(surefirething_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame used for signals", "General")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Trend filter EMA period", "Indicators")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    def OnReseted(self):
        super(surefirething_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._initialized = False

    def OnStarted2(self, time):
        super(surefirething_strategy, self).OnStarted2(time)

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._initialized = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(ema, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ev = float(ema_value)
        close = float(candle.ClosePrice)

        if not self._initialized:
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            self._initialized = True
            return

        if close > self._prev_high and close > ev and self.Position <= 0:
            self.BuyMarket()
        elif close < self._prev_low and close < ev and self.Position >= 0:
            self.SellMarket()

        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)

    def CreateClone(self):
        return surefirething_strategy()
