import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class mad_trader_strategy(Strategy):
    def __init__(self):
        super(mad_trader_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._rsi_upper = self.Param("RsiUpper", 70.0) \
            .SetDisplay("RSI Upper", "Overbought level", "Indicators")
        self._rsi_lower = self.Param("RsiLower", 30.0) \
            .SetDisplay("RSI Lower", "Oversold level", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_rsi = 0.0
        self._has_prev = False

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def rsi_upper(self):
        return self._rsi_upper.Value

    @property
    def rsi_lower(self):
        return self._rsi_lower.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mad_trader_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(mad_trader_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        self.SubscribeCandles(self.candle_type) \
            .Bind(rsi, self.process_candle) \
            .Start()

    def process_candle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        rsi_val = float(rsi_val)
        if not self._has_prev:
            self._prev_rsi = rsi_val
            self._has_prev = True
            return
        if self._prev_rsi <= float(self.rsi_lower) and rsi_val > float(self.rsi_lower) and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_rsi >= float(self.rsi_upper) and rsi_val < float(self.rsi_upper) and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_rsi = rsi_val

    def CreateClone(self):
        return mad_trader_strategy()
