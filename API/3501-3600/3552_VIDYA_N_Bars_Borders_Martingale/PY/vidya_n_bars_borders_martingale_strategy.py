import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class vidya_n_bars_borders_martingale_strategy(Strategy):
    def __init__(self):
        super(vidya_n_bars_borders_martingale_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._ema_period = self.Param("EmaPeriod", 20)
        self._range_period = self.Param("RangePeriod", 10)

        self._high_history = []
        self._low_history = []
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def RangePeriod(self):
        return self._range_period.Value

    @RangePeriod.setter
    def RangePeriod(self, value):
        self._range_period.Value = value

    def OnReseted(self):
        super(vidya_n_bars_borders_martingale_strategy, self).OnReseted()
        self._high_history = []
        self._low_history = []
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(vidya_n_bars_borders_martingale_strategy, self).OnStarted2(time)
        self._high_history = []
        self._low_history = []
        self._entry_price = 0.0

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._process_candle).Start()

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        rng_period = self.RangePeriod

        self._high_history.append(high)
        self._low_history.append(low)
        if len(self._high_history) > rng_period:
            self._high_history.pop(0)
            self._low_history.pop(0)

        if len(self._high_history) < rng_period:
            return

        highest = max(self._high_history)
        lowest = min(self._low_history)
        rng = (highest - lowest) * 0.75
        if rng <= 0:
            return

        upper = ema_val + rng
        lower = ema_val - rng

        if close < lower and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
        elif close > upper and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close

    def CreateClone(self):
        return vidya_n_bars_borders_martingale_strategy()
