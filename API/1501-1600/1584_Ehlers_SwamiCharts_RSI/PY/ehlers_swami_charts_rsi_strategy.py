import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, IIndicator
from StockSharp.Algo.Strategies import Strategy


class ehlers_swami_charts_rsi_strategy(Strategy):
    def __init__(self):
        super(ehlers_swami_charts_rsi_strategy, self).__init__()
        self._long_color = self.Param("LongColor", 50) \
            .SetDisplay("LongColor", "Long color threshold", "General")
        self._short_color = self.Param("ShortColor", 50) \
            .SetDisplay("ShortColor", "Short color threshold", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")

    @property
    def long_color(self):
        return self._long_color.Value

    @property
    def short_color(self):
        return self._short_color.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ehlers_swami_charts_rsi_strategy, self).OnStarted2(time)
        rsis = []
        for i in range(24):
            ind = RelativeStrengthIndex()
            ind.Length = i + 10
            rsis.append(ind)
        arr = Array[IIndicator](rsis)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(arr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, values):
        if candle.State != CandleStates.Finished:
            return
        color1_tot = 0
        color2_tot = 0
        count = 0
        for val in values:
            if val.IsEmpty:
                continue
            rsi = float(val) / 100.0
            if rsi >= 0.5:
                c1 = int(Math.Ceiling(255.0 * (2.0 - 2.0 * rsi)))
                c2 = 255
            else:
                c1 = 255
                c2 = int(Math.Ceiling(255.0 * 2.0 * rsi))
            color1_tot += c1
            color2_tot += c2
            count += 1
        if count == 0:
            return
        color1_avg = int(Math.Ceiling(float(color1_tot) / float(count)))
        color2_avg = int(Math.Ceiling(float(color2_tot) / float(count)))
        long_signal = color1_avg == 255 and color2_avg > int(self.long_color)
        short_signal = color1_avg > int(self.short_color) and color2_avg == 255
        if long_signal and self.Position <= 0:
            self.BuyMarket()
        elif short_signal and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return ehlers_swami_charts_rsi_strategy()
