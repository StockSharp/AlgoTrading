import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class supertrend_stochastic_strategy(Strategy):
    """
    Supertrend Stochastic strategy.
    A close above the Supertrend line with %K below StochOversold goes long and a close below it with %K above StochOverbought goes short,
    reversing an opposite position; %K is the stochastic over StochPeriod candles smoothed over StochK candles. The Supertrend line is the trailing stop: a long closes when Supertrend flips down and a short
    when it flips up.
    """

    def __init__(self):
        super(supertrend_stochastic_strategy, self).__init__()
        self._supertrend_period = self.Param("SupertrendPeriod", 10).SetGreaterThanZero().SetDisplay("Supertrend Period", "ATR period of Supertrend", "Supertrend")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0).SetGreaterThanZero().SetDisplay("Supertrend Multiplier", "ATR multiplier of Supertrend", "Supertrend")
        self._stoch_period = self.Param("StochPeriod", 14).SetGreaterThanZero().SetDisplay("Stochastic Period", "Lookback period of the raw stochastic", "Stochastic")
        self._stoch_k = self.Param("StochK", 3).SetGreaterThanZero().SetDisplay("Stochastic %K", "Smoothing period of %K", "Stochastic")
        self._stoch_oversold = self.Param("StochOversold", 20.0).SetDisplay("Stochastic Oversold", "%K level for longs", "Stochastic")
        self._stoch_overbought = self.Param("StochOverbought", 80.0).SetDisplay("Stochastic Overbought", "%K level for shorts", "Stochastic")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(supertrend_stochastic_strategy, self).OnStarted2(time)

        supertrend = SuperTrend()
        supertrend.Length = self._supertrend_period.Value
        supertrend.Multiplier = Decimal(self._supertrend_multiplier.Value)
        # The D line of the core oscillator is the smoothed %K.
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_period.Value
        stochastic.D.Length = self._stoch_k.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(supertrend, stochastic, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, stochastic)

    def _process_candle(self, candle, supertrend_value, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        if not supertrend_value.IsFormed or not stochastic_value.IsFormed or stochastic_value.D is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        line = supertrend_value.Value
        is_up_trend = supertrend_value.IsUpTrend
        k = stochastic_value.D
        close = candle.ClosePrice

        if close > line and k < Decimal(self._stoch_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < line and k > Decimal(self._stoch_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and not is_up_trend:
            self.SellMarket(self.Position)
        elif self.Position < 0 and is_up_trend:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return supertrend_stochastic_strategy()
