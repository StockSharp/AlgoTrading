import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class fraktrak_xonax_strategy(Strategy):
    def __init__(self):
        super(fraktrak_xonax_strategy, self).__init__()

        self._fractal_offset = self.Param("FractalOffset", 50.0) \
            .SetDisplay("Fractal Offset", "Price offset beyond fractal for entry", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Source candles", "General")

        self._h1 = 0.0
        self._h2 = 0.0
        self._h3 = 0.0
        self._h4 = 0.0
        self._h5 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0
        self._l4 = 0.0
        self._l5 = 0.0
        self._up_fractal = None
        self._down_fractal = None
        self._last_up_fractal = None
        self._last_down_fractal = None

    @property
    def fractal_offset(self):
        return self._fractal_offset.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fraktrak_xonax_strategy, self).OnReseted()
        self._h1 = self._h2 = self._h3 = self._h4 = self._h5 = 0.0
        self._l1 = self._l2 = self._l3 = self._l4 = self._l5 = 0.0
        self._up_fractal = None
        self._down_fractal = None
        self._last_up_fractal = None
        self._last_down_fractal = None

    def OnStarted(self, time):
        super(fraktrak_xonax_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Shift high and low buffers
        self._h1 = self._h2
        self._h2 = self._h3
        self._h3 = self._h4
        self._h4 = self._h5
        self._h5 = float(candle.HighPrice)
        self._l1 = self._l2
        self._l2 = self._l3
        self._l3 = self._l4
        self._l4 = self._l5
        self._l5 = float(candle.LowPrice)

        # Need at least 5 bars for fractal detection
        if self._h1 == 0 or self._l1 == 0:
            return

        # Detect new fractals (bar 3 is the middle of 5 bars)
        if self._h3 > self._h1 and self._h3 > self._h2 and self._h3 > self._h4 and self._h3 > self._h5:
            self._up_fractal = self._h3
        if self._l3 < self._l1 and self._l3 < self._l2 and self._l3 < self._l4 and self._l3 < self._l5:
            self._down_fractal = self._l3

        close = float(candle.ClosePrice)
        offset = float(self.fractal_offset)

        # Buy signal: close above up fractal + offset
        if self._up_fractal is not None and self._last_up_fractal != self._up_fractal:
            trigger = self._up_fractal + offset
            if close > trigger and self.Position <= 0:
                self.BuyMarket()
                self._last_up_fractal = self._up_fractal

        # Sell signal: close below down fractal - offset
        if self._down_fractal is not None and self._last_down_fractal != self._down_fractal:
            trigger = self._down_fractal - offset
            if close < trigger and self.Position >= 0:
                self.SellMarket()
                self._last_down_fractal = self._down_fractal

    def CreateClone(self):
        return fraktrak_xonax_strategy()
