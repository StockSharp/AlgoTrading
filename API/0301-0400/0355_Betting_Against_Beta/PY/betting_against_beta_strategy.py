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


class betting_against_beta_strategy(Strategy):
    """Betting Against Beta with explicit inputs and execution-based portfolio accounting."""

    def __init__(self):
        super(betting_against_beta_strategy, self).__init__()
        self._universe = self.Param[IEnumerable[Security]]("Universe", None).SetRequired() \
            .SetDisplay("Universe", "Execution securities with USD prices and valid volume metadata", "Parameters")
        self._capital = self.Param("Capital", Decimal(100000)).SetGreaterThanZero() \
            .SetDisplay("Capital", "Fixed gross capital budget in USD", "Parameters")
        self._leverage = self.Param("Leverage", Decimal(1)).SetGreaterThanZero() \
            .SetDisplay("Leverage", "Gross target exposure divided by the capital budget", "Parameters")
        self._min_trade_usd = self.Param("MinTradeUsd", Decimal(100)).SetRange(Decimal(0), Decimal.MaxValue) \
            .SetDisplay("MinTradeUsd", "Minimum notional adjustment; complete liquidation bypasses this filter", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("CandleType", "Closed candle type; daily statistics count distinct UTC dates", "Parameters")
        self._deciles = self.Param("Deciles", 10).SetRange(2, 10000) \
            .SetDisplay("Deciles", "Number of cross-sectional buckets, requiring at least this many eligible securities", "Parameters")
        self._price_history_file = self.Param[str]("PriceHistoryFile", None) \
            .SetDisplay("PriceHistoryFile", "Optional daily close CSV for warmup; current and future dates cannot prefill a decision", "Parameters")
        self._benchmark_security = self.Param[Security]("BenchmarkSecurity", None).SetRequired() \
            .SetDisplay("BenchmarkSecurity", "Market benchmark for synchronized daily simple returns", "Parameters")
        self._window_days = self.Param("WindowDays", 252).SetGreaterThanZero() \
            .SetDisplay("WindowDays", "Daily return pairs required by each covariance estimate", "Parameters")
        self._clear_state()

    def _clear_state(self):
        self._securities = []
        self._latest = {}
        self._history = {}
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
    def Deciles(self):
        return self._deciles.Value

    @Deciles.setter
    def Deciles(self, value):
        self._deciles.Value = value

    @property
    def PriceHistoryFile(self):
        return self._price_history_file.Value

    @PriceHistoryFile.setter
    def PriceHistoryFile(self, value):
        self._price_history_file.Value = value

    @property
    def BenchmarkSecurity(self):
        return self._benchmark_security.Value

    @BenchmarkSecurity.setter
    def BenchmarkSecurity(self, value):
        self._benchmark_security.Value = value

    @property
    def WindowDays(self):
        return self._window_days.Value

    @WindowDays.setter
    def WindowDays(self, value):
        self._window_days.Value = value

    def GetWorkingSecurities(self):
        securities = list(self.Universe) if self.Universe is not None else []
        if self.BenchmarkSecurity is not None and self.BenchmarkSecurity not in securities:
            securities.append(self.BenchmarkSecurity)
        return [(s, self.CandleType) for s in securities]

    def OnReseted(self):
        super(betting_against_beta_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        self._securities = list(self.Universe) if self.Universe is not None else []
        if not self._securities or any(s is None or not s.Id for s in self._securities) or len(set(s.Id.upper() for s in self._securities)) != len(self._securities):
            raise InvalidOperationException("Universe must contain distinct execution securities.")
        if self.BenchmarkSecurity is None:
            raise InvalidOperationException("BenchmarkSecurity is required.")
        for s, dt in self.GetWorkingSecurities():
            if s.VolumeStep is None or s.MinVolume is None or s.MaxVolume is None or s.VolumeStep <= 0 or s.MinVolume <= 0 or s.MaxVolume <= 0:
                raise InvalidOperationException("Every security needs positive VolumeStep, MinVolume and MaxVolume.")
            self._history[s] = {}
        self._load_price_history()
        super(betting_against_beta_strategy, self).OnStarted2(time)
        for security, dt in self.GetWorkingSecurities():
            self.SubscribeCandles(dt, True, security).Bind(lambda candle, s=security: self._process_candle(s, candle)).Start()

    def _process_candle(self, security, candle):
        if candle.State != CandleStates.Finished or candle.ClosePrice <= 0:
            return
        self._latest[security] = (candle.OpenTime, candle.ClosePrice)
        self._history[security][candle.OpenTime.Date] = candle.ClosePrice
        if len(self._latest) != len(self._history) or any(t != candle.OpenTime for t, p in self._latest.values()) or self._last_bar == candle.OpenTime or not self.IsFormedAndOnlineAndAllowTrading():
            return
        self._last_bar = candle.OpenTime
        month = candle.OpenTime.Year * 100 + candle.OpenTime.Month

        if self._last_rebalance != month and self._select_targets(candle.OpenTime):
            self._last_rebalance = month

        self._reconcile_targets()

    def _daily(self, security, time):
        return [self._history[security][d] for d in sorted(self._history[security]) if d <= self._last_closed_date(time)]

    def _last_closed_date(self, time):
        return time.Date if self.CandleType.Arg == TimeSpan.FromDays(1) else time.Date.AddDays(-1)

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
            order.Comment = "0355Rebalance"
            self._pending.append(order)
            self.RegisterOrder(order)

    def _load_price_history(self):
        if not self.PriceHistoryFile:
            return
        lines = iter(File.ReadLines(self.PriceHistoryFile))
        if next(lines, "").strip() != "SecurityId,Date,Close":
            raise InvalidOperationException("Price history must begin with SecurityId,Date,Close.")
        for line in lines:
            if not line.strip():
                continue
            row = line.split(",")
            if len(row) != 3:
                raise InvalidOperationException("A daily price row must have three fields.")
            security = next((s for s in self._history if s.Id.lower() == row[0].strip().lower()), None)
            if security is None:
                continue
            date = DateTime.ParseExact(row[1].strip(), "yyyy-MM-dd", CultureInfo.InvariantCulture)
            price = Decimal.Parse(row[2], CultureInfo.InvariantCulture)
            if price <= 0 or date in self._history[security]:
                raise InvalidOperationException("Daily prices must be positive and unique per security and UTC date.")
            self._history[security][date] = price

    def _select_targets(self, time):
        streams = list(self._securities)
        if self.BenchmarkSecurity not in streams:
            streams.append(self.BenchmarkSecurity)
        dates = set(d for d in self._history[streams[0]] if d <= self._last_closed_date(time))
        for s in streams[1:]:
            dates.intersection_update(d for d in self._history[s] if d <= self._last_closed_date(time))
        dates = sorted(dates)[-self.WindowDays - 1:]
        if len(dates) < self.WindowDays + 1:
            return False
        def returns(s):
            return [self._history[s][dates[i + 1]] / self._history[s][dates[i]] - Decimal(1)
                    for i in range(self.WindowDays)]
        benchmark = returns(self.BenchmarkSecurity)
        mean = sum(benchmark, Decimal(0)) / Decimal(len(benchmark))
        variance = sum(((r - mean) * (r - mean) for r in benchmark), Decimal(0))
        if variance <= 0:
            return False
        scores = []
        for s in self._securities:
            values = returns(s)
            average = sum(values, Decimal(0)) / Decimal(len(values))
            beta = sum(((r - average) * (b - mean) for r, b in zip(values, benchmark)), Decimal(0)) / variance
            scores.append((s, beta))
        ranked = [s for s, beta in sorted(scores, key=lambda x: (x[1], x[0].Id))]
        if len(ranked) < self.Deciles:
            return False
        count = len(ranked) // self.Deciles
        self._set_basket_targets(ranked[:count], ranked[-count:], self.Capital * self.Leverage)
        return True

    def _set_basket_targets(self, longs, shorts, gross_capital):
        self._targets = dict((s, Decimal(0)) for s in self._securities)
        for s in longs:
            self._targets[s] = self._units(s, gross_capital / Decimal(2 * len(longs)))
        for s in shorts:
            self._targets[s] = -self._units(s, gross_capital / Decimal(2 * len(shorts)))

    def CreateClone(self):
        return betting_against_beta_strategy()
