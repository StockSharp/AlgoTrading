import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (Momentum, RateOfChange, RelativeStrengthIndex, StochasticOscillator, AwesomeOscillator,
                                        Highest, StandardDeviation, DecimalIndicatorValue)
from StockSharp.Algo.Strategies import Strategy


class logistic_rsi_stoch_roc_ao_strategy(Strategy):
    """
    Logistic RSI, Stochastic, ROC, AO strategy.
    The selected indicator is centred around zero and scaled by its highest absolute value over Length bars, then passed through
    the logistic map x * (1 - |x|). The standard deviation of the mapped values over Length bars, signed by the latest mapped value,
    goes long when it crosses above zero and short when it crosses below zero, reversing any open position.
    Indicator: LogisticDominance, Roc, Rsi, Stochastic or AwesomeOscillator.
    """

    def __init__(self):
        super(logistic_rsi_stoch_roc_ao_strategy, self).__init__()
        self._indicator = self.Param("Indicator", "LogisticDominance").SetDisplay("Indicator", "Indicator fed into the logistic map", "General")
        self._length = self.Param("Length", 13).SetGreaterThanZero().SetDisplay("Length", "Scaling and standard deviation length", "Indicators")
        self._len_ld = self.Param("LenLd", 5).SetGreaterThanZero().SetDisplay("LD Length", "Momentum length of the logistic dominance source", "Indicators")
        self._len_roc = self.Param("LenRoc", 9).SetGreaterThanZero().SetDisplay("ROC Length", "ROC length", "Indicators")
        self._len_rsi = self.Param("LenRsi", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "Indicators")
        self._len_sto = self.Param("LenSto", 14).SetGreaterThanZero().SetDisplay("Stochastic Length", "Stochastic length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._scale = None
        self._deviation = None
        self._prev_signed = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(logistic_rsi_stoch_roc_ao_strategy, self).OnReseted()
        self._prev_signed = None

    def OnStarted2(self, time):
        super(logistic_rsi_stoch_roc_ao_strategy, self).OnStarted2(time)

        self._prev_signed = None

        momentum = Momentum()
        momentum.Length = self._len_ld.Value
        roc = RateOfChange()
        roc.Length = self._len_roc.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._len_rsi.Value
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._len_sto.Value
        stochastic.D.Length = 3
        ao = AwesomeOscillator()
        self._scale = Highest()
        self._scale.Length = self._length.Value
        self._deviation = StandardDeviation()
        self._deviation.Length = self._length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(momentum, roc, rsi, stochastic, ao, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _source(self, momentum_value, roc_value, rsi_value, stochastic_value, ao_value):
        indicator = str(self._indicator.Value)
        if indicator == "LogisticDominance":
            return momentum_value.GetValue[Decimal](None) if momentum_value.IsFormed else None
        if indicator == "Roc":
            return roc_value.GetValue[Decimal](None) if roc_value.IsFormed else None
        if indicator == "Rsi":
            return rsi_value.GetValue[Decimal](None) - Decimal(50) if rsi_value.IsFormed else None
        if indicator == "Stochastic":
            if not stochastic_value.IsFormed or stochastic_value.K is None:
                return None
            return Decimal(stochastic_value.K) - Decimal(50)
        if indicator == "AwesomeOscillator":
            return ao_value.GetValue[Decimal](None) if ao_value.IsFormed else None
        return None

    def _process_candle(self, candle, momentum_value, roc_value, rsi_value, stochastic_value, ao_value):
        if candle.State != CandleStates.Finished:
            return

        value = self._source(momentum_value, roc_value, rsi_value, stochastic_value, ao_value)
        if value is None:
            return

        scale_input = DecimalIndicatorValue(self._scale, abs(value), candle.OpenTime)
        scale_input.IsFinal = True
        scale_value = self._scale.Process(scale_input)
        if not scale_value.IsFormed:
            return

        scale = scale_value.GetValue[Decimal](None)
        x = value / scale if scale > 0 else Decimal(0)
        mapped = x * (Decimal(1) - abs(x))

        deviation_input = DecimalIndicatorValue(self._deviation, mapped, candle.OpenTime)
        deviation_input.IsFinal = True
        deviation_value = self._deviation.Process(deviation_input)
        if not deviation_value.IsFormed:
            return

        sign = 1 if mapped > 0 else (-1 if mapped < 0 else 0)
        signed = deviation_value.GetValue[Decimal](None) * Decimal(sign)
        prev = self._prev_signed
        self._prev_signed = signed

        if prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if prev <= 0 and signed > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif prev >= 0 and signed < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return logistic_rsi_stoch_roc_ao_strategy()
