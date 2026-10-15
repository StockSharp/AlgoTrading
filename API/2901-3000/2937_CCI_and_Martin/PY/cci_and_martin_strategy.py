import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class cci_and_martin_strategy(Strategy):
    def __init__(self):
        super(cci_and_martin_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._cci_period = self.Param("CciPeriod", 27) \
            .SetDisplay("CCI Period", "CCI indicator length", "Indicators")
        self._overbought = self.Param("Overbought", 100.0) \
            .SetDisplay("Overbought", "CCI overbought level", "Indicators")
        self._oversold = self.Param("Oversold", -100.0) \
            .SetDisplay("Oversold", "CCI oversold level", "Indicators")

        self._prev_cci = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @property
    def Overbought(self):
        return self._overbought.Value

    @property
    def Oversold(self):
        return self._oversold.Value

    def OnReseted(self):
        super(cci_and_martin_strategy, self).OnReseted()
        self._prev_cci = None

    def OnStarted2(self, time):
        super(cci_and_martin_strategy, self).OnStarted2(time)
        self._prev_cci = None

        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(cci, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        cv = float(cci_value)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_cci = cv
            return

        if self._prev_cci is None:
            self._prev_cci = cv
            return

        prev = self._prev_cci
        self._prev_cci = cv

        ob = float(self.Overbought)
        os_level = float(self.Oversold)

        # Buy signal: CCI crosses above oversold level from below
        if prev < os_level and cv >= os_level and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Sell signal: CCI crosses below overbought level from above
        elif prev > ob and cv <= ob and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return cci_and_martin_strategy()
