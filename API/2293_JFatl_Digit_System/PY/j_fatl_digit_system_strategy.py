import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class j_fatl_digit_system_strategy(Strategy):
    def __init__(self):
        super(j_fatl_digit_system_strategy, self).__init__()
        self._jma_length = self.Param("JmaLength", 5) \
            .SetDisplay("JMA Length", "JMA period", "Parameters")
        self._jma_phase = self.Param("JmaPhase", -100) \
            .SetDisplay("JMA Phase", "JMA phase", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "Parameters")
        self._prev_jma = None
        self._prev_slope = None

    @property
    def jma_length(self):
        return self._jma_length.Value

    @property
    def jma_phase(self):
        return self._jma_phase.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(j_fatl_digit_system_strategy, self).OnReseted()
        self._prev_jma = None
        self._prev_slope = None

    def OnStarted(self, time):
        super(j_fatl_digit_system_strategy, self).OnStarted(time)
        self._prev_jma = None
        self._prev_slope = None
        jma = JurikMovingAverage()
        jma.Length = self.jma_length
        jma.Phase = self.jma_phase
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(jma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, jma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, jma_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        jma_value = float(jma_value)
        if self._prev_jma is not None:
            slope = jma_value - self._prev_jma
            if self._prev_slope is not None:
                turned_up = self._prev_slope <= 0 and slope > 0
                turned_down = self._prev_slope >= 0 and slope < 0
                if turned_up and self.Position <= 0:
                    self.BuyMarket()
                elif turned_down and self.Position >= 0:
                    self.SellMarket()
            self._prev_slope = slope
        self._prev_jma = jma_value

    def CreateClone(self):
        return j_fatl_digit_system_strategy()
