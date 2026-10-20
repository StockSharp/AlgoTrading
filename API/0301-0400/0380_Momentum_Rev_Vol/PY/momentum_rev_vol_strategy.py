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


class momentum_rev_vol_strategy(Strategy):
    """Momentum Rev Vol with point-in-time inputs and execution-based portfolio accounting."""

    def __init__(self):
        super(momentum_rev_vol_strategy, self).__init__()
        self._universe = self.Param[IEnumerable[Security]]("Universe", None).SetRequired() \
            .SetDisplay("Universe", "Explicit execution universe with native volume metadata.", "Parameters")
        self._capital = self.Param("Capital", Decimal(100000)).SetRange(Decimal(0.000001), Decimal.MaxValue) \
            .SetDisplay("Capital", "Fixed USD capital budget.", "Parameters")
        self._leverage = self.Param("Leverage", Decimal(1)).SetRange(Decimal(0.000001), Decimal.MaxValue) \
            .SetDisplay("Leverage", "Gross exposure divided by capital.", "Parameters")
        self._min_trade_usd = self.Param("MinTradeUsd", Decimal(200)).SetRange(Decimal(0), Decimal.MaxValue) \
            .SetDisplay("MinTradeUsd", "Minimum USD adjustment; complete liquidation is exempt.", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1440))) \
            .SetDisplay("CandleType", "Finished execution candle type.", "Parameters")
        self._price_history_file = self.Param[str]("PriceHistoryFile", None) \
            .SetDisplay("PriceHistoryFile", "Optional CSV of causal daily close prices.", "Parameters")
        self._lookback12 = self.Param("Lookback12", 252).SetRange(1, 2147483647) \
            .SetDisplay("Lookback12", "Daily return intervals for long-term momentum.", "Parameters")
        self._lookback1 = self.Param("Lookback1", 21).SetRange(1, 2147483647) \
            .SetDisplay("Lookback1", "Daily return intervals for reversal.", "Parameters")
        self._vol_window = self.Param("VolWindow", 60).SetRange(2, 2147483647) \
            .SetDisplay("VolWindow", "Daily returns in the sample volatility window.", "Parameters")
        self._wm = self.Param("WM", Decimal(1)).SetRange(Decimal(0), Decimal.MaxValue) \
            .SetDisplay("WM", "Weight of long-term momentum.", "Parameters")
        self._wr = self.Param("WR", Decimal(1)).SetRange(Decimal(0), Decimal.MaxValue) \
            .SetDisplay("WR", "Weight of negative short-term returns.", "Parameters")
        self._wv = self.Param("WV", Decimal(1)).SetRange(Decimal(0), Decimal.MaxValue) \
            .SetDisplay("WV", "Weight of negative sample return volatility.", "Parameters")
        self._decile = self.Param("Decile", 10).SetRange(2, 2147483647) \
            .SetDisplay("Decile", "Number of equal rank buckets.", "Parameters")
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
    def Lookback12(self):
        return self._lookback12.Value

    @Lookback12.setter
    def Lookback12(self, value):
        self._lookback12.Value = value

    @property
    def Lookback1(self):
        return self._lookback1.Value

    @Lookback1.setter
    def Lookback1(self, value):
        self._lookback1.Value = value

    @property
    def VolWindow(self):
        return self._vol_window.Value

    @VolWindow.setter
    def VolWindow(self, value):
        self._vol_window.Value = value

    @property
    def WM(self):
        return self._wm.Value

    @WM.setter
    def WM(self, value):
        self._wm.Value = value

    @property
    def WR(self):
        return self._wr.Value

    @WR.setter
    def WR(self, value):
        self._wr.Value = value

    @property
    def WV(self):
        return self._wv.Value

    @WV.setter
    def WV(self, value):
        self._wv.Value = value

    @property
    def Decile(self):
        return self._decile.Value

    @Decile.setter
    def Decile(self, value):
        self._decile.Value = value

    def GetWorkingSecurities(self):
        return [(s, self.CandleType) for s in self.Universe] if self.Universe is not None else []

    def OnReseted(self):
        super(momentum_rev_vol_strategy, self).OnReseted()
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
        super(momentum_rev_vol_strategy, self).OnStarted2(time)
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
            order.Comment = "0380Rebalance"
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
        scores = []
        lookback = max(self.Lookback12, self.Lookback1, self.VolWindow)
        for security in self._securities:
            prices = self._daily(security, time, lookback + 1, DateTime.MaxValue)
            if len(prices) != lookback + 1:
                continue
            sample = prices[-self.VolWindow - 1:]
            returns = [sample[i + 1] / sample[i] - Decimal(1) for i in range(len(sample) - 1)]
            mean = sum(returns, Decimal(0)) / Decimal(len(returns))
            variance = sum(((r - mean) * (r - mean) for r in returns), Decimal(0)) / Decimal(len(returns) - 1)
            volatility = Decimal(Math.Sqrt(float(variance)))
            score = self.WM * (prices[-1] / prices[-self.Lookback12 - 1] - Decimal(1)) - self.WR * (prices[-1] / prices[-self.Lookback1 - 1] - Decimal(1)) - self.WV * volatility
            scores.append((security, score))
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
        return momentum_rev_vol_strategy()