import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.BusinessEntities import Security
from StockSharp.Algo.Strategies import Strategy

class cointegration_pairs_strategy(Strategy):
    """
    Cointegration pairs trading strategy.
    The residual is the first instrument's close minus Beta times Asset2's on candles of the same time, and its z-score is measured against
    the mean and standard deviation of the last Period residuals. A z-score below minus EntryThreshold buys the first instrument and sells
    Beta times as much of Asset2, one above EntryThreshold does the opposite, reversing an opposite pair. Both legs close once the z-score
    is back within ExitThreshold of zero, or once the residual moves StopLossPercent of its entry value against the pair.
    """

    def __init__(self):
        super(cointegration_pairs_strategy, self).__init__()
        self._period = self.Param("Period", 20).SetGreaterThanZero().SetDisplay("Period", "Residuals the mean and standard deviation are measured over", "Parameters")
        self._entry_threshold = self.Param("EntryThreshold", 2.0).SetGreaterThanZero().SetDisplay("Entry Threshold", "Z-score distance from zero that opens a pair", "Parameters")
        self._exit_threshold = self.Param("ExitThreshold", 0.5).SetNotNegative().SetDisplay("Exit Threshold", "Z-score distance from zero within which the pair closes", "Parameters")
        self._beta = self.Param("Beta", 1.0).SetGreaterThanZero().SetDisplay("Beta", "Hedge ratio of Asset2 to the first instrument", "Parameters")
        self._asset2 = self.Param[Security]("Asset2", None).SetDisplay("Asset 2", "Second asset of the pair", "Parameters").SetRequired()
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Adverse residual move in percent of the entry residual", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def asset2(self):
        return self._asset2.Value

    @asset2.setter
    def asset2(self, value):
        self._asset2.Value = value

    def _reset_state(self):
        self._first_closes = {}
        self._second_closes = {}
        self._residuals = []
        # 1 while long the pair, -1 while short it, 0 while flat.
        self._side = 0
        self._entry_residual = Decimal(0)

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.asset2, self.candle_type)]

    def OnReseted(self):
        super(cointegration_pairs_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(cointegration_pairs_strategy, self).OnStarted2(time)

        if self.asset2 is None:
            raise Exception("Second asset is not specified.")

        self._reset_state()

        first_subscription = self.SubscribeCandles(self.candle_type)
        first_subscription.Bind(self._process_first_candle).Start()

        second_subscription = self.SubscribeCandles(self.candle_type, security=self.asset2)
        second_subscription.Bind(self._process_second_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, first_subscription)
            self.DrawOwnTrades(area)

    def _process_first_candle(self, candle):
        self._process_candle(candle, self._first_closes)

    def _process_second_candle(self, candle):
        self._process_candle(candle, self._second_closes)

    def _process_candle(self, candle, closes):
        if candle.State != CandleStates.Finished:
            return

        time = candle.OpenTime
        closes[time] = candle.ClosePrice

        # The residual needs both instruments' candles of the same time, whichever arrives last.
        if time not in self._first_closes or time not in self._second_closes:
            return

        first = self._first_closes[time]
        second = self._second_closes[time]

        for stale in [t for t in self._first_closes if t <= time]:
            del self._first_closes[stale]
        for stale in [t for t in self._second_closes if t <= time]:
            del self._second_closes[stale]

        residual = first - Decimal(self._beta.Value) * second
        period = self._period.Value

        self._residuals.append(residual)
        if len(self._residuals) > period:
            self._residuals.pop(0)

        if len(self._residuals) < period:
            return

        total = Decimal(0)
        for value in self._residuals:
            total += value
        mean = total / Decimal(period)

        squares = Decimal(0)
        for value in self._residuals:
            squares += (value - mean) * (value - mean)
        deviation = Decimal(Math.Sqrt(Decimal.ToDouble(squares / Decimal(period))))

        if deviation == 0:
            return

        z_score = (residual - mean) / deviation

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        entry = Decimal(self._entry_threshold.Value)
        exit_level = Decimal(self._exit_threshold.Value)
        stop_percent = Decimal(self._stop_loss_percent.Value)
        stop_distance = abs(self._entry_residual) * stop_percent / Decimal(100)

        if z_score < -entry and self._side <= 0:
            self._move_legs(1)
            self._entry_residual = residual
        elif z_score > entry and self._side >= 0:
            self._move_legs(-1)
            self._entry_residual = residual
        elif self._side > 0 and (abs(z_score) < exit_level or (stop_percent > 0 and residual <= self._entry_residual - stop_distance)):
            self._move_legs(0)
        elif self._side < 0 and (abs(z_score) < exit_level or (stop_percent > 0 and residual >= self._entry_residual + stop_distance)):
            self._move_legs(0)

    def _move_legs(self, side):
        self._side = side

        # Long the pair holds Volume of the first instrument and is short Beta times as much of Asset2.
        first_change = Decimal(side) * self.Volume - self.Position

        if first_change > 0:
            self.BuyMarket(first_change)
        elif first_change < 0:
            self.SellMarket(-first_change)

        second_position = self.GetPositionValue(self.asset2, self.Portfolio)
        if second_position is None:
            second_position = Decimal(0)

        second_change = Decimal(-side) * self.Volume * Decimal(self._beta.Value) - second_position

        if second_change > 0:
            self.BuyMarket(second_change, self.asset2)
        elif second_change < 0:
            self.SellMarket(-second_change, self.asset2)

    def CreateClone(self):
        return cointegration_pairs_strategy()
