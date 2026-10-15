import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class parabolic_sar_stochastic_strategy(Strategy):
    """
    Parabolic SAR Stochastic strategy.
    A close above the Parabolic SAR with %K below StochOversold goes long and a close below the SAR with %K above StochOverbought goes short,
    reversing an opposite position; %K is the stochastic over StochPeriod candles smoothed over StochK candles. The SAR is the trailing stop:
    a long closes when the SAR flips above price and a short when it flips below.
    """

    def __init__(self):
        super(parabolic_sar_stochastic_strategy, self).__init__()
        self._acceleration_factor = self.Param("AccelerationFactor", 0.02).SetGreaterThanZero().SetDisplay("SAR Acceleration", "Initial acceleration factor of the SAR", "SAR")
        self._max_acceleration_factor = self.Param("MaxAccelerationFactor", 0.2).SetGreaterThanZero().SetDisplay("SAR Max Acceleration", "Maximum acceleration factor of the SAR", "SAR")
        self._stoch_k = self.Param("StochK", 3).SetGreaterThanZero().SetDisplay("Stochastic %K", "Smoothing period of %K", "Stochastic")
        self._stoch_period = self.Param("StochPeriod", 14).SetGreaterThanZero().SetDisplay("Stochastic Period", "Lookback period of the raw stochastic", "Stochastic")
        self._stoch_oversold = self.Param("StochOversold", 20.0).SetDisplay("Stochastic Oversold", "%K level for longs", "Stochastic")
        self._stoch_overbought = self.Param("StochOverbought", 80.0).SetDisplay("Stochastic Overbought", "%K level for shorts", "Stochastic")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(parabolic_sar_stochastic_strategy, self).OnStarted2(time)

        sar = ParabolicSar()
        sar.Acceleration = Decimal(self._acceleration_factor.Value)
        sar.AccelerationMax = Decimal(self._max_acceleration_factor.Value)
        # The D line of the core oscillator is the smoothed %K.
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_period.Value
        stochastic.D.Length = self._stoch_k.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sar, stochastic, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, stochastic)

    def _process_candle(self, candle, sar_value, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        # The first SAR value is formed but empty.
        if not sar_value.IsFormed or sar_value.IsEmpty or not stochastic_value.IsFormed or stochastic_value.D is None:
            return

        sar = sar_value.GetValue[Decimal](None)
        k = stochastic_value.D
        close = candle.ClosePrice

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if close > sar and k < Decimal(self._stoch_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < sar and k > Decimal(self._stoch_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close < sar:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > sar:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return parabolic_sar_stochastic_strategy()
