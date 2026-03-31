import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bollinger_bands_session_reversal_strategy(Strategy):

    def __init__(self):
        super(bollinger_bands_session_reversal_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary candle series", "General")
        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Length", "MA period for Bollinger Bands", "Indicators")
        self._bollinger_width = self.Param("BollingerWidth", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Width", "Band width multiplier", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BollingerLength(self):
        return self._bollinger_length.Value

    @BollingerLength.setter
    def BollingerLength(self, value):
        self._bollinger_length.Value = value

    @property
    def BollingerWidth(self):
        return self._bollinger_width.Value

    @BollingerWidth.setter
    def BollingerWidth(self, value):
        self._bollinger_width.Value = value

    def OnStarted2(self, time):
        super(bollinger_bands_session_reversal_strategy, self).OnStarted2(time)

        bollinger = BollingerBands()
        bollinger.Length = self.BollingerLength
        bollinger.Width = self.BollingerWidth

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, self._process_candle).Start()

    def _process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        upper = bb_value.UpBand
        lower = bb_value.LowBand
        middle = bb_value.MovingAverage

        if upper is None or lower is None or middle is None:
            return

        up = float(upper)
        lo = float(lower)
        mid = float(middle)

        if up == 0 or lo == 0 or mid == 0:
            return

        price = float(candle.ClosePrice)

        if price < lo and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif price > up and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        elif self.Position > 0 and price >= mid:
            self.SellMarket()
        elif self.Position < 0 and price <= mid:
            self.BuyMarket()

    def CreateClone(self):
        return bollinger_bands_session_reversal_strategy()
