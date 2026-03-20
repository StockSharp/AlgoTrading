import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class rmacd_reversal_strategy(Strategy):
    # Entry modes
    BREAKDOWN = 0
    MACD_TWIST = 1
    SIGNAL_TWIST = 2
    MACD_DISPOSITION = 3

    def __init__(self):
        super(rmacd_reversal_strategy, self).__init__()
        self._signal_length = self.Param("SignalLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Length", "Signal smoothing period", "Indicator")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._mode = self.Param("Mode", 0) \
            .SetDisplay("Mode", "Entry algorithm (0=Breakdown,1=MacdTwist,2=SignalTwist,3=MacdDisposition)", "Trading")
        self._prev_macd = 0.0
        self._prev_macd2 = 0.0
        self._prev_signal = 0.0
        self._prev_signal2 = 0.0
        self._initialized = 0
        self._bars_since_trade = 0

    @property
    def signal_length(self):
        return self._signal_length.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def mode(self):
        return self._mode.Value

    def OnReseted(self):
        super(rmacd_reversal_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._prev_macd2 = 0.0
        self._prev_signal = 0.0
        self._prev_signal2 = 0.0
        self._initialized = 0
        self._bars_since_trade = self.cooldown_bars

    def OnStarted(self, time):
        super(rmacd_reversal_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.SignalMa.Length = self.signal_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self.process_macd).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def process_macd(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        if not macd_value.IsFormed:
            return
        self._bars_since_trade += 1
        macd_v = macd_value.Macd
        signal_v = macd_value.Signal
        if macd_v is None or signal_v is None:
            return
        macd = float(macd_v)
        signal = float(signal_v)
        if self._initialized == 0:
            self._prev_macd = macd
            self._prev_signal = signal
            self._initialized = 1
            return
        elif self._initialized == 1:
            self._prev_macd2 = self._prev_macd
            self._prev_signal2 = self._prev_signal
            self._prev_macd = macd
            self._prev_signal = signal
            self._initialized = 2
            return
        buy = False
        sell = False
        m = self.mode
        if m == self.BREAKDOWN:
            buy = self._prev_macd > 0.0 and macd <= 0.0
            sell = self._prev_macd < 0.0 and macd >= 0.0
        elif m == self.MACD_TWIST:
            buy = self._prev_macd < self._prev_macd2 and macd > self._prev_macd
            sell = self._prev_macd > self._prev_macd2 and macd < self._prev_macd
        elif m == self.SIGNAL_TWIST:
            buy = self._prev_signal < self._prev_signal2 and signal > self._prev_signal
            sell = self._prev_signal > self._prev_signal2 and signal < self._prev_signal
        elif m == self.MACD_DISPOSITION:
            buy = self._prev_macd > self._prev_signal and macd <= signal
            sell = self._prev_macd < self._prev_signal and macd >= signal
        if buy and self.Position <= 0 and self._bars_since_trade >= self.cooldown_bars:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._bars_since_trade = 0
        elif sell and self.Position >= 0 and self._bars_since_trade >= self.cooldown_bars:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._bars_since_trade = 0
        self._prev_macd2 = self._prev_macd
        self._prev_signal2 = self._prev_signal
        self._prev_macd = macd
        self._prev_signal = signal

    def CreateClone(self):
        return rmacd_reversal_strategy()
