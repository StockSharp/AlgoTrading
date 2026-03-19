import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class sudoku_ui_strategy(Strategy):
    """SMA mean reversion: buy when close crosses below SMA, sell when crosses above."""
    def __init__(self):
        super(sudoku_ui_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 20).SetGreaterThanZero().SetDisplay("SMA Period", "SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(sudoku_ui_strategy, self).OnReseted()
        self._prev_close = 0
        self._prev_sma = 0

    def OnStarted(self, time):
        super(sudoku_ui_strategy, self).OnStarted(time)
        self._prev_close = 0
        self._prev_sma = 0

        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sma, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        if self._prev_close > 0 and self._prev_sma > 0:
            cross_below = self._prev_close >= self._prev_sma and close < sma_val
            cross_above = self._prev_close <= self._prev_sma and close > sma_val

            if cross_below and self.Position <= 0:
                self.BuyMarket()
            elif cross_above and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_sma = sma_val

    def CreateClone(self):
        return sudoku_ui_strategy()
