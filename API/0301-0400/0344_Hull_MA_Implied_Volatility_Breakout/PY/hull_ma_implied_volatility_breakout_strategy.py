import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTime, Decimal, Math, TimeSpan, InvalidOperationException
from System.Globalization import CultureInfo, DateTimeStyles
from System.IO import File
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Subscription
from StockSharp.Messages import CandleStates, DataType, Level1Fields, OrderStates, OrderTypes, Sides, Unit, UnitTypes


class hull_ma_implied_volatility_breakout_strategy(Strategy):
    """Observed option statistics filter, full signal exits and frozen native ATR protection."""

    def __init__(self):
        super(hull_ma_implied_volatility_breakout_strategy, self).__init__()
        self._hma_period = self.Param("HmaPeriod", 9).SetRange(2, 100000).SetOptimize(5, 15, 2) \
            .SetDisplay("HmaPeriod", "Hull moving average length", "Parameters")
        self._iv_period = self.Param("IVPeriod", 20).SetRange(2, 100000).SetOptimize(10, 30, 5) \
            .SetDisplay("IVPeriod", "Prior consecutive implied volatility observations used for the threshold", "Parameters")
        self._iv_multiplier = self.Param("IVMultiplier", Decimal(2)).SetGreaterThanZero().SetOptimize(Decimal(1.5), Decimal(3), Decimal(0.5)) \
            .SetDisplay("IVMultiplier", "Sample standard deviation multiplier for the IV breakout", "Parameters")
        self._iv_history_file = self.Param[str]("IVHistoryFile", None).SetRequired() \
            .SetDisplay("IVHistoryFile", "CSV observed implied volatility in a consistent unit, keyed by candle UTC open time", "Parameters")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero() \
            .SetDisplay("AtrPeriod", "Wilder ATR length for entry protection", "Parameters")
        self._stop_loss_atr = self.Param("StopLossAtr", Decimal(2)).SetRange(Decimal(0), Decimal.MaxValue).SetOptimize(Decimal(1), Decimal(3), Decimal(0.5)) \
            .SetDisplay("StopLossAtr", "Entry-frozen native stop distance in ATR multiples; zero disables it for a fresh run", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("CandleType", "Completed signal candle type", "Parameters")
        self._observations = {}
        self._history = []
        self._previous_hull = None
        self._signal_order = None

    @property
    def HmaPeriod(self):
        return self._hma_period.Value

    @HmaPeriod.setter
    def HmaPeriod(self, value):
        self._hma_period.Value = value

    @property
    def IVPeriod(self):
        return self._iv_period.Value

    @IVPeriod.setter
    def IVPeriod(self, value):
        self._iv_period.Value = value

    @property
    def IVMultiplier(self):
        return self._iv_multiplier.Value

    @IVMultiplier.setter
    def IVMultiplier(self, value):
        self._iv_multiplier.Value = value

    @property
    def IVHistoryFile(self):
        return self._iv_history_file.Value

    @IVHistoryFile.setter
    def IVHistoryFile(self, value):
        self._iv_history_file.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def StopLossAtr(self):
        return self._stop_loss_atr.Value

    @StopLossAtr.setter
    def StopLossAtr(self, value):
        self._stop_loss_atr.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(hull_ma_implied_volatility_breakout_strategy, self).OnReseted()
        self._observations = {}
        self._history = []
        self._previous_hull = None
        self._signal_order = None

    def OnStarted2(self, time):
        self._load_observations()
        super(hull_ma_implied_volatility_breakout_strategy, self).OnStarted2(time)
        signal = HullMovingAverage()
        signal.Length = self.HmaPeriod

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(signal, atr, self._process_candle, True).Start()
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quote = Subscription(DataType.Level1, self.Security)
            quote.MarketData.BuildField = field
            self.SubscribeLevel1(quote).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, signal)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, signal_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        ready = candle.OpenTime in self._observations
        low = False
        high = False
        if ready and len(self._history) == self.IVPeriod:
            observation = self._observations[candle.OpenTime]
            mean = sum(self._history, Decimal(0)) / Decimal(len(self._history))
            variance = sum(((v - mean) * (v - mean) for v in self._history), Decimal(0)) / Decimal(len(self._history) - 1)
            deviation = Decimal(Math.Sqrt(float(variance)))
            low = observation < mean - self.IVMultiplier * deviation
            high = observation > mean + self.IVMultiplier * deviation
        if not ready:
            self._history = []
        else:
            self._history.append(self._observations[candle.OpenTime])
            self._history = self._history[-self.IVPeriod:]
        if signal_value.IsEmpty or not signal_value.IsFormed:
            return
        current = signal_value.GetValue[Decimal](None)
        up = self._previous_hull is not None and current > self._previous_hull
        down = self._previous_hull is not None and current < self._previous_hull
        self._previous_hull = current
        if not self.IsFormedAndOnlineAndAllowTrading() or (self._signal_order is not None and self._signal_order.State not in (OrderStates.Done, OrderStates.Failed)):
            return
        if (self.Position > 0 and down) or (self.Position < 0 and up):
            self._submit(Sides.Sell if self.Position > 0 else Sides.Buy, abs(self.Position), "0344Exit")
            return
        if self.Position != 0 or atr_value.IsEmpty or not atr_value.IsFormed:
            return
        atr = atr_value.GetValue[Decimal](None)
        if atr <= 0:
            return
        side = Sides.Buy if up and high else Sides.Sell if down and high else None
        if side is None:
            return
        if self.StopLossAtr > 0:
            self.StartProtection(Unit(), Unit(atr * self.StopLossAtr, UnitTypes.Absolute), useMarketOrders=True, isLocalStop=True)
        self._submit(side, self.Volume, "0344Entry")

    def _submit(self, side, quantity, comment):
        order = self.CreateOrder(side, Decimal(0), quantity)
        order.Type = OrderTypes.Market
        order.Comment = comment
        self._signal_order = order
        self.RegisterOrder(order)

    def _load_observations(self):
        if not self.IVHistoryFile:
            raise InvalidOperationException("IVHistoryFile is required.")
        self._observations = {}
        lines = iter(File.ReadLines(self.IVHistoryFile))
        if next(lines, "").strip() != "SecurityId,Time,Value":
            raise InvalidOperationException("Option observations must begin with SecurityId,Time,Value.")
        for line in lines:
            if not line.strip():
                continue
            row = line.split(",")
            if len(row) != 3:
                raise InvalidOperationException("An option observation must have three fields.")
            if row[0].strip().lower() != self.Security.Id.lower():
                continue
            time = DateTime.ParseExact(row[1].strip(), "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture,
                                       DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
            value = Decimal.Parse(row[2], CultureInfo.InvariantCulture)
            if value < 0 or time in self._observations:
                raise InvalidOperationException("Option observations must be nonnegative and unique per timestamp.")
            self._observations[time] = value
        if not self._observations:
            raise InvalidOperationException("No option observations match the execution security.")

    def CreateClone(self):
        return hull_ma_implied_volatility_breakout_strategy()
