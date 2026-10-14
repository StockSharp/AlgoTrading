import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class atr_macd_strategy(Strategy):
    """
    ATR MACD strategy.
    A MACD cross above its signal line goes long and a cross below goes short, the opposite cross reversing the position.
    Each new position is sized as Volume times the average of the last AtrAvgPeriod ATR values divided by the current ATR,
    rounded down to the volume step, so higher volatility trades smaller. A percent stop limits the loss.
    """

    def __init__(self):
        super(atr_macd_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal line period of MACD", "MACD")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of ATR", "Sizing")
        self._atr_avg_period = self.Param("AtrAvgPeriod", 20).SetGreaterThanZero().SetDisplay("ATR Average Period", "ATR values the typical volatility is averaged over", "Sizing")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_above = None
        self._atrs = []

    def OnReseted(self):
        super(atr_macd_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(atr_macd_strategy, self).OnStarted2(time)

        self._reset_state()

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, atr, self._process_candle).Start()

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
                self.DrawIndicator(oscillators, macd)
                self.DrawIndicator(oscillators, atr)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, macd_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        avg_period = self._atr_avg_period.Value
        if atr_value.IsFormed:
            self._atrs.append(atr_value.GetValue[Decimal](None))
            if len(self._atrs) > avg_period:
                self._atrs.pop(0)

        if not macd_value.IsFormed:
            return
        if macd_value.Macd is None or macd_value.Signal is None:
            return

        is_above = macd_value.Macd > macd_value.Signal
        was_above = self._prev_above
        self._prev_above = is_above

        if was_above is None or was_above == is_above or len(self._atrs) < avg_period:
            return

        current_atr = self._atrs[-1]
        if current_atr <= 0 or not self.IsFormedAndOnlineAndAllowTrading():
            return

        total = Decimal(0)
        for value in self._atrs:
            total += value
        size = self.Volume * (total / Decimal(avg_period)) / current_atr

        step = self.Security.VolumeStep if self.Security is not None else None
        if step is not None and step > 0:
            size = Math.Max(step, Math.Floor(size / step) * step)

        if is_above and self.Position <= 0:
            self.BuyMarket(size + abs(self.Position))
        elif not is_above and self.Position >= 0:
            self.SellMarket(size + abs(self.Position))

    def CreateClone(self):
        return atr_macd_strategy()
