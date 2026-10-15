import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields, Sides
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class macd_divergence_strategy(Strategy):
    """
    Compares confirmed price pivots with their MACD-line values.
    Enters only on a later MACD/signal cross; fully exits on reverse cross or stop.
    """

    def __init__(self):
        super(macd_divergence_strategy, self).__init__()
        self._fast_macd_period = self.Param("FastMacdPeriod", 12).SetGreaterThanZero().SetDisplay("Fast MACD Period", "Fast close EMA length", "Indicators")
        self._slow_macd_period = self.Param("SlowMacdPeriod", 26).SetGreaterThanZero().SetDisplay("Slow MACD Period", "Slow close EMA length", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 9).SetGreaterThanZero().SetDisplay("Signal Period", "MACD signal EMA length", "Indicators")
        self._divergence_period = self.Param("DivergencePeriod", 5).SetRange(3, 31).SetDisplay("Divergence Period", "Odd-width confirmed pivot window and maximum bars to wait for a cross", "Pattern")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "MACD and pivot timeframe", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._clear_state()
        self.OrderRegistering += self._track_pending

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order
        if self.Position != 0 and order.Side == (Sides.Sell if self.Position > 0 else Sides.Buy):
            self._bullish_until = self._bearish_until = 0

    def _clear_state(self):
        self._window = []
        self._last_low = None
        self._last_high = None
        self._previous_macd = None
        self._previous_signal = None
        self._bar = 0
        self._bullish_until = 0
        self._bearish_until = 0
        self._pending_order = None

    def OnReseted(self):
        super(macd_divergence_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        super(macd_divergence_strategy, self).OnStarted2(time)
        if self._fast_macd_period.Value >= self._slow_macd_period.Value or self._divergence_period.Value % 2 != 1:
            raise ValueError("FastMacdPeriod must be below SlowMacdPeriod and DivergencePeriod must be odd.")
        self._clear_state()
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_macd_period.Value
        macd.Macd.LongMa.Length = self._slow_macd_period.Value
        macd.SignalMa.Length = self._signal_period.Value
        candles = self.SubscribeCandles(self.candle_type)
        candles.BindEx(macd, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native actual-fill protection evaluates executable quotes between signal candles.
        pass

    def _process_candle(self, candle, output):
        if candle.State != CandleStates.Finished or not output.Indicator.IsFormed:
            return
        line = output.Macd
        signal = output.Signal
        if line is None or signal is None:
            return
        self._bar += 1
        up_cross = (self._previous_macd is not None and self._previous_signal is not None and
                    self._previous_macd <= self._previous_signal and line > signal)
        down_cross = (self._previous_macd is not None and self._previous_signal is not None and
                      self._previous_macd >= self._previous_signal and line < signal)
        bullish_ready = self._bullish_until >= self._bar
        bearish_ready = self._bearish_until >= self._bar
        self._previous_macd = line
        self._previous_signal = signal
        self._window.append((candle.HighPrice, candle.LowPrice, line))
        width = self._divergence_period.Value
        if len(self._window) == width:
            center_index = width // 2
            center = self._window[center_index]
            if all(center[1] < bar[1] for i, bar in enumerate(self._window) if i != center_index):
                divergence = self._last_low is not None and center[1] < self._last_low[0] and center[2] > self._last_low[1]
                self._last_low = (center[1], center[2])
                self._bullish_until = self._bar + width if divergence else 0
            if all(center[0] > bar[0] for i, bar in enumerate(self._window) if i != center_index):
                divergence = self._last_high is not None and center[0] > self._last_high[0] and center[2] < self._last_high[1]
                self._last_high = (center[0], center[2])
                self._bearish_until = self._bar + width if divergence else 0
            self._window.pop(0)
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if self.Position > 0 and down_cross:
            self.SellMarket(self.Position)
            self._bullish_until = self._bearish_until = 0
        elif self.Position < 0 and up_cross:
            self.BuyMarket(Math.Abs(self.Position))
            self._bullish_until = self._bearish_until = 0
        elif self.Position == 0:
            if bullish_ready and up_cross:
                self.BuyMarket(self.Volume)
                self._bullish_until = self._bearish_until = 0
            elif bearish_ready and down_cross:
                self.SellMarket(self.Volume)
                self._bullish_until = self._bearish_until = 0

    def CreateClone(self):
        return macd_divergence_strategy()
