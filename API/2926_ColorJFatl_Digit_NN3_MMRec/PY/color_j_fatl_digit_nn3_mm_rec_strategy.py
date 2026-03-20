import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_j_fatl_digit_nn3_mm_rec_strategy(Strategy):
    def __init__(self):
        super(color_j_fatl_digit_nn3_mm_rec_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._jma_length = self.Param("JmaLength", 5) \
            .SetDisplay("JMA Length", "Jurik MA period", "Indicators")

        self._prev_jma = None
        self._prev_signal = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def JmaLength(self):
        return self._jma_length.Value

    def OnReseted(self):
        super(color_j_fatl_digit_nn3_mm_rec_strategy, self).OnReseted()
        self._prev_jma = None
        self._prev_signal = 0

    def OnStarted(self, time):
        super(color_j_fatl_digit_nn3_mm_rec_strategy, self).OnStarted(time)
        self._prev_jma = None
        self._prev_signal = 0

        jma = JurikMovingAverage()
        jma.Length = self.JmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(jma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, jma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, jma_value):
        if candle.State != CandleStates.Finished:
            return
        jv = float(jma_value)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_jma = jv
            return
        if self._prev_jma is None:
            self._prev_jma = jv
            return
        diff = jv - self._prev_jma
        if diff > 0:
            signal = 1
        elif diff < 0:
            signal = -1
        else:
            signal = self._prev_signal
        self._prev_jma = jv
        if signal == self._prev_signal:
            return
        old_signal = self._prev_signal
        self._prev_signal = signal
        if signal == 1 and old_signal == -1:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif signal == -1 and old_signal == 1:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return color_j_fatl_digit_nn3_mm_rec_strategy()
