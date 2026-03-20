import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class symbol_swap_strategy(Strategy):

    def __init__(self):
        super(symbol_swap_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle series for signals", "General")
        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Period", "Moving average period", "Indicators")
        self._spread_threshold = self.Param("SpreadThreshold", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Spread Threshold", "Price deviation from SMA to trigger entry", "Signals")

        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def SmaPeriod(self):
        return self._sma_period.Value

    @SmaPeriod.setter
    def SmaPeriod(self, value):
        self._sma_period.Value = value

    @property
    def SpreadThreshold(self):
        return self._spread_threshold.Value

    @SpreadThreshold.setter
    def SpreadThreshold(self, value):
        self._spread_threshold.Value = value

    def OnReseted(self):
        super(symbol_swap_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(symbol_swap_strategy, self).OnStarted(time)

        sma = SimpleMovingAverage()
        sma.Length = self.SmaPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(sma, self._process_candle) \
            .Start()

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormed:
            return

        price = float(candle.ClosePrice)
        sma_v = float(sma_value)
        threshold = float(self.SpreadThreshold)

        # Exit on mean reversion
        if self.Position != 0 and self._entry_price > 0:
            pnl = price - self._entry_price if self.Position > 0 else self._entry_price - price

            if pnl >= threshold or pnl <= -threshold * 2:
                if self.Position > 0:
                    self.SellMarket(abs(self.Position))
                else:
                    self.BuyMarket(abs(self.Position))
                self._entry_price = 0.0
                return

        # Entry on deviation
        if self.Position == 0:
            deviation = price - sma_v

            if deviation > threshold:
                self.SellMarket()
                self._entry_price = price
            elif deviation < -threshold:
                self.BuyMarket()
                self._entry_price = price

    def CreateClone(self):
        return symbol_swap_strategy()
