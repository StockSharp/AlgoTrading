import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, OrderStates, OrderTypes, Sides, Level1Fields
from StockSharp.BusinessEntities import Subscription, Order
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class keltner_reversion_strategy(Strategy):
    """
    Keltner Channel mean reversion strategy.
    Buys below lower band with RSI oversold, sells above upper band with RSI overbought,
    exits on return inside with ATR protection.
    """

    def __init__(self):
        super(keltner_reversion_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetGreaterThanZero().SetDisplay("EMA Period", "Period for EMA calculation (middle band)", "Technical Parameters")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Wilder ATR lookback for channel width and protection", "Technical Parameters")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiplier for Keltner Channel width", "Technical Parameters")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period of the RSI confirming entries", "Technical Parameters")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetRange(0.0, 100.0).SetDisplay("RSI Oversold", "Long entries below the lower band require RSI below this level", "Technical Parameters")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetRange(0.0, 100.0).SetDisplay("RSI Overbought", "Short entries above the upper band require RSI above this level", "Technical Parameters")
        self._stop_loss_atr = self.Param("StopLossAtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier (Stop Loss)", "Frozen entry ATR stop distance; zero disables protection", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "Technical Parameters")

        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False
        self.OrderRegistering += self._track_pending

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(keltner_reversion_strategy, self).OnReseted()
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False

    def OnStarted2(self, time):
        super(keltner_reversion_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, atr, rsi, self._process_candle, False).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, atr)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def _process_candle(self, candle, ema_val, atr_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if not ema_val.IsFormed or not atr_val.IsFormed or not rsi_val.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return

        ev = ema_val.GetValue[Decimal](None)
        av = atr_val.GetValue[Decimal](None)
        rv = rsi_val.GetValue[Decimal](None)
        mult = Decimal(self._atr_multiplier.Value)
        upper = ev + av * mult
        lower = ev - av * mult
        close = candle.ClosePrice
        if self.Position > 0 and close >= lower:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close <= upper:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and close < lower and rv < Decimal(self._rsi_oversold.Value):
            self._enter(Sides.Buy, av)
        elif self.Position == 0 and close > upper and rv > Decimal(self._rsi_overbought.Value):
            self._enter(Sides.Sell, av)

    def _enter(self, side, atr):
        distance = atr * Decimal(self._stop_loss_atr.Value)
        if self._stop_distance is None:
            self._stop_distance = Unit(distance)
        # Update the same Unit retained by native cached controllers between flat entries.
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
        order.Comment = "Keltner reversion entry"
        self.RegisterOrder(order)

    def CreateClone(self):
        return keltner_reversion_strategy()
