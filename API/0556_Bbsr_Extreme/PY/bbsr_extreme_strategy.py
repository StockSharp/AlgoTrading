import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class bbsr_extreme_strategy(Strategy):
    def __init__(self):
        super(bbsr_extreme_strategy, self).__init__()
        self._bb_period = self.Param("BollingerPeriod", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Bollinger Bands length", "Indicators")
        self._bb_mult = self.Param("BollingerMultiplier", 2.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Multiplier", "Standard deviation multiplier", "Indicators")
        self._ma_length = self.Param("MaLength", 7) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Length for moving average", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR indicator period", "Risk Management")
        self._atr_stop_mult = self.Param("AtrStopMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Stop Multiplier", "ATR multiplier for stop", "Risk Management")
        self._atr_profit_mult = self.Param("AtrProfitMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Profit Multiplier", "ATR multiplier for take profit", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_lower = 0.0
        self._prev_upper = 0.0
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._is_initialized = False
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(bbsr_extreme_strategy, self).OnReseted()
        self._prev_lower = 0.0
        self._prev_upper = 0.0
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._is_initialized = False
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(bbsr_extreme_strategy, self).OnStarted(time)
        bb = BollingerBands()
        bb.Length = self._bb_period.Value
        bb.Width = self._bb_mult.Value
        ma = ExponentialMovingAverage()
        ma.Length = self._ma_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, ma, atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value, ma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        bb = bb_value
        upper = bb.UpBand
        lower = bb.LowBand
        if upper is None or lower is None:
            return
        if not ma_value.IsFormed or not atr_value.IsFormed:
            return
        upper_v = float(upper)
        lower_v = float(lower)
        ma_v = float(ma_value)
        atr_v = float(atr_value)
        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        if not self._is_initialized:
            self._prev_lower = lower_v
            self._prev_upper = upper_v
            self._prev_close = close
            self._prev_ma = ma_v
            self._is_initialized = True
            return
        if self._cooldown > 0:
            self._cooldown -= 1
        bull = self._prev_close < self._prev_lower and close > lower_v and ma_v > self._prev_ma
        bear = self._prev_close > self._prev_upper and close < upper_v and ma_v < self._prev_ma
        if bull and self.Position <= 0 and self._cooldown == 0:
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 20
        elif bear and self.Position >= 0 and self._cooldown == 0:
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 20
        stop_mult = float(self._atr_stop_mult.Value)
        profit_mult = float(self._atr_profit_mult.Value)
        stop = stop_mult * atr_v
        profit = profit_mult * atr_v
        if self.Position > 0:
            if low <= self._entry_price - stop or high >= self._entry_price + profit:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 20
        elif self.Position < 0:
            if high >= self._entry_price + stop or low <= self._entry_price - profit:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 20
        self._prev_lower = lower_v
        self._prev_upper = upper_v
        self._prev_close = close
        self._prev_ma = ma_v

    def CreateClone(self):
        return bbsr_extreme_strategy()
