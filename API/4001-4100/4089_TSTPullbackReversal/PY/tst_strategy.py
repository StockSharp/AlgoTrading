import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange

class tst_strategy(Strategy):
    def __init__(self):
        super(tst_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")
        self._pullback_multiplier = self.Param("PullbackMultiplier", 0.5) \
            .SetDisplay("Pullback Mult", "ATR multiplier for pullback threshold", "Signals")
        self._stop_multiplier = self.Param("StopMultiplier", 2.0) \
            .SetDisplay("Stop Mult", "ATR multiplier for stop loss", "Risk")
        self._take_multiplier = self.Param("TakeMultiplier", 1.0) \
            .SetDisplay("Take Mult", "ATR multiplier for take profit", "Risk")

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @property
    def PullbackMultiplier(self):
        return self._pullback_multiplier.Value

    @property
    def StopMultiplier(self):
        return self._stop_multiplier.Value

    @property
    def TakeMultiplier(self):
        return self._take_multiplier.Value

    def OnStarted2(self, time):
        super(tst_strategy, self).OnStarted2(time)

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_val)
        if av <= 0:
            return

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        threshold = av * float(self.PullbackMultiplier)
        stop_dist = av * float(self.StopMultiplier)
        take_dist = av * float(self.TakeMultiplier)

        # Risk management
        if self.Position > 0:
            if self._stop_price > 0 and close <= self._stop_price:
                self.SellMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
                self._take_price = 0.0
                return
            if self._take_price > 0 and close >= self._take_price:
                self.SellMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
                self._take_price = 0.0
                return
        elif self.Position < 0:
            if self._stop_price > 0 and close >= self._stop_price:
                self.BuyMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
                self._take_price = 0.0
                return
            if self._take_price > 0 and close <= self._take_price:
                self.BuyMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
                self._take_price = 0.0
                return

        # Entry: deep pullback from high = buy reversal
        if self.Position == 0:
            if open_p > close and high - close > threshold:
                self._entry_price = close
                self._stop_price = close - stop_dist
                self._take_price = close + take_dist
                self.BuyMarket()
            elif close > open_p and close - low > threshold:
                self._entry_price = close
                self._stop_price = close + stop_dist
                self._take_price = close - take_dist
                self.SellMarket()

    def OnReseted(self):
        super(tst_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def CreateClone(self):
        return tst_strategy()
