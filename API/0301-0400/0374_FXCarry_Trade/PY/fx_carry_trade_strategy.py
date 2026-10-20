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


class fx_carry_trade_strategy(Strategy):
    """FX Carry Trade with point-in-time inputs and execution-based portfolio accounting."""

    def __init__(self):
        super(fx_carry_trade_strategy, self).__init__()
        self._universe = self.Param[IEnumerable[Security]]("Universe", None).SetRequired() \
            .SetDisplay("Universe", "Explicit execution securities with valid native volume metadata", "Parameters")
        self._capital = self.Param("Capital", Decimal(100000)).SetGreaterThanZero() \
            .SetDisplay("Capital", "Fixed capital budget in USD", "Parameters")
        self._leverage = self.Param("Leverage", Decimal(1)).SetGreaterThanZero() \
            .SetDisplay("Leverage", "Gross target exposure divided by the capital budget", "Parameters")
        self._min_trade_usd = self.Param("MinTradeUsd", Decimal(100)).SetRange(Decimal(0), Decimal.MaxValue) \
            .SetDisplay("MinTradeUsd", "Minimum USD adjustment; complete liquidation bypasses this filter", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1440))) \
            .SetDisplay("CandleType", "Finished execution candle type", "Parameters")
        self._top_k = self.Param("TopK", 3).SetRange(1, 2147483647) \
            .SetDisplay("TopK", "Currency pairs in each carry basket", "Parameters")
        self._rates_file = self.Param[str]("RatesFile", None).SetRequired() \
            .SetDisplay("RatesFile", "Required point-in-time base and quote interest rates and USD unit values", "Parameters")
        self._max_rate_age_days = self.Param("MaxRateAgeDays", 45).SetRange(1, 2147483647) \
            .SetDisplay("MaxRateAgeDays", "Maximum age of the published rate snapshot in calendar days", "Parameters")
        self._clear_state()

    def _clear_state(self):
        self._securities = []
        self._latest = {}
        self._targets = {}
        self._pending = []
        self._last_rebalance = 0
        self._last_bar = None
        self._rates = []
        self._usd_prices = {}

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
    def TopK(self):
        return self._top_k.Value

    @TopK.setter
    def TopK(self, value):
        self._top_k.Value = value

    @property
    def RatesFile(self):
        return self._rates_file.Value

    @RatesFile.setter
    def RatesFile(self, value):
        self._rates_file.Value = value

    @property
    def MaxRateAgeDays(self):
        return self._max_rate_age_days.Value

    @MaxRateAgeDays.setter
    def MaxRateAgeDays(self, value):
        self._max_rate_age_days.Value = value

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe] if self.Universe is not None else []

    def OnReseted(self):
        super(fx_carry_trade_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        self._securities = list(self.Universe) if self.Universe is not None else []
        if not self._securities or any(s is None or not s.Id for s in self._securities) or len(set(s.Id.upper() for s in self._securities)) != len(self._securities):
            raise InvalidOperationException("Universe must contain distinct execution securities.")
        for security in self._securities:
            if security.VolumeStep is None or security.MinVolume is None or security.MaxVolume is None or security.VolumeStep <= 0 or security.MinVolume <= 0 or security.MaxVolume <= 0:
                raise InvalidOperationException("Every security needs positive VolumeStep, MinVolume and MaxVolume.")
        self._load_rates()
        super(fx_carry_trade_strategy, self).OnStarted2(time)
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
        return self._usd_prices[security]

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
            order.Comment = "0374Rebalance"
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

    def _load_rates(self):
        keys = set()
        for row in self._read_rows(self.RatesFile, "SecurityId,PublishedAt,BaseRate,QuoteRate,UsdPerUnit", 5):
            security = next((s for s in self._securities if s.Id.upper() == row[0].strip().upper()), None)
            if security is None:
                continue
            published = self._parse_utc(row[1])
            base_rate, quote_rate, usd = [Decimal.Parse(x, CultureInfo.InvariantCulture) for x in row[2:5]]
            if usd <= 0 or (security, published) in keys:
                raise InvalidOperationException("Rate snapshots need positive USD unit values and unique publication timestamps.")
            keys.add((security, published))
            self._rates.append((security, published, base_rate, quote_rate, usd))

    def _select_targets(self, time, as_of):
        ranked = []
        for security in self._securities:
            available = [r for r in self._rates if r[0] == security and r[1] <= as_of]
            rate = max(available, key=lambda r: r[1]) if available else None
            if rate is not None and (as_of - rate[1]).TotalDays <= self.MaxRateAgeDays:
                ranked.append(rate)
        ranked.sort(key=lambda r: (r[2] - r[3], r[0].Id))
        if len(ranked) < 2 * self.TopK:
            return False
        for rate in ranked:
            self._usd_prices[rate[0]] = rate[4]
        self._targets = {s: Decimal(0) for s in self._securities}
        self._set_basket_targets([r[0] for r in ranked[-self.TopK:]], [r[0] for r in ranked[:self.TopK]], self._budget())
        return True

    def _set_basket_targets(self, longs, shorts, gross):
        self._targets = {s: Decimal(0) for s in self._securities}
        for security in longs:
            self._targets[security] = self._units(security, gross / Decimal(2 * len(longs)))
        for security in shorts:
            self._targets[security] = -self._units(security, gross / Decimal(2 * len(shorts)))

    def CreateClone(self):
        return fx_carry_trade_strategy()
