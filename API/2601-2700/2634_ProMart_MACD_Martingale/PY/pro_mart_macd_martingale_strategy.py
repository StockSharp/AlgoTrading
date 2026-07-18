import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    MovingAverageConvergenceDivergence, ExponentialMovingAverage
)


class pro_mart_macd_martingale_strategy(Strategy):
    """MACD-based martingale strategy with dual MACD and volume doubling on loss."""

    def __init__(self):
        super(pro_mart_macd_martingale_strategy, self).__init__()

        self._max_doubling = self.Param("MaxDoublingCount", 2) \
            .SetDisplay("Max Doubling", "Max volume doublings after losses", "Risk")
        self._macd1_fast = self.Param("Macd1Fast", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD1 Fast", "Fast EMA period for primary MACD", "Signal")
        self._macd1_slow = self.Param("Macd1Slow", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD1 Slow", "Slow EMA period for primary MACD", "Signal")
        self._macd2_fast = self.Param("Macd2Fast", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD2 Fast", "Fast EMA period for secondary MACD", "Filter")
        self._macd2_slow = self.Param("Macd2Slow", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD2 Slow", "Slow EMA period for secondary MACD", "Filter")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Data type for signal generation", "General")

        self._macd1_history = []
        self._macd2_history = []
        self._entry_price = 0.0
        self._in_position = False
        self._is_long = False
        self._last_loss = False
        self._mart_counter = 0
        self._current_volume = 0.0

    @property
    def MaxDoublingCount(self):
        return self._max_doubling.Value
    @property
    def Macd1Fast(self):
        return self._macd1_fast.Value
    @property
    def Macd1Slow(self):
        return self._macd1_slow.Value
    @property
    def Macd2Fast(self):
        return self._macd2_fast.Value
    @property
    def Macd2Slow(self):
        return self._macd2_slow.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(pro_mart_macd_martingale_strategy, self).OnStarted2(time)

        self._macd1_history = []
        self._macd2_history = []
        self._in_position = False
        self._is_long = False
        self._last_loss = False
        self._mart_counter = 0
        self._current_volume = float(self.Volume) if self.Volume > 0 else 1.0
        self._entry_price = 0.0

        slow1 = ExponentialMovingAverage()
        slow1.Length = self.Macd1Slow
        fast1 = ExponentialMovingAverage()
        fast1.Length = self.Macd1Fast
        macd1 = MovingAverageConvergenceDivergence(slow1, fast1)

        slow2 = ExponentialMovingAverage()
        slow2.Length = self.Macd2Slow
        fast2 = ExponentialMovingAverage()
        fast2.Length = self.Macd2Fast
        macd2 = MovingAverageConvergenceDivergence(slow2, fast2)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(macd1, macd2, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd1)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, macd1_val, macd2_val):
        if candle.State != CandleStates.Finished:
            return

        m1 = float(macd1_val)
        m2 = float(macd2_val)

        self._macd1_history.append(m1)
        self._macd2_history.append(m2)

        if len(self._macd1_history) > 4:
            self._macd1_history.pop(0)
        if len(self._macd2_history) > 3:
            self._macd2_history.pop(0)

        close = float(candle.ClosePrice)

        # Check exit
        if self._in_position:
            pnl = close - self._entry_price if self._is_long else self._entry_price - close

            should_exit = False
            if len(self._macd1_history) >= 3:
                m0 = self._macd1_history[-1]
                m1p = self._macd1_history[-2]
                m2p = self._macd1_history[-3]

                if self._is_long and m0 < m1p and m1p > m2p:
                    should_exit = True
                elif not self._is_long and m0 > m1p and m1p < m2p:
                    should_exit = True

            if should_exit:
                if self._is_long:
                    self.SellMarket()
                else:
                    self.BuyMarket()

                self._last_loss = pnl < 0
                if self._last_loss and self._mart_counter < self.MaxDoublingCount:
                    self._current_volume *= 2.0
                    self._mart_counter += 1
                else:
                    self._current_volume = float(self.Volume) if self.Volume > 0 else 1.0
                    self._mart_counter = 0

                self._in_position = False
                return

        # Check entry
        if not self._in_position and len(self._macd1_history) >= 3 and len(self._macd2_history) >= 2:
            m0 = self._macd1_history[-1]
            m1p = self._macd1_history[-2]
            m2p = self._macd1_history[-3]
            f0 = self._macd2_history[-1]
            f1 = self._macd2_history[-2]

            buy_signal = m0 > m1p and m1p < m2p and f1 > f0
            sell_signal = m0 < m1p and m1p > m2p and f1 < f0

            if buy_signal and self.Position <= 0:
                self.BuyMarket()
                self._in_position = True
                self._is_long = True
                self._entry_price = close
            elif sell_signal and self.Position >= 0:
                self.SellMarket()
                self._in_position = True
                self._is_long = False
                self._entry_price = close

    def OnReseted(self):
        super(pro_mart_macd_martingale_strategy, self).OnReseted()
        self._macd1_history = []
        self._macd2_history = []
        self._entry_price = 0.0
        self._in_position = False
        self._is_long = False
        self._last_loss = False
        self._mart_counter = 0
        self._current_volume = 0.0

    def CreateClone(self):
        return pro_mart_macd_martingale_strategy()
