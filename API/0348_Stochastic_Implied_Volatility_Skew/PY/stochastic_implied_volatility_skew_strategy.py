import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator, SimpleMovingAverage, DecimalIndicatorValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class stochastic_implied_volatility_skew_strategy(Strategy):
    """Stochastic strategy filtered by deterministic implied-volatility skew regime changes."""

    def __init__(self):
        super(stochastic_implied_volatility_skew_strategy, self).__init__()

        self._stoch_length = self.Param("StochLength", 14) \
            .SetRange(5, 30) \
            .SetDisplay("Stoch Length", "Period for stochastic oscillator", "Indicators")

        self._stoch_k = self.Param("StochK", 3) \
            .SetRange(1, 10) \
            .SetDisplay("Stoch %K", "Smoothing for stochastic %K line", "Indicators")

        self._stoch_d = self.Param("StochD", 3) \
            .SetRange(1, 10) \
            .SetDisplay("Stoch %D", "Smoothing for stochastic %D line", "Indicators")

        self._iv_period = self.Param("IvPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("IV Period", "Period for IV skew averaging", "Options")

        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetRange(1.0, 5.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 18) \
            .SetNotNegative() \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stochastic = None
        self._iv_skew_sma = None
        self._current_iv_skew = 0.0
        self._avg_iv_skew = 0.0
        self._prev_k = None
        self._prev_high_skew = False
        self._prev_low_skew = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(stochastic_implied_volatility_skew_strategy, self).OnReseted()
        self._stochastic = None
        self._iv_skew_sma = None
        self._current_iv_skew = 0.0
        self._avg_iv_skew = 0.0
        self._prev_k = None
        self._prev_high_skew = False
        self._prev_low_skew = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(stochastic_implied_volatility_skew_strategy, self).OnStarted2(time)

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = int(self._stoch_length.Value)
        self._stochastic.D.Length = int(self._stoch_d.Value)

        self._iv_skew_sma = SimpleMovingAverage()
        self._iv_skew_sma.Length = int(self._iv_period.Value)

        self.Indicators.Add(self._stochastic)
        self.Indicators.Add(self._iv_skew_sma)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._stochastic)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(float(self._stop_loss.Value), UnitTypes.Percent)
        )

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self.SimulateIvSkew(candle)

        iv_sma_iv = DecimalIndicatorValue(self._iv_skew_sma, self._current_iv_skew, candle.OpenTime)
        iv_sma_iv.IsFinal = True
        iv_sma_result = self._iv_skew_sma.Process(iv_sma_iv)
        if not self._iv_skew_sma.IsFormed or iv_sma_result.IsEmpty:
            return

        self._avg_iv_skew = float(iv_sma_result)

        civ = CandleIndicatorValue(self._stochastic, candle)
        civ.IsFinal = True
        stoch_result = self._stochastic.Process(civ)
        if not self._stochastic.IsFormed:
            return

        stoch_k_val = stoch_result.K
        if stoch_k_val is None:
            return

        stoch_k = float(stoch_k_val)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        cooldown = int(self._cooldown_bars.Value)
        high_skew = self._current_iv_skew > self._avg_iv_skew
        low_skew = self._current_iv_skew < self._avg_iv_skew
        oversold = stoch_k < 25.0
        overbought = stoch_k > 75.0

        if self._cooldown_remaining == 0 and self.Position == 0 and oversold and high_skew:
            self.BuyMarket()
            self._cooldown_remaining = cooldown
        elif self._cooldown_remaining == 0 and self.Position == 0 and overbought and low_skew:
            self.SellMarket()
            self._cooldown_remaining = cooldown

        self._prev_k = stoch_k
        self._prev_high_skew = high_skew
        self._prev_low_skew = low_skew

    def SimulateIvSkew(self, candle):
        range_val = max(float(candle.HighPrice - candle.LowPrice), 1.0)
        body = float(candle.ClosePrice - candle.OpenPrice)
        range_ratio = range_val / max(float(candle.OpenPrice), 1.0)
        body_ratio = body / range_val

        self._current_iv_skew = (body_ratio * 0.2) - min(0.15, range_ratio * 10.0)

    def CreateClone(self):
        return stochastic_implied_volatility_skew_strategy()
