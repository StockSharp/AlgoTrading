import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Boolean, DateTime, Decimal, Int32, Math, TimeSpan, InvalidOperationException
from System.Collections.Generic import IEnumerable
from System.Globalization import CultureInfo, DateTimeStyles
from System.IO import File, Path
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from StockSharp.Messages import CandleStates, DataType, OrderStates, OrderTypes, Sides


class momentum_asset_growth_strategy(Strategy):
    """Momentum Asset Growth with point-in-time inputs and execution-based portfolio accounting."""

    def __init__(self):
        super(momentum_asset_growth_strategy, self).__init__()
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
        self._price_history_file = self.Param[str]("PriceHistoryFile", None) \
            .SetDisplay("PriceHistoryFile", "Optional daily close CSV; future dates cannot prefill a decision", "Parameters")
        self._mom_look = self.Param("MomLook", 252).SetRange(1, 2147483647) \
            .SetDisplay("MomLook", "Completed daily returns ending before the skipped calendar months", "Parameters")
        self._skip_months = self.Param("SkipMonths", 1).SetRange(0, 24) \
            .SetDisplay("SkipMonths", "Recent whole calendar months excluded from momentum", "Parameters")
        self._asset_decile = self.Param("AssetDecile", 10).SetRange(1, 2147483647) \
            .SetDisplay("AssetDecile", "Number of asset-growth buckets; only the highest is eligible", "Parameters")
        self._quintile = self.Param("Quintile", 5).SetRange(2, 2147483647) \
            .SetDisplay("Quintile", "Number of momentum buckets within the growth subset", "Parameters")
        self._max_report_age_days = self.Param("MaxReportAgeDays", 550).SetRange(1, 2147483647) \
            .SetDisplay("MaxReportAgeDays", "Maximum age of the latest fiscal year end in calendar days", "Parameters")
        self._fundamentals_file = self.Param[str]("FundamentalsFile", None).SetRequired() \
            .SetDisplay("FundamentalsFile", "Required point-in-time annual financial reports", "Parameters")
        self._clear_state()

    def _clear_state(self):
        self._securities = []
        self._latest = {}
        self._history = {}
        self._targets = {}
        self._pending = []
        self._last_rebalance = 0
        self._last_bar = None
        self._reports = []


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
    def PriceHistoryFile(self):
        return self._price_history_file.Value

    @PriceHistoryFile.setter
    def PriceHistoryFile(self, value):
        self._price_history_file.Value = value

    @property
    def MomLook(self):
        return self._mom_look.Value

    @MomLook.setter
    def MomLook(self, value):
        self._mom_look.Value = value

    @property
    def SkipMonths(self):
        return self._skip_months.Value

    @SkipMonths.setter
    def SkipMonths(self, value):
        self._skip_months.Value = value

    @property
    def AssetDecile(self):
        return self._asset_decile.Value

    @AssetDecile.setter
    def AssetDecile(self, value):
        self._asset_decile.Value = value

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
    def FundamentalsFile(self):
        return self._fundamentals_file.Value

    @FundamentalsFile.setter
    def FundamentalsFile(self, value):
        self._fundamentals_file.Value = value

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe] if self.Universe is not None else []

    def OnReseted(self):
        super(momentum_asset_growth_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        self._securities = list(self.Universe) if self.Universe is not None else []
        if not self._securities or any(s is None or not s.Id for s in self._securities) or len(set(s.Id.upper() for s in self._securities)) != len(self._securities):
            raise InvalidOperationException("Universe must contain distinct execution securities.")
        for security in self._securities:
            if security.VolumeStep is None or security.MinVolume is None or security.MaxVolume is None or security.VolumeStep <= 0 or security.MinVolume <= 0 or security.MaxVolume <= 0:
                raise InvalidOperationException("Every security needs positive VolumeStep, MinVolume and MaxVolume.")
            self._history[security] = {}
        self._load_price_history()
        self._load_reports()

        super(momentum_asset_growth_strategy, self).OnStarted2(time)
        for security in self._securities:
            self.SubscribeCandles(self.CandleType, True, security).Bind(lambda candle, s=security: self._process_candle(s, candle)).Start()

    def _process_candle(self, security, candle):
        if candle.State != CandleStates.Finished or candle.ClosePrice <= 0:
            return
        self._latest[security] = (candle.OpenTime, candle.ClosePrice, candle.CloseTime)
        self._history[security][candle.OpenTime.Date] = candle.ClosePrice
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
            order.Comment = "0378Rebalance"
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

    def _last_closed_date(self, time):
        return time.Date if self.CandleType.Arg == TimeSpan.FromDays(1) else time.Date.AddDays(-1)

    def _daily(self, security, time, count, cutoff):
        dates = [d for d in sorted(self._history[security]) if d <= self._last_closed_date(time) and d < cutoff]
        return [self._history[security][d] for d in dates[-count:]]

    def _load_price_history(self):
        if not self.PriceHistoryFile:
            return
        for row in self._read_rows(self.PriceHistoryFile, "SecurityId,Date,Close", 3):
            security = next((s for s in self._securities if s.Id.upper() == row[0].strip().upper()), None)
            if security is None:
                continue
            date = DateTime.ParseExact(row[1].strip(), "yyyy-MM-dd", CultureInfo.InvariantCulture)
            price = Decimal.Parse(row[2], CultureInfo.InvariantCulture)
            if price <= 0 or date in self._history[security]:
                raise InvalidOperationException("Daily prices must be positive and unique per security and UTC date.")
            self._history[security][date] = price

    def _load_reports(self):
        keys = set()
        for row in self._read_rows(self.FundamentalsFile, "SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding", 8):
            identifier = row[0].strip().upper()
            if not any(s.Id.upper() == identifier for s in self._securities):
                continue
            period = DateTime.ParseExact(row[1].strip(), "yyyy-MM-dd", CultureInfo.InvariantCulture)
            published = self._parse_utc(row[2])
            values = [Decimal.Parse(x, CultureInfo.InvariantCulture) for x in row[3:]]
            key = (identifier, period, published)
            if values[0] <= 0 or values[4] <= 0 or published.Date < period or key in keys:
                raise InvalidOperationException("Reports need positive assets/shares, causal publication dates and unique revisions.")
            keys.add(key)
            self._reports.append((identifier, period, published, values[0], values[1], values[2]))

    def _find_report(self, security, as_of, period=None):
        eligible = [r for r in self._reports if r[0] == security.Id.upper() and r[2] <= as_of and r[1] <= as_of.Date and (period is None or r[1] == period)]
        return max(eligible, key=lambda r: (r[1], r[2])) if eligible else None

    def _select_targets(self, time, as_of):
        if time.Month == 1:
            return False
        growth = []
        for security in self._securities:
            latest = self._find_report(security, as_of)
            if latest is None or (time.Date - latest[1]).TotalDays > self.MaxReportAgeDays:
                continue
            previous = self._find_report(security, as_of, latest[1].AddYears(-1))
            if previous is not None:
                growth.append((security, latest[3] / previous[3] - Decimal(1)))
        if len(growth) < self.AssetDecile:
            return False
        eligible = sorted(growth, key=lambda x: (-x[1], x[0].Id))[:len(growth) // self.AssetDecile]
        cutoff = DateTime(time.Year, time.Month, 1).AddMonths(-self.SkipMonths)
        momentum = []
        for security, score in eligible:
            prices = self._daily(security, time, self.MomLook + 1, cutoff)
            if len(prices) == self.MomLook + 1:
                momentum.append((security, prices[-1] / prices[0] - Decimal(1)))
        if len(momentum) < self.Quintile:
            return False
        ranked = [s for s, score in sorted(momentum, key=lambda x: (-x[1], x[0].Id))]
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
        return momentum_asset_growth_strategy()
