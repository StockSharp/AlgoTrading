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


class esg_factor_momentum_strategy(Strategy):
    """ESG Factor Momentum with point-in-time inputs and execution-based portfolio accounting."""

    def __init__(self):
        super(esg_factor_momentum_strategy, self).__init__()
        self._universe = self.Param[IEnumerable[Security]]("Universe", None).SetRequired() \
            .SetDisplay("Universe", "Explicit execution securities with valid native volume metadata", "Parameters")
        self._capital = self.Param("Capital", Decimal(100000)).SetGreaterThanZero() \
            .SetDisplay("Capital", "Fixed capital budget in USD", "Parameters")
        self._min_trade_usd = self.Param("MinTradeUsd", Decimal(100)).SetRange(Decimal(0), Decimal.MaxValue) \
            .SetDisplay("MinTradeUsd", "Minimum USD adjustment; complete liquidation bypasses this filter", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1440))) \
            .SetDisplay("CandleType", "Finished execution candle type", "Parameters")
        self._price_history_file = self.Param[str]("PriceHistoryFile", None) \
            .SetDisplay("PriceHistoryFile", "Optional daily close CSV; future dates cannot prefill a decision", "Parameters")
        self._lookback_days = self.Param("LookbackDays", 252).SetRange(1, 2147483647) \
            .SetDisplay("LookbackDays", "Completed daily returns in the trailing momentum window", "Parameters")
        self._clear_state()
        self.Trades.TradeAdded += self._on_trade

    def _clear_state(self):
        self._securities = []
        self._latest = {}
        self._history = {}
        self._targets = {}
        self._pending = []
        self._last_rebalance = 0
        self._last_bar = None
        self._next_targets = {}
        self._is_rotating = False
        self._is_reconciling = False


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
    def LookbackDays(self):
        return self._lookback_days.Value

    @LookbackDays.setter
    def LookbackDays(self, value):
        self._lookback_days.Value = value

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe] if self.Universe is not None else []

    def OnReseted(self):
        super(esg_factor_momentum_strategy, self).OnReseted()
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

        super(esg_factor_momentum_strategy, self).OnStarted2(time)
        for security in self._securities:
            self.SubscribeCandles(self.CandleType, True, security).Bind(lambda candle, s=security: self._process_candle(s, candle)).Start()

    def _process_candle(self, security, candle):
        if candle.State != CandleStates.Finished or candle.ClosePrice <= 0:
            return
        self._latest[security] = (candle.OpenTime, candle.ClosePrice)
        self._history[security][candle.OpenTime.Date] = candle.ClosePrice
        if len(self._latest) != len(self._securities) or any(t != candle.OpenTime for t, p in self._latest.values()) or self._last_bar == candle.OpenTime or not self.IsFormedAndOnlineAndAllowTrading():
            return
        self._last_bar = candle.OpenTime
        month = candle.OpenTime.Year * 100 + candle.OpenTime.Month
        if not self._is_rotating and self._last_rebalance != month and self._select_targets(candle.OpenTime):
            self._last_rebalance = month
        self._reconcile_targets()

    def _budget(self):
        return max(Decimal(0), min(self.Capital, self.Portfolio.CurrentValue if self.Portfolio.CurrentValue is not None else Decimal(0)))

    def _as_of(self, time):
        return time.Add(self.CandleType.Arg).AddTicks(-1)

    def _unit_price(self, security):
        return self._latest[security][1]

    def _units(self, security, notional):
        return Math.Floor(notional / self._unit_price(security) / security.VolumeStep) * security.VolumeStep

    def _reconcile_targets(self):
        if self._is_reconciling:
            return
        if any(o.State != OrderStates.Done and o.State != OrderStates.Failed for o in self._pending):
            return
        self._pending = []
        self._is_reconciling = True
        try:
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
                order.Comment = "0371Rebalance"
                self._pending.append(order)
                self.RegisterOrder(order)
        finally:
            self._is_reconciling = False
        self._continue_rotation()

    def _begin_rotation(self, targets):
        self._next_targets = targets
        self._targets = {s: Decimal(0) for s in self._securities}
        self._is_rotating = True

    def _on_trade(self, trade):
        self._continue_rotation()

    def _continue_rotation(self):
        if not self._is_rotating or self._is_reconciling or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if any(o.State != OrderStates.Done and o.State != OrderStates.Failed for o in self._pending):
            return
        if any((self.GetPositionValue(s, self.Portfolio) or Decimal(0)) != 0 for s in self._securities):
            return
        self._is_rotating = False
        self._targets = {s: self._next_targets.get(s, Decimal(0)) for s in self._securities}
        self._next_targets = {}
        self._reconcile_targets()

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

    def _select_targets(self, time):
        scores = []
        for security in self._securities:
            prices = self._daily(security, time, self.LookbackDays + 1, DateTime.MaxValue)
            if len(prices) == self.LookbackDays + 1:
                scores.append((security, prices[-1] / prices[0] - Decimal(1)))
        if not scores:
            return False
        winner = sorted(scores, key=lambda x: (-x[1], x[0].Id))[0][0]
        self._begin_rotation({winner: self._units(winner, self._budget())})
        return True


    def CreateClone(self):
        return esg_factor_momentum_strategy()
