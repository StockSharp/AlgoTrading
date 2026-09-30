import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, OrderStates, OrderTypes, Sides, Level1Fields
from StockSharp.BusinessEntities import Subscription, Order
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class keltner_channel_reversal_strategy(Strategy):
    """Fade a directional outside close with separate EMA/ATR lengths and frozen ATR protection."""

    def __init__(self):
        super(keltner_channel_reversal_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetGreaterThanZero().SetDisplay("EMA Period", "Close EMA length for middle band", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Wilder ATR length for channel width and stop", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "ATR multiple for channel width", "Indicators")
        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 2.0).SetNotNegative().SetDisplay("Stop ATR Multiplier", "Frozen entry ATR distance; zero disables the stop", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Keltner and stop candle timeframe", "General")
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

    def OnReseted(self):
        super(keltner_channel_reversal_strategy, self).OnReseted()
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False

    def OnStarted2(self, time):
        super(keltner_channel_reversal_strategy, self).OnStarted2(time)
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        candles = self.SubscribeCandles(self.candle_type)
        candles.BindEx(ema, atr, self._process_candle, False).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection evaluates executable quotes between finished signal candles.
        pass

    def _process_candle(self, candle, ema_value, atr_value):
        if (candle.State != CandleStates.Finished or not ema_value.IsFormed or
                not atr_value.IsFormed or not self.IsFormedAndOnlineAndAllowTrading()):
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        middle = ema_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        lower = middle - atr * Decimal(self._atr_multiplier.Value)
        upper = middle + atr * Decimal(self._atr_multiplier.Value)
        close = candle.ClosePrice
        if self.Position > 0 and close >= middle:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close <= middle:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and close < lower and close > candle.OpenPrice:
            self._enter(Sides.Buy, atr)
        elif self.Position == 0 and close > upper and close < candle.OpenPrice:
            self._enter(Sides.Sell, atr)

    def _enter(self, side, atr):
        distance = atr * Decimal(self._stop_loss_atr_multiplier.Value)
        if self._stop_distance is None:
            self._stop_distance = Unit(distance)
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
        order.Comment = "Keltner reversal entry"
        self.RegisterOrder(order)

    def CreateClone(self):
        return keltner_channel_reversal_strategy()
