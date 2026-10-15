import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Array, ArgumentException, Decimal, Nullable, TimeSpan
from StockSharp.Messages import CandleStates, DataType, Level1Fields, OrderStates, OrderTypes, Sides
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Indicators import AverageTrueRange, IIndicator, RelativeMomentumIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class present_trend_rmi_synergy_strategy(Strategy):
    def __init__(self):
        super(present_trend_rmi_synergy_strategy, self).__init__()
        self._rmi_period = self.Param("RmiPeriod", 21).SetGreaterThanZero() \
            .SetDisplay("RMI Period", "Native rolling mean period of positive/negative momentum", "Indicators")
        self._rmi_momentum = self.Param("RmiMomentum", 3).SetGreaterThanZero() \
            .SetDisplay("RMI Momentum", "Closed-bar lag of each close-price momentum sample", "Indicators")
        self._super_trend_length = self.Param("SuperTrendLength", 5).SetGreaterThanZero() \
            .SetDisplay("SuperTrend Length", "Shared close SMA and Wilder ATR period for trailing bands", "Protection")
        self._super_trend_multiplier = self.Param("SuperTrendMultiplier", Decimal(4)).SetGreaterThanZero() \
            .SetDisplay("SuperTrend Multiplier", "ATR width of the ratcheted SMA price stop", "Protection")
        self._direction = self.Param[Nullable[Sides]]("Direction", None) \
            .SetDisplay("Direction", "Entry side filter: Buy for long, Sell for short, unset for both", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Finished candles supplying RMI, SMA and ATR", "General")
        self._reset_state()
        self.OrderRegistering += self._observe_order

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def _reset_state(self):
        self._stop_price = None
        self._pending_order = None

    def OnReseted(self):
        super(present_trend_rmi_synergy_strategy, self).OnReseted()
        self._reset_state()

    def _observe_order(self, order):
        self._pending_order = order

    def OnStarted2(self, time):
        super(present_trend_rmi_synergy_strategy, self).OnStarted2(time)
        direction = self._direction.Value
        if self._rmi_period.Value <= 0 or self._rmi_momentum.Value <= 0 or self._super_trend_length.Value <= 0 \
                or self._super_trend_multiplier.Value <= 0 \
                or direction is not None and direction != Sides.Buy and direction != Sides.Sell:
            raise ArgumentException("Periods and ATR multiplier must be positive; direction must be Buy, Sell or unset for both.")
        self._reset_state()
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_quote).Start()
        rmi = RelativeMomentumIndex()
        rmi.Length = self._rmi_period.Value
        rmi.MomentumPeriod = self._rmi_momentum.Value
        mean = SimpleMovingAverage()
        mean.Length = self._super_trend_length.Value
        atr = AverageTrueRange()
        atr.Length = self._super_trend_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(Array[IIndicator]([rmi, mean, atr]), self.OnProcess, True).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, mean)
            self.DrawOwnTrades(area)

    def _is_pending(self):
        order = self._pending_order
        return order is not None and order.State != OrderStates.Done and order.State != OrderStates.Failed

    def _submit(self, side, volume, comment):
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = volume
        order.Comment = comment
        self.RegisterOrder(order)

    def _try_stop_exit(self, price):
        stop = self._stop_price
        if self.Position == 0 or stop is None or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading():
            return False
        if price > stop if self.Position > 0 else price < stop:
            return False
        self._submit(Sides.Sell if self.Position > 0 else Sides.Buy, abs(self.Position), "RMI ATR trailing stop")
        return True

    def _observe_quote(self, quote):
        field = Level1Fields.BestBidPrice if self.Position > 0 else Level1Fields.BestAskPrice
        if quote.Changes.ContainsKey(field) and quote.Changes[field] > 0:
            self._try_stop_exit(quote.Changes[field])

    def OnProcess(self, candle, values):
        if candle.State != CandleStates.Finished or values[1].IsEmpty or not values[1].IsFormed \
                or values[2].IsEmpty or not values[2].IsFormed:
            return
        mean = values[1].GetValue[Decimal](None)
        width = values[2].GetValue[Decimal](None) * self._super_trend_multiplier.Value
        if self.Position != 0:
            candidate = mean - width if self.Position > 0 else mean + width
            self._stop_price = candidate if self._stop_price is None \
                else max(self._stop_price, candidate) if self.Position > 0 else min(self._stop_price, candidate)
            self._try_stop_exit(candle.ClosePrice)
            return
        if values[0].IsEmpty or not values[0].IsFormed or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading():
            return
        rmi = values[0].GetValue[Decimal](None)
        side = Sides.Buy if rmi > 60 and candle.ClosePrice > mean \
            else Sides.Sell if rmi < 40 and candle.ClosePrice < mean else None
        direction = self._direction.Value
        if side is None or direction is not None and side != direction:
            return
        self._stop_price = mean - width if side == Sides.Buy else mean + width
        self._submit(side, self.Volume, "RMI SMA aligned entry")

    def CreateClone(self):
        return present_trend_rmi_synergy_strategy()
