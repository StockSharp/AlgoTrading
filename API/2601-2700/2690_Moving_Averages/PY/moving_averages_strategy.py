import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage


class moving_averages_strategy(Strategy):
    """Moving average crossover strategy with shifted MA and loss streak position sizing."""

    def __init__(self):
        super(moving_averages_strategy, self).__init__()

        self._maximum_risk = self.Param("MaximumRisk", 0.02) \
            .SetGreaterThanZero() \
            .SetDisplay("Maximum Risk", "Fraction of equity risked per trade", "Risk")
        self._decrease_factor = self.Param("DecreaseFactor", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Decrease Factor", "Loss streak divisor for position sizing", "Risk")
        self._moving_period = self.Param("MovingPeriod", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Moving Period", "Simple moving average lookback", "Indicator")
        self._moving_shift = self.Param("MovingShift", 6) \
            .SetDisplay("Moving Shift", "Bars to shift the moving average", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for signals", "Data")

        self._shift_buffer = []
        self._shift_index = 0
        self._shift_fill_count = 0
        self._consecutive_losses = 0

    @property
    def MaximumRisk(self):
        return float(self._maximum_risk.Value)
    @property
    def DecreaseFactor(self):
        return float(self._decrease_factor.Value)
    @property
    def MovingPeriod(self):
        return int(self._moving_period.Value)
    @property
    def MovingShift(self):
        return int(self._moving_shift.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(moving_averages_strategy, self).OnStarted2(time)

        buf_size = max(1, self.MovingShift + 1)
        self._shift_buffer = [0.0] * buf_size
        self._shift_index = 0
        self._shift_fill_count = 0
        self._consecutive_losses = 0

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.MovingPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._sma, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma.IsFormed:
            return

        ma_val = float(ma_value)

        # Update shift buffer
        self._update_shift_buffer(ma_val)

        if not self._is_shift_ready():
            return

        shifted_ma = self._get_shifted_value()
        open_price = float(candle.OpenPrice)
        close = float(candle.ClosePrice)

        cross_down = open_price > shifted_ma and close < shifted_ma
        cross_up = open_price < shifted_ma and close > shifted_ma

        # Manage existing long position
        if self.Position > 0:
            if cross_down:
                self.SellMarket()
            return

        # Manage existing short position
        if self.Position < 0:
            if cross_up:
                self.BuyMarket()
            return

        # No position - evaluate entries
        if cross_up:
            self.BuyMarket()
        elif cross_down:
            self.SellMarket()

    def _update_shift_buffer(self, value):
        self._shift_buffer[self._shift_index] = value
        if self._shift_fill_count < len(self._shift_buffer):
            self._shift_fill_count += 1
        self._shift_index += 1
        if self._shift_index >= len(self._shift_buffer):
            self._shift_index = 0

    def _is_shift_ready(self):
        return self._shift_fill_count > self.MovingShift

    def _get_shifted_value(self):
        if len(self._shift_buffer) == 0:
            return 0.0
        offset = min(self.MovingShift, self._shift_fill_count - 1)
        index = self._shift_index - 1 - offset
        while index < 0:
            index += len(self._shift_buffer)
        return self._shift_buffer[index]

    def OnReseted(self):
        super(moving_averages_strategy, self).OnReseted()
        self._shift_buffer = []
        self._shift_index = 0
        self._shift_fill_count = 0
        self._consecutive_losses = 0

    def CreateClone(self):
        return moving_averages_strategy()
