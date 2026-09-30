import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class vcp_strategy(Strategy):
    """
    Volatility Contraction Pattern using prior rolling High/Low range breakouts.
    Exits on an adverse price/SMA crossing or actual-fill percent protection.
    """

    def __init__(self):
        super(vcp_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._lookback_period = self.Param("LookbackPeriod", 20).SetGreaterThanZero().SetDisplay("Lookback Period", "Rolling High/Low range length", "Indicators")
        self._contraction_bars = self.Param("ContractionBars", 3).SetGreaterThanZero().SetDisplay("Contractions", "Strict range reductions without an intervening expansion; equal widths are neutral.", "Entry")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_pattern()
        self.OrderRegistering += self._track_pending

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def _reset_pattern(self):
        self._previous_high = None
        self._previous_low = Decimal.Zero
        self._contraction_count = 0
        self._previous_close = None
        self._previous_mean = Decimal.Zero
        self._pending_order = None

    def OnReseted(self):
        super(vcp_strategy, self).OnReseted()
        self._reset_pattern()

    def OnStarted2(self, time):
        super(vcp_strategy, self).OnStarted2(time)
        self._reset_pattern()
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        highest = Highest()
        highest.Length = self._lookback_period.Value
        lowest = Lowest()
        lowest.Length = self._lookback_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(highest, lowest, sma, self._process_candle, False).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle, highest_value, lowest_value, sma_value):
        if candle.State != CandleStates.Finished or not highest_value.Indicator.IsFormed or not lowest_value.Indicator.IsFormed:
            return

        # Use the PRIOR completed channel, before the current breakout can expand it.
        upper = self._previous_high
        lower = self._previous_low
        contracted = self._contraction_count >= self._contraction_bars.Value
        high = highest_value.GetValue[Decimal](None)
        low = lowest_value.GetValue[Decimal](None)
        if upper is not None:
            change = high - low - (upper - lower)
            if change < 0:
                self._contraction_count += 1
            elif change > 0:
                self._contraction_count = 0
            # Equal width is neutral, not another contraction or an invalidation.
        self._previous_high = high
        self._previous_low = low

        if not sma_value.Indicator.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return
        close = candle.ClosePrice
        mean = sma_value.GetValue[Decimal](None)
        seeded = self._previous_close is not None
        upward_cross = seeded and self._previous_close <= self._previous_mean and close > mean
        downward_cross = seeded and self._previous_close >= self._previous_mean and close < mean
        self._previous_close = close
        self._previous_mean = mean
        if not seeded or upper is None or (self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed)):
            return
        if self.Position > 0 and downward_cross:
            self.SellMarket(self.Position)
        elif self.Position < 0 and upward_cross:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and contracted:
            if close > upper:
                self._contraction_count = 0
                self.BuyMarket(self.Volume)
            elif close < lower:
                self._contraction_count = 0
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return vcp_strategy()
