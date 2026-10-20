import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("Ecng.ComponentModel")

from System import Boolean, DateTime, Decimal, Int32, Math, TimeSpan, InvalidOperationException
from System.Collections.Generic import IEnumerable
from System.Globalization import CultureInfo, DateTimeStyles, StringInfo, CharUnicodeInfo, UnicodeCategory
from System.IO import File, Path
from Ecng.ComponentModel import WorkingTimeExtensions
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from StockSharp.Messages import CandleStates, DataType, OrderStates, OrderTypes, Sides


class lexical_density_filings_strategy(Strategy):
    """Lexical Density Filings with point-in-time inputs and execution-based portfolio accounting."""

    def __init__(self):
        super(lexical_density_filings_strategy, self).__init__()
        self._universe = self.Param[IEnumerable[Security]]("Universe", None).SetRequired() \
            .SetDisplay("Universe", "Explicit execution securities with valid native volume metadata", "Parameters")
        self._capital = self.Param("Capital", Decimal(100000)).SetGreaterThanZero() \
            .SetDisplay("Capital", "Fixed capital budget in USD", "Parameters")
        self._leverage = self.Param("Leverage", Decimal(1)).SetGreaterThanZero() \
            .SetDisplay("Leverage", "Gross target exposure divided by the capital budget", "Parameters")
        self._min_trade_usd = self.Param("MinTradeUsd", Decimal(200)).SetRange(Decimal(0), Decimal.MaxValue) \
            .SetDisplay("MinTradeUsd", "Minimum USD adjustment; complete liquidation bypasses this filter", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1440))) \
            .SetDisplay("CandleType", "Finished execution candle type", "Parameters")
        self._quintile = self.Param("Quintile", 5).SetRange(2, 2147483647) \
            .SetDisplay("Quintile", "Number of lexical-density buckets", "Parameters")
        self._max_report_age_days = self.Param("MaxReportAgeDays", 550).SetRange(1, 2147483647) \
            .SetDisplay("MaxReportAgeDays", "Maximum age of the latest filing period end in calendar days", "Parameters")
        self._filings_file = self.Param[str]("FilingsFile", None).SetRequired() \
            .SetDisplay("FilingsFile", "Required filing manifest containing publication dates and document paths", "Parameters")
        self._clear_state()

    def _clear_state(self):
        self._securities = []
        self._latest = {}
        self._targets = {}
        self._pending = []
        self._last_rebalance = 0
        self._last_bar = None
        self._filings = []

    @property
    def Universe(self):
        return self._universe.Value

    @Universe.setter
    def Universe(self, value):
        self._universe.Value = value

    @property
    def Capital(self):
        return self._capital.Value

    @Capital.setter
    def Capital(self, value):
        self._capital.Value = value

    @property
    def Leverage(self):
        return self._leverage.Value

    @Leverage.setter
    def Leverage(self, value):
        self._leverage.Value = value

    @property
    def MinTradeUsd(self):
        return self._min_trade_usd.Value

    @MinTradeUsd.setter
    def MinTradeUsd(self, value):
        self._min_trade_usd.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Quintile(self):
        return self._quintile.Value

    @Quintile.setter
    def Quintile(self, value):
        self._quintile.Value = value

    @property
    def MaxReportAgeDays(self):
        return self._max_report_age_days.Value

    @MaxReportAgeDays.setter
    def MaxReportAgeDays(self, value):
        self._max_report_age_days.Value = value

    @property
    def FilingsFile(self):
        return self._filings_file.Value

    @FilingsFile.setter
    def FilingsFile(self, value):
        self._filings_file.Value = value

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe] if self.Universe is not None else []

    def OnReseted(self):
        super(lexical_density_filings_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        self._securities = list(self.Universe) if self.Universe is not None else []
        if not self._securities or any(s is None or not s.Id for s in self._securities) or len(set(s.Id.upper() for s in self._securities)) != len(self._securities):
            raise InvalidOperationException("Universe must contain distinct execution securities.")
        for security in self._securities:
            if security.VolumeStep is None or security.MinVolume is None or security.MaxVolume is None or security.VolumeStep <= 0 or security.MinVolume <= 0 or security.MaxVolume <= 0:
                raise InvalidOperationException("Every security needs positive VolumeStep, MinVolume and MaxVolume.")
        if any(s.Board is None for s in self._securities):
            raise InvalidOperationException("A review calendar requires exchange boards.")
        self._load_filings()
        super(lexical_density_filings_strategy, self).OnStarted2(time)
        for security in self._securities:
            self.SubscribeCandles(self.CandleType, True, security).Bind(lambda candle, s=security: self._process_candle(s, candle)).Start()

    def _process_candle(self, security, candle):
        if candle.State != CandleStates.Finished or candle.ClosePrice <= 0:
            return
        self._latest[security] = (candle.OpenTime, candle.ClosePrice, candle.CloseTime)
        if len(self._latest) != len(self._securities) or any(value[0] != candle.OpenTime for value in self._latest.values()) or self._last_bar == candle.OpenTime or not self.IsFormedAndOnlineAndAllowTrading():
            return
        self._last_bar = candle.OpenTime
        month = candle.OpenTime.Year * 100 + candle.OpenTime.Month
        if self._last_rebalance != month and self._select_targets(candle.OpenTime, max(value[2] for value in self._latest.values())):
            self._last_rebalance = month
        self._reconcile_targets()

    def _budget(self):
        return self.Capital * self.Leverage

    def _unit_price(self, security):
        return self._latest[security][1]

    def _units(self, security, notional):
        return Math.Floor(notional / self._unit_price(security) / security.VolumeStep) * security.VolumeStep

    def _reconcile_targets(self):
        if any(o.State != OrderStates.Done and o.State != OrderStates.Failed for o in self._pending):
            return
        self._pending = []
        for security in self._securities:
            if security not in self._targets:
                continue
            target = self._targets[security]
            position = self.GetPositionValue(security, self.Portfolio)
            delta = target - (position if position is not None else Decimal(0))
            if delta == 0 or (target != 0 and abs(delta) * self._unit_price(security) < self.MinTradeUsd):
                continue
            quantity = Math.Floor(min(abs(delta), security.MaxVolume) / security.VolumeStep) * security.VolumeStep
            if quantity < security.MinVolume:
                continue
            order = Order()
            order.Security = security
            order.Portfolio = self.Portfolio
            order.Type = OrderTypes.Market
            order.Side = Sides.Buy if delta > 0 else Sides.Sell
            order.Volume = quantity
            order.Comment = "0376Rebalance"
            self._pending.append(order)
            self.RegisterOrder(order)

    @staticmethod
    def _parse_utc(value):
        return DateTime.ParseExact(value.strip(), "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)

    @staticmethod
    def _read_rows(path, header, fields):
        lines = list(File.ReadLines(path))
        if not lines or lines[0].strip() != header:
            raise InvalidOperationException("Invalid CSV header: expected " + header + ".")
        for line in lines[1:]:
            if not line.strip():
                continue
            row = line.split(",")
            if len(row) != fields:
                raise InvalidOperationException("Invalid CSV field count.")
            yield row

    def _load_filings(self):
        keys = set()
        for row in self._read_rows(self.FilingsFile, "SecurityId,PeriodEnd,PublishedAt,DocumentPath", 4):
            security = next((s for s in self._securities if s.Id.upper() == row[0].strip().upper()), None)
            if security is None:
                continue
            period = DateTime.ParseExact(row[1].strip(), "yyyy-MM-dd", CultureInfo.InvariantCulture)
            published = self._parse_utc(row[2])
            key = (security, period, published)
            if published.Date < period or key in keys:
                raise InvalidOperationException("Filing revisions need causal, unique publication dates.")
            keys.add(key)
            path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(self.FilingsFile)), row[3].strip()))
            words = list(self._words(File.ReadAllText(path)))
            if not words:
                raise InvalidOperationException("A filing must contain word tokens.")
            self._filings.append((security, period, published, Decimal(len(set(words))) / Decimal(len(words))))

    @staticmethod
    def _words(text):
        buffer = []
        elements = StringInfo.GetTextElementEnumerator(text)
        categories = (UnicodeCategory.UppercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.TitlecaseLetter,
                      UnicodeCategory.ModifierLetter, UnicodeCategory.OtherLetter, UnicodeCategory.NonSpacingMark,
                      UnicodeCategory.SpacingCombiningMark, UnicodeCategory.EnclosingMark)
        while elements.MoveNext():
            element = elements.GetTextElement()
            if CharUnicodeInfo.GetUnicodeCategory(element, 0) in categories:
                buffer.append(element)
            elif buffer:
                yield CultureInfo.InvariantCulture.TextInfo.ToLower("".join(buffer))
                buffer = []
        if buffer:
            yield CultureInfo.InvariantCulture.TextInfo.ToLower("".join(buffer))

    def _select_targets(self, time, as_of):
        if time.Month not in (2, 5, 8, 11):
            return False
        calendar = self._securities[0].Board.WorkingTime
        if not WorkingTimeExtensions.IsWorkingDate(calendar, time.Date, True):
            return False
        first = DateTime(time.Year, time.Month, 1)
        if sum(1 for i in range(time.Day) if WorkingTimeExtensions.IsWorkingDate(calendar, first.AddDays(i), True)) > 3:
            return False
        ranked = []
        for security in self._securities:
            available = [f for f in self._filings if f[0] == security and f[2] <= as_of]
            filing = max(available, key=lambda f: (f[1], f[2])) if available else None
            if filing is not None and (time.Date - filing[1]).TotalDays <= self.MaxReportAgeDays:
                ranked.append(filing)
        if len(ranked) < self.Quintile:
            return False
        ranked = [f[0] for f in sorted(ranked, key=lambda f: (-f[3], f[0].Id))]
        count = len(ranked) // self.Quintile
        self._set_basket_targets(ranked[:count], ranked[-count:], self._budget())
        return True

    def _set_basket_targets(self, longs, shorts, gross):
        self._targets = {s: Decimal(0) for s in self._securities}
        for security in longs:
            self._targets[security] = self._units(security, gross / Decimal(2 * len(longs)))
        for security in shorts:
            self._targets[security] = -self._units(security, gross / Decimal(2 * len(shorts)))

    def CreateClone(self):
        return lexical_density_filings_strategy()
