import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class mean_reversion_momentum_strategy(Strategy):
    def __init__(self):
        super(mean_reversion_momentum_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._bars_to_count = self.Param("BarsToCount", 5)
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._rsi_overbought = self.Param("RsiOverbought", 70.0)
        self._rsi_oversold = self.Param("RsiOversold", 30.0)

        self._close_history = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BarsToCount(self):
        return self._bars_to_count.Value

    @BarsToCount.setter
    def BarsToCount(self, value):
        self._bars_to_count.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiOverbought(self):
        return self._rsi_overbought.Value

    @RsiOverbought.setter
    def RsiOverbought(self, value):
        self._rsi_overbought.Value = value

    @property
    def RsiOversold(self):
        return self._rsi_oversold.Value

    @RsiOversold.setter
    def RsiOversold(self, value):
        self._rsi_oversold.Value = value

    def OnReseted(self):
        super(mean_reversion_momentum_strategy, self).OnReseted()
        self._close_history = []

    def OnStarted2(self, time):
        super(mean_reversion_momentum_strategy, self).OnStarted2(time)
        self._close_history = []

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        rsi_val = float(rsi_value)
        bars_to_count = self.BarsToCount

        self._close_history.append(close)
        while len(self._close_history) > bars_to_count + 1:
            self._close_history.pop(0)

        if len(self._close_history) < bars_to_count + 1:
            return

        # Count consecutive down bars
        down_count = 0
        for i in range(len(self._close_history) - 1, 0, -1):
            if self._close_history[i] < self._close_history[i - 1]:
                down_count += 1
            else:
                break

        # Count consecutive up bars
        up_count = 0
        for i in range(len(self._close_history) - 1, 0, -1):
            if self._close_history[i] > self._close_history[i - 1]:
                up_count += 1
            else:
                break

        # Multi-bar sell-off + RSI oversold -> mean reversion buy
        if down_count >= bars_to_count and rsi_val < float(self.RsiOversold):
            if self.Position <= 0:
                self.BuyMarket()

        # Multi-bar rally + RSI overbought -> mean reversion sell
        elif up_count >= bars_to_count and rsi_val > float(self.RsiOverbought):
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return mean_reversion_momentum_strategy()
