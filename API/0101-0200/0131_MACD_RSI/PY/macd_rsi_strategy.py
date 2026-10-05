import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class macd_rsi_strategy(Strategy):
    """
    MACD RSI strategy.
    RSI falling below RsiOversold arms a long and rising above RsiOverbought arms a short; the latest extreme wins.
    A MACD cross above its signal line confirms the rise from oversold and goes long, a cross below confirms the fall
    from overbought and goes short, reversing an opposite position. Each extreme confirms one cross. A percent stop limits the loss.
    """

    def __init__(self):
        super(macd_rsi_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal line period of MACD", "MACD")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period of RSI", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI level that arms a long", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI level that arms a short", "RSI")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_above = None
        # The latest RSI extreme not yet confirmed by MACD: 1 oversold, -1 overbought, 0 none.
        self._zone = 0

    def OnReseted(self):
        super(macd_rsi_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(macd_rsi_strategy, self).OnStarted2(time)

        self._reset_state()

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, rsi, self._process_candle).Start()

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
                self.DrawIndicator(oscillators, rsi)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, macd_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if rsi_value.IsFormed:
            rsi = rsi_value.GetValue[Decimal](None)
            if rsi < Decimal(self._rsi_oversold.Value):
                self._zone = 1
            elif rsi > Decimal(self._rsi_overbought.Value):
                self._zone = -1

        if not macd_value.IsFormed:
            return
        if macd_value.Macd is None or macd_value.Signal is None:
            return

        is_above = macd_value.Macd > macd_value.Signal
        was_above = self._prev_above
        self._prev_above = is_above

        if was_above is None or was_above == is_above:
            return

        side = 1 if is_above else -1

        # Each extreme confirms one cross.
        if self._zone != side:
            return

        self._zone = 0

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if side > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif side < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return macd_rsi_strategy()
