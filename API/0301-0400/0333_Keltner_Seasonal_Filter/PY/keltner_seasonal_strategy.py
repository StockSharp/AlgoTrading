import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTime, Decimal, InvalidOperationException
from System.Globalization import CultureInfo
from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class keltner_seasonal_strategy(Strategy):
    """
    Strategy that trades based on Keltner Channel breakouts with seasonal bias filter.
    """

    def __init__(self):
        super(keltner_seasonal_strategy, self).__init__()
        self._seasonality_history_file = self.Param[str]("SeasonalityHistoryFile", None).SetRequired()
        self._minimum_seasonal_samples = self.Param("MinimumSeasonalSamples", 2).SetGreaterThanZero()
        self._seasonality_lookback_years = self.Param("SeasonalityLookbackYears", 10).SetRange(1, 100)
        self._seasonal_history = []
        self._seasonal_month = None
        self._seasonal_value = None

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period for EMA in Keltner Channel", "Indicator Settings")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR in Keltner Channel", "Indicator Settings")

        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to set channel width", "Indicator Settings")

        self._seasonal_threshold = self.Param("SeasonalThreshold", 0.5) \
            .SetDisplay("Seasonal Threshold", "Minimum seasonal strength to consider for trading", "Seasonal Settings")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._seasonal_history = []
        self._current_seasonal_strength = 0.0
        self._stop_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(keltner_seasonal_strategy, self).OnReseted()
        self._seasonal_history = []
        self._seasonal_month = None
        self._seasonal_value = None
        self._current_seasonal_strength = 0.0
        self._stop_price = 0.0

    def OnStarted2(self, time):
        self._load_seasonality_history()
        super(keltner_seasonal_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = int(self._ema_period.Value)

        atr = AverageTrueRange()
        atr.Length = int(self._atr_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self._process_keltner).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    @staticmethod
    def _get_entry_signal(seasonal_strength, close_price, ema, atr, multiplier, threshold, position):
        """Original directional Keltner breakout; +1 buy, -1 sell, 0 no entry."""
        if seasonal_strength > threshold and close_price > ema + atr * multiplier and position <= 0:
            return 1
        if seasonal_strength < -threshold and close_price < ema - atr * multiplier and position >= 0:
            return -1
        return 0

    def _process_keltner(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Seasonality belongs to the candle being evaluated, never to host/start time.
        self._update_seasonal_strength(candle.OpenTime)

        multiplier = float(self._atr_multiplier.Value)
        threshold = float(self._seasonal_threshold.Value)
        ema_val = float(ema_value)
        atr_val = float(atr_value)

        upper_band = ema_val + atr_val * multiplier
        lower_band = ema_val - atr_val * multiplier
        close_price = float(candle.ClosePrice)

        # Stop placed AtrMultiplier x ATR from the entry close, checked on the finished candle
        if self._stop_price > 0 and \
           ((self.Position > 0 and close_price <= self._stop_price) or
            (self.Position < 0 and close_price >= self._stop_price)):
            self._close_position()
            return

        signal = self._get_entry_signal(
            self._current_seasonal_strength, close_price, ema_val, atr_val,
            multiplier, threshold, self.Position
        )

        if not self._is_seasonal_ready:
            signal = 0
        if signal > 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self._stop_price = close_price - atr_val * multiplier
            return
        if signal < 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._stop_price = close_price + atr_val * multiplier
            return

        if (self.Position > 0 and close_price < ema_val) or \
           (self.Position < 0 and close_price > ema_val):
            self._close_position()

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
        self._stop_price = 0.0

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
    def _update_seasonal_strength(self, time):
        value = self._get_seasonal_strength(time)
        self._is_seasonal_ready = value is not None
        self._current_seasonal_strength = value if value is not None else 0.0
    def CreateClone(self):
        return keltner_seasonal_strategy()
