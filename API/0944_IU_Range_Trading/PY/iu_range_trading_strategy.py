import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class iu_range_trading_strategy(Strategy):
    def __init__(self):
        super(iu_range_trading_strategy, self).__init__()
        self._range_length = self.Param("RangeLength", 10) \
            .SetDisplay("Range Length", "Lookback period for range detection", "Parameters")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Parameters")
        self._atr_target_factor = self.Param("AtrTargetFactor", 2.0) \
            .SetDisplay("ATR Target Factor", "Multiplier for trailing stop step", "Parameters")
        self._atr_range_factor = self.Param("AtrRangeFactor", 1.75) \
            .SetDisplay("ATR Range Factor", "ATR multiplier to validate range", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_range_cond = False
        self._range_high = 0.0
        self._range_low = 0.0
        self._sl0 = None
        self._trailing_sl = None
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(iu_range_trading_strategy, self).OnReseted()
        self._prev_range_cond = False
        self._range_high = 0.0
        self._range_low = 0.0
        self._sl0 = None
        self._trailing_sl = None
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(iu_range_trading_strategy, self).OnStarted2(time)
        highest = Highest()
        highest.Length = self._range_length.Value
        lowest = Lowest()
        lowest.Length = self._range_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, highest_val, lowest_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        h_val = float(highest_val)
        l_val = float(lowest_val)
        atr_v = float(atr_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        rf = float(self._atr_range_factor.Value)
        tf = float(self._atr_target_factor.Value)
        range_cond = (h_val - l_val) <= atr_v * rf
        if range_cond and not self._prev_range_cond and self.Position == 0:
            self._range_high = h_val
            self._range_low = l_val
        elif range_cond and self._prev_range_cond and self.Position == 0:
            if h_val > self._range_high:
                self._range_high = h_val
            if l_val < self._range_low:
                self._range_low = l_val
        if self.Position == 0 and self._range_high != 0 and self._range_low != 0:
            if close > self._range_high:
                self.BuyMarket()
                self._entry_price = close
                self._sl0 = self._entry_price - atr_v * tf
                self._trailing_sl = self._entry_price + atr_v * tf
            elif close < self._range_low:
                self.SellMarket()
                self._entry_price = close
                self._sl0 = self._entry_price + atr_v * tf
                self._trailing_sl = self._entry_price - atr_v * tf
        if self.Position > 0 and self._sl0 is not None and self._trailing_sl is not None:
            if high > self._trailing_sl:
                step = atr_v * tf
                self._sl0 = self._trailing_sl - step
                self._trailing_sl += step
            if low <= self._sl0:
                self.SellMarket()
                self._sl0 = None
                self._trailing_sl = None
                self._range_high = 0.0
                self._range_low = 0.0
        elif self.Position < 0 and self._sl0 is not None and self._trailing_sl is not None:
            if low < self._trailing_sl:
                step = atr_v * tf
                self._sl0 = self._trailing_sl + step
                self._trailing_sl -= step
            if high >= self._sl0:
                self.BuyMarket()
                self._sl0 = None
                self._trailing_sl = None
                self._range_high = 0.0
                self._range_low = 0.0
        self._prev_range_cond = range_cond

    def CreateClone(self):
        return iu_range_trading_strategy()
