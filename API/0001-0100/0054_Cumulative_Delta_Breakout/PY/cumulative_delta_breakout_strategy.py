import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields, MarketDataBuildModes
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class cumulative_delta_breakout_strategy(Strategy):
    """
    Trades breakouts of the running aggressor-side cumulative delta beyond its prior lookback range.
    Fully exits when the cumulative delta crosses zero or actual-fill protection triggers.
    """

    def __init__(self):
        super(cumulative_delta_breakout_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20).SetGreaterThanZero().SetDisplay("Lookback Period", "Number of prior cumulative delta values", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Trade-built signal candles", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._prior_cumulative = []
        self._cumulative_delta = Decimal(0)
        self._previous_cumulative = None
        self._pending_order = None
        self.OrderRegistering += self._track_pending

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def _clear_signal_state(self):
        self._prior_cumulative = []
        self._cumulative_delta = Decimal(0)
        self._previous_cumulative = None
        self._pending_order = None

    def OnReseted(self):
        super(cumulative_delta_breakout_strategy, self).OnReseted()
        self._clear_signal_state()

    def OnStarted2(self, time):
        super(cumulative_delta_breakout_strategy, self).OnStarted2(time)
        self._clear_signal_state()
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        # Archived minute bars are not consistently signed; build bars from true signed trades.
        candles = Subscription(self.candle_type, self.Security)
        candles.MarketData.BuildMode = MarketDataBuildModes.Build
        candles.MarketData.BuildFrom = DataType.Ticks
        self.SubscribeCandles(candles).Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection also watches the market between finished signal candles.
        pass

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        zero = Decimal(0)
        buy = candle.BuyVolume if candle.BuyVolume is not None else zero
        sell = candle.SellVolume if candle.SellVolume is not None else zero
        previous = self._previous_cumulative
        self._cumulative_delta += buy - sell
        period = self._lookback_period.Value
        full = len(self._prior_cumulative) == period
        up = full and self._cumulative_delta > max(self._prior_cumulative)
        down = full and self._cumulative_delta < min(self._prior_cumulative)
        if full:
            self._prior_cumulative.pop(0)
        self._prior_cumulative.append(self._cumulative_delta)
        self._previous_cumulative = self._cumulative_delta
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if self.Position > 0 and previous is not None and previous >= zero and self._cumulative_delta < zero:
            self.SellMarket(self.Position)
        elif self.Position < 0 and previous is not None and previous <= zero and self._cumulative_delta > zero:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0:
            if up:
                self.BuyMarket(self.Volume)
            elif down:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return cumulative_delta_breakout_strategy()
