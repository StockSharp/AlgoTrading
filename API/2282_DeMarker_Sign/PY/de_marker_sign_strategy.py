import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DeMarker
from StockSharp.Algo.Strategies import Strategy


class de_marker_sign_strategy(Strategy):
    def __init__(self):
        super(de_marker_sign_strategy, self).__init__()
        self._de_marker_period = self.Param("DeMarkerPeriod", 14) \
            .SetDisplay("DeMarker Period", "Indicator period", "General")
        self._up_level = self.Param("UpLevel", 0.7) \
            .SetDisplay("Upper Level", "Sell when DeMarker falls below", "General")
        self._down_level = self.Param("DownLevel", 0.3) \
            .SetDisplay("Lower Level", "Buy when DeMarker rises above", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._prev_de_marker = None

    @property
    def de_marker_period(self):
        return self._de_marker_period.Value

    @property
    def up_level(self):
        return self._up_level.Value

    @property
    def down_level(self):
        return self._down_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(de_marker_sign_strategy, self).OnReseted()
        self._prev_de_marker = None

    def OnStarted(self, time):
        super(de_marker_sign_strategy, self).OnStarted(time)
        self._prev_de_marker = None
        de_marker = DeMarker()
        de_marker.Length = self.de_marker_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(de_marker, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, de_marker)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, de_marker):
        if candle.State != CandleStates.Finished:
            return
        de_marker = float(de_marker)
        if self._prev_de_marker is None:
            self._prev_de_marker = de_marker
            return
        down_lvl = float(self.down_level)
        up_lvl = float(self.up_level)
        if de_marker > down_lvl and self._prev_de_marker <= down_lvl and self.Position <= 0:
            self.BuyMarket()
        elif de_marker < up_lvl and self._prev_de_marker >= up_lvl and self.Position >= 0:
            self.SellMarket()
        self._prev_de_marker = de_marker

    def CreateClone(self):
        return de_marker_sign_strategy()
