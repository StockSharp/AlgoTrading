import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import VolumeWeightedAveragePrice, RelativeStrengthIndex, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class vwap_reversion_strategy(Strategy):
    """
    VWAP Reversion strategy.
    Trades on deviations from VWAP, exits when price returns.
    """

    def __init__(self):
        super(vwap_reversion_strategy, self).__init__()
        self._deviation_percent = self.Param("DeviationPercent", 2.0).SetGreaterThanZero().SetDisplay("Deviation %", "Deviation from VWAP for entry", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative() \
            .SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period of the RSI confirming entries", "Entry")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetRange(0.0, 100.0) \
            .SetDisplay("RSI Oversold", "Long entries require RSI below this level", "Entry")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetRange(0.0, 100.0) \
            .SetDisplay("RSI Overbought", "Short entries require RSI above this level", "Entry")

        self._pending_order = None
        self._vwap = None
        self._rsi = None
        self._session_date = None
        self.OrderRegistering += self._track_pending

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_reversion_strategy, self).OnReseted()
        self._pending_order = None
        self._vwap = None
        self._rsi = None
        self._session_date = None

    def OnStarted2(self, time):
        super(vwap_reversion_strategy, self).OnStarted2(time)

        self._vwap = VolumeWeightedAveragePrice()
        self.Indicators.Add(self._vwap)
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_period.Value
        self.Indicators.Add(self._rsi)
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._vwap)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # RSI runs over every finished candle and, unlike VWAP, is never reset per session.
        rsi_value = self._rsi.Process(CandleIndicatorValue(self._rsi, candle))

        date = candle.OpenTime.ToUniversalTime().Date
        if self._session_date != date:
            self._session_date = date
            # Reset before the first finished candle of the explicit UTC session.
            self._vwap.Reset()
        value = self._vwap.Process(CandleIndicatorValue(self._vwap, candle))
        if value.IsEmpty or not value.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return

        vwap = value.GetValue[Decimal](None)
        if vwap <= 0:
            return
        close = candle.ClosePrice
        deviation = Decimal(100) * (close - vwap) / vwap
        threshold = Decimal(self._deviation_percent.Value)
        if self.Position > 0 and close >= vwap:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close <= vwap:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and rsi_value.IsFormed and deviation < -threshold \
                and rsi_value.GetValue[Decimal](None) < Decimal(self._rsi_oversold.Value):
            self.BuyMarket(self.Volume)
        elif self.Position == 0 and rsi_value.IsFormed and deviation > threshold \
                and rsi_value.GetValue[Decimal](None) > Decimal(self._rsi_overbought.Value):
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return vwap_reversion_strategy()
