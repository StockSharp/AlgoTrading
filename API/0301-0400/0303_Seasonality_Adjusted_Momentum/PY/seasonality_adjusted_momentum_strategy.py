import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTime, InvalidOperationException
from System.Globalization import CultureInfo
from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Momentum, SimpleMovingAverage
from StockSharp.BusinessEntities import Subscription
from StockSharp.Messages import Level1Fields
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class seasonality_adjusted_momentum_strategy(Strategy):
    """
    Momentum strategy that allows longs or shorts only when the current month
    historically supports that seasonal bias.
    """

    def __init__(self):
        super(seasonality_adjusted_momentum_strategy, self).__init__()
        self._seasonality_history_file = self.Param[str]("SeasonalityHistoryFile", None).SetRequired()
        self._minimum_seasonal_samples = self.Param("MinimumSeasonalSamples", 2).SetGreaterThanZero()
        self._seasonality_lookback_years = self.Param("SeasonalityLookbackYears", 10).SetRange(1, 100)
        self._seasonal_history = []
        self._seasonal_month = None
        self._seasonal_value = None

        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetDisplay("Momentum Period", "Period for the momentum indicator", "Indicators")

        self._seasonality_threshold = self.Param("SeasonalityThreshold", 0.5) \
            .SetDisplay("Seasonality Threshold", "Minimum absolute seasonality strength required for entries", "Signals")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 120) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for the strategy", "General")

        self._seasonal_history = []
        self._momentum = None
        self._momentum_average = None
        self._cooldown = 0


    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(seasonality_adjusted_momentum_strategy, self).OnReseted()
        self._seasonal_history = []
        self._seasonal_month = None
        self._seasonal_value = None
        self._momentum = None
        self._momentum_average = None
        self._cooldown = 0
        self._seasonal_history.clear()

    def OnStarted2(self, time):
        self._load_seasonality_history()
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

        self.StartProtection(Unit(0, UnitTypes.Absolute), Unit(self._stop_loss_percent.Value, UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Start()

    def _process_candle(self, candle, momentum_value):
        if candle.State != CandleStates.Finished:
            return

        mv = float(momentum_value)

        momentum_avg_val = float(process_float(self._momentum_average, Decimal(mv), candle.OpenTime, True))

        if not self._momentum.IsFormed or not self._momentum_average.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        seasonal_strength = self._get_seasonal_strength(candle.OpenTime)
        st = float(self._seasonality_threshold.Value)
        allow_long = seasonal_strength is not None and seasonal_strength >= st
        allow_short = seasonal_strength is not None and seasonal_strength <= -st
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

    def _load_seasonality_history(self):
        if self.Security is None:
            raise InvalidOperationException("Security is required for seasonal history")
        path = self._seasonality_history_file.Value
        if not path or not str(path).strip():
            raise InvalidOperationException("SeasonalityHistoryFile is required")
        self._seasonal_history = []
        self._seasonal_month = None
        self._seasonal_value = None
        months = set()
        with open(str(path), "r", encoding="utf-8-sig") as stream:
            if stream.readline().strip() != "SecurityId,Month,Open,Close":
                raise InvalidOperationException("Seasonality history must begin with SecurityId,Month,Open,Close")
            for line in stream:
                if not line.strip():
                    continue
                fields = line.strip().split(",")
                if len(fields) != 4:
                    raise InvalidOperationException("Every seasonal observation must contain four CSV fields")
                if fields[0].strip().lower() != self.Security.Id.lower():
                    continue
                month = DateTime.ParseExact(fields[1].strip(), "yyyy-MM", CultureInfo.InvariantCulture)
                opening = Decimal.Parse(fields[2], CultureInfo.InvariantCulture)
                closing = Decimal.Parse(fields[3], CultureInfo.InvariantCulture)
                if opening <= 0 or closing <= 0 or month in months:
                    raise InvalidOperationException("Seasonality prices must be positive and months must be unique")
                months.add(month)
                self._seasonal_history.append((month, Decimal.Multiply(Decimal(100), Decimal.Subtract(Decimal.Divide(closing, opening), Decimal.One))))
        if not self._seasonal_history:
            raise InvalidOperationException("Seasonality history contains no observations for the execution instrument")

    def _get_seasonal_strength(self, time):
        month = DateTime(time.Year, time.Month, 1)
        if self._seasonal_month != month:
            earliest = month.AddYears(-int(self._seasonality_lookback_years.Value))
            values = [value for observed, value in self._seasonal_history
                      if observed.Month == month.Month and earliest <= observed < month]
            minimum = int(self._minimum_seasonal_samples.Value)
            self._seasonal_value = None
            if len(values) >= minimum:
                total = Decimal.Zero
                for value in values:
                    total = Decimal.Add(total, value)
                self._seasonal_value = float(Decimal.Divide(total, Decimal(len(values))))
            self._seasonal_month = month
        return self._seasonal_value
    def CreateClone(self):
        return seasonality_adjusted_momentum_strategy()
