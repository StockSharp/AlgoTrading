import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class laguerre_cci_ma_strategy(Strategy):
    def __init__(self):
        super(laguerre_cci_ma_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Period for moving average", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_cci = 0.0
        self._has_prev = False

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(laguerre_cci_ma_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(laguerre_cci_ma_strategy, self).OnStarted(time)
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        ma = ExponentialMovingAverage()
        ma.Length = self.ma_period
        self.SubscribeCandles(self.candle_type).Bind(cci, ma, self.process_candle).Start()

    def process_candle(self, candle, cci_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        cv = float(cci_value)
        mv = float(ma_value)

        if not self._has_prev:
            self._prev_cci = cv
            self._has_prev = True
            return

        close = float(candle.ClosePrice)

        if self._prev_cci <= 0 and cv > 0 and close > mv and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_cci >= 0 and cv < 0 and close < mv and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_cci = cv

    def CreateClone(self):
        return laguerre_cci_ma_strategy()
