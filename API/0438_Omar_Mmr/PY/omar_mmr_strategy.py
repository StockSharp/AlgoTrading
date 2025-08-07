import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (
    RelativeStrengthIndex,
    ExponentialMovingAverage,
    MovingAverageConvergenceDivergenceSignal,
)
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class omar_mmr_strategy(Strategy):
    """Omar MMR strategy combining RSI, three EMAs and MACD crossover.

    Enters long when price is above the slow EMA, the fast EMA is above the
    medium EMA, MACD crosses above its signal line and RSI is between 29
    and 70. Uses fixed take-profit and stop-loss percentages.
    """

    def __init__(self):
        super(omar_mmr_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)).SetDisplay(
            "Candle type", "Candle type for strategy calculation.", "General"
        )
        self._rsi_length = self.Param("RsiLength", 14).SetDisplay(
            "RSI Length", "RSI period", "RSI"
        )
        self._ema_a_length = self.Param("EmaALength", 20).SetDisplay(
            "EMA A Length", "First EMA period", "Moving Averages"
        )
        self._ema_b_length = self.Param("EmaBLength", 50).SetDisplay(
            "EMA B Length", "Second EMA period", "Moving Averages"
        )
        self._ema_c_length = self.Param("EmaCLength", 200).SetDisplay(
            "EMA C Length", "Third EMA period", "Moving Averages"
        )
        self._macd_fast = self.Param("MacdFastLength", 12).SetDisplay(
            "MACD Fast Length", "Fast MA period", "MACD"
        )
        self._macd_slow = self.Param("MacdSlowLength", 26).SetDisplay(
            "MACD Slow Length", "Slow MA period", "MACD"
        )
        self._macd_signal = self.Param("MacdSignalLength", 9).SetDisplay(
            "MACD Signal Length", "Signal period", "MACD"
        )
        self._tp_percent = self.Param("TakeProfitPercent", 1.5).SetDisplay(
            "Take Profit %", "Take profit percentage", "Strategy"
        )
        self._sl_percent = self.Param("StopLossPercent", 2.0).SetDisplay(
            "Stop Loss %", "Stop loss percentage", "Strategy"
        )

        self._rsi = None
        self._ema_a = None
        self._ema_b = None
        self._ema_c = None
        self._macd = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(omar_mmr_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value

        self._ema_a = ExponentialMovingAverage()
        self._ema_a.Length = self._ema_a_length.Value
        self._ema_b = ExponentialMovingAverage()
        self._ema_b.Length = self._ema_b_length.Value
        self._ema_c = ExponentialMovingAverage()
        self._ema_c.Length = self._ema_c_length.Value

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self._macd_fast.Value
        self._macd.Macd.LongMa.Length = self._macd_slow.Value
        self._macd.SignalMa.Length = self._macd_signal.Value

        sub = self.SubscribeCandles(self.candle_type)
        sub.BindEx(self._rsi, self._macd, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._ema_a)
            self.DrawIndicator(area, self._ema_b)
            self.DrawIndicator(area, self._ema_c)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(self._tp_percent.Value, UnitTypes.Percent),
            Unit(self._sl_percent.Value, UnitTypes.Percent),
        )

    def OnProcess(self, candle, rsi_val, macd_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._rsi.IsFormed or not self._ema_a.IsFormed or not self._ema_b.IsFormed or not self._ema_c.IsFormed or not self._macd.IsFormed:
            return

        ema_a = self._ema_a.Process(candle).ToDecimal()
        ema_b = self._ema_b.Process(candle).ToDecimal()
        ema_c = self._ema_c.Process(candle).ToDecimal()

        macd_line = macd_val.Macd
        signal_line = macd_val.Signal
        prev = self._macd.GetValue(1)
        prev_macd = prev.Macd
        prev_signal = prev.Signal
        macd_cross = macd_line > signal_line and prev_macd <= prev_signal

        rsi = rsi_val.ToDecimal()

        long_entry = (
            candle.ClosePrice > ema_c
            and ema_a > ema_b
            and macd_cross
            and rsi > 29
            and rsi < 70
        )

        if long_entry and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return omar_mmr_strategy()
