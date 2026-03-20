import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex

class billy_expert_reversal_strategy(Strategy):
    def __init__(self):
        super(billy_expert_reversal_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "Length for RSI indicator", "Indicators")

        self._prev_high1 = 0.0
        self._prev_high2 = 0.0
        self._prev_high3 = 0.0
        self._bar_count = 0
        self._prev_rsi = 50.0
        self._has_prev_rsi = False
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    def OnStarted(self, time):
        super(billy_expert_reversal_strategy, self).OnStarted(time)

        self._prev_high1 = 0.0
        self._prev_high2 = 0.0
        self._prev_high3 = 0.0
        self._bar_count = 0
        self._prev_rsi = 50.0
        self._has_prev_rsi = False
        self._entry_price = 0.0

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_count += 1

        high = float(candle.HighPrice)
        close = float(candle.ClosePrice)
        rsi_val = float(rsi_value)

        # Check descending highs pattern (3 consecutive lower highs)
        descending_highs = (self._bar_count >= 4 and
                            high < self._prev_high1 and
                            self._prev_high1 < self._prev_high2 and
                            self._prev_high2 < self._prev_high3)

        # RSI turning up from oversold
        rsi_bullish = self._has_prev_rsi and self._prev_rsi < 40 and rsi_val > self._prev_rsi

        # Manage long position
        if self.Position > 0:
            if self._entry_price > 0 and close >= self._entry_price * 1.015:
                self.SellMarket()
            elif self._entry_price > 0 and close <= self._entry_price * 0.985:
                self.SellMarket()
            elif rsi_val > 75:
                self.SellMarket()

        # Manage short position (exit only)
        if self.Position < 0:
            if rsi_val < 30:
                self.BuyMarket()

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_high3 = self._prev_high2
            self._prev_high2 = self._prev_high1
            self._prev_high1 = high
            self._prev_rsi = rsi_val
            self._has_prev_rsi = True
            return

        # Entry
        if self.Position == 0:
            if descending_highs and rsi_bullish:
                self._entry_price = close
                self.BuyMarket()
            elif self._bar_count >= 4 and rsi_val > 70 and self._prev_rsi > 70:
                self._entry_price = close
                self.SellMarket()

        # Update history
        self._prev_high3 = self._prev_high2
        self._prev_high2 = self._prev_high1
        self._prev_high1 = high
        self._prev_rsi = rsi_val
        self._has_prev_rsi = True

    def OnReseted(self):
        super(billy_expert_reversal_strategy, self).OnReseted()
        self._prev_high1 = 0.0
        self._prev_high2 = 0.0
        self._prev_high3 = 0.0
        self._bar_count = 0
        self._prev_rsi = 50.0
        self._has_prev_rsi = False
        self._entry_price = 0.0

    def CreateClone(self):
        return billy_expert_reversal_strategy()
