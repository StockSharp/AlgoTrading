import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    RelativeStrengthIndex, StochasticOscillator, BollingerBands,
    AverageDirectionalIndex, MovingAverageConvergenceDivergenceSignal
)
from StockSharp.Algo.Strategies import Strategy


class multi_ea_v12_strategy(Strategy):
    """5-indicator consensus strategy (RSI, Stochastic, Bollinger, ADX, MACD)."""

    def __init__(self):
        super(multi_ea_v12_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._required_confirmations = self.Param("RequiredConfirmations", 3) \
            .SetDisplay("Required Confirmations", "Number of modules required for entry", "Consensus")

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI length", "RSI")
        self._rsi_upper = self.Param("RsiUpper", 65.0) \
            .SetDisplay("RSI Upper", "Overbought level", "RSI")
        self._rsi_lower = self.Param("RsiLower", 35.0) \
            .SetDisplay("RSI Lower", "Oversold level", "RSI")

        self._stoch_k_period = self.Param("StochKPeriod", 10) \
            .SetDisplay("Stochastic %K", "%K period", "Stochastic")
        self._stoch_d_period = self.Param("StochDPeriod", 3) \
            .SetDisplay("Stochastic %D", "%D period", "Stochastic")
        self._stoch_upper = self.Param("StochUpper", 70.0) \
            .SetDisplay("Stoch Upper", "Overbought", "Stochastic")
        self._stoch_lower = self.Param("StochLower", 30.0) \
            .SetDisplay("Stoch Lower", "Oversold", "Stochastic")

        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "BB length", "Bollinger")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "BB width", "Bollinger")

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "ADX length", "ADX")
        self._adx_trend_level = self.Param("AdxTrendLevel", 20.0) \
            .SetDisplay("ADX Trend Level", "Min ADX for trend", "ADX")

        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast", "Fast EMA period", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow", "Slow EMA period", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal", "Signal line period", "MACD")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RequiredConfirmations(self):
        return self._required_confirmations.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def RsiUpper(self):
        return self._rsi_upper.Value

    @property
    def RsiLower(self):
        return self._rsi_lower.Value

    @property
    def StochKPeriod(self):
        return self._stoch_k_period.Value

    @property
    def StochDPeriod(self):
        return self._stoch_d_period.Value

    @property
    def StochUpper(self):
        return self._stoch_upper.Value

    @property
    def StochLower(self):
        return self._stoch_lower.Value

    @property
    def BollingerPeriod(self):
        return self._bollinger_period.Value

    @property
    def BollingerDeviation(self):
        return self._bollinger_deviation.Value

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @property
    def AdxTrendLevel(self):
        return self._adx_trend_level.Value

    @property
    def MacdFast(self):
        return self._macd_fast.Value

    @property
    def MacdSlow(self):
        return self._macd_slow.Value

    @property
    def MacdSignal(self):
        return self._macd_signal.Value

    def OnStarted2(self, time):
        super(multi_ea_v12_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.StochKPeriod
        stochastic.D.Length = self.StochDPeriod

        bollinger = BollingerBands()
        bollinger.Length = self.BollingerPeriod
        bollinger.Width = float(self.BollingerDeviation)

        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.MacdFast
        macd.Macd.LongMa.Length = self.MacdSlow
        macd.SignalMa.Length = self.MacdSignal

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(rsi, stochastic, bollinger, adx, macd, self._process_candle).Start()

    def _process_candle(self, candle, rsi_val, stoch_val, bb_val, adx_val, macd_val):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_val.IsFormed or not stoch_val.IsFormed or not bb_val.IsFormed or \
                not adx_val.IsFormed or not macd_val.IsFormed:
            return

        rsi = float(rsi_val)

        stoch_k_raw = stoch_val.K
        stoch_k = float(stoch_k_raw) if stoch_k_raw is not None else 50.0

        bb_upper_raw = bb_val.UpBand
        bb_lower_raw = bb_val.LowBand
        bb_upper = float(bb_upper_raw) if bb_upper_raw is not None else float(candle.ClosePrice)
        bb_lower = float(bb_lower_raw) if bb_lower_raw is not None else float(candle.ClosePrice)

        adx_ma_raw = adx_val.MovingAverage
        adx_main = float(adx_ma_raw) if adx_ma_raw is not None else 0.0
        adx_plus_raw = adx_val.Dx.Plus
        adx_minus_raw = adx_val.Dx.Minus
        adx_plus = float(adx_plus_raw) if adx_plus_raw is not None else 0.0
        adx_minus = float(adx_minus_raw) if adx_minus_raw is not None else 0.0

        macd_line_raw = macd_val.Macd
        macd_signal_raw = macd_val.Signal
        macd_line = float(macd_line_raw) if macd_line_raw is not None else 0.0
        macd_signal_line = float(macd_signal_raw) if macd_signal_raw is not None else 0.0

        close = float(candle.ClosePrice)

        bullish = 0
        bearish = 0

        # Module 1: RSI
        if rsi < float(self.RsiLower):
            bullish += 1
        elif rsi > float(self.RsiUpper):
            bearish += 1

        # Module 2: Stochastic
        if stoch_k < float(self.StochLower):
            bullish += 1
        elif stoch_k > float(self.StochUpper):
            bearish += 1

        # Module 3: Bollinger Bands
        if close <= bb_lower:
            bullish += 1
        elif close >= bb_upper:
            bearish += 1

        # Module 4: ADX directional
        if adx_main >= float(self.AdxTrendLevel):
            if adx_plus > adx_minus:
                bullish += 1
            elif adx_minus > adx_plus:
                bearish += 1

        # Module 5: MACD
        if macd_line > macd_signal_line and macd_line > 0:
            bullish += 1
        elif macd_line < macd_signal_line and macd_line < 0:
            bearish += 1

        min_confirmations = self.RequiredConfirmations

        # Enter on consensus
        if bullish >= min_confirmations and bearish == 0 and self.Position <= 0:
            self.BuyMarket()
        elif bearish >= min_confirmations and bullish == 0 and self.Position >= 0:
            self.SellMarket()
        # Exit when consensus breaks
        elif self.Position > 0 and bearish >= 2:
            self.SellMarket()
        elif self.Position < 0 and bullish >= 2:
            self.BuyMarket()

    def CreateClone(self):
        return multi_ea_v12_strategy()
