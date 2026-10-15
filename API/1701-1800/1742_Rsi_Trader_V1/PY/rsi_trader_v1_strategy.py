import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_trader_v1_strategy(Strategy):
    def __init__(self):
        super(rsi_trader_v1_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Calculation period", "RSI")
        self._buy_point = self.Param("BuyPoint", 30.0) \
            .SetDisplay("Buy Threshold", "RSI level for long entry", "RSI")
        self._sell_point = self.Param("SellPoint", 70.0) \
            .SetDisplay("Sell Threshold", "RSI level for short entry", "RSI")
        self._prev_rsi = 0.0
        self._prev_prev_rsi = 0.0
        self._has_prev = False
        self._has_prev_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def buy_point(self):
        return self._buy_point.Value

    @property
    def sell_point(self):
        return self._sell_point.Value

    def OnReseted(self):
        super(rsi_trader_v1_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._prev_prev_rsi = 0.0
        self._has_prev = False
        self._has_prev_prev = False

    def OnStarted2(self, time):
        super(rsi_trader_v1_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        self.SubscribeCandles(self.candle_type).Bind(rsi, self.process_candle).Start()

    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_value)

        if not self._has_prev:
            self._prev_rsi = rv
            self._has_prev = True
            return

        if not self._has_prev_prev:
            self._prev_prev_rsi = self._prev_rsi
            self._prev_rsi = rv
            self._has_prev_prev = True
            return

        bp = float(self.buy_point)
        sp = float(self.sell_point)

        long_signal = rv > bp and self._prev_rsi < bp and self._prev_prev_rsi < bp
        short_signal = rv < sp and self._prev_rsi > sp and self._prev_prev_rsi > sp

        if long_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif short_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_rsi = self._prev_rsi
        self._prev_rsi = rv

    def CreateClone(self):
        return rsi_trader_v1_strategy()
