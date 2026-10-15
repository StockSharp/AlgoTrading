import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class williams_percent_r_divergence_strategy(Strategy):
    """Compare each finished bar's close and Williams %R with the reading DivergencePeriod bars back
    and trade the divergence in the %R extreme zone."""

    def __init__(self):
        super(williams_percent_r_divergence_strategy, self).__init__()
        self._wr_period = self.Param("WilliamsRPeriod", 14).SetGreaterThanZero().SetDisplay("Williams %R Period", "Highest-high/lowest-low lookback", "Indicators")
        self._divergence_period = self.Param("DivergencePeriod", 5).SetGreaterThanZero().SetDisplay("Divergence Period", "Bars back to the compared close and %R reading", "Pattern")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Williams %R and divergence timeframe", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it", "Protection")
        self._clear_state()
        self.OrderRegistering += self._track_pending

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def _clear_state(self):
        self._history = []
        self._wr = None
        self._pending_order = None

    def OnReseted(self):
        super(williams_percent_r_divergence_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        super(williams_percent_r_divergence_strategy, self).OnStarted2(time)
        self._clear_state()
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        self._wr = WilliamsR()
        self._wr.Length = self._wr_period.Value
        candles = self.SubscribeCandles(self.candle_type)
        candles.Bind(self._wr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, self._wr)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native actual-fill protection evaluates executable quotes between signal candles.
        pass

    def _process_candle(self, candle, value):
        if candle.State != CandleStates.Finished or not self._wr.IsFormed:
            return
        bullish = False
        bearish = False
        period = self._divergence_period.Value
        close = candle.ClosePrice
        if len(self._history) == period:
            prior_close, prior_wr = self._history[0]
            bullish = close < prior_close and value > prior_wr and value < Decimal(-80)
            bearish = close > prior_close and value < prior_wr and value > Decimal(-20)
        self._history.append((close, value))
        if len(self._history) > period:
            self._history.pop(0)
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if self.Position > 0 and value >= Decimal(-20):
            self.SellMarket(self.Position)
        elif self.Position < 0 and value <= Decimal(-80):
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0:
            if bullish:
                self.BuyMarket(self.Volume)
            elif bearish:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return williams_percent_r_divergence_strategy()
