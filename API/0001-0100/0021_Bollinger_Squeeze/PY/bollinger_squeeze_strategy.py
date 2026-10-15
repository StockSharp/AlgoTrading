import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, OrderStates, OrderTypes, Sides, Level1Fields
from StockSharp.BusinessEntities import Subscription, Order
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class bollinger_squeeze_strategy(Strategy):
    """Preceding-bar squeeze, outside-band crossing, middle exit and optional actual-fill ATR stop."""
    def __init__(self):
        super(bollinger_squeeze_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._squeeze_threshold = self.Param("SqueezeThreshold", 0.1).SetGreaterThanZero() \
            .SetDisplay("Squeeze Threshold", "Maximum preceding-bar band-width/middle ratio.", "Signal")
        self._use_atr_stop = self.Param("UseAtrStop", False) \
            .SetDisplay("Use ATR Stop", "Optional actual-fill local ATR protection.", "Protection")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Wilder ATR lookback when protection is enabled.", "Protection")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative() \
            .SetDisplay("ATR Multiplier", "Frozen signal ATR distance; zero disables the stop.", "Protection")
        self._reset_state()
        self.OrderRegistering += self._track_pending
        self.Trades.TradeAdded += self._process_entry_fill

    def GetWorkingSecurities(self):
        result = [(self.Security, self.candle_type)]
        if self._use_atr_stop.Value:
            result.append((self.Security, DataType.Level1))
        return result

    def _track_pending(self, order):
        self._pending_order = order

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value
    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_squeeze_strategy, self).OnReseted()
        self._reset_state()

    def _reset_state(self):
        self._prev_band_width = Decimal.Zero
        self._has_prev_values = False
        self._prev_close = self._prev_upper = self._prev_lower = Decimal.Zero
        self._pending_order = self._entry_order = None
        self._stop_distance = None
        self._requested_distance = Decimal.Zero
        self._protection_started = False

    def OnStarted2(self, time):
        super(bollinger_squeeze_strategy, self).OnStarted2(time)
        bb = BollingerBands()
        bb.Length = self.bollinger_period
        bb.Width = Decimal(self.bollinger_deviation)
        subscription = self.SubscribeCandles(self.candle_type)
        atr = None
        if self._use_atr_stop.Value:
            atr = AverageTrueRange()
            atr.Length = self._atr_period.Value
            subscription.BindEx(bb, atr, self._process_with_atr, False)
            for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
                quotes = Subscription(DataType.Level1, self.Security)
                quotes.MarketData.BuildField = field
                self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        else:
            subscription.BindEx(bb, self._process_without_atr, False)
        subscription.Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            if atr is not None:
                self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def _process_with_atr(self, candle, bb_value, atr_value):
        self._process_candle(candle, bb_value, atr_value.GetValue[Decimal](None))

    def _process_without_atr(self, candle, bb_value):
        self._process_candle(candle, bb_value, Decimal.Zero)

    def _process_candle(self, candle, bb_value, atr):
        if candle.State != CandleStates.Finished or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        upper = bb_value.UpBand
        lower = bb_value.LowBand
        middle = bb_value.MovingAverage

        if middle == 0:
            return
        band_width = (upper - lower) / middle

        if not self._has_prev_values:
            self._has_prev_values = True
            self._save_previous(candle.ClosePrice, upper, lower, band_width)
            return

        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            self._save_previous(candle.ClosePrice, upper, lower, band_width)
            return

        price = candle.ClosePrice
        narrow = self._prev_band_width <= Decimal(self._squeeze_threshold.Value)
        if narrow and self._prev_close <= self._prev_upper and price > upper and self.Position <= 0:
            self._enter(Sides.Buy, atr)
        elif narrow and self._prev_close >= self._prev_lower and price < lower and self.Position >= 0:
            self._enter(Sides.Sell, atr)
        elif self.Position > 0 and price <= middle:
            self.SellMarket(self.Position)
        elif self.Position < 0 and price >= middle:
            self.BuyMarket(Math.Abs(self.Position))

        self._save_previous(price, upper, lower, band_width)

    def _save_previous(self, close, upper, lower, width):
        self._prev_close = close
        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_band_width = width

    def _enter(self, side, atr):
        self._requested_distance = atr * Decimal(self._atr_multiplier.Value) if self._use_atr_stop.Value else Decimal.Zero
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
        order.Comment = "Squeeze signal"
        self._entry_order = order
        self.RegisterOrder(order)

    def _process_entry_fill(self, trade):
        # Preserve the old distance until an actual reversal fill changes direction.
        if (trade.Order == self._entry_order and self.Position != 0
                and (self.Position > 0) == (trade.Order.Side == Sides.Buy)):
            self._stop_distance.Value = self._requested_distance

    def CreateClone(self):
        return bollinger_squeeze_strategy()
