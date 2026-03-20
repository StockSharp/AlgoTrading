import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class grid_template_strategy(Strategy):
    def __init__(self):
        super(grid_template_strategy, self).__init__()

        self._grid_step_percent = self.Param("GridStepPercent", 3.0) \
            .SetDisplay("Grid Step %", "Price change percentage for grid level", "Grid")

        self._sma = None
        self._last_trade_price = None

    @property
    def grid_step_percent(self):
        return self._grid_step_percent.Value

    def OnReseted(self):
        super(grid_template_strategy, self).OnReseted()
        self._sma = None
        self._last_trade_price = None

    def OnStarted(self, time):
        super(grid_template_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = 10

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._sma, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma.IsFormed:
            return

        close = float(candle.ClosePrice)

        if self._last_trade_price is None:
            self._last_trade_price = close
            return

        step = self._last_trade_price * self.grid_step_percent / 100.0

        if close <= self._last_trade_price - step:
            self.BuyMarket()
            self._last_trade_price = close
        elif close >= self._last_trade_price + step:
            self.SellMarket()
            self._last_trade_price = close

    def CreateClone(self):
        return grid_template_strategy()
