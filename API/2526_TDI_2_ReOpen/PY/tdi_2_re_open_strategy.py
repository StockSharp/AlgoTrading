import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class tdi_2_re_open_strategy(Strategy):
    """TDI momentum/index crossover: custom EMA-smoothed momentum lines."""
    def __init__(self):
        super(tdi_2_re_open_strategy, self).__init__()
        self._tdi_period = self.Param("TdiPeriod", 10).SetGreaterThanZero().SetDisplay("TDI Period", "Momentum lookback period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(tdi_2_re_open_strategy, self).OnReseted()
        self._last_close = 0
        self._directional = None
        self._index = None
        self._prev_dir = None
        self._prev_idx = None

    def OnStarted2(self, time):
        super(tdi_2_re_open_strategy, self).OnStarted2(time)
        self._last_close = 0
        self._directional = None
        self._index = None
        self._prev_dir = None
        self._prev_idx = None

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        if self._last_close == 0:
            self._last_close = close
            return

        momentum = close - self._last_close
        self._last_close = close
        alpha = 2.0 / (self._tdi_period.Value + 1.0)

        if self._directional is None or self._index is None:
            self._directional = momentum
            self._index = momentum
            return

        directional = self._directional + alpha * (momentum - self._directional)
        index = self._index + alpha * (directional - self._index)

        if self._prev_dir is None or self._prev_idx is None:
            self._directional = directional
            self._index = index
            self._prev_dir = self._directional
            self._prev_idx = self._index
            return

        cross_up = self._prev_dir <= self._prev_idx and directional > index
        cross_down = self._prev_dir >= self._prev_idx and directional < index

        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()

        self._directional = directional
        self._index = index
        self._prev_dir = directional
        self._prev_idx = index

    def CreateClone(self):
        return tdi_2_re_open_strategy()
