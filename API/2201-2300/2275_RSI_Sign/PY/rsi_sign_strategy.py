import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_sign_strategy(Strategy):
    def __init__(self):
        super(rsi_sign_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Length of RSI indicator", "Indicator")
        self._up_level = self.Param("UpLevel", 70.0) \
            .SetDisplay("RSI Upper Level", "Sell when RSI falls below this value", "Indicator")
        self._down_level = self.Param("DownLevel", 30.0) \
            .SetDisplay("RSI Lower Level", "Buy when RSI rises above this value", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General")
        self._previous_rsi = None

    @property
    def rsi_period(self):
        return self._rsi_period.Value

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
        super(rsi_sign_strategy, self).OnReseted()
        self._previous_rsi = None

    def OnStarted2(self, time):
        super(rsi_sign_strategy, self).OnStarted2(time)
        self._previous_rsi = None
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        rsi_value = float(rsi_value)
        prev_rsi = self._previous_rsi
        self._previous_rsi = rsi_value
        if prev_rsi is None:
            return
        down_lvl = float(self.down_level)
        up_lvl = float(self.up_level)
        if prev_rsi <= down_lvl and rsi_value > down_lvl and self.Position <= 0:
            self.BuyMarket()
        elif prev_rsi >= up_lvl and rsi_value < up_lvl and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return rsi_sign_strategy()
