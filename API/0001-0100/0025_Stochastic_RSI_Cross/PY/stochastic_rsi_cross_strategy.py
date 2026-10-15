import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class stochastic_rsi_cross_strategy(Strategy):
    """
    Sequential RSI normalization and smoothed StochRSI K/D crossover strategy.
    Buys when %K crosses above %D in oversold zone.
    Sells when %K crosses below %D in overbought zone.
    """

    def __init__(self):
        super(stochastic_rsi_cross_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 3).SetGreaterThanZero().SetDisplay("K Period", "SMA period of raw StochRSI.", "Indicators")
        self._d_period = self.Param("DPeriod", 3).SetGreaterThanZero().SetDisplay("D Period", "SMA period of formed K values.", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period of native close-price RSI.", "Indicators")
        self._stoch_period = self.Param("StochPeriod", 14).SetGreaterThanZero() \
            .SetDisplay("Stochastic Period", "Rolling range of formed RSI values.", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative() \
            .SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")

        self._prev_k = Decimal.Zero
        self._prev_d = Decimal.Zero
        self._has_prev = False
        self._pending_order = None
        self._rsi_window = []
        self._rsi = None
        self._k_average = None
        self._d_average = None
        self.OrderRegistering += self._track_pending

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stochastic_rsi_cross_strategy, self).OnReseted()
        self._prev_k = Decimal.Zero
        self._prev_d = Decimal.Zero
        self._has_prev = False
        self._pending_order = None
        self._rsi_window = []
        self._rsi = None
        self._k_average = None
        self._d_average = None

    def OnStarted2(self, time):
        super(stochastic_rsi_cross_strategy, self).OnStarted2(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_period.Value
        self._k_average = SimpleMovingAverage()
        self._k_average.Length = self._k_period.Value
        self._d_average = SimpleMovingAverage()
        self._d_average.Length = self._d_period.Value
        self.Indicators.Add(self._k_average)
        self.Indicators.Add(self._d_average)
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawIndicator(area, self._k_average)
            self.DrawIndicator(area, self._d_average)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    @staticmethod
    def _feed(average, value, time):
        indicator_input = DecimalIndicatorValue(average, value, time)
        indicator_input.IsFinal = True
        return average.Process(indicator_input).GetValue[Decimal](None)

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished or not self._rsi.IsFormed:
            return

        self._rsi_window.append(rsi_value)
        if len(self._rsi_window) > self._stoch_period.Value:
            del self._rsi_window[0]
        if len(self._rsi_window) < self._stoch_period.Value:
            return

        low = min(self._rsi_window)
        high = max(self._rsi_window)
        raw = Decimal(50) if high == low else Decimal(100) * (rsi_value - low) / (high - low)
        k = self._feed(self._k_average, raw, candle.OpenTime)
        if not self._k_average.IsFormed:
            return
        d = self._feed(self._d_average, k, candle.OpenTime)
        if not self._d_average.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Quantize decisions only; the D SMA receives the unrounded K value.
        k = Decimal.Round(k, 8)
        d = Decimal.Round(d, 8)

        if not self._has_prev:
            self._has_prev = True
            self._prev_k = k
            self._prev_d = d
            return

        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            self._prev_k = k
            self._prev_d = d
            return

        crossed_up = self._prev_k <= self._prev_d and k > d
        crossed_down = self._prev_k >= self._prev_d and k < d
        if crossed_up and k < 20 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif crossed_down and k > 80 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif self.Position > 0 and crossed_down:
            self.SellMarket(self.Position)
        elif self.Position < 0 and crossed_up:
            self.BuyMarket(Math.Abs(self.Position))

        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return stochastic_rsi_cross_strategy()
