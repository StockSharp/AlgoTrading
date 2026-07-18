import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class auto_pending_by_rsi_strategy(Strategy):
    def __init__(self):
        super(auto_pending_by_rsi_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators") \
            .SetOptimize(7, 21, 7)
        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetDisplay("RSI Overbought", "Overbought level", "Indicators") \
            .SetOptimize(60.0, 80.0, 5.0)
        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetDisplay("RSI Oversold", "Oversold level", "Indicators") \
            .SetOptimize(20.0, 40.0, 5.0)
        self._match_count = self.Param("MatchCount", 3) \
            .SetDisplay("Match Count", "Consecutive candles before entry", "General") \
            .SetOptimize(1, 10, 1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Time frame for analysis", "General")

        self._overbought_count = 0
        self._oversold_count = 0

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    @property
    def match_count(self):
        return self._match_count.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(auto_pending_by_rsi_strategy, self).OnReseted()
        self._overbought_count = 0
        self._oversold_count = 0

    def OnStarted2(self, time):
        super(auto_pending_by_rsi_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process).Start()
        self.StartProtection(None, None)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process(self, candle, rsi):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi)

        if rsi_val < float(self.rsi_oversold):
            self._oversold_count += 1
            self._overbought_count = 0
        elif rsi_val > float(self.rsi_overbought):
            self._overbought_count += 1
            self._oversold_count = 0
        else:
            self._overbought_count = 0
            self._oversold_count = 0

        if self._oversold_count >= self.match_count and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._oversold_count = 0

        if self._overbought_count >= self.match_count and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._overbought_count = 0

    def CreateClone(self):
        return auto_pending_by_rsi_strategy()
