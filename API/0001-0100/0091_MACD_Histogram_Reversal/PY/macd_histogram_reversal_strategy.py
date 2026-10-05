import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceHistogram
from StockSharp.Algo.Strategies import Strategy

class macd_histogram_reversal_strategy(Strategy):
    """
    MACD Histogram Reversal strategy.
    The position turns long when the MACD histogram (MACD - Signal) turns positive and short when it turns negative;
    a zero histogram keeps its previous sign. A percent stop limits the loss.
    """

    def __init__(self):
        super(macd_histogram_reversal_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12).SetRange(8, 16).SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD")
        self._slow_period = self.Param("SlowPeriod", 26).SetRange(20, 30).SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD")
        self._signal_period = self.Param("SignalPeriod", 9).SetRange(7, 13).SetDisplay("Signal Period", "Signal line period", "MACD")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")

        self._prev_positive = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_histogram_reversal_strategy, self).OnReseted()
        self._prev_positive = None

    def OnStarted2(self, time):
        super(macd_histogram_reversal_strategy, self).OnStarted2(time)

        self._prev_positive = None

        macd_hist = MovingAverageConvergenceDivergenceHistogram()
        macd_hist.Macd.ShortMa.Length = self._fast_period.Value
        macd_hist.Macd.LongMa.Length = self._slow_period.Value
        macd_hist.SignalMa.Length = self._signal_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd_hist, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd_hist)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, macd_iv):
        if candle.State != CandleStates.Finished or not macd_iv.IsFormed:
            return
        if macd_iv.Macd is None or macd_iv.Signal is None:
            return

        histogram = macd_iv.Macd - macd_iv.Signal
        if histogram == 0:
            return

        positive = histogram > 0
        was_positive = self._prev_positive
        self._prev_positive = positive

        if was_positive is None or was_positive == positive or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if positive and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif not positive and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return macd_histogram_reversal_strategy()
