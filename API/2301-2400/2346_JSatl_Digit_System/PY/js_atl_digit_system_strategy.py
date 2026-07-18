import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class js_atl_digit_system_strategy(Strategy):
    def __init__(self):
        super(js_atl_digit_system_strategy, self).__init__()
        self._jma_length = self.Param("JmaLength", 14) \
            .SetDisplay("JMA Length", "Period of Jurik moving average", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._is_first_value = True
        self._prev_jma = 0.0

    @property
    def jma_length(self):
        return self._jma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(js_atl_digit_system_strategy, self).OnReseted()
        self._is_first_value = True
        self._prev_jma = 0.0

    def OnStarted2(self, time):
        super(js_atl_digit_system_strategy, self).OnStarted2(time)
        self._is_first_value = True
        self._prev_jma = 0.0
        jma = JurikMovingAverage()
        jma.Length = int(self.jma_length)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(jma, self.process_candle).Start()

    def process_candle(self, candle, jma_value):
        if candle.State != CandleStates.Finished:
            return
        jma_value = float(jma_value)
        if self._is_first_value:
            self._prev_jma = jma_value
            self._is_first_value = False
            return
        price = float(candle.ClosePrice)
        slope = jma_value - self._prev_jma
        if slope > 0 and price > jma_value:
            if self.Position <= 0:
                self.BuyMarket()
        elif slope < 0 and price < jma_value:
            if self.Position >= 0:
                self.SellMarket()
        self._prev_jma = jma_value

    def CreateClone(self):
        return js_atl_digit_system_strategy()
