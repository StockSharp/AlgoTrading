import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, OrderStates, OrderTypes, Sides, Level1Fields
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class volume_divergence_strategy(Strategy):
    """
    Buys a lower close and sells a higher close when volume rises over the previous candle.
    Fully exits when Close crosses the SMA in either direction or on actual-fill entry-ATR protection.
    """

    def __init__(self):
        super(volume_divergence_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Current-inclusive Close SMA length", "Indicators")
        self._atr_period = self.Param("ATRPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Wilder ATR length for actual-fill protection", "Indicators")
        self._stop_loss_atr_multiplier = self.Param("StopLossATRMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Stop Multiplier", "Frozen signal ATR distance from actual fills; zero disables it", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ma = None
        self._atr = None
        self._previous_close = None
        self._previous_volume = None
        self._previous_ma = None
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False
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
        self._atr = None
        self._previous_close = None
        self._previous_volume = None
        self._previous_ma = None
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False

    def OnReseted(self):
        super(volume_divergence_strategy, self).OnReseted()
        self._clear_signal_state()

    def OnStarted2(self, time):
        super(volume_divergence_strategy, self).OnStarted2(time)
        self._clear_signal_state()
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        self._ma = SimpleMovingAverage()
        self._ma.Length = self._ma_period.Value
        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_period.Value
        self.Indicators.Add(self._ma)
        self.Indicators.Add(self._atr)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        ma_value = self._ma.Process(CandleIndicatorValue(self._ma, candle))
        atr_value = self._atr.Process(CandleIndicatorValue(self._atr, candle))
        ma = ma_value.GetValue[Decimal](None) if not ma_value.IsEmpty and self._ma.IsFormed else None
        atr = atr_value.GetValue[Decimal](None) if not atr_value.IsEmpty and self._atr.IsFormed else None
        close = candle.ClosePrice
        volume = candle.TotalVolume
        price_down = self._previous_close is not None and close < self._previous_close
        price_up = self._previous_close is not None and close > self._previous_close
        volume_up = self._previous_volume is not None and volume > self._previous_volume
        ma_up = self._previous_close is not None and self._previous_ma is not None and ma is not None and self._previous_close <= self._previous_ma and close > ma
        ma_down = self._previous_close is not None and self._previous_ma is not None and ma is not None and self._previous_close >= self._previous_ma and close < ma
        # Advance raw price/volume and formed MA history even during warmup or disabled trading.
        self._previous_close = close
        self._previous_volume = volume
        self._previous_ma = ma
        if ma is None or atr is None or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        ma_cross = ma_up or ma_down
        if self.Position > 0 and ma_cross:
            self.SellMarket(self.Position)
        elif self.Position < 0 and ma_cross:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0:
            if price_down and volume_up:
                self._enter(Sides.Buy, atr)
            elif price_up and volume_up:
                self._enter(Sides.Sell, atr)

    def _enter(self, side, atr):
        distance = atr * Decimal(self._stop_loss_atr_multiplier.Value)
        if self._stop_distance is None:
            self._stop_distance = Unit(distance)
        # Preserve the Unit reference cached by native protection controllers.
        self._stop_distance.Value = distance
        if not self._protection_started and distance > 0:
            self.StartProtection(Unit(), self._stop_distance, useMarketOrders=True, isLocalStop=True)
            self._protection_started = True
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = self.Volume
        order.Comment = "Volume divergence entry"
        self.RegisterOrder(order)

    def CreateClone(self):
        return volume_divergence_strategy()
