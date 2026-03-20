import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange

class sto_m5x_m15x_m30_strategy(Strategy):
    def __init__(self):
        super(sto_m5x_m15x_m30_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 30) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period for stops", "Indicators")

        self._prev_rsi = 0.0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    def OnStarted(self, time):
        super(sto_m5x_m15x_m30_strategy, self).OnStarted(time)

        self._prev_rsi = 0.0
        self._entry_price = 0.0

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiLength
        self._fast = ExponentialMovingAverage()
        self._fast.Length = self.FastLength
        self._slow = ExponentialMovingAverage()
        self._slow.Length = self.SlowLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self._fast, self._slow, self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_val, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_val)
        fv = float(fast_val)
        sv = float(slow_val)
        av = float(atr_val)

        if self._prev_rsi == 0 or av <= 0:
            self._prev_rsi = rv
            return

        close = float(candle.ClosePrice)

        # Exit management
        if self.Position > 0:
            if close <= self._entry_price - av * 2.0 or close >= self._entry_price + av * 3.0:
                self.SellMarket()
                self._entry_price = 0.0
            elif rv < 40 and fv < sv:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close >= self._entry_price + av * 2.0 or close <= self._entry_price - av * 3.0:
                self.BuyMarket()
                self._entry_price = 0.0
            elif rv > 60 and fv > sv:
                self.BuyMarket()
                self._entry_price = 0.0

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rv
            return

        # Entry: RSI crosses 50 with EMA alignment
        if self.Position == 0:
            if self._prev_rsi <= 50 and rv > 50 and fv > sv:
                self._entry_price = close
                self.BuyMarket()
            elif self._prev_rsi >= 50 and rv < 50 and fv < sv:
                self._entry_price = close
                self.SellMarket()

        self._prev_rsi = rv

    def OnReseted(self):
        super(sto_m5x_m15x_m30_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return sto_m5x_m15x_m30_strategy()
