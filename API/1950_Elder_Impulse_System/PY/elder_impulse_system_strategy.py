import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class elder_impulse_system_strategy(Strategy):

    def __init__(self):
        super(elder_impulse_system_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 13) \
            .SetDisplay("EMA Period", "Period for EMA calculation", "Indicators")
        self._macd_fast_period = self.Param("MacdFastPeriod", 12) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("MACD Signal Period", "Signal EMA period for MACD", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 1) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._previous_ema = None
        self._previous_macd_hist = None
        self._previous_color = None
        self._bars_since_trade = 0

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def MacdFastPeriod(self):
        return self._macd_fast_period.Value

    @MacdFastPeriod.setter
    def MacdFastPeriod(self, value):
        self._macd_fast_period.Value = value

    @property
    def MacdSlowPeriod(self):
        return self._macd_slow_period.Value

    @MacdSlowPeriod.setter
    def MacdSlowPeriod(self, value):
        self._macd_slow_period.Value = value

    @property
    def MacdSignalPeriod(self):
        return self._macd_signal_period.Value

    @MacdSignalPeriod.setter
    def MacdSignalPeriod(self, value):
        self._macd_signal_period.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(elder_impulse_system_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.MacdFastPeriod
        macd.Macd.LongMa.Length = self.MacdSlowPeriod
        macd.SignalMa.Length = self.MacdSignalPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .BindEx(ema, macd, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        ema = float(ema_value)

        macd_raw = macd_value.Macd
        signal_raw = macd_value.Signal
        if macd_raw is None or signal_raw is None:
            return

        macd_val = float(macd_raw)
        signal_val = float(signal_raw)
        macd_hist = macd_val - signal_val

        if self._previous_ema is None or self._previous_macd_hist is None:
            self._previous_ema = ema
            self._previous_macd_hist = macd_hist
            return

        ema_delta = ema - self._previous_ema
        macd_delta = macd_hist - self._previous_macd_hist
        color = 0

        if ema_delta > 0 and macd_hist > 0 and macd_delta > 0:
            color = 2
        elif ema_delta < 0 and macd_hist < 0 and macd_delta < 0:
            color = 1

        if self._previous_color is not None and self._bars_since_trade >= self.CooldownBars:
            if self._previous_color == 2 and color != 2:
                pos = self.Position
                if pos < 0:
                    self.BuyMarket(abs(pos))

                if self.Position <= 0:
                    self.BuyMarket(self.Volume + abs(self.Position))
                    self._bars_since_trade = 0

            elif self._previous_color == 1 and color != 1:
                pos = self.Position
                if pos > 0:
                    self.SellMarket(abs(pos))

                if self.Position >= 0:
                    self.SellMarket(self.Volume + abs(self.Position))
                    self._bars_since_trade = 0

        self._previous_color = color
        self._previous_ema = ema
        self._previous_macd_hist = macd_hist

    def OnReseted(self):
        super(elder_impulse_system_strategy, self).OnReseted()
        self._previous_ema = None
        self._previous_macd_hist = None
        self._previous_color = None
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return elder_impulse_system_strategy()
