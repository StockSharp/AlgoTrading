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


class two_x_spy_tips_strategy(Strategy):
    """Two X Spy Tips with point-in-time inputs and execution-based portfolio accounting."""

    def __init__(self):
        super(two_x_spy_tips_strategy, self).__init__()
        self._universe = self.Param[IEnumerable[Security]]("Universe", None).SetRequired() \
            .SetDisplay("Universe", "Explicit execution universe with native volume metadata.", "Parameters")
        self._capital = self.Param("Capital", Decimal(100000)).SetRange(Decimal(0.000001), Decimal.MaxValue) \
            .SetDisplay("Capital", "Fixed USD capital budget.", "Parameters")
        self._leverage = self.Param("Leverage", Decimal(2)).SetRange(Decimal(0.000001), Decimal.MaxValue) \
            .SetDisplay("Leverage", "Gross exposure divided by capital.", "Parameters")
        self._min_trade_usd = self.Param("MinTradeUsd", Decimal(200)).SetRange(Decimal(0), Decimal.MaxValue) \
            .SetDisplay("MinTradeUsd", "Minimum USD adjustment; complete liquidation is exempt.", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1440))) \
            .SetDisplay("CandleType", "Finished execution candle type.", "Parameters")
        self._price_history_file = self.Param[str]("PriceHistoryFile", None) \
            .SetDisplay("PriceHistoryFile", "Optional CSV of causal daily close prices.", "Parameters")
        self._spy_security = self.Param[Security]("SpySecurity", None).SetRequired() \
            .SetDisplay("SpySecurity", "Explicit S&P 500 signal security.", "Parameters")
        self._tips_security = self.Param[Security]("TipsSecurity", None).SetRequired() \
            .SetDisplay("TipsSecurity", "Explicit TIPS signal security.", "Parameters")
        self._sma_length = self.Param("SmaLength", 200).SetRange(2, 2147483647) \
            .SetDisplay("SmaLength", "Completed daily closes in each signal SMA.", "Parameters")
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
    def PriceHistoryFile(self):
        return self._price_history_file.Value

    @PriceHistoryFile.setter
    def PriceHistoryFile(self, value):
        self._price_history_file.Value = value

    @property
    def SpySecurity(self):
        return self._spy_security.Value

    @SpySecurity.setter
    def SpySecurity(self, value):
        self._spy_security.Value = value

    @property
    def TipsSecurity(self):
        return self._tips_security.Value

    @TipsSecurity.setter
    def TipsSecurity(self, value):
        self._tips_security.Value = value

    @property
    def SmaLength(self):
        return self._sma_length.Value

    @SmaLength.setter
    def SmaLength(self, value):
        self._sma_length.Value = value

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe] if self.Universe is not None else []

    def OnReseted(self):
        super(two_x_spy_tips_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        self._securities = list(self.Universe) if self.Universe is not None else []
        if not self._securities or any(s is None or not s.Id for s in self._securities) or len(set(s.Id.upper() for s in self._securities)) != len(self._securities):
            raise InvalidOperationException("Universe must contain distinct execution securities.")
        for security in self._securities:
            if security.VolumeStep is None or security.MinVolume is None or security.MaxVolume is None or security.VolumeStep <= 0 or security.MinVolume <= 0 or security.MaxVolume <= 0:
                raise InvalidOperationException("Every security needs positive VolumeStep, MinVolume and MaxVolume.")
            self._history[security] = {}
        if self.SpySecurity is None or self.TipsSecurity is None or self.SpySecurity.Id == self.TipsSecurity.Id or self.SpySecurity not in self._securities or self.TipsSecurity not in self._securities or self.Security not in self._securities:
            raise InvalidOperationException("Distinct SPY and TIPS signals and the execution security must belong to Universe.")
        self._load_price_history()
        super(two_x_spy_tips_strategy, self).OnStarted2(time)
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
        if self._last_rebalance != month and self._select_targets(candle.OpenTime):
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
            order.Comment = "0468Rebalance"
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

    def _select_targets(self, time):
        position = self.GetPositionValue(self.Security, self.Portfolio)
        if position is not None and position > 0:
            return True
        passes = True
        for signal in [self.SpySecurity, self.TipsSecurity]:
            prices = self._daily(signal, time, self.SmaLength, DateTime.MaxValue)
            if len(prices) != self.SmaLength:
                return False
            passes = passes and prices[-1] > sum(prices, Decimal(0)) / Decimal(len(prices))
        if not passes:
            return True
        self._targets[self.Security] = self._units(self.Security, self._budget())
        return True

    def _set_basket_targets(self, longs, shorts, gross):
        self._targets = {s: Decimal(0) for s in self._securities}
        for security in longs:
            self._targets[security] = self._units(security, gross / Decimal(2 * len(longs)))
        for security in shorts:
            self._targets[security] = -self._units(security, gross / Decimal(2 * len(shorts)))

    def CreateClone(self):
        return two_x_spy_tips_strategy()