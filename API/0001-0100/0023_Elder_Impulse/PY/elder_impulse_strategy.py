import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class elder_impulse_strategy(Strategy):
    """
    Strategy based on Elder's Impulse System.
    Uses EMA direction and MACD histogram to determine impulse.
    Green (bullish): EMA rising + MACD histogram rising -> buy
    Red (bearish): EMA falling + MACD histogram falling -> sell
    """

    def __init__(self):
        super(elder_impulse_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 13).SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period for EMA calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._macd_fast_period = self.Param("MacdFastPeriod", 12).SetGreaterThanZero() \
            .SetDisplay("MACD Fast Period", "Fast EMA period.", "Indicators")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26).SetGreaterThanZero() \
            .SetDisplay("MACD Slow Period", "Slow EMA period.", "Indicators")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9).SetGreaterThanZero() \
            .SetDisplay("MACD Signal Period", "Signal EMA period.", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative() \
            .SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")

        self._prev_ema = Decimal.Zero
        self._prev_histogram = Decimal.Zero
        self._has_prev_values = False
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
        super(elder_impulse_strategy, self).OnReseted()
        self._prev_ema = Decimal.Zero
        self._prev_histogram = Decimal.Zero
        self._has_prev_values = False
        self._pending_order = None

    def OnStarted2(self, time):
        super(elder_impulse_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        macd_signal = MovingAverageConvergenceDivergenceSignal()
        macd_signal.Macd.ShortMa.Length = self._macd_fast_period.Value
        macd_signal.Macd.LongMa.Length = self._macd_slow_period.Value
        macd_signal.SignalMa.Length = self._macd_signal_period.Value
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, macd_signal, self._process_candle, False).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, macd_signal)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def _process_candle(self, candle, ema_value, macd_value):
        if candle.State != CandleStates.Finished or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if ema_value.IsEmpty:
            return

        ema_dec = ema_value.GetValue[Decimal](None)
        if ema_dec == Decimal.Zero:
            return

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal
        if macd_line is None or signal_line is None:
            return

        histogram = macd_line - signal_line

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_ema = ema_dec
            self._prev_histogram = histogram
            return

        if ema_dec > self._prev_ema and histogram > self._prev_histogram:
            impulse = 1
        elif ema_dec < self._prev_ema and histogram < self._prev_histogram:
            impulse = -1
        else:
            impulse = 0

        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            self._prev_ema = ema_dec
            self._prev_histogram = histogram
            return

        if impulse == 1 and candle.ClosePrice > ema_dec and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif impulse == -1 and candle.ClosePrice < ema_dec and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif self.Position > 0 and impulse != 1:
            self.SellMarket(self.Position)
        elif self.Position < 0 and impulse != -1:
            self.BuyMarket(Math.Abs(self.Position))

        self._prev_ema = ema_dec
        self._prev_histogram = histogram

    def CreateClone(self):
        return elder_impulse_strategy()
