import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

# Delta-RSI events, matching the C# DeltaRsiConditions enum.
ZERO_CROSSING = 0
SIGNAL_LINE_CROSSING = 1
DIRECTION_CHANGE = 2


class delta_rsi_oscillator_strategy(Strategy):
    """
    Delta-RSI oscillator strategy.
    Delta-RSI is the bar-to-bar change of RSI and its signal line is an EMA of it.
    Entries follow BuyCondition: Delta-RSI crossing zero, crossing its signal line or changing direction;
    a bullish event opens a long, a bearish event a short (each side enabled by UseLong and UseShort), reversing an opposite position.
    Otherwise positions are closed by the opposite event of ExitCondition.
    """

    def __init__(self):
        super(delta_rsi_oscillator_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 21).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "Indicators")
        self._signal_length = self.Param("SignalLength", 9).SetGreaterThanZero().SetDisplay("Signal Length", "EMA length of the signal line", "Indicators")
        self._buy_condition = self.Param("BuyCondition", ZERO_CROSSING).SetDisplay("Entry Condition", "Event that opens positions: 0 zero crossing, 1 signal line crossing, 2 direction change", "Signals")
        self._exit_condition = self.Param("ExitCondition", ZERO_CROSSING).SetDisplay("Exit Condition", "Event that closes positions: 0 zero crossing, 1 signal line crossing, 2 direction change", "Signals")
        self._use_long = self.Param("UseLong", True).SetDisplay("Use Long", "Allow long trades", "Signals")
        self._use_short = self.Param("UseShort", True).SetDisplay("Use Short", "Allow short trades", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._signal = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _reset_state(self):
        self._prev_rsi = None
        self._prev_delta = None
        self._prev_prev_delta = None
        self._prev_signal = None

    def OnReseted(self):
        super(delta_rsi_oscillator_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(delta_rsi_oscillator_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        self._signal = ExponentialMovingAverage()
        self._signal.Length = self._signal_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_value.IsFormed:
            return

        rsi = float(rsi_value.GetValue[Decimal](None))
        prev_rsi = self._prev_rsi
        self._prev_rsi = rsi

        if prev_rsi is None:
            return

        delta = rsi - prev_rsi
        signal_value = process_float(self._signal, delta, candle.ServerTime, True)
        signal = float(signal_value.GetValue[Decimal](None))

        pd = self._prev_delta
        ppd = self._prev_prev_delta
        ps = self._prev_signal
        self._prev_prev_delta = self._prev_delta
        self._prev_delta = delta
        self._prev_signal = signal if signal_value.IsFormed else None

        if not signal_value.IsFormed or pd is None or ppd is None or ps is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        def is_bullish(condition):
            if condition == ZERO_CROSSING:
                return pd <= 0 and delta > 0
            if condition == SIGNAL_LINE_CROSSING:
                return pd <= ps and delta > signal
            return pd <= ppd and delta > pd

        def is_bearish(condition):
            if condition == ZERO_CROSSING:
                return pd >= 0 and delta < 0
            if condition == SIGNAL_LINE_CROSSING:
                return pd >= ps and delta < signal
            return pd >= ppd and delta < pd

        buy_condition = int(self._buy_condition.Value)
        exit_condition = int(self._exit_condition.Value)

        if self._use_long.Value and is_bullish(buy_condition) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self._use_short.Value and is_bearish(buy_condition) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and is_bearish(exit_condition):
            self.SellMarket(self.Position)
        elif self.Position < 0 and is_bullish(exit_condition):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return delta_rsi_oscillator_strategy()
