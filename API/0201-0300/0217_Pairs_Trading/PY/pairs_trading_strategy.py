import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.BusinessEntities import Security
from StockSharp.Algo.Strategies import Strategy

class pairs_trading_strategy(Strategy):
    """
    Pairs trading strategy.
    The spread is the first instrument's close minus the second's on candles of the same time. A spread more than DeviationMultiplier
    standard deviations below the mean of the last LookbackPeriod spreads buys the first instrument and sells the second, one more than
    that above it does the opposite, reversing an opposite pair. Both legs close once the spread returns to the mean, or once it moves
    StopLossPercent of its entry value against the pair.
    """

    def __init__(self):
        super(pairs_trading_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20).SetGreaterThanZero().SetDisplay("Lookback Period", "Spreads the mean and standard deviation are measured over", "Parameters")
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Deviation Multiplier", "Standard deviations between the mean and an entry level", "Parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop-loss %", "Adverse spread move in percent of the entry spread", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._second_security = self.Param[Security]("SecondSecurity", None).SetDisplay("Second Security", "Second security in the pair", "General").SetRequired()
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def second_security(self):
        return self._second_security.Value

    @second_security.setter
    def second_security(self, value):
        self._second_security.Value = value

    def _reset_state(self):
        self._first_closes = {}
        self._second_closes = {}
        self._spreads = []
        # 1 while long the spread, -1 while short it, 0 while flat.
        self._side = 0
        self._entry_spread = Decimal(0)

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.second_security, self.candle_type)]

    def OnReseted(self):
        super(pairs_trading_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(pairs_trading_strategy, self).OnStarted2(time)

        if self.second_security is None:
            raise Exception("Second security is not specified.")

        self._reset_state()

        first_subscription = self.SubscribeCandles(self.candle_type)
        first_subscription.Bind(self._process_first_candle).Start()

        second_subscription = self.SubscribeCandles(self.candle_type, security=self.second_security)
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

        # The spread needs both instruments' candles of the same time, whichever arrives last.
        if time not in self._first_closes or time not in self._second_closes:
            return

        first = self._first_closes[time]
        second = self._second_closes[time]

        for stale in [t for t in self._first_closes if t <= time]:
            del self._first_closes[stale]
        for stale in [t for t in self._second_closes if t <= time]:
            del self._second_closes[stale]

        spread = first - second
        period = self._lookback_period.Value

        self._spreads.append(spread)
        if len(self._spreads) > period:
            self._spreads.pop(0)

        if len(self._spreads) < period:
            return

        total = Decimal(0)
        for value in self._spreads:
            total += value
        mean = total / Decimal(period)

        squares = Decimal(0)
        for value in self._spreads:
            squares += (value - mean) * (value - mean)
        deviation = Decimal(Math.Sqrt(Decimal.ToDouble(squares / Decimal(period))))

        multiplier = Decimal(self._deviation_multiplier.Value)
        upper = mean + multiplier * deviation
        lower = mean - multiplier * deviation

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        stop_percent = Decimal(self._stop_loss_percent.Value)
        stop_distance = abs(self._entry_spread) * stop_percent / Decimal(100)

        if spread < lower and self._side <= 0:
            self._move_legs(1)
            self._entry_spread = spread
        elif spread > upper and self._side >= 0:
            self._move_legs(-1)
            self._entry_spread = spread
        elif self._side > 0 and (spread >= mean or (stop_percent > 0 and spread <= self._entry_spread - stop_distance)):
            self._move_legs(0)
        elif self._side < 0 and (spread <= mean or (stop_percent > 0 and spread >= self._entry_spread + stop_distance)):
            self._move_legs(0)

    def _move_legs(self, side):
        self._side = side

        # Long the spread holds the first instrument and is short the second, each by Volume.
        first_change = Decimal(side) * self.Volume - self.Position

        if first_change > 0:
            self.BuyMarket(first_change)
        elif first_change < 0:
            self.SellMarket(-first_change)

        second_position = self.GetPositionValue(self.second_security, self.Portfolio)
        if second_position is None:
            second_position = Decimal(0)

        second_change = Decimal(-side) * self.Volume - second_position

        if second_change > 0:
            self.BuyMarket(second_change, self.second_security)
        elif second_change < 0:
            self.SellMarket(-second_change, self.second_security)

    def CreateClone(self):
        return pairs_trading_strategy()
