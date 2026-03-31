import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy

class color_j_variation_strategy(Strategy):
    """
    Strategy based on Jurik moving average slope reversals.
    Buys when JMA turns up, sells when JMA turns down.
    """

    def __init__(self):
        super(color_j_variation_strategy, self).__init__()
        self._jma_period = self.Param("JmaPeriod", 12) \
            .SetDisplay("JMA Period", "JMA averaging period", "Indicator")
        self._jma_phase = self.Param("JmaPhase", 100) \
            .SetDisplay("JMA Phase", "Phase for JMA", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")

        self._prev_jma = 0.0
        self._prev_prev_jma = 0.0
        self._count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_j_variation_strategy, self).OnReseted()
        self._prev_jma = 0.0
        self._prev_prev_jma = 0.0
        self._count = 0

    def OnStarted2(self, time):
        super(color_j_variation_strategy, self).OnStarted2(time)

        jma = JurikMovingAverage()
        jma.Length = self._jma_period.Value
        jma.Phase = self._jma_phase.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(jma, self.on_process).Start()

    def on_process(self, candle, jma_val):
        if candle.State != CandleStates.Finished:
            return

        self._count += 1
        if self._count < 3:
            self._prev_prev_jma = self._prev_jma
            self._prev_jma = jma_val
            return

        turn_up = self._prev_jma < self._prev_prev_jma and jma_val > self._prev_jma
        turn_down = self._prev_jma > self._prev_prev_jma and jma_val < self._prev_jma

        if turn_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif turn_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_jma = self._prev_jma
        self._prev_jma = jma_val

    def CreateClone(self):
        return color_j_variation_strategy()
