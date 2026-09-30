import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class rci_strategy(Strategy):
    MA_SIMPLE = 0
    MA_EXPONENTIAL = 1
    DIR_BOTH = 0
    DIR_LONG_ONLY = 1
    DIR_SHORT_ONLY = 2

    def __init__(self):
        super(rci_strategy, self).__init__()
        self._rci_length = self.Param("RciLength", 10).SetGreaterThanZero()
        self._ma_type = self.Param("MaType", self.MA_SIMPLE)
        self._ma_length = self.Param("MaLength", 14).SetGreaterThanZero()
        self._direction = self.Param("Direction", self.DIR_BOTH)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))

        self._closes = []
        self._rci_values = []
        self._ma_ema = None
        self._previous_rci = None
        self._previous_ma = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(rci_strategy, self).OnReseted()
        self._closes = []
        self._rci_values = []
        self._ma_ema = None
        self._previous_rci = None
        self._previous_ma = None

    def OnStarted2(self, time):
        super(rci_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        length = int(self._rci_length.Value)
        self._closes.append(float(candle.ClosePrice))
        if len(self._closes) > length:
            del self._closes[0]

        if len(self._closes) < length:
            return

        rci = self.calculate_rci(self._closes)
        ma_length = int(self._ma_length.Value)
        self._rci_values.append(rci)
        if len(self._rci_values) > ma_length:
            del self._rci_values[0]

        if int(self._ma_type.Value) == self.MA_SIMPLE:
            if len(self._rci_values) < ma_length:
                return
            ma = sum(self._rci_values) / float(ma_length)
        else:
            self._ma_ema = self._ema(self._ma_ema, rci, ma_length)
            if len(self._rci_values) < ma_length:
                return
            ma = self._ma_ema

        if self._previous_rci is not None and self._previous_ma is not None:
            if self._previous_rci <= self._previous_ma and rci > ma:
                self._apply_signal(1)
            elif self._previous_rci >= self._previous_ma and rci < ma:
                self._apply_signal(-1)

        self._previous_rci = rci
        self._previous_ma = ma

    def _apply_signal(self, signal):
        direction = int(self._direction.Value)
        allow_long = direction in (self.DIR_BOTH, self.DIR_LONG_ONLY)
        allow_short = direction in (self.DIR_BOTH, self.DIR_SHORT_ONLY)

        if signal > 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position) + (self.Volume if allow_long else 0))
            elif self.Position == 0 and allow_long:
                self.BuyMarket()
        elif signal < 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position) + (self.Volume if allow_short else 0))
            elif self.Position == 0 and allow_short:
                self.SellMarket()

    @staticmethod
    def calculate_rci(values):
        n = len(values)
        if n < 2:
            return 0.0

        indexed = sorted(enumerate(values), key=lambda item: item[1])
        ranks = [0.0] * n
        i = 0
        while i < n:
            j = i + 1
            while j < n and indexed[j][1] == indexed[i][1]:
                j += 1
            average_rank = ((i + 1) + j) / 2.0
            for k in range(i, j):
                ranks[indexed[k][0]] = average_rank
            i = j

        sum_squared = 0.0
        for i, rank in enumerate(ranks):
            diff = (i + 1) - rank
            sum_squared += diff * diff

        return (1.0 - 6.0 * sum_squared / (n * (n * n - 1.0))) * 100.0

    @staticmethod
    def _ema(previous, value, period):
        if previous is None:
            return value
        alpha = 2.0 / (period + 1.0)
        return previous + alpha * (value - previous)

    def CreateClone(self):
        return rci_strategy()
