import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Array
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (MovingAverageConvergenceDivergenceSignal,
    RelativeStrengthIndex, StochasticOscillator, AwesomeOscillator,
    VolumeWeightedMovingAverage, SimpleMovingAverage, SmoothedMovingAverage, IIndicator)
from StockSharp.Algo.Strategies import Strategy


class bias_sentiment_strength_strategy(Strategy):
    def __init__(self):
        super(bias_sentiment_strength_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators")
        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal", "Signal period for MACD", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation length", "Indicators")
        self._stoch_k = self.Param("StochK", 21) \
            .SetDisplay("Stochastic K", "%K period for Stochastic", "Indicators")
        self._stoch_d = self.Param("StochD", 14) \
            .SetDisplay("Stochastic D", "%D period for Stochastic", "Indicators")
        self._ao_short = self.Param("AoShort", 5) \
            .SetDisplay("AO Short", "Short period for Awesome Oscillator", "Indicators")
        self._ao_long = self.Param("AoLong", 34) \
            .SetDisplay("AO Long", "Long period for Awesome Oscillator", "Indicators")
        self._volume_length = self.Param("VolumeLength", 30) \
            .SetDisplay("Volume Bias Length", "Length for VWMA/SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Time frame for strategy", "General")
        self._prev_bass = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bias_sentiment_strength_strategy, self).OnReseted()
        self._prev_bass = 0.0

    def OnStarted2(self, time):
        super(bias_sentiment_strength_strategy, self).OnStarted2(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        stoch = StochasticOscillator()
        stoch.K.Length = self._stoch_k.Value
        stoch.D.Length = self._stoch_d.Value
        ao = AwesomeOscillator()
        ao.ShortMa.Length = self._ao_short.Value
        ao.LongMa.Length = self._ao_long.Value
        vwma = VolumeWeightedMovingAverage()
        vwma.Length = self._volume_length.Value
        sma = SimpleMovingAverage()
        sma.Length = self._volume_length.Value
        jaw = SmoothedMovingAverage()
        jaw.Length = 13
        teeth = SmoothedMovingAverage()
        teeth.Length = 8
        lips = SmoothedMovingAverage()
        lips.Length = 5
        subscription = self.SubscribeCandles(self.candle_type)
        indicators = Array[IIndicator]([macd, rsi, stoch, ao, vwma, sma, jaw, teeth, lips])
        subscription.BindEx(indicators, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, values):
        if candle.State != CandleStates.Finished:
            return
        if values.Length < 9:
            return
        for i in range(values.Length):
            if values[i] is None or not values[i].IsFinal:
                return
        try:
            macd_val = values[0]
            macd_line = float(macd_val.Macd) if macd_val.Macd is not None else 0.0
            signal_line = float(macd_val.Signal) if macd_val.Signal is not None else 0.0
            macd_hist = (macd_line - signal_line) * 2.0

            rsi = float(values[1])
            rsi_hist = (rsi - 50.0) / 5.0

            stoch_val = values[2]
            stoch_k_val = stoch_val.K
            stoch_d_val = stoch_val.D
            if stoch_k_val is None or stoch_d_val is None:
                return
            stoch_hist = ((float(stoch_k_val) - float(stoch_d_val)) / 10.0) * 1.5

            ao_val = float(values[3]) * 0.6

            vwma_val = float(values[4])
            sma_val = float(values[5])
            volume_hist = vwma_val - sma_val

            jaw_val = float(values[6])
            teeth_val = float(values[7])
            lips_val = float(values[8])
            gator_hist = (lips_val - teeth_val) + (teeth_val - jaw_val)
        except Exception:
            return

        bass = macd_hist + rsi_hist + stoch_hist + ao_val + gator_hist + volume_hist

        if bass > 0 and self._prev_bass <= 0 and self.Position <= 0:
            self.BuyMarket()
        elif bass < 0 and self._prev_bass >= 0 and self.Position >= 0:
            self.SellMarket()

        self._prev_bass = bass

    def CreateClone(self):
        return bias_sentiment_strength_strategy()
