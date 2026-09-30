import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class z_score_strategy(Strategy):
    """
    Z-Score mean reversion strategy.
    Buys when Z-Score is below negative threshold, sells when above positive threshold.
    """

    def __init__(self):
        super(z_score_strategy, self).__init__()
        self._z_entry = self.Param("ZScoreEntryThreshold", 2.0).SetGreaterThanZero().SetDisplay("Z-Score Entry", "Distance from mean in std devs for entry", "Z-Score")
        self._z_exit = self.Param("ZScoreExitThreshold", 0.0).SetNotNegative().SetDisplay("Z-Score Exit", "Half-width of the neutral exit zone in standard deviations", "Z-Score")
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for Moving Average", "Indicators")
        self._std_period = self.Param("StdDevPeriod", 20).SetGreaterThanZero().SetDisplay("StdDev Period", "Period for Standard Deviation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative() \
            .SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")

        self._pending_order = None
        self.OrderRegistering += self._track_pending

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(z_score_strategy, self).OnReseted()
        self._pending_order = None

    def OnStarted2(self, time):
        super(z_score_strategy, self).OnStarted2(time)

        if self._z_exit.Value >= self._z_entry.Value:
            raise ValueError("ZScoreExitThreshold must be below ZScoreEntryThreshold.")

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        std = StandardDeviation()
        std.Length = self._std_period.Value
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, std, self._process_candle, False).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, std)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def _process_candle(self, candle, ma_val, std_val):
        if candle.State != CandleStates.Finished:
            return

        if not ma_val.IsFormed or not std_val.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return

        sigma = std_val.GetValue[Decimal](None)
        # A constant-price variance window is explicitly neutral, not an exit blackout.
        z = Decimal.Zero if sigma == 0 else (candle.ClosePrice - ma_val.GetValue[Decimal](None)) / sigma
        entry = Decimal(self._z_entry.Value)
        exit_t = Decimal(self._z_exit.Value)
        if self.Position > 0 and z >= -exit_t:
            self.SellMarket(self.Position)
        elif self.Position < 0 and z <= exit_t:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and z < -entry:
            self.BuyMarket(self.Volume)
        elif self.Position == 0 and z > entry:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return z_score_strategy()
