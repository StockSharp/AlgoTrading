import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, OrderStates, OrderTypes, Sides, Level1Fields
from StockSharp.BusinessEntities import Subscription, Order
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class bollinger_band_reversal_strategy(Strategy):
    """Fade an outside Bollinger close with an opposing candle and frozen ATR protection."""

    def __init__(self):
        super(bollinger_band_reversal_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("Bollinger Period", "Close SMA and population deviation length", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetNotNegative().SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Wilder ATR length", "Protection")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "Frozen signal ATR distance; zero disables the stop", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Bollinger and ATR candle timeframe", "General")
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
        super(bollinger_band_reversal_strategy, self).OnReseted()
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False

    def OnStarted2(self, time):
        super(bollinger_band_reversal_strategy, self).OnStarted2(time)
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False
        bands = BollingerBands()
        bands.Length = self._bollinger_period.Value
        bands.Width = Decimal(self._bollinger_deviation.Value)
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        candles = self.SubscribeCandles(self.candle_type)
        candles.BindEx(bands, atr, self._process_candle, False).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, bands)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection evaluates executable quotes between finished signal candles.
        pass

    def _process_candle(self, candle, bands_value, atr_value):
        if (candle.State != CandleStates.Finished or not bands_value.IsFormed or
                not atr_value.IsFormed or not self.IsFormedAndOnlineAndAllowTrading()):
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        upper = bands_value.UpBand
        lower = bands_value.LowBand
        middle = bands_value.MovingAverage
        if upper is None or lower is None or middle is None:
            return
        close = candle.ClosePrice
        if self.Position > 0 and close >= middle:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close <= middle:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and close < lower and close > candle.OpenPrice:
            self._enter(Sides.Buy, atr_value.GetValue[Decimal](None))
        elif self.Position == 0 and close > upper and close < candle.OpenPrice:
            self._enter(Sides.Sell, atr_value.GetValue[Decimal](None))

    def _enter(self, side, atr):
        distance = atr * Decimal(self._atr_multiplier.Value)
        if self._stop_distance is None:
            self._stop_distance = Unit(distance)
        # Cached native controllers retain this Unit across flat-to-position cycles.
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
        order.Comment = "Bollinger reversal entry"
        self.RegisterOrder(order)

    def CreateClone(self):
        return bollinger_band_reversal_strategy()
