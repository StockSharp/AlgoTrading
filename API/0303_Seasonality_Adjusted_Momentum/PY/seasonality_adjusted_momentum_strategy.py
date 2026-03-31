import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Momentum, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class seasonality_adjusted_momentum_strategy(Strategy):
    """
    Momentum strategy that allows longs or shorts only when the current month
    historically supports that seasonal bias.
    """

    def __init__(self):
        super(seasonality_adjusted_momentum_strategy, self).__init__()

        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetDisplay("Momentum Period", "Period for the momentum indicator", "Indicators")

        self._seasonality_threshold = self.Param("SeasonalityThreshold", 0.2) \
            .SetDisplay("Seasonality Threshold", "Minimum absolute seasonality strength required for entries", "Signals")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 120) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for the strategy", "General")

        self._seasonal_strength_by_month = {}
        self._momentum = None
        self._momentum_average = None
        self._cooldown = 0

        self._initialize_seasonality_data()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _initialize_seasonality_data(self):
        self._seasonal_strength_by_month[1] = 0.8
        self._seasonal_strength_by_month[2] = 0.2
        self._seasonal_strength_by_month[3] = 0.5
        self._seasonal_strength_by_month[4] = 0.7
        self._seasonal_strength_by_month[5] = 0.3
        self._seasonal_strength_by_month[6] = -0.2
        self._seasonal_strength_by_month[7] = -0.3
        self._seasonal_strength_by_month[8] = -0.4
        self._seasonal_strength_by_month[9] = -0.7
        self._seasonal_strength_by_month[10] = 0.4
        self._seasonal_strength_by_month[11] = 0.6
        self._seasonal_strength_by_month[12] = 0.9

    def OnReseted(self):
        super(seasonality_adjusted_momentum_strategy, self).OnReseted()
        self._momentum = None
        self._momentum_average = None
        self._cooldown = 0
        self._seasonal_strength_by_month.clear()
        self._initialize_seasonality_data()

    def OnStarted2(self, time):
        super(seasonality_adjusted_momentum_strategy, self).OnStarted2(time)

        mp = int(self._momentum_period.Value)
        self._momentum = Momentum()
        self._momentum.Length = mp
        self._momentum_average = SimpleMovingAverage()
        self._momentum_average.Length = mp
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._momentum, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._momentum)
            self.DrawIndicator(area, self._momentum_average)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(0, UnitTypes.Absolute), Unit(self._stop_loss_percent.Value, UnitTypes.Percent), False)

    def _process_candle(self, candle, momentum_value):
        if candle.State != CandleStates.Finished:
            return

        mv = float(momentum_value)

        avg_input = DecimalIndicatorValue(self._momentum_average, Decimal(mv), candle.OpenTime)
        avg_input.IsFinal = True
        momentum_avg_val = float(self._momentum_average.Process(avg_input))

        if not self._momentum.IsFormed or not self._momentum_average.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        seasonal_strength = self._seasonal_strength_by_month.get(candle.OpenTime.Month, 0.0)
        st = float(self._seasonality_threshold.Value)
        allow_long = seasonal_strength >= st
        allow_short = seasonal_strength <= -st
        bullish_momentum = mv > momentum_avg_val
        bearish_momentum = mv < momentum_avg_val
        cd = int(self._cooldown_bars.Value)

        if self.Position > 0:
            if not allow_long or bearish_momentum:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = cd
            return

        if self.Position < 0:
            if not allow_short or bullish_momentum:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown = cd
            return

        if allow_long and bullish_momentum:
            self.BuyMarket()
            self._cooldown = cd
        elif allow_short and bearish_momentum:
            self.SellMarket()
            self._cooldown = cd

    def CreateClone(self):
        return seasonality_adjusted_momentum_strategy()
