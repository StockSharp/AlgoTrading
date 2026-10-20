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


class roa_effect_stocks_strategy(Strategy):
    """ROAEffect Stocks with point-in-time inputs and execution-based portfolio accounting."""

    def __init__(self):
        super(roa_effect_stocks_strategy, self).__init__()
        self._universe = self.Param[IEnumerable[Security]]("Universe", None).SetRequired() \
            .SetDisplay("Universe", "Explicit execution universe with native volume metadata.", "Parameters")
        self._capital = self.Param("Capital", Decimal(100000)).SetRange(Decimal(0.000001), Decimal.MaxValue) \
            .SetDisplay("Capital", "Fixed USD capital budget.", "Parameters")
        self._leverage = self.Param("Leverage", Decimal(1)).SetRange(Decimal(0.000001), Decimal.MaxValue) \
            .SetDisplay("Leverage", "Gross exposure divided by capital.", "Parameters")
        self._min_trade_usd = self.Param("MinTradeUsd", Decimal(200)).SetRange(Decimal(0), Decimal.MaxValue) \
            .SetDisplay("MinTradeUsd", "Minimum USD adjustment; complete liquidation is exempt.", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("CandleType", "Finished execution candle type.", "Parameters")
        self._roa_file = self.Param[str]("RoaFile", None).SetRequired() \
            .SetDisplay("RoaFile", "Required CSV of externally published scores.", "Parameters")
        self._decile = self.Param("Decile", 10).SetRange(2, 2147483647) \
            .SetDisplay("Decile", "Number of equal monthly rank buckets.", "Parameters")
        self._max_score_age_days = self.Param("MaxScoreAgeDays", 400).SetRange(1, 2147483647) \
            .SetDisplay("MaxScoreAgeDays", "Maximum age of a published score's period end.", "Parameters")
        self._clear_state()

    def _clear_state(self):
        self._securities = []
        self._latest = {}
        self._scores = []
        self._targets = {}
        self._pending = []
        self._last_rebalance = 0
        self._last_bar = None


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
    def RoaFile(self):
        return self._roa_file.Value

    @RoaFile.setter
    def RoaFile(self, value):
        self._roa_file.Value = value

    @property
    def Decile(self):
        return self._decile.Value

    @Decile.setter
    def Decile(self, value):
        self._decile.Value = value

    @property
    def MaxScoreAgeDays(self):
        return self._max_score_age_days.Value

    @MaxScoreAgeDays.setter
    def MaxScoreAgeDays(self, value):
        self._max_score_age_days.Value = value

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe] if self.Universe is not None else []

    def OnReseted(self):
        super(roa_effect_stocks_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        self._securities = list(self.Universe) if self.Universe is not None else []
        if not self._securities or any(s is None or not s.Id for s in self._securities) or len(set(s.Id.upper() for s in self._securities)) != len(self._securities):
            raise InvalidOperationException("Universe must contain distinct execution securities.")
        for security in self._securities:
            if security.VolumeStep is None or security.MinVolume is None or security.MaxVolume is None or security.VolumeStep <= 0 or security.MinVolume <= 0 or security.MaxVolume <= 0:
                raise InvalidOperationException("Every security needs positive VolumeStep, MinVolume and MaxVolume.")
        self._load_scores()
        super(roa_effect_stocks_strategy, self).OnStarted2(time)
        for security in self._securities:
            self.SubscribeCandles(self.CandleType, True, security).Bind(lambda candle, s=security: self._process_candle(s, candle)).Start()

    def _process_candle(self, security, candle):
        if candle.State != CandleStates.Finished or candle.ClosePrice <= 0:
            return
        self._latest[security] = (candle.OpenTime, candle.ClosePrice, candle.CloseTime)
        if len(self._latest) != len(self._securities) or any(v[0] != candle.OpenTime for v in self._latest.values()) or self._last_bar == candle.OpenTime or not self.IsFormedAndOnlineAndAllowTrading():
            return
        self._last_bar = candle.OpenTime
        month = candle.OpenTime.Year * 100 + candle.OpenTime.Month
        if self._last_rebalance != month and self._select_targets(candle.OpenTime, max(v[2] for v in self._latest.values())):
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
            order.Comment = "0393Rebalance"
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

    def _load_scores(self):
        keys = set()
        for row in self._read_rows(self.RoaFile, "SecurityId,PeriodEnd,PublishedAt,Score", 4):
            security = next((s for s in self._securities if s.Id.upper() == row[0].strip().upper()), None)
            if security is None:
                continue
            period = DateTime.ParseExact(row[1].strip(), "yyyy-MM-dd", CultureInfo.InvariantCulture)
            published = self._parse_utc(row[2])
            value = Decimal.Parse(row[3], CultureInfo.InvariantCulture)
            if published.Date < period or (security, period, published) in keys:
                raise InvalidOperationException("Scores need causal, unique publication timestamps.")
            keys.add((security, period, published))
            self._scores.append((security, period, published, value))

    def _select_targets(self, time, as_of):
        scores = []
        for security in self._securities:
            reports = [r for r in self._scores if r[0] == security and r[2] <= as_of and r[1] <= time.Date]
            if not reports:
                continue
            report = max(reports, key=lambda r: (r[1], r[2]))
            if (time.Date - report[1]).TotalDays <= self.MaxScoreAgeDays:
                scores.append((security, report[3]))
        if len(scores) < self.Decile:
            return False
        ranked = [s for s, score in sorted(scores, key=lambda x: (-x[1], x[0].Id))]
        count = len(ranked) // self.Decile
        self._set_basket_targets(ranked[:count], ranked[-count:], self._budget())
        return True

    def _set_basket_targets(self, longs, shorts, gross):
        self._targets = {s: Decimal(0) for s in self._securities}
        for security in longs:
            self._targets[security] = self._units(security, gross / Decimal(2 * len(longs)))
        for security in shorts:
            self._targets[security] = -self._units(security, gross / Decimal(2 * len(shorts)))

    def CreateClone(self):
        return roa_effect_stocks_strategy()