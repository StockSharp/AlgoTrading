import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class aver4_stoch_post_zig_zag_strategy(Strategy):
    def __init__(self):
        super(aver4_stoch_post_zig_zag_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._pivot_length = self.Param("PivotLength", 20) \
            .SetDisplay("Pivot Length", "Highest/Lowest period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_rsi = 0.0
        self._has_prev = False

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def pivot_length(self):
        return self._pivot_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(aver4_stoch_post_zig_zag_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(aver4_stoch_post_zig_zag_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        highest = Highest()
        highest.Length = self.pivot_length
        lowest = Lowest()
        lowest.Length = self.pivot_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, highest, lowest, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi, high, low):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_rsi = rsi
            self._has_prev = True
            return
        close = candle.ClosePrice
        # Near pivot low + RSI oversold -> buy
        if close <= low * 1.001 and rsi < 30 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Near pivot high + RSI overbought -> sell
        elif close >= high * 0.999 and rsi > 70 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Exit long on RSI overbought
        elif self.Position > 0 and rsi > 70:
            self.SellMarket()
        # Exit short on RSI oversold
        elif self.Position < 0 and rsi < 30:
            self.BuyMarket()
        self._prev_rsi = rsi

    def CreateClone(self):
        return aver4_stoch_post_zig_zag_strategy()
