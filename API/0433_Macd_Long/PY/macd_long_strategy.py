import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Array, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class macd_long_strategy(Strategy):
    """MACD Long Strategy.

    Combines RSI extremes with MACD crossovers.
    After RSI drops below an oversold level the strategy waits for a
    bullish MACD crossover to open a long position. After RSI rises above
    an overbought level a bearish crossover can trigger a short.
    Positions are reversed on opposite signals.
    """

    def __init__(self):
        super(macd_long_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)).SetDisplay(
            "Candle type", "Candle type for strategy calculation.", "General"
        )

        self._rsi_length = self.Param("RsiLength", 14).SetDisplay(
            "RSI Length", "RSI period", "RSI"
        )
        self._rsi_over_sold = self.Param("RsiOversold", 30).SetDisplay(
            "RSI Oversold", "Oversold level", "RSI"
        )
        self._rsi_over_bought = self.Param("RsiOverbought", 70).SetDisplay(
            "RSI Overbought", "Overbought level", "RSI"
        )

        self._macd_fast = self.Param("MacdFast", 12).SetDisplay(
            "MACD Fast Length", "Fast MA period", "MACD"
        )
        self._macd_slow = self.Param("MacdSlow", 26).SetDisplay(
            "MACD Slow Length", "Slow MA period", "MACD"
        )
        self._macd_signal = self.Param("MacdSignal", 9).SetDisplay(
            "MACD Signal Length", "Signal period", "MACD"
        )

        self._lookback = self.Param("LookbackBars", 10).SetDisplay(
            "Lookback Bars", "Bars to check for RSI extremes", "Strategy"
        )

        self._rsi = None
        self._macd = None
        self._bars_since_oversold = 1000
        self._bars_since_overbought = 1000

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(macd_long_strategy, self).OnReseted()
        self._bars_since_overbought = 1000
        self._bars_since_oversold = 1000

    def OnStarted(self, time):
        super(macd_long_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self._macd_fast.Value
        self._macd.Macd.LongMa.Length = self._macd_slow.Value
        self._macd.SignalMa.Length = self._macd_signal.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._rsi, self._macd, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_value, macd_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._rsi.IsFormed or not self._macd.IsFormed:
            return

        rsi = float(rsi_value)
        if rsi <= self._rsi_over_sold.Value:
            self._bars_since_oversold = 0
        else:
            self._bars_since_oversold += 1

        if rsi >= self._rsi_over_bought.Value:
            self._bars_since_overbought = 0
        else:
            self._bars_since_overbought += 1

        macd_data = macd_value
        macd_line = macd_data.Macd
        signal_line = macd_data.Signal

        prev = self._macd.GetValue[MovingAverageConvergenceDivergenceSignal](1)
        prev_macd = prev.Macd
        prev_signal = prev.Signal

        crossover = macd_line > signal_line and prev_macd <= prev_signal
        crossunder = macd_line < signal_line and prev_macd >= prev_signal

        buy_signal = crossover and self._bars_since_oversold <= self._lookback.Value
        sell_signal = crossunder and self._bars_since_overbought <= self._lookback.Value

        if buy_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif sell_signal and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif sell_signal and self.Position > 0:
            self.ClosePosition()
        elif buy_signal and self.Position < 0:
            self.ClosePosition()

    def CreateClone(self):
        return macd_long_strategy()
