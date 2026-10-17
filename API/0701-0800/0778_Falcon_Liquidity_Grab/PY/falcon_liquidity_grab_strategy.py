import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import Decimal, TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Subscription


class falcon_liquidity_grab_strategy(Strategy):
    """Swing-wick liquidity grabs with SMA confirmation and real native tick protection."""

    def __init__(self):
        super(falcon_liquidity_grab_strategy, self).__init__()
        self._swing_period = self.Param("SwingPeriod", 20).SetGreaterThanZero() \
            .SetDisplay("Swing Period", "Number of preceding swing highs/lows.", "Signal")
        self._trend_length = self.Param("TrendLength", 50).SetGreaterThanZero() \
            .SetDisplay("Trend Length", "SMA trend-filter period.", "Signal")
        self._stop_loss_points = self.Param("StopLossPoints", 50).SetGreaterThanZero() \
            .SetDisplay("Stop Loss Points", "Stop distance in price steps.", "Protection")
        self._take_profit_multiplier = self.Param("TakeProfitMultiplier", 2.0).SetGreaterThanZero() \
            .SetDisplay("Take Profit Multiplier", "Take profit as a multiple of stop distance.", "Protection")
        self._use_session = self.Param("UseSession", True).SetDisplay(
            "Use Session", "Restrict entries to configured UTC hours.", "Session")
        self._session_start = self.Param("SessionStart", TimeSpan.FromHours(7)).SetDisplay(
            "Session Start", "UTC inclusive session start.", "Session")
        self._session_end = self.Param("SessionEnd", TimeSpan.FromHours(21)).SetDisplay(
            "Session End", "UTC exclusive session end.", "Session")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle timeframe.", "General")
        self._previous_highs = []
        self._previous_lows = []
        self._pending_order = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value), (self.Security, DataType.Level1)]

    def OnReseted(self):
        super(falcon_liquidity_grab_strategy, self).OnReseted()
        self._previous_highs = []
        self._previous_lows = []
        self._pending_order = None

    def OnStarted2(self, time):
        super(falcon_liquidity_grab_strategy, self).OnStarted2(time)
        step = Decimal(self.Security.PriceStep) if self.Security.PriceStep is not None else Decimal.Zero
        if step <= Decimal.Zero:
            raise ValueError("Positive Security.PriceStep is required for tick-based protection")
        stop = step * Decimal(self._stop_loss_points.Value)
        target = stop * Decimal(self._take_profit_multiplier.Value)
        self.StartProtection(Unit(target, UnitTypes.Absolute),
                             Unit(stop, UnitTypes.Absolute),
                             useMarketOrders=True, isLocalStop=True)

        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        self.OrderRegistering += self._on_order_registering
        self._trend = SimpleMovingAverage()
        self._trend.Length = int(self._trend_length.Value)
        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.Bind(self._trend, self.on_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._trend)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection uses both sides of Level1 between signal candles.
        pass

    def _on_order_registering(self, order):
        self._pending_order = order

    def on_candle(self, candle, trend_value):
        if candle.State != CandleStates.Finished:
            return

        swing = int(self._swing_period.Value)
        active = self._pending_order is not None and self._pending_order.State not in (
            OrderStates.Done, OrderStates.Failed)
        if self._trend.IsFormed and len(self._previous_highs) == swing and not active and self.Position == 0:
            utc_time = candle.OpenTime.ToUniversalTime().TimeOfDay
            start = self._session_start.Value
            end = self._session_end.Value
            in_session = (not self._use_session.Value or
                          (start <= utc_time < end if start <= end else
                           (utc_time >= start or utc_time < end)))
            if in_session:
                close = float(candle.ClosePrice)
                low = float(candle.LowPrice)
                high = float(candle.HighPrice)
                trend = float(trend_value)
                if low < min(self._previous_lows) and close > trend:
                    self.BuyMarket(self.Volume)
                elif high > max(self._previous_highs) and close < trend:
                    self.SellMarket(self.Volume)

        self._previous_highs.append(float(candle.HighPrice))
        self._previous_lows.append(float(candle.LowPrice))
        if len(self._previous_highs) > swing:
            self._previous_highs.pop(0)
        if len(self._previous_lows) > swing:
            self._previous_lows.pop(0)

    def CreateClone(self):
        return falcon_liquidity_grab_strategy()
