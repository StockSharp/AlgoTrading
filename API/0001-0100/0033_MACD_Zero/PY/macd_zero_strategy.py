import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class macd_zero_strategy(Strategy):
    """
    Enters while the MACD line approaches zero, before reaching it.
    Exits on either signal-line crossing and protects actual fills with a percent stop.
    """

    def __init__(self):
        super(macd_zero_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period for MACD", "MACD")
        self._slow_period = self.Param("SlowPeriod", 26).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period for MACD", "MACD")
        self._signal_period = self.Param("SignalPeriod", 9).SetGreaterThanZero().SetDisplay("Signal", "Signal line period for MACD", "MACD")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative() \
            .SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")

        self._prev_macd = Decimal.Zero
        self._prev_signal = Decimal.Zero
        self._has_prev = False
        self._pending_order = None

        self.OrderRegistering += self._track_pending

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_zero_strategy, self).OnReseted()
        self._prev_macd = Decimal.Zero
        self._prev_signal = Decimal.Zero
        self._has_prev = False
        self._pending_order = None

    def OnStarted2(self, time):
        super(macd_zero_strategy, self).OnStarted2(time)

        self._prev_macd = Decimal.Zero
        self._prev_signal = Decimal.Zero
        self._has_prev = False
        self._pending_order = None

        if self._fast_period.Value >= self._slow_period.Value:
            raise ValueError("FastPeriod must be below SlowPeriod.")
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_period.Value
        macd.Macd.LongMa.Length = self._slow_period.Value
        macd.SignalMa.Length = self._signal_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def _process_candle(self, candle, value):
        # Complex values snapshot formation before processing their inner indicators.
        if candle.State != CandleStates.Finished or not value.Indicator.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if value.Macd is None or value.Signal is None:
            return
        macd = value.Macd
        signal = value.Signal
        if not self._has_prev:
            self._has_prev = True
            self._prev_macd = macd
            self._prev_signal = signal
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            self._prev_macd = macd
            self._prev_signal = signal
            return
        crossed = (self._prev_macd <= self._prev_signal and macd > signal) or (self._prev_macd >= self._prev_signal and macd < signal)
        # The published exit is a crossing in either direction, not a zero-line exit.
        if self.Position != 0 and crossed:
            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and macd < 0 and macd > self._prev_macd:
            self.BuyMarket(self.Volume)
        elif self.Position == 0 and macd > 0 and macd < self._prev_macd:
            self.SellMarket(self.Volume)
        self._prev_macd = macd
        self._prev_signal = signal

    def CreateClone(self):
        return macd_zero_strategy()
