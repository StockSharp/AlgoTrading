import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class macd_trend_strategy(Strategy):
    """
    MACD Trend: enters long on MACD cross above signal, short on cross below,
    reversing on the opposite cross, and protects the position with a percent stop.
    """

    def __init__(self):
        super(macd_trend_strategy, self).__init__()
        self._fast_ema = self.Param("FastEmaPeriod", 12).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema = self.Param("SlowEmaPeriod", 26).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 9).SetGreaterThanZero().SetDisplay("Signal Period", "Signal line period", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Stop loss as a percentage of entry price", "Risk parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        # Side of the signal line on the previous candle; unknown until the indicator forms.
        self._prev_above = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_trend_strategy, self).OnReseted()
        self._prev_above = None

    def OnStarted2(self, time):
        super(macd_trend_strategy, self).OnStarted2(time)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_ema.Value
        macd.Macd.LongMa.Length = self._slow_ema.Value
        macd.SignalMa.Length = self._signal_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished or not macd_value.IsFormed:
            return
        if macd_value.Macd is None or macd_value.Signal is None:
            return

        is_above = macd_value.Macd > macd_value.Signal
        was_above = self._prev_above
        self._prev_above = is_above

        if was_above is None or was_above == is_above:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if is_above and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif not is_above and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return macd_trend_strategy()
