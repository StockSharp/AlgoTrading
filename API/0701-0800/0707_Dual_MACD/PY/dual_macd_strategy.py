import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class dual_macd_strategy(Strategy):
    """
    Dual MACD strategy.
    MACD 1 is the fast MACD and MACD 2 the slow one; a histogram is the MACD line minus its signal line. When the slow histogram
    crosses above zero while the fast histogram is positive the strategy goes long, and when it crosses below zero while the fast
    histogram is negative it goes short, reversing an opposite position. A long closes when the fast histogram turns negative and
    a short when it turns positive. Percent stop loss and take profit protect the position.
    """

    def __init__(self):
        super(dual_macd_strategy, self).__init__()
        self._macd1_fast_length = self.Param("Macd1FastLength", 34).SetGreaterThanZero().SetDisplay("MACD1 Fast", "Fast period of the fast MACD", "MACD 1")
        self._macd1_slow_length = self.Param("Macd1SlowLength", 144).SetGreaterThanZero().SetDisplay("MACD1 Slow", "Slow period of the fast MACD", "MACD 1")
        self._macd1_signal_length = self.Param("Macd1SignalLength", 9).SetGreaterThanZero().SetDisplay("MACD1 Signal", "Signal period of the fast MACD", "MACD 1")
        self._macd2_fast_length = self.Param("Macd2FastLength", 100).SetGreaterThanZero().SetDisplay("MACD2 Fast", "Fast period of the slow MACD", "MACD 2")
        self._macd2_slow_length = self.Param("Macd2SlowLength", 200).SetGreaterThanZero().SetDisplay("MACD2 Slow", "Slow period of the slow MACD", "MACD 2")
        self._macd2_signal_length = self.Param("Macd2SignalLength", 50).SetGreaterThanZero().SetDisplay("MACD2 Signal", "Signal period of the slow MACD", "MACD 2")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 1.5).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_slow_hist = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dual_macd_strategy, self).OnReseted()
        self._prev_slow_hist = None

    def OnStarted2(self, time):
        super(dual_macd_strategy, self).OnStarted2(time)

        self._prev_slow_hist = None

        fast_macd = MovingAverageConvergenceDivergenceSignal()
        fast_macd.Macd.ShortMa.Length = self._macd1_fast_length.Value
        fast_macd.Macd.LongMa.Length = self._macd1_slow_length.Value
        fast_macd.SignalMa.Length = self._macd1_signal_length.Value

        slow_macd = MovingAverageConvergenceDivergenceSignal()
        slow_macd.Macd.ShortMa.Length = self._macd2_fast_length.Value
        slow_macd.Macd.LongMa.Length = self._macd2_slow_length.Value
        slow_macd.SignalMa.Length = self._macd2_signal_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_macd, slow_macd, self._process_candle).Start()

        take_percent = Decimal(self._take_profit_percent.Value)
        stop_percent = Decimal(self._stop_loss_percent.Value)
        take = Unit(take_percent, UnitTypes.Percent) if take_percent > 0 else Unit()
        stop = Unit(stop_percent, UnitTypes.Percent) if stop_percent > 0 else Unit()
        self.StartProtection(take, stop, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, fast_macd)
                self.DrawIndicator(oscillators, slow_macd)

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        if not fast_value.IsFormed or fast_value.Macd is None or fast_value.Signal is None:
            return

        if not slow_value.IsFormed or slow_value.Macd is None or slow_value.Signal is None:
            return

        fast_hist = fast_value.Macd - fast_value.Signal
        slow_hist = slow_value.Macd - slow_value.Signal

        prev = self._prev_slow_hist
        self._prev_slow_hist = slow_hist

        if prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_up = prev <= 0 and slow_hist > 0
        cross_down = prev >= 0 and slow_hist < 0

        if cross_up and fast_hist > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and fast_hist < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and fast_hist < 0:
            self.SellMarket(self.Position)
        elif self.Position < 0 and fast_hist > 0:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return dual_macd_strategy()
