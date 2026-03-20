import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class mbkasctrend3_strategy(Strategy):
    def __init__(self):
        super(mbkasctrend3_strategy, self).__init__()
        self._wpr1 = self.Param("WprLength1", 9) \
            .SetDisplay("WPR Length 1", "Period for the first WPR", "Indicator")
        self._wpr2 = self.Param("WprLength2", 33) \
            .SetDisplay("WPR Length 2", "Period for the second WPR", "Indicator")
        self._wpr3 = self.Param("WprLength3", 77) \
            .SetDisplay("WPR Length 3", "Period for the third WPR", "Indicator")
        self._swing = self.Param("Swing", 3) \
            .SetDisplay("Swing", "Swing adjustment", "Indicator")
        self._avg_swing = self.Param("AverageSwing", -5) \
            .SetDisplay("Average Swing", "Average swing adjustment", "Indicator")
        self._w1 = self.Param("Weight1", 1.0) \
            .SetDisplay("Weight 1", "Weight for WPR1", "Indicator")
        self._w2 = self.Param("Weight2", 3.0) \
            .SetDisplay("Weight 2", "Weight for WPR2", "Indicator")
        self._w3 = self.Param("Weight3", 1.0) \
            .SetDisplay("Weight 3", "Weight for WPR3", "Indicator")
        self._sl = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop loss in points", "Protection")
        self._tp = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take profit in points", "Protection")
        self._candle = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for calculations", "General")
        self._prev_trend = 0

    @property
    def wpr_length1(self):
        return self._wpr1.Value

    @property
    def wpr_length2(self):
        return self._wpr2.Value

    @property
    def wpr_length3(self):
        return self._wpr3.Value

    @property
    def swing(self):
        return self._swing.Value

    @property
    def average_swing(self):
        return self._avg_swing.Value

    @property
    def weight1(self):
        return self._w1.Value

    @property
    def weight2(self):
        return self._w2.Value

    @property
    def weight3(self):
        return self._w3.Value

    @property
    def candle_type(self):
        return self._candle.Value

    def OnReseted(self):
        super(mbkasctrend3_strategy, self).OnReseted()
        self._prev_trend = 0

    def OnStarted(self, time):
        super(mbkasctrend3_strategy, self).OnStarted(time)

        w1 = WilliamsR()
        w1.Length = self.wpr_length1
        w2 = WilliamsR()
        w2.Length = self.wpr_length2
        w3 = WilliamsR()
        w3.Length = self.wpr_length3

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(w1, w2, w3, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, v1, v2, v3):
        if candle.State != CandleStates.Finished or not self.IsFormedAndOnlineAndAllowTrading():
            return

        r1 = 100.0 + float(v1)
        r2 = 100.0 + float(v2)
        r3 = 100.0 + float(v3)
        wt1 = float(self.weight1)
        wt2 = float(self.weight2)
        wt3 = float(self.weight3)
        total = wt1 + wt2 + wt3
        avg = (wt1 * r1 + wt2 * r2 + wt3 * r3) / total
        up_level = 67.0 + float(self.swing)
        dn_level = 33.0 - float(self.swing)
        up1 = 50.0 - float(self.average_swing)
        dn1 = 50.0 + float(self.average_swing)

        trend = 0
        if avg > up_level and r3 >= up1:
            trend = 1
        elif avg < dn_level and r3 <= dn1:
            trend = -1

        if self._prev_trend <= 0 and trend > 0 and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_trend >= 0 and trend < 0 and self.Position >= 0:
            self.SellMarket()

        if trend != 0:
            self._prev_trend = trend

    def CreateClone(self):
        return mbkasctrend3_strategy()
