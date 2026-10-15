import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange

class ais2_trading_robot20005_strategy(Strategy):
    def __init__(self):
        super(ais2_trading_robot20005_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")
        self._take_factor = self.Param("TakeFactor", 1.7) \
            .SetDisplay("Take Factor", "ATR multiplier for take profit", "Risk")
        self._stop_factor = self.Param("StopFactor", 1.0) \
            .SetDisplay("Stop Factor", "ATR multiplier for stop loss", "Risk")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_mid = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @property
    def TakeFactor(self):
        return self._take_factor.Value

    @property
    def StopFactor(self):
        return self._stop_factor.Value

    def OnStarted2(self, time):
        super(ais2_trading_robot20005_strategy, self).OnStarted2(time)

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_mid = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_val)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self._prev_high == 0 or av <= 0:
            self._prev_high = high
            self._prev_low = low
            self._prev_mid = (high + low) / 2.0
            return

        take_distance = av * float(self.TakeFactor)
        stop_distance = av * float(self.StopFactor)

        # Manage open position
        if self.Position > 0:
            if close - self._entry_price >= take_distance:
                self.SellMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
            elif self._stop_price > 0 and close <= self._stop_price:
                self.SellMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
            else:
                new_stop = close - stop_distance
                if new_stop > self._stop_price:
                    self._stop_price = new_stop
        elif self.Position < 0:
            if self._entry_price - close >= take_distance:
                self.BuyMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
            elif self._stop_price > 0 and close >= self._stop_price:
                self.BuyMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
            else:
                new_stop = close + stop_distance
                if new_stop < self._stop_price or self._stop_price == 0:
                    self._stop_price = new_stop

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_high = high
            self._prev_low = low
            self._prev_mid = (high + low) / 2.0
            return

        # New entry
        if self.Position == 0:
            if close > self._prev_high and close > self._prev_mid:
                self._entry_price = close
                self._stop_price = close - stop_distance
                self.BuyMarket()
            elif close < self._prev_low and close < self._prev_mid:
                self._entry_price = close
                self._stop_price = close + stop_distance
                self.SellMarket()

        self._prev_high = high
        self._prev_low = low
        self._prev_mid = (high + low) / 2.0

    def OnReseted(self):
        super(ais2_trading_robot20005_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_mid = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0

    def CreateClone(self):
        return ais2_trading_robot20005_strategy()
