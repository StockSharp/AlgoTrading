import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vwap_macd_strategy(Strategy):
    """
    Strategy based on VWAP and MACD.
    Enters long when price is above VWAP and MACD crosses above Signal.
    Enters short when price is below VWAP and MACD crosses below Signal.
    Exits when MACD crosses its signal line in the opposite direction.
    """

    def __init__(self):
        super(vwap_macd_strategy, self).__init__()

        self._macd_fast_period = self.Param("MacdFastPeriod", 12) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD calculation", "Indicators")

        self._macd_slow_period = self.Param("MacdSlowPeriod", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD calculation", "Indicators")

        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("MACD Signal Period", "Signal line period for MACD calculation", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 30) \
            .SetRange(1, 200) \
            .SetDisplay("Cooldown Bars", "Bars between entries", "General")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss (%)", "Stop loss percentage from entry price", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Timeframe of data for strategy", "General")

        self._vwap = None
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_macd_strategy, self).OnReseted()
        self._vwap = None
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(vwap_macd_strategy, self).OnStarted(time)
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._cooldown = 0

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast_period.Value
        macd.Macd.LongMa.Length = self._macd_slow_period.Value
        macd.SignalMa.Length = self._macd_signal_period.Value

        self._vwap = VolumeWeightedMovingAverage()
        self._vwap.Length = self._macd_signal_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

            macd_area = self.CreateChartArea()
            if macd_area is not None:
                self.DrawIndicator(macd_area, macd)

    def ProcessCandle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        vwap = float(process_candle(self._vwap, candle))

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        macd = float(macd_value.Macd)
        signal = float(macd_value.Signal)

        macd_crossed_above = self._prev_macd <= self._prev_signal and macd > signal
        macd_crossed_below = self._prev_macd >= self._prev_signal and macd < signal

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_macd = macd
            self._prev_signal = signal
            return

        if self._cooldown > 0:
            self._cooldown -= 1

        cooldown_val = int(self._cooldown_bars.Value)

        if self._cooldown == 0 and float(candle.ClosePrice) > vwap * 1.001 and macd_crossed_above and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._cooldown = cooldown_val
        elif self._cooldown == 0 and float(candle.ClosePrice) < vwap * 0.999 and macd_crossed_below and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._cooldown = cooldown_val

        if self.Position > 0 and macd_crossed_below:
            self.ClosePosition()
            self._cooldown = cooldown_val
        elif self.Position < 0 and macd_crossed_above:
            self.ClosePosition()
            self._cooldown = cooldown_val

        self._prev_macd = macd
        self._prev_signal = signal

    def CreateClone(self):
        return vwap_macd_strategy()
