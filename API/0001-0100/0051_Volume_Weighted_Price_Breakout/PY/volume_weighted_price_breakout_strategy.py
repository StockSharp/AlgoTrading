import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, VolumeWeightedMovingAverage, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class volume_weighted_price_breakout_strategy(Strategy):
    """
    Enters on an actual Close/VWMA cross confirmed by the Close/SMA side.
    Exits the full remaining position on an adverse Close/SMA cross or actual-fill protection.
    """

    def __init__(self):
        super(volume_weighted_price_breakout_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Current-inclusive Close SMA length", "Indicators")
        # The legacy name controls rolling VWMA, not session VWAP.
        self._vwap_period = self.Param("VWAPPeriod", 20).SetGreaterThanZero().SetDisplay("VWAP Period", "Current-inclusive rolling Close/volume VWMA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._ma = None
        self._vwma = None
        self._previous_close = None
        self._previous_ma = None
        self._previous_vwma = None
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
        self._ma = None
        self._vwma = None
        self._previous_close = None
        self._previous_ma = None
        self._previous_vwma = None
        self._pending_order = None

    def OnReseted(self):
        super(volume_weighted_price_breakout_strategy, self).OnReseted()
        self._clear_signal_state()

    def OnStarted2(self, time):
        super(volume_weighted_price_breakout_strategy, self).OnStarted2(time)
        self._clear_signal_state()
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        self._ma = SimpleMovingAverage()
        self._ma.Length = self._ma_period.Value
        self._vwma = VolumeWeightedMovingAverage()
        self._vwma.Length = self._vwap_period.Value
        self.Indicators.Add(self._ma)
        self.Indicators.Add(self._vwma)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawIndicator(area, self._vwma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        ma_value = self._ma.Process(CandleIndicatorValue(self._ma, candle))
        vwma_value = self._vwma.Process(CandleIndicatorValue(self._vwma, candle))
        ma = ma_value.GetValue[Decimal](None) if not ma_value.IsEmpty and self._ma.IsFormed else None
        vwma = vwma_value.GetValue[Decimal](None) if not vwma_value.IsEmpty and self._vwma.IsFormed else None
        close = candle.ClosePrice
        ma_up = self._previous_close is not None and self._previous_ma is not None and ma is not None and self._previous_close <= self._previous_ma and close > ma
        ma_down = self._previous_close is not None and self._previous_ma is not None and ma is not None and self._previous_close >= self._previous_ma and close < ma
        vwma_up = self._previous_close is not None and self._previous_vwma is not None and vwma is not None and self._previous_close <= self._previous_vwma and close > vwma
        vwma_down = self._previous_close is not None and self._previous_vwma is not None and vwma is not None and self._previous_close >= self._previous_vwma and close < vwma
        # Update raw close and independent indicator readiness even while disabled or pending.
        # An undefined VWMA window cannot bridge a later crossing.
        self._previous_close = close
        self._previous_ma = ma
        self._previous_vwma = vwma
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if self.Position > 0 and ma_down:
            self.SellMarket(self.Position)
        elif self.Position < 0 and ma_up:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and ma is not None:
            if vwma_up and close > ma:
                self.BuyMarket(self.Volume)
            elif vwma_down and close < ma:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return volume_weighted_price_breakout_strategy()
