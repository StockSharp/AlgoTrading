import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import AverageDirectionalIndex, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class adx_macd_strategy(Strategy):
    """
    ADX MACD strategy.
    While flat, a MACD cross above its signal line goes long and a cross below goes short, but only while ADX is rising.
    The position closes once ADX weakens below its previous value or MACD returns to the other side of its signal line,
    and a percent stop limits the loss.
    """

    def __init__(self):
        super(adx_macd_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero().SetDisplay("ADX Period", "Period of ADX", "ADX")
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal line period of MACD", "MACD")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_above = None
        self._prev_adx = None

    def OnReseted(self):
        super(adx_macd_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(adx_macd_strategy, self).OnStarted2(time)

        self._reset_state()

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, macd, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)
                self.DrawIndicator(oscillators, macd)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, adx_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not adx_value.IsFormed or not macd_value.IsFormed:
            return
        if adx_value.MovingAverage is None or macd_value.Macd is None or macd_value.Signal is None:
            return

        strength = adx_value.MovingAverage
        is_above = macd_value.Macd > macd_value.Signal
        was_above = self._prev_above
        prev_adx = self._prev_adx
        self._prev_above = is_above
        self._prev_adx = strength

        if was_above is None or prev_adx is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rising = strength > prev_adx

        if self.Position > 0:
            if not rising or not is_above:
                self.SellMarket(self.Position)
        elif self.Position < 0:
            if not rising or is_above:
                self.BuyMarket(-self.Position)
        elif rising and was_above != is_above:
            if is_above:
                self.BuyMarket(self.Volume)
            else:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return adx_macd_strategy()
