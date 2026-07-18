import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class kwan_ccc_strategy(Strategy):
    def __init__(self):
        super(kwan_ccc_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI length", "Indicators")

        self._prev_cci = 0.0
        self._prev_close = 0.0
        self._initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    def OnReseted(self):
        super(kwan_ccc_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._prev_close = 0.0
        self._initialized = False

    def OnStarted2(self, time):
        super(kwan_ccc_strategy, self).OnStarted2(time)

        self._prev_cci = 0.0
        self._prev_close = 0.0
        self._initialized = False

        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(cci, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        cv = float(cci_value)
        close = float(candle.ClosePrice)

        if not self._initialized:
            self._prev_cci = cv
            self._prev_close = close
            self._initialized = True
            return

        close_up = close > self._prev_close
        close_down = close < self._prev_close

        if self._prev_cci <= 0 and cv > 0 and close_up and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_cci >= 0 and cv < 0 and close_down and self.Position >= 0:
            self.SellMarket()

        self._prev_cci = cv
        self._prev_close = close

    def CreateClone(self):
        return kwan_ccc_strategy()
