import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTime, Decimal, Math, TimeSpan, InvalidOperationException
from System.Collections.Generic import IEnumerable
from System.Globalization import CultureInfo, DateTimeStyles
from System.IO import File
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from StockSharp.Messages import CandleStates, DataType, OrderStates, OrderTypes, Sides


class book_to_market_value_strategy(Strategy):
    """Book To Market Value with explicit inputs and execution-based portfolio accounting."""

    def __init__(self):
        super(book_to_market_value_strategy, self).__init__()
        self._universe = self.Param[IEnumerable[Security]]("Universe", None).SetRequired() \
            .SetDisplay("Universe", "Execution securities with USD prices and valid volume metadata", "Parameters")
        self._capital = self.Param("Capital", Decimal(100000)).SetGreaterThanZero() \
            .SetDisplay("Capital", "Fixed gross capital budget in USD", "Parameters")
        self._leverage = self.Param("Leverage", Decimal(1)).SetGreaterThanZero() \
            .SetDisplay("Leverage", "Gross target exposure divided by the capital budget", "Parameters")
        self._min_trade_usd = self.Param("MinTradeUsd", Decimal(200)).SetRange(Decimal(0), Decimal.MaxValue) \
            .SetDisplay("MinTradeUsd", "Minimum notional adjustment; complete liquidation bypasses this filter", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("CandleType", "Closed candle type; daily statistics count distinct UTC dates", "Parameters")
        self._deciles = self.Param("Deciles", 10).SetRange(2, 10000) \
            .SetDisplay("Deciles", "Number of cross-sectional buckets, requiring at least this many eligible securities", "Parameters")
        self._fundamentals_file = self.Param[str]("FundamentalsFile", None).SetRequired() \
            .SetDisplay("FundamentalsFile", "Point-in-time annual financial reports in CSV format", "Parameters")
        self._rebalance_month = self.Param("RebalanceMonth", 7).SetRange(1, 12) \
            .SetDisplay("RebalanceMonth", "UTC calendar month of the annual portfolio rebalance", "Parameters")
        self._max_report_age_days = self.Param("MaxReportAgeDays", 550).SetGreaterThanZero() \
            .SetDisplay("MaxReportAgeDays", "Maximum age of the fiscal period at a rebalance", "Parameters")
        self._clear_state()

    def _clear_state(self):
        self._securities = []
        self._latest = {}
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
    def Deciles(self):
        return self._deciles.Value

    @Deciles.setter
    def Deciles(self, value):
        self._deciles.Value = value

    @property
    def FundamentalsFile(self):
        return self._fundamentals_file.Value

    @FundamentalsFile.setter
    def FundamentalsFile(self, value):
        self._fundamentals_file.Value = value

    @property
    def RebalanceMonth(self):
        return self._rebalance_month.Value

    @RebalanceMonth.setter
    def RebalanceMonth(self, value):
        self._rebalance_month.Value = value

    @property
    def MaxReportAgeDays(self):
        return self._max_report_age_days.Value

    @MaxReportAgeDays.setter
    def MaxReportAgeDays(self, value):
        self._max_report_age_days.Value = value

    def GetWorkingSecurities(self):
        securities = list(self.Universe) if self.Universe is not None else []

        return [(s, self.CandleType) for s in securities]

    def OnReseted(self):
        super(book_to_market_value_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        self._securities = list(self.Universe) if self.Universe is not None else []
        if not self._securities or any(s is None or not s.Id for s in self._securities) or len(set(s.Id.upper() for s in self._securities)) != len(self._securities):
            raise InvalidOperationException("Universe must contain distinct execution securities.")

        for s, dt in self.GetWorkingSecurities():
            if s.VolumeStep is None or s.MinVolume is None or s.MaxVolume is None or s.VolumeStep <= 0 or s.MinVolume <= 0 or s.MaxVolume <= 0:
                raise InvalidOperationException("Every security needs positive VolumeStep, MinVolume and MaxVolume.")
        if not self.FundamentalsFile:
            raise InvalidOperationException("FundamentalsFile is required.")
        self._load_reports()
        super(book_to_market_value_strategy, self).OnStarted2(time)
        for security, dt in self.GetWorkingSecurities():
            self.SubscribeCandles(dt, True, security).Bind(lambda candle, s=security: self._process_candle(s, candle)).Start()

    def _process_candle(self, security, candle):
        if candle.State != CandleStates.Finished or candle.ClosePrice <= 0:
            return
        self._latest[security] = (candle.OpenTime, candle.ClosePrice)
        if len(self._latest) != len(self._securities) or any(t != candle.OpenTime for t, p in self._latest.values()) or self._last_bar == candle.OpenTime or not self.IsFormedAndOnlineAndAllowTrading():
            return
        self._last_bar = candle.OpenTime
        month = candle.OpenTime.Year * 100 + candle.OpenTime.Month

        if self._last_rebalance // 100 != candle.OpenTime.Year and self._select_targets(candle.OpenTime):
            self._last_rebalance = month

        self._reconcile_targets()

    def _units(self, security, notional):
        return Math.Floor(notional / self._latest[security][1] / security.VolumeStep) * security.VolumeStep

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
            if delta == 0 or (target != 0 and abs(delta) * self._latest[security][1] < self.MinTradeUsd):
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
            order.Comment = "0357Rebalance"
            self._pending.append(order)
            self.RegisterOrder(order)

    def _load_reports(self):
        lines = iter(File.ReadLines(self.FundamentalsFile))
        if next(lines, "").strip() != "SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding":
            raise InvalidOperationException("Invalid annual financial report CSV header.")
        keys = set()
        for line in lines:
            if not line.strip():
                continue
            row = line.split(",")
            if len(row) != 8:
                raise InvalidOperationException("A financial report must have eight fields.")
            security_id = row[0].strip()
            if not any(s.Id.lower() == security_id.lower() for s in self._securities):
                continue
            period = DateTime.ParseExact(row[1].strip(), "yyyy-MM-dd", CultureInfo.InvariantCulture)
            published = DateTime.ParseExact(row[2].strip(), "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture,
                                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
            values = [Decimal.Parse(v, CultureInfo.InvariantCulture) for v in row[3:]]
            key = (security_id.upper(), period, published)
            if values[0] <= 0 or values[4] <= 0 or published.Date < period or key in keys:
                raise InvalidOperationException("Reports need positive assets/shares, causal publication dates and unique revisions.")
            keys.add(key)
            self._reports.append((security_id, period, published) + tuple(values))

    def _find_report(self, security, time, period=None):
        eligible = [r for r in self._reports if r[0].lower() == security.Id.lower()
                    and r[2] <= time and r[1] <= time.Date and (period is None or r[1] == period)]
        return max(eligible, key=lambda r: (r[1], r[2])) if eligible else None

    def _select_targets(self, time):
        if time.Month != self.RebalanceMonth:
            return False
        scores = []
        for security in self._securities:
            report = self._find_report(security, time)
            if report is None or (time.Date - report[1]).TotalDays > self.MaxReportAgeDays:
                continue
            if report[6] <= 0:
                continue
            score = report[6] / (report[7] * self._latest[security][1])
            scores.append((security, score))
        if len(scores) < self.Deciles:
            return False
        ranked = [s for s, score in sorted(scores, key=lambda x: (x[1], x[0].Id))]
        count = len(ranked) // self.Deciles
        self._set_basket_targets(ranked[-count:], ranked[:count], self.Capital * self.Leverage)
        return True

    def _set_basket_targets(self, longs, shorts, gross_capital):
        self._targets = dict((s, Decimal(0)) for s in self._securities)
        for s in longs:
            self._targets[s] = self._units(s, gross_capital / Decimal(2 * len(longs)))
        for s in shorts:
            self._targets[s] = -self._units(s, gross_capital / Decimal(2 * len(shorts)))

    def CreateClone(self):
        return book_to_market_value_strategy()
