import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class line_order_dual_level_strategy(Strategy):
    def __init__(self):
        super(line_order_dual_level_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 10) \
            .SetDisplay("SMA Period", "SMA period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period", "Indicators")
        self._atr_mult = self.Param("AtrMult", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("ATR Mult", "ATR multiplier for levels", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def atr_mult(self):
        return self._atr_mult.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(line_order_dual_level_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(line_order_dual_level_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.sma_period
        atr = StandardDeviation()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if atr_val <= 0:
            close = candle.ClosePrice
        upper_level = sma_val + atr_val * self.atr_mult
        lower_level = sma_val - atr_val * self.atr_mult
        # Breakout above upper level => long
        if close > upper_level and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
            self._entry_price = close
        # Breakout below lower level => short
        elif close < lower_level and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()
            self._entry_price = close
        # Exit long at SMA or if loss > 2*ATR
        elif self.Position > 0:
            if close <= sma_val or (self._entry_price > 0 and close <= self._entry_price - atr_val * 2):
                self.SellMarket()
                self._entry_price = 0
        # Exit short at SMA or if loss > 2*ATR
        elif self.Position < 0:
            if close >= sma_val or (self._entry_price > 0 and close >= self._entry_price + atr_val * 2):
                self.BuyMarket()
                self._entry_price = 0

    def CreateClone(self):
        return line_order_dual_level_strategy()
