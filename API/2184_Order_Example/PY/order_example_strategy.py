import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class order_example_strategy(Strategy):
    def __init__(self):
        super(order_example_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 5) \
            .SetDisplay("Lookback", "Candles to calculate highs and lows", "General")
        self._sma_period = self.Param("SmaPeriod", 5) \
            .SetDisplay("SMA Period", "Trend filter SMA period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._highs = []
        self._lows = []

    @property
    def lookback(self):
        return self._lookback.Value

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(order_example_strategy, self).OnReseted()
        self._highs = []
        self._lows = []

    def OnStarted2(self, time):
        super(order_example_strategy, self).OnStarted2(time)

        sma = ExponentialMovingAverage()
        sma.Length = self.sma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        lb = int(self.lookback)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._highs.append(high)
            self._lows.append(low)
            if len(self._highs) > lb:
                self._highs.pop(0)
            if len(self._lows) > lb:
                self._lows.pop(0)
            return

        if len(self._highs) >= 3:
            high_level = max(self._highs)
            low_level = min(self._lows)

            if close > high_level and self.Position <= 0:
                self.BuyMarket()
            elif close < low_level and self.Position >= 0:
                self.SellMarket()

        self._highs.append(high)
        self._lows.append(low)
        if len(self._highs) > lb:
            self._highs.pop(0)
        if len(self._lows) > lb:
            self._lows.pop(0)

    def CreateClone(self):
        return order_example_strategy()
