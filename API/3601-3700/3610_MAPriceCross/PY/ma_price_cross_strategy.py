import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_price_cross_strategy(Strategy):
    def __init__(self):
        super(ma_price_cross_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._ma_period = self.Param("MaPeriod", 100)

        self._prev_average = None
        self._prev_close = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    def OnReseted(self):
        super(ma_price_cross_strategy, self).OnReseted()
        self._prev_average = None
        self._prev_close = None

    def OnStarted2(self, time):
        super(ma_price_cross_strategy, self).OnStarted2(time)
        self._prev_average = None
        self._prev_close = None

        sma = ExponentialMovingAverage()
        sma.Length = self.MaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, self._process_candle).Start()

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        sma_val = float(sma_value)
        close = float(candle.ClosePrice)

        if self._prev_average is None or self._prev_close is None:
            self._prev_average = sma_val
            self._prev_close = close
            return

        # Price crosses above MA -> buy
        buy_signal = self._prev_close <= self._prev_average and close > sma_val
        # Price crosses below MA -> sell
        sell_signal = self._prev_close >= self._prev_average and close < sma_val

        if buy_signal:
            if self.Position <= 0:
                self.BuyMarket()
        elif sell_signal:
            if self.Position >= 0:
                self.SellMarket()

        self._prev_average = sma_val
        self._prev_close = close

    def CreateClone(self):
        return ma_price_cross_strategy()
