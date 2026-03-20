import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SmoothedMovingAverage


class vector_strategy(Strategy):
    """Trend strategy using smoothed moving averages with equity-based exit."""

    def __init__(self):
        super(vector_strategy, self).__init__()

        self._fast_ma_period = self.Param("FastMaPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA", "Fast smoothed moving average period", "Indicators")

        self._slow_ma_period = self.Param("SlowMaPeriod", 7) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA", "Slow smoothed moving average period", "Indicators")

        self._ma_shift = self.Param("MaShift", 8) \
            .SetNotNegative() \
            .SetDisplay("MA Shift", "Additional warm-up bars before signals", "Indicators")

        self._profit_percent = self.Param("ProfitPercent", 0.5) \
            .SetNotNegative() \
            .SetDisplay("Equity TP %", "Close all when floating profit reaches this percent", "Risk")

        self._loss_percent = self.Param("LossPercent", 30.0) \
            .SetNotNegative() \
            .SetDisplay("Equity SL %", "Close all when floating loss reaches this percent", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Signal Timeframe", "Timeframe for moving averages", "General")

        self._entry_price = 0.0
        self._initial_balance = 0.0
        self._processed_bars = 0

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    @property
    def MaShift(self):
        return self._ma_shift.Value

    @property
    def ProfitPercent(self):
        return self._profit_percent.Value

    @property
    def LossPercent(self):
        return self._loss_percent.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(vector_strategy, self).OnStarted(time)

        fast = SmoothedMovingAverage()
        fast.Length = self.FastMaPeriod

        slow = SmoothedMovingAverage()
        slow.Length = self.SlowMaPeriod

        portfolio = self.Portfolio
        self._initial_balance = float(portfolio.CurrentValue) if portfolio is not None and portfolio.CurrentValue is not None else 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(fast, slow, self.process_candle) \
            .Start()

    def process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        self._processed_bars += 1

        if not self.IsFormed:
            return

        if self._processed_bars <= self.MaShift:
            return

        fast = float(fast_val)
        slow = float(slow_val)

        # Check equity thresholds
        if self.Position != 0 and self._initial_balance > 0:
            portfolio = self.Portfolio
            equity = float(portfolio.CurrentValue) if portfolio is not None and portfolio.CurrentValue is not None else 0.0
            floating = equity - self._initial_balance
            profit_threshold = self._initial_balance * float(self.ProfitPercent) / 100.0
            loss_threshold = self._initial_balance * float(self.LossPercent) / 100.0

            if (profit_threshold > 0 and floating >= profit_threshold) or \
               (loss_threshold > 0 and floating <= -loss_threshold):
                if self.Position > 0:
                    self.SellMarket()
                elif self.Position < 0:
                    self.BuyMarket()
                self._entry_price = 0.0
                return

        # Entry/exit logic
        if fast > slow and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = float(candle.ClosePrice)
        elif fast < slow and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = float(candle.ClosePrice)

    def OnReseted(self):
        super(vector_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._initial_balance = 0.0
        self._processed_bars = 0

    def CreateClone(self):
        return vector_strategy()
