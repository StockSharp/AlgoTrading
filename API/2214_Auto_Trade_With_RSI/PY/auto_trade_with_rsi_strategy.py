import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class auto_trade_with_rsi_strategy(Strategy):
    def __init__(self):
        super(auto_trade_with_rsi_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicator")
        self._average_period = self.Param("AveragePeriod", 21) \
            .SetDisplay("Average Period", "SMA period to smooth RSI", "Indicator")
        self._buy_threshold = self.Param("BuyThreshold", 55.0) \
            .SetDisplay("Buy Threshold", "Averaged RSI above which to buy", "Rules")
        self._sell_threshold = self.Param("SellThreshold", 45.0) \
            .SetDisplay("Sell Threshold", "Averaged RSI below which to sell", "Rules")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle data type", "General")
        self._rsi_avg = None

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def average_period(self):
        return self._average_period.Value

    @property
    def buy_threshold(self):
        return self._buy_threshold.Value

    @property
    def sell_threshold(self):
        return self._sell_threshold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(auto_trade_with_rsi_strategy, self).OnReseted()
        self._rsi_avg = None

    def OnStarted(self, time):
        super(auto_trade_with_rsi_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        self._rsi_avg = ExponentialMovingAverage()
        self._rsi_avg.Length = self.average_period
        self.Indicators.Add(self._rsi_avg)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        if not rsi_value.IsFormed:
            return
        avg_result = self._rsi_avg.Process(rsi_value)
        if not avg_result.IsFormed:
            return
        avg_rsi = float(avg_result)
        if avg_rsi > float(self.buy_threshold) and self.Position <= 0:
            self.BuyMarket()
        elif avg_rsi < float(self.sell_threshold) and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return auto_trade_with_rsi_strategy()
