import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class above_below_ma_strategy(Strategy):
    """
    Above/Below MA strategy. Trades when price crosses above or below a moving average.
    """

    def __init__(self):
        super(above_below_ma_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Moving average period", "Indicators")

        self._prev_close = None
        self._prev_ma = None

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
        super(above_below_ma_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_ma = None

    def OnStarted(self, time):
        super(above_below_ma_strategy, self).OnStarted(time)

        self._prev_close = None
        self._prev_ma = None

        sma = SimpleMovingAverage()
        sma.Length = self.MaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        if self._prev_close is None or self._prev_ma is None:
            self._prev_close = close
            self._prev_ma = ma_val
            return

        # Price crosses above MA -> buy
        if self._prev_close <= self._prev_ma and close > ma_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Price crosses below MA -> sell
        elif self._prev_close >= self._prev_ma and close < ma_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_close = close
        self._prev_ma = ma_val

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return above_below_ma_strategy()
