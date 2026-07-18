import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class macd_sample_hedging_grid_strategy(Strategy):
    def __init__(self):
        super(macd_sample_hedging_grid_strategy, self).__init__()

        self._macd = None
        self._prev_macd = None

    def OnReseted(self):
        super(macd_sample_hedging_grid_strategy, self).OnReseted()
        self._macd = None
        self._prev_macd = None

    def OnStarted2(self, time):
        super(macd_sample_hedging_grid_strategy, self).OnStarted2(time)

        self._macd = MovingAverageConvergenceDivergence()

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._macd, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._macd.IsFormed:
            return

        macd_line = float(macd_value)

        if self._prev_macd is not None:
            if self._prev_macd <= 0.0 and macd_line > 0.0 and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_macd >= 0.0 and macd_line < 0.0 and self.Position >= 0:
                self.SellMarket()

        self._prev_macd = macd_line

    def CreateClone(self):
        return macd_sample_hedging_grid_strategy()
