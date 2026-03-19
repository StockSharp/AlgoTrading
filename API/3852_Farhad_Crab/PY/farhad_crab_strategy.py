import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class farhad_crab_strategy(Strategy):
    """
    Farhad Crab strategy - EMA and SMA crossover with trend filter.
    Buys when EMA crosses above SMA, sells when EMA crosses below SMA.
    """

    def __init__(self):
        super(farhad_crab_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 10) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetDisplay("SMA Period", "SMA lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_ema = 0.0
        self._prev_sma = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(farhad_crab_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_sma = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(farhad_crab_strategy, self).OnStarted(time)

        self._has_prev = False
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, sma, self._process_candle).Start()

    def _process_candle(self, candle, ema_val, sma_val):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_val)
        sma_val = float(sma_val)

        if not self._has_prev:
            self._prev_ema = ema_val
            self._prev_sma = sma_val
            self._has_prev = True
            return

        if self._prev_ema <= self._prev_sma and ema_val > sma_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_ema >= self._prev_sma and ema_val < sma_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_ema = ema_val
        self._prev_sma = sma_val

    def CreateClone(self):
        return farhad_crab_strategy()
