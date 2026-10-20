import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTime, Decimal, Math, TimeSpan, InvalidOperationException
from System.Globalization import CultureInfo, DateTimeStyles
from System.IO import File
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, AverageTrueRange, AlligatorLine, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Subscription
from StockSharp.Messages import Level1Fields, ITickTradeMessage, CandleStates, DataType, OrderStates, OrderTypes, Sides, Unit, UnitTypes


class fib_hurst_breakout_strategy(Strategy):
    """Prior completed daily Fibonacci range and daily R/S Hurst filter with capped intraday entries."""

    def __init__(self):
        super(fib_hurst_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("CandleType", "Finished signal candle type.", "Parameters")
        self._hurst_period = self.Param("HurstPeriod", 50).SetRange(2, 2147483647) \
            .SetDisplay("HurstPeriod", "Completed daily closes in the R/S Hurst window.", "Parameters")
        self._lookback_period = self.Param("LookbackPeriod", 20).SetRange(1, 2147483647) \
            .SetDisplay("LookbackPeriod", "Prior completed daily bars in the Fibonacci range.", "Parameters")
        self._max_trades_per_day = self.Param("MaxTradesPerDay", 5).SetRange(1, 2147483647) \
            .SetDisplay("MaxTradesPerDay", "Maximum filled-entry attempts per historical UTC day.", "Parameters")
        self._max_total_trades = self.Param("MaxTotalTrades", 510).SetRange(1, 2147483647) \
            .SetDisplay("MaxTotalTrades", "Maximum entry attempts in one run.", "Parameters")
        self._risk_percent = self.Param("RiskPercent", Decimal(2)).SetRange(Decimal(0.000001), Decimal.MaxValue) \
            .SetDisplay("RiskPercent", "Native stop distance as a percentage of the actual fill.", "Parameters")
        self._risk_reward = self.Param("RiskReward", Decimal(2)).SetRange(Decimal(0.000001), Decimal.MaxValue) \
            .SetDisplay("RiskReward", "Take-profit percentage divided by the stop percentage.", "Parameters")
        self._daily_history_file = self.Param[str]("DailyHistoryFile", None) \
            .SetDisplay("DailyHistoryFile", "Optional dated daily High/Low/Close CSV for warmup.", "Parameters")
        self._clear_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def HurstPeriod(self):
        return self._hurst_period.Value

    @HurstPeriod.setter
    def HurstPeriod(self, value):
        self._hurst_period.Value = value

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def MaxTradesPerDay(self):
        return self._max_trades_per_day.Value

    @MaxTradesPerDay.setter
    def MaxTradesPerDay(self, value):
        self._max_trades_per_day.Value = value

    @property
    def MaxTotalTrades(self):
        return self._max_total_trades.Value

    @MaxTotalTrades.setter
    def MaxTotalTrades(self, value):
        self._max_total_trades.Value = value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @RiskPercent.setter
    def RiskPercent(self, value):
        self._risk_percent.Value = value

    @property
    def RiskReward(self):
        return self._risk_reward.Value

    @RiskReward.setter
    def RiskReward(self, value):
        self._risk_reward.Value = value

    @property
    def DailyHistoryFile(self):
        return self._daily_history_file.Value

    @DailyHistoryFile.setter
    def DailyHistoryFile(self, value):
        self._daily_history_file.Value = value

    def _clear_state(self):
        self._pending = None
        self._daily = {}
        self._previous_close = None
        self._day = None
        self._daily_trades = 0
        self._total_trades = 0

    def OnReseted(self):
        super(fib_hurst_breakout_strategy, self).OnReseted()
        self._clear_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, DataType.TimeFrame(TimeSpan.FromDays(1)))]

    def OnStarted2(self, time):
        if self.CandleType.Arg <= TimeSpan.Zero or self.CandleType.Arg >= TimeSpan.FromDays(1):
            raise InvalidOperationException("CandleType must be an intraday time frame.")
        self._load_daily_history()
        super(fib_hurst_breakout_strategy, self).OnStarted2(time)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quote = Subscription(DataType.Level1, self.Security)
            quote.MarketData.BuildField = field
            self.SubscribeLevel1(quote).Start()
        self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromDays(1))).Bind(self._process_daily).Start()
        self.SubscribeCandles(self.CandleType).Bind(self._process_candle).Start()

    def _is_pending(self):
        return self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)

    def _submit(self, side, quantity, comment):
        self._pending = self.CreateOrder(side, Decimal(0), quantity)
        self._pending.Type = OrderTypes.Market
        self._pending.Comment = comment
        self.RegisterOrder(self._pending)

    @staticmethod
    def _process(indicator, candle):
        value = CandleIndicatorValue(indicator, candle)
        value.IsFinal = True
        result = indicator.Process(value)
        return None if result.IsEmpty else result.GetValue[Decimal](None)

    def _load_daily_history(self):
        if not self.DailyHistoryFile:
            return
        lines = list(File.ReadLines(self.DailyHistoryFile))
        if not lines or lines[0].strip() != "Date,High,Low,Close":
            raise InvalidOperationException("Daily CSV header must be Date,High,Low,Close.")
        for line in lines[1:]:
            if not line.strip():
                continue
            row = line.split(",")
            if len(row) != 4:
                raise InvalidOperationException("Invalid daily CSV field count.")
            date = DateTime.ParseExact(row[0].strip(), "yyyy-MM-dd", CultureInfo.InvariantCulture)
            high, low, close = [Decimal.Parse(v, CultureInfo.InvariantCulture) for v in row[1:]]
            if low <= 0 or high < low or close < low or close > high or date in self._daily:
                raise InvalidOperationException("Daily history must contain unique dates and valid positive OHLC.")
            self._daily[date] = (high, low, close)

    def _process_daily(self, candle):
        if candle.State == CandleStates.Finished:
            self._daily[candle.OpenTime.Date] = (candle.HighPrice, candle.LowPrice, candle.ClosePrice)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        previous = self._previous_close
        self._previous_close = candle.ClosePrice
        if self._day != candle.OpenTime.Date:
            self._day = candle.OpenTime.Date
            self._daily_trades = 0
        days = [self._daily[d] for d in sorted(self._daily) if d < self._day]
        if len(days) < max(self.HurstPeriod, self.LookbackPeriod) or previous is None or self.Position != 0 or self._is_pending() or self._daily_trades >= self.MaxTradesPerDay or self._total_trades >= self.MaxTotalTrades or not self.IsFormedAndOnlineAndAllowTrading():
            return
        prices = [float(d[2]) for d in days[-self.HurstPeriod:]]
        mean = sum(prices) / len(prices)
        variance = sum((p - mean) * (p - mean) for p in prices) / len(prices)
        if variance <= 0:
            return
        cumulative = 0.0
        path = []
        for price in prices:
            cumulative += price - mean
            path.append(cumulative)
        rescaled = (max(path) - min(path)) / Math.Sqrt(variance)
        if rescaled <= 0:
            return
        hurst = Math.Log(rescaled) / Math.Log(float(len(prices)))
        window = days[-self.LookbackPeriod:]
        low = min(d[1] for d in window)
        high = max(d[0] for d in window)
        if high <= low:
            return
        upper = low + Decimal(0.618) * (high - low)
        lower = low + Decimal(0.382) * (high - low)
        side = Sides.Buy if previous <= upper and candle.ClosePrice > upper and hurst > 0.5 else Sides.Sell if previous >= lower and candle.ClosePrice < lower and hurst < 0.5 else None
        if side is None:
            return
        self._daily_trades += 1
        self._total_trades += 1
        self.StartProtection(Unit(self.RiskPercent * self.RiskReward, UnitTypes.Percent), Unit(self.RiskPercent, UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        self._submit(side, self.Volume, "0502Entry")

    def CreateClone(self):
        return fib_hurst_breakout_strategy()
