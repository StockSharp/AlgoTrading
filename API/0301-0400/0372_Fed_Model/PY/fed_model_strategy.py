import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("Ecng.ComponentModel")

from System import Boolean, DateTime, Decimal, Int32, Math, TimeSpan, InvalidOperationException
from System.Collections.Generic import IEnumerable
from System.Globalization import CultureInfo, DateTimeStyles
from System.IO import File, Path
from Ecng.ComponentModel import WorkingTimeExtensions
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from StockSharp.Messages import CandleStates, DataType, OrderStates, OrderTypes, Sides


class fed_model_strategy(Strategy):
    """Fed Model with point-in-time inputs and execution-based portfolio accounting."""

    def __init__(self):
        super(fed_model_strategy, self).__init__()
        self._universe = self.Param[IEnumerable[Security]]("Universe", None).SetRequired() \
            .SetDisplay("Universe", "Explicit execution securities with valid native volume metadata", "Parameters")
        self._capital = self.Param("Capital", Decimal(100000)).SetGreaterThanZero() \
            .SetDisplay("Capital", "Fixed capital budget in USD", "Parameters")
        self._min_trade_usd = self.Param("MinTradeUsd", Decimal(100)).SetRange(Decimal(0), Decimal.MaxValue) \
            .SetDisplay("MinTradeUsd", "Minimum USD adjustment; complete liquidation bypasses this filter", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1440))) \
            .SetDisplay("CandleType", "Finished execution candle type", "Parameters")
        self._regression_months = self.Param("RegressionMonths", 12).SetRange(2, 120) \
            .SetDisplay("RegressionMonths", "Consecutive monthly yield spreads in the OLS forecast", "Parameters")
        self._yield_history_file = self.Param[str]("YieldHistoryFile", None).SetRequired() \
            .SetDisplay("YieldHistoryFile", "Required point-in-time monthly equity earnings and ten-year bond yields", "Parameters")
        self._clear_state()
        self.Trades.TradeAdded += self._on_trade

    def _clear_state(self):
        self._securities = []
        self._latest = {}
        self._targets = {}
        self._pending = []
        self._last_rebalance = 0
        self._last_bar = None
        self._next_targets = {}
        self._is_rotating = False
        self._is_reconciling = False
        self._yields = []
        self._equity_regime = None

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
    def RegressionMonths(self):
        return self._regression_months.Value

    @RegressionMonths.setter
    def RegressionMonths(self, value):
        self._regression_months.Value = value

    @property
    def YieldHistoryFile(self):
        return self._yield_history_file.Value

    @YieldHistoryFile.setter
    def YieldHistoryFile(self, value):
        self._yield_history_file.Value = value

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe] if self.Universe is not None else []

    def OnReseted(self):
        super(fed_model_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        self._securities = list(self.Universe) if self.Universe is not None else []
        if not self._securities or any(s is None or not s.Id for s in self._securities) or len(set(s.Id.upper() for s in self._securities)) != len(self._securities):
            raise InvalidOperationException("Universe must contain distinct execution securities.")
        for security in self._securities:
            if security.VolumeStep is None or security.MinVolume is None or security.MaxVolume is None or security.VolumeStep <= 0 or security.MinVolume <= 0 or security.MaxVolume <= 0:
                raise InvalidOperationException("Every security needs positive VolumeStep, MinVolume and MaxVolume.")
        if len(self._securities) > 2 or self.CandleType.Arg != TimeSpan.FromDays(1) or any(s.Board is None for s in self._securities):
            raise InvalidOperationException("The Fed model requires daily candles, an equity security and at most one cash security, with exchange calendars.")
        self._load_yields()
        super(fed_model_strategy, self).OnStarted2(time)
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
        if not self._is_rotating and self._last_rebalance != month and self._select_targets(candle.OpenTime, max(value[2] for value in self._latest.values())):
            self._last_rebalance = month
        self._reconcile_targets()

    def _budget(self):
        return max(Decimal(0), min(self.Capital, self.Portfolio.CurrentValue if self.Portfolio.CurrentValue is not None else Decimal(0)))

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
                order.Comment = "0372Rebalance"
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

    def _load_yields(self):
        keys = set()
        for row in self._read_rows(self.YieldHistoryFile, "Month,PublishedAt,EarningsYield,BondYield", 4):
            month = DateTime.ParseExact(row[0].strip(), "yyyy-MM", CultureInfo.InvariantCulture)
            published = self._parse_utc(row[1])
            earnings = Decimal.Parse(row[2], CultureInfo.InvariantCulture)
            bond = Decimal.Parse(row[3], CultureInfo.InvariantCulture)
            key = (month, published)
            if published.Date < month or key in keys:
                raise InvalidOperationException("Monthly yield observations need causal, unique publication dates.")
            keys.add(key)
            self._yields.append((month, published, earnings - bond))

    def _select_targets(self, time, as_of):
        calendar = self._securities[0].Board.WorkingTime
        if not WorkingTimeExtensions.IsWorkingDate(calendar, time.Date, True):
            return False
        following = time.Date.AddDays(1)
        while following.Month == time.Month:
            if WorkingTimeExtensions.IsWorkingDate(calendar, following, True):
                return False
            following = following.AddDays(1)
        month = DateTime(time.Year, time.Month, 1)
        y = []
        for i in range(self.RegressionMonths - 1, -1, -1):
            available = [v for v in self._yields if v[0] == month.AddMonths(-i) and v[1] <= as_of]
            if not available:
                return False
            y.append(max(available, key=lambda v: v[1])[2])
        mean_x = Decimal(self.RegressionMonths - 1) / Decimal(2)
        mean_y = sum(y, Decimal(0)) / Decimal(len(y))
        variance = sum(((Decimal(i) - mean_x) * (Decimal(i) - mean_x) for i in range(len(y))), Decimal(0))
        slope = sum(((Decimal(i) - mean_x) * (v - mean_y) for i, v in enumerate(y)), Decimal(0)) / variance
        prediction = mean_y + slope * (Decimal(self.RegressionMonths) - mean_x)
        if prediction == 0:
            return False
        equity = prediction > 0
        if self._equity_regime == equity:
            return True
        self._equity_regime = equity
        security = self._securities[0] if equity else self._securities[1] if len(self._securities) > 1 else None
        self._begin_rotation({security: self._units(security, self._budget())} if security is not None else {})
        return True


    def CreateClone(self):
        return fed_model_strategy()
