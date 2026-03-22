import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    CommodityChannelIndex,
    ExponentialMovingAverage,
    MovingAverageConvergenceDivergenceSignal
)
from StockSharp.Algo.Strategies import Strategy


class smc_trader_camel_cci_macd1_strategy(Strategy):
    """Strategy combining CCI and MACD signal crossover with EMA trend filter.
    Buy when MACD crosses above signal with CCI positive and price above EMA.
    Sell when MACD crosses below signal with CCI negative and price below EMA."""

    def __init__(self):
        super(smc_trader_camel_cci_macd1_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ema_length = self.Param("EmaLength", 34) \
            .SetDisplay("EMA Length", "Trend EMA period", "Indicators")
        self._macd_fast_period = self.Param("MacdFastPeriod", 12) \
            .SetDisplay("MACD Fast", "Fast EMA for MACD", "Indicators")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26) \
            .SetDisplay("MACD Slow", "Slow EMA for MACD", "Indicators")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("MACD Signal", "Signal line period", "Indicators")
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "CCI period", "Indicators")

        self._prev_macd_main = None
        self._prev_macd_signal = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @property
    def MacdFastPeriod(self):
        return self._macd_fast_period.Value

    @property
    def MacdSlowPeriod(self):
        return self._macd_slow_period.Value

    @property
    def MacdSignalPeriod(self):
        return self._macd_signal_period.Value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    def OnReseted(self):
        super(smc_trader_camel_cci_macd1_strategy, self).OnReseted()
        self._prev_macd_main = None
        self._prev_macd_signal = None

    def OnStarted(self, time):
        super(smc_trader_camel_cci_macd1_strategy, self).OnStarted(time)

        self._prev_macd_main = None
        self._prev_macd_signal = None

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.MacdFastPeriod
        macd.Macd.LongMa.Length = self.MacdSlowPeriod
        macd.SignalMa.Length = self.MacdSignalPeriod

        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, cci, ema, self._process_candle).Start()

    def _process_candle(self, candle, macd_value, cci_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFinal or not cci_value.IsFinal or not ema_value.IsFinal:
            return

        macd_raw = macd_value.Macd if hasattr(macd_value, 'Macd') else None
        signal_raw = macd_value.Signal if hasattr(macd_value, 'Signal') else None

        if macd_raw is None or signal_raw is None:
            return

        macd_main = float(macd_raw)
        macd_signal = float(signal_raw)

        cci_val = float(cci_value)
        ema_val = float(ema_value)

        if self._prev_macd_main is None or self._prev_macd_signal is None:
            self._prev_macd_main = macd_main
            self._prev_macd_signal = macd_signal
            return

        prev_main = self._prev_macd_main
        prev_signal = self._prev_macd_signal

        macd_bull_cross = prev_main <= prev_signal and macd_main > macd_signal
        macd_bear_cross = prev_main >= prev_signal and macd_main < macd_signal

        close = float(candle.ClosePrice)

        # Long: MACD bullish cross + CCI > 0 + price above EMA
        if self.Position <= 0 and macd_bull_cross and cci_val > 0 and close > ema_val:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Short: MACD bearish cross + CCI < 0 + price below EMA
        elif self.Position >= 0 and macd_bear_cross and cci_val < 0 and close < ema_val:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_macd_main = macd_main
        self._prev_macd_signal = macd_signal

    def CreateClone(self):
        return smc_trader_camel_cci_macd1_strategy()
