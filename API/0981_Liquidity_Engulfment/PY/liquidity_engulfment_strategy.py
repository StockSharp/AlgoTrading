import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class liquidity_engulfment_strategy(Strategy):
    def __init__(self):
        super(liquidity_engulfment_strategy, self).__init__()
        self._upper_lookback = self.Param("UpperLookback", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Upper Lookback", "Upper liquidity", "Indicators")
        self._lower_lookback = self.Param("LowerLookback", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Lower Lookback", "Lower liquidity", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(liquidity_engulfment_strategy, self).OnStarted(time)
        highest = Highest()
        highest.Length = self._upper_lookback.Value
        lowest = Lowest()
        lowest.Length = self._lower_lookback.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, highest_val, lowest_val):
        if candle.State != CandleStates.Finished:
            return
        hv = float(highest_val)
        lv = float(lowest_val)
        if hv == 0.0 or lv == 0.0:
            return
        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        bull = close > opn
        bear = close < opn
        body = abs(close - opn)
        rng = high - low
        engulfing = rng > 0 and body / rng > 0.6
        bull_signal = bull and engulfing and low <= lv
        bear_signal = bear and engulfing and high >= hv
        if bear_signal and self.Position >= 0:
            self.SellMarket()
        elif bull_signal and self.Position <= 0:
            self.BuyMarket()

    def CreateClone(self):
        return liquidity_engulfment_strategy()
