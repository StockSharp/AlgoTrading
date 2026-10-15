import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.BusinessEntities import Security
from StockSharp.Algo.Strategies import Strategy

class statistical_arbitrage_strategy(Strategy):
    """
    Statistical arbitrage strategy.
    Each instrument is compared with the simple moving average of its last LookbackPeriod closes on candles of the same time. The first
    instrument below its average while the second is above its own buys the first and sells the second, the mirror does the opposite,
    reversing an opposite pair. Both legs close once the first instrument closes back across its average, or once the spread, the first
    close minus the second, moves StopLossPercent of its entry value against the pair.
    """

    def __init__(self):
        super(statistical_arbitrage_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20).SetGreaterThanZero().SetDisplay("Lookback Period", "Closes each moving average spans", "Parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop-loss %", "Adverse spread move in percent of the entry spread", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
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
        self._first_history = []
        self._second_history = []
        # 1 while long the spread, -1 while short it, 0 while flat.
        self._side = 0
        self._entry_spread = Decimal(0)

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.second_security, self.candle_type)]

    def OnReseted(self):
        super(statistical_arbitrage_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(statistical_arbitrage_strategy, self).OnStarted2(time)

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

        period = self._lookback_period.Value

        self._first_history.append(first)
        self._second_history.append(second)
        if len(self._first_history) > period:
            self._first_history.pop(0)
            self._second_history.pop(0)

        if len(self._first_history) < period:
            return

        first_total = Decimal(0)
        second_total = Decimal(0)
        for value in self._first_history:
            first_total += value
        for value in self._second_history:
            second_total += value
        first_average = first_total / Decimal(period)
        second_average = second_total / Decimal(period)
        spread = first - second

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        stop_percent = Decimal(self._stop_loss_percent.Value)
        stop_distance = abs(self._entry_spread) * stop_percent / Decimal(100)

        if first < first_average and second > second_average and self._side <= 0:
            self._move_legs(1)
            self._entry_spread = spread
        elif first > first_average and second < second_average and self._side >= 0:
            self._move_legs(-1)
            self._entry_spread = spread
        elif self._side > 0 and (first > first_average or (stop_percent > 0 and spread <= self._entry_spread - stop_distance)):
            self._move_legs(0)
        elif self._side < 0 and (first < first_average or (stop_percent > 0 and spread >= self._entry_spread + stop_distance)):
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
        return statistical_arbitrage_strategy()
