import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, OrderStates, OrderTypes, Sides, Level1Fields
from StockSharp.BusinessEntities import Subscription, Order
from StockSharp.Algo.Indicators import AverageDirectionalIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class adx_di_strategy(Strategy):
    """Strict DI crossings with rising ADX, weakening/opposite exits and actual-fill ATR protection."""

    def __init__(self):
        super(adx_di_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetDisplay("ADX Threshold", "ADX level to confirm trend", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Wilder ATR lookback for entry-frozen protection.", "Protection")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative() \
            .SetDisplay("ATR Multiplier", "Signal ATR distance; zero disables the stop.", "Protection")

        self._reset_state()
        self.OrderRegistering += self._track_pending
        self.Trades.TradeAdded += self._process_entry_fill

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def _reset_state(self):
        self._prev_plus_di = self._prev_minus_di = self._prev_adx = Decimal.Zero
        self._has_prev_values = False
        self._pending_order = self._entry_order = None
        self._stop_distance = None
        self._requested_distance = Decimal.Zero
        self._protection_started = False

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def AdxThreshold(self):
        return self._adx_threshold.Value

    @AdxThreshold.setter
    def AdxThreshold(self, value):
        self._adx_threshold.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(adx_di_strategy, self).OnStarted2(time)

        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        self.SubscribeCandles(self.CandleType) \
            .BindEx(adx, atr, self.ProcessCandle, False) \
            .Start()

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def ProcessCandle(self, candle, adx_value, atr_value):
        if candle.State != CandleStates.Finished or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if adx_value.IsEmpty:
            return

        adx_main = adx_value.MovingAverage
        plus_di = adx_value.Dx.Plus
        minus_di = adx_value.Dx.Minus

        if adx_main is None or plus_di is None or minus_di is None:
            return

        if not self._has_prev_values:
            self._has_prev_values = True
            self._save_previous(plus_di, minus_di, adx_main)
            return

        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            self._save_previous(plus_di, minus_di, adx_main)
            return

        up_cross = self._prev_plus_di <= self._prev_minus_di and plus_di > minus_di
        down_cross = self._prev_minus_di <= self._prev_plus_di and minus_di > plus_di
        confirmed = adx_main >= Decimal(self.AdxThreshold) and adx_main > self._prev_adx
        if confirmed and up_cross and self.Position <= 0:
            self._enter(Sides.Buy, atr_value.GetValue[Decimal](None))
        elif confirmed and down_cross and self.Position >= 0:
            self._enter(Sides.Sell, atr_value.GetValue[Decimal](None))
        elif self.Position > 0 and (adx_main < self._prev_adx or down_cross):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (adx_main < self._prev_adx or up_cross):
            self.BuyMarket(Math.Abs(self.Position))

        self._save_previous(plus_di, minus_di, adx_main)

    def _save_previous(self, plus, minus, adx):
        self._prev_plus_di = plus
        self._prev_minus_di = minus
        self._prev_adx = adx

    def _enter(self, side, atr):
        self._requested_distance = atr * Decimal(self._atr_multiplier.Value)
        if self._stop_distance is None:
            self._stop_distance = Unit(self._requested_distance)
        if self.Position == 0:
            self._stop_distance.Value = self._requested_distance
        if not self._protection_started and self._requested_distance > 0:
            self.StartProtection(Unit(), self._stop_distance, useMarketOrders=True, isLocalStop=True)
            self._protection_started = True
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = self.Volume + Math.Abs(self.Position)
        order.Comment = "ADX signal"
        self._entry_order = order
        self.RegisterOrder(order)

    def _process_entry_fill(self, trade):
        # Preserve the old distance until an actual reversal fill changes direction.
        if (trade.Order == self._entry_order and self.Position != 0
                and (self.Position > 0) == (trade.Order.Side == Sides.Buy)):
            self._stop_distance.Value = self._requested_distance

    def OnReseted(self):
        super(adx_di_strategy, self).OnReseted()
        self._reset_state()

    def CreateClone(self):
        return adx_di_strategy()
