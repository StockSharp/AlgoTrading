import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class x_man_strategy(Strategy):
    def __init__(self):
        super(x_man_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetDisplay("SMA Period", "SMA period", "Indicators")

        self._sma = None
        self._rsi = None
        self._prev_close = None
        self._prev_sma = None

    @property
    def sma_period(self):
        return self._sma_period.Value

    def OnReseted(self):
        super(x_man_strategy, self).OnReseted()
        self._sma = None
        self._rsi = None
        self._prev_close = None
        self._prev_sma = None

    def OnStarted2(self, time):
        super(x_man_strategy, self).OnStarted2(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = 14

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._sma, self._rsi, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, sma_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma.IsFormed or not self._rsi.IsFormed:
            return

        close = float(candle.ClosePrice)
        sma_val = float(sma_value)
        rsi_val = float(rsi_value)

        if self._prev_close is not None and self._prev_sma is not None:
            cross_up = self._prev_close <= self._prev_sma and close > sma_val
            cross_down = self._prev_close >= self._prev_sma and close < sma_val

            if cross_up and rsi_val < 70.0 and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and rsi_val > 30.0 and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_sma = sma_val

    def CreateClone(self):
        return x_man_strategy()
