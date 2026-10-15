import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Array, ArgumentException, Decimal, TimeSpan
from StockSharp.Messages import DataType, CandleStates, Level1Fields, OrderStates, OrderTypes, Sides, Unit
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Indicators import IIndicator, BollingerBands, ExponentialMovingAverage, RelativeStrengthIndex, AverageDirectionalIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class post_open_long_atr_stop_loss_take_profit_strategy(Strategy):
    def __init__(self):
        super(post_open_long_atr_stop_loss_take_profit_strategy, self).__init__()
        self._bb_length = self.Param("BbLength", 14).SetGreaterThanZero() \
            .SetDisplay("BB Length", "Bollinger close-price SMA and deviation period", "Indicators")
        self._bb_mult = self.Param("BbMult", Decimal.Parse("1.5")).SetGreaterThanZero() \
            .SetDisplay("BB Mult", "Bollinger population deviation multiplier", "Indicators")
        self._ema_length = self.Param("EmaLength", 10).SetGreaterThanZero() \
            .SetDisplay("EMA Length", "Short close-price EMA period", "Indicators")
        self._ema_long_length = self.Param("EmaLongLength", 200).SetGreaterThanZero() \
            .SetDisplay("EMA Long Length", "Long close-price EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 7).SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_threshold = self.Param("RsiThreshold", Decimal.Parse("30")).SetRange(Decimal.Zero, Decimal(100)) \
            .SetDisplay("RSI Threshold", "RSI must be strictly above this value", "Indicators")
        self._adx_length = self.Param("AdxLength", 7).SetGreaterThanZero() \
            .SetDisplay("ADX Length", "ADX period", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", Decimal.Parse("10")).SetRange(Decimal.Zero, Decimal(100)) \
            .SetDisplay("ADX Threshold", "ADX must be strictly above this value", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero() \
            .SetDisplay("ATR Length", "Wilder ATR period", "Indicators")
        self._atr_stop_loss_multiplier = self.Param("AtrStopLossMultiplier", Decimal.Parse("2")).SetGreaterThanZero() \
            .SetDisplay("ATR SL Mult", "Frozen entry ATR stop distance", "Protection")
        self._atr_take_profit_multiplier = self.Param("AtrTakeProfitMultiplier", Decimal.Parse("4")).SetGreaterThanZero() \
            .SetDisplay("ATR TP Mult", "Frozen entry ATR take-profit distance", "Protection")
        self._session_start = self.Param("SessionStart", TimeSpan.FromMinutes(570)) \
            .SetRange(TimeSpan.Zero, TimeSpan.FromDays(1) - TimeSpan.FromTicks(1)) \
            .SetDisplay("Session Start", "Inclusive daily opening time in UTC", "Entry")
        self._session_duration = self.Param("SessionDuration", TimeSpan.FromHours(1)) \
            .SetRange(TimeSpan.FromTicks(1), TimeSpan.FromDays(1)) \
            .SetDisplay("Session Duration", "Opening-window duration; the end is exclusive", "Entry")
        self._resistance_period = self.Param("ResistancePeriod", 5).SetGreaterThanZero() \
            .SetDisplay("Resistance Period", "Prior finished highs excluding the signal candle", "Entry")
        self._basis_proximity = self.Param("BasisProximity", Decimal.Parse("1")).SetGreaterThanZero() \
            .SetDisplay("Basis Proximity", "Maximum distance from the basis in Bollinger half-widths", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of finished signal candles", "General")
        self._highs = []
        self._pending_order = None
        self._stop_distance = None
        self._take_distance = None
        self._protection_started = False
        self.OrderRegistering += self._observe_order

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def _observe_order(self, order):
        self._pending_order = order

    def OnReseted(self):
        super(post_open_long_atr_stop_loss_take_profit_strategy, self).OnReseted()
        self._highs = []
        self._pending_order = None
        self._stop_distance = None
        self._take_distance = None
        self._protection_started = False

    def OnStarted2(self, time):
        super(post_open_long_atr_stop_loss_take_profit_strategy, self).OnStarted2(time)
        start = self._session_start.Value
        duration = self._session_duration.Value
        if start < TimeSpan.Zero or start >= TimeSpan.FromDays(1) or duration <= TimeSpan.Zero or duration > TimeSpan.FromDays(1):
            raise ArgumentException("The UTC opening time must be within a day and duration within (0, 24 hours].")
        self._highs = []
        self._pending_order = None
        quotes = Subscription(DataType.Level1, self.Security)
        quotes.MarketData.BuildField = Level1Fields.BestBidPrice
        self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        bb = BollingerBands()
        bb.Length = self._bb_length.Value
        bb.Width = self._bb_mult.Value
        fast = ExponentialMovingAverage()
        fast.Length = self._ema_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._ema_long_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(Array[IIndicator]([bb, fast, slow, rsi, adx, atr]), self.OnProcess, True).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native bid-price protection runs before this callback.
        pass

    def OnProcess(self, candle, values):
        if candle.State != CandleStates.Finished:
            return
        period = self._resistance_period.Value
        resistance = max(self._highs) if len(self._highs) == period else None
        self._highs.append(candle.HighPrice)
        if len(self._highs) > period:
            self._highs.pop(0)
        pending = self._pending_order
        if resistance is None or any(value.IsEmpty or not value.IsFormed for value in values) \
                or not self.IsFormedAndOnlineAndAllowTrading() or self.Position != 0 \
                or pending is not None and pending.State != OrderStates.Done and pending.State != OrderStates.Failed:
            return
        basis = values[0].MovingAverage
        upper = values[0].UpBand
        strength = values[4].MovingAverage
        if basis is None or upper is None or strength is None:
            return
        age = (candle.OpenTime.TimeOfDay - self._session_start.Value + TimeSpan.FromDays(1)).Ticks % TimeSpan.TicksPerDay
        close = candle.ClosePrice
        fast = values[1].GetValue[Decimal](None)
        slow = values[2].GetValue[Decimal](None)
        rsi = values[3].GetValue[Decimal](None)
        atr = values[5].GetValue[Decimal](None)
        if age >= self._session_duration.Value.Ticks or close <= resistance or close < candle.OpenPrice \
                or abs(close - basis) > (upper - basis) * self._basis_proximity.Value \
                or close <= fast or fast <= slow or rsi <= self._rsi_threshold.Value \
                or strength <= self._adx_threshold.Value or atr <= 0:
            return
        if self._stop_distance is None:
            self._stop_distance = Unit()
            self._take_distance = Unit()
        self._stop_distance.Value = atr * self._atr_stop_loss_multiplier.Value
        self._take_distance.Value = atr * self._atr_take_profit_multiplier.Value
        if not self._protection_started:
            self.StartProtection(self._take_distance, self._stop_distance, useMarketOrders=True, isLocalStop=True)
            self._protection_started = True
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = Sides.Buy
        order.Volume = self.Volume
        order.Comment = "Post-open breakout entry"
        self.RegisterOrder(order)

    def CreateClone(self):
        return post_open_long_atr_stop_loss_take_profit_strategy()
