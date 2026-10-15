import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, OrderStates, OrderTypes, Sides, Level1Fields
from StockSharp.BusinessEntities import Subscription, Order
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class ma_deviation_strategy(Strategy):
    """
    MA Deviation strategy.
    Trades when price deviates significantly from its moving average.
    """

    def __init__(self):
        super(ma_deviation_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._deviation_percent = self.Param("DeviationPercent", 5.0).SetGreaterThanZero().SetDisplay("Deviation %", "Deviation percentage from MA required for entry", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Wilder ATR lookback for sizing and protection.", "Protection")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative() \
            .SetDisplay("ATR Multiplier", "Frozen entry ATR distance; zero disables stop and ATR sizing.", "Protection")
        self._risk_percent = self.Param("RiskPercent", 1.0).SetGreaterThanZero() \
            .SetDisplay("Risk (%)", "Portfolio value percentage budgeted against the entry ATR stop.", "Protection")

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
        super(ma_deviation_strategy, self).OnReseted()
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False

    def OnStarted2(self, time):
        super(ma_deviation_strategy, self).OnStarted2(time)

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, atr, self._process_candle, False).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def _process_candle(self, candle, ma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if not ma_val.IsFormed or not atr_val.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return

        mean = ma_val.GetValue[Decimal](None)
        if mean <= 0:
            return
        close = candle.ClosePrice
        deviation = Decimal(100) * (close - mean) / mean
        if self.Position > 0 and close >= mean:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close <= mean:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and Math.Abs(deviation) > Decimal(self._deviation_percent.Value):
            self._enter(Sides.Buy if deviation < 0 else Sides.Sell, close, atr_val.GetValue[Decimal](None))

    def _enter(self, side, close, atr):
        distance = atr * Decimal(self._atr_multiplier.Value)
        volume = self._calculate_volume(close, distance)
        if volume <= 0:
            return
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
        order.Volume = volume
        order.Comment = "MA deviation entry"
        self.RegisterOrder(order)

    def _calculate_volume(self, close, distance):
        if self._atr_multiplier.Value == 0:
            return self._normalize_volume(self.Volume)
        balance = self.Portfolio.CurrentValue
        if balance is None:
            balance = self.Portfolio.BeginValue
        if balance is None:
            balance = Decimal.Zero
        step_price = self.Security.StepPrice if self.Security.StepPrice is not None else Decimal.Zero
        price_step = self.Security.PriceStep if self.Security.PriceStep is not None else Decimal.Zero
        money_factor = step_price / price_step if step_price > 0 and price_step > 0 else self.Security.Multiplier
        if money_factor is None:
            money_factor = Decimal.One
        if balance <= 0 or distance <= 0 or close <= 0 or money_factor <= 0:
            return Decimal.Zero
        budget = balance * Decimal(self._risk_percent.Value) / Decimal(100)
        risk_volume = budget / (distance * money_factor)
        cash_volume = balance / (close * money_factor)
        return self._normalize_volume(Math.Min(risk_volume, cash_volume))

    def _normalize_volume(self, volume):
        minimum = self.Security.MinVolume if self.Security.MinVolume is not None else Decimal.Zero
        minimum = Math.Max(Decimal.Zero, minimum)
        maximum = self.Security.MaxVolume
        if maximum is None or maximum <= 0:
            maximum = Decimal.MaxValue
        volume = Math.Min(volume, maximum)
        step = self.Security.VolumeStep
        if step is not None and step > 0:
            minimum = Math.Ceiling(minimum / step) * step
            if maximum != Decimal.MaxValue:
                maximum = Math.Floor(maximum / step) * step
            volume = Math.Floor(volume / step) * step
        # Never round up through the risk budget to force a minimum-size trade.
        return volume if minimum <= maximum and volume >= minimum else Decimal.Zero

    def CreateClone(self):
        return ma_deviation_strategy()
