import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class fraktrak_xonax_strategy(Strategy):
    def __init__(self):
        super(fraktrak_xonax_strategy, self).__init__()

        self._fractal_offset = self.Param("FractalOffset", 50) \
            .SetDisplay("Fractal Offset", "Price offset beyond fractal for entry", "Signals")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Fractal Offset", "Price offset beyond fractal for entry", "Signals")

        self._up_fractal = None
        self._down_fractal = None
        self._last_up_fractal = None
        self._last_down_fractal = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fraktrak_xonax_strategy, self).OnReseted()
        self._up_fractal = None
        self._down_fractal = None
        self._last_up_fractal = None
        self._last_down_fractal = None

    def OnStarted(self, time):
        super(fraktrak_xonax_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return fraktrak_xonax_strategy()
