import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage

class glfx_strategy(Strategy):
    def __init__(self):
        super(glfx_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._rsi_upper = self.Param("RsiUpper", 65.0) \
            .SetDisplay("RSI Upper", "Overbought level", "Indicators")
        self._rsi_lower = self.Param("RsiLower", 35.0) \
            .SetDisplay("RSI Lower", "Oversold level", "Indicators")
        self._ma_period = self.Param("MaPeriod", 60) \
            .SetDisplay("MA Period", "SMA period", "Indicators")
        self._signals_repeat = self.Param("SignalsRepeat", 2) \
            .SetDisplay("Signals Repeat", "Consecutive confirmations needed", "Signals")

        self._prev_rsi = 0.0
        self._prev_ma = 0.0
        self._buy_count = 0
        self._sell_count = 0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def RsiUpper(self):
        return self._rsi_upper.Value

    @property
    def RsiLower(self):
        return self._rsi_lower.Value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @property
    def SignalsRepeat(self):
        return self._signals_repeat.Value

    def OnStarted(self, time):
        super(glfx_strategy, self).OnStarted(time)

        self._prev_rsi = 0.0
        self._prev_ma = 0.0
        self._buy_count = 0
        self._sell_count = 0
        self._entry_price = 0.0

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self._ma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_val, ma_val):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_val)
        mv = float(ma_val)

        if self._prev_rsi == 0 or self._prev_ma == 0:
            self._prev_rsi = rv
            self._prev_ma = mv
            return

        close = float(candle.ClosePrice)
        rsi_upper = float(self.RsiUpper)
        rsi_lower = float(self.RsiLower)

        # RSI signal
        rsi_signal = 0
        if rv > self._prev_rsi and rv < rsi_upper:
            rsi_signal = 1
        elif rv < self._prev_rsi and rv > rsi_lower:
            rsi_signal = -1

        # MA signal
        ma_signal = 0
        if mv > self._prev_ma and close > mv:
            ma_signal = 1
        elif mv < self._prev_ma and close < mv:
            ma_signal = -1

        # Both signals must agree
        if rsi_signal > 0 and ma_signal > 0:
            self._buy_count += 1
            self._sell_count = 0
        elif rsi_signal < 0 and ma_signal < 0:
            self._sell_count += 1
            self._buy_count = 0
        else:
            self._buy_count = 0
            self._sell_count = 0

        signals_needed = self.SignalsRepeat

        # Exit on opposite signal
        if self.Position > 0 and self._sell_count >= signals_needed:
            self.SellMarket()
            self._entry_price = 0.0
            self._buy_count = 0
            self._sell_count = 0
        elif self.Position < 0 and self._buy_count >= signals_needed:
            self.BuyMarket()
            self._entry_price = 0.0
            self._buy_count = 0
            self._sell_count = 0

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rv
            self._prev_ma = mv
            return

        # Entry after required confirmations
        if self.Position == 0:
            if self._buy_count >= signals_needed:
                self._entry_price = close
                self.BuyMarket()
                self._buy_count = 0
                self._sell_count = 0
            elif self._sell_count >= signals_needed:
                self._entry_price = close
                self.SellMarket()
                self._buy_count = 0
                self._sell_count = 0

        self._prev_rsi = rv
        self._prev_ma = mv

    def OnReseted(self):
        super(glfx_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._prev_ma = 0.0
        self._buy_count = 0
        self._sell_count = 0
        self._entry_price = 0.0

    def CreateClone(self):
        return glfx_strategy()
