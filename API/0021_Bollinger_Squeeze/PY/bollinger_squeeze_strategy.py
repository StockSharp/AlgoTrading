import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bollinger_squeeze_strategy(Strategy):
    def __init__(self):
        super(bollinger_squeeze_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 1.8) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_band_width = 0.0
        self._has_prev_values = False
        self._cooldown = 0

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value
    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_squeeze_strategy, self).OnReseted()
        self._prev_band_width = 0.0
        self._has_prev_values = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(bollinger_squeeze_strategy, self).OnStarted2(time)
        bb = BollingerBands()
        bb.Length = self.bollinger_period
        bb.Width = self.bollinger_deviation
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        middle = float(bb_value.MovingAverage)

        if middle == 0:
            return
        band_width = (upper - lower) / middle

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_band_width = band_width
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_band_width = band_width
            return

        price = float(candle.ClosePrice)
        if price > upper and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._cooldown = 10
        elif price < lower and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._cooldown = 10

        self._prev_band_width = band_width

    def CreateClone(self):
        return bollinger_squeeze_strategy()
