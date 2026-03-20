import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_martingale_strategy(Strategy):
    def __init__(self):
        super(rsi_martingale_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._bars_for_condition = self.Param("BarsForCondition", 10)

        self._recent_rsi = []
        self._entry_price = 0.0
        self._direction = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def BarsForCondition(self):
        return self._bars_for_condition.Value

    @BarsForCondition.setter
    def BarsForCondition(self, value):
        self._bars_for_condition.Value = value

    def OnReseted(self):
        super(rsi_martingale_strategy, self).OnReseted()
        self._recent_rsi = []
        self._entry_price = 0.0
        self._direction = 0

    def OnStarted(self, time):
        super(rsi_martingale_strategy, self).OnStarted(time)
        self._recent_rsi = []
        self._entry_price = 0.0
        self._direction = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _is_local_minimum(self):
        if len(self._recent_rsi) < 2:
            return False
        current = self._recent_rsi[-1]
        for i in range(len(self._recent_rsi) - 1):
            if current > self._recent_rsi[i]:
                return False
        return True

    def _is_local_maximum(self):
        if len(self._recent_rsi) < 2:
            return False
        current = self._recent_rsi[-1]
        for i in range(len(self._recent_rsi) - 1):
            if current < self._recent_rsi[i]:
                return False
        return True

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        bars_for_cond = self.BarsForCondition

        self._recent_rsi.append(rsi_val)
        while len(self._recent_rsi) > bars_for_cond:
            self._recent_rsi.pop(0)

        if len(self._recent_rsi) < bars_for_cond:
            return

        # Check exit: close on RSI crossing 50
        if self._direction > 0 and rsi_val > 50:
            self.SellMarket()
            self._direction = 0
            return
        elif self._direction < 0 and rsi_val < 50:
            self.BuyMarket()
            self._direction = 0
            return

        if self.Position != 0:
            return

        close = float(candle.ClosePrice)

        # Local minimum + RSI below 50 -> buy
        if self._is_local_minimum() and rsi_val < 50 and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._direction = 1
        # Local maximum + RSI above 50 -> sell
        elif self._is_local_maximum() and rsi_val > 50 and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._direction = -1

    def CreateClone(self):
        return rsi_martingale_strategy()
