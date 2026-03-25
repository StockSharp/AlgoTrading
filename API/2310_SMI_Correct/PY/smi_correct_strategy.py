import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator, SimpleMovingAverage, DecimalIndicatorValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class smi_correct_strategy(Strategy):
    def __init__(self):
        super(smi_correct_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._smi_length = self.Param("SmiLength", 13) \
            .SetDisplay("SMI Length", "Period for SMI calculation", "SMI")
        self._signal_length = self.Param("SignalLength", 5) \
            .SetDisplay("Signal Length", "Smoothing period", "SMI")
        self._stochastic = None
        self._signal = None
        self._prev_smi = None
        self._prev_signal = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def smi_length(self):
        return self._smi_length.Value

    @property
    def signal_length(self):
        return self._signal_length.Value

    def OnReseted(self):
        super(smi_correct_strategy, self).OnReseted()
        self._stochastic = None
        self._signal = None
        self._prev_smi = None
        self._prev_signal = None

    def OnStarted(self, time):
        super(smi_correct_strategy, self).OnStarted(time)
        self._prev_smi = None
        self._prev_signal = None
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.smi_length
        self._stochastic.D.Length = 1
        self._signal = SimpleMovingAverage()
        self._signal.Length = self.signal_length
        self.Indicators.Add(self._stochastic)
        self.Indicators.Add(self._signal)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._stochastic)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        stoch_input = CandleIndicatorValue(self._stochastic, candle)
        stoch_input.IsFinal = True
        stoch_result = self._stochastic.Process(stoch_input)
        if not self._stochastic.IsFormed:
            return
        k = stoch_result.K
        if k is None:
            return
        k = float(k)
        signal_input = DecimalIndicatorValue(self._signal, k, candle.OpenTime)
        signal_input.IsFinal = True
        signal_result = self._signal.Process(signal_input)
        if not self._signal.IsFormed:
            self._prev_smi = k
            return
        signal = float(signal_result)
        if self._prev_smi is None or self._prev_signal is None:
            self._prev_smi = k
            self._prev_signal = signal
            return
        cross_up = self._prev_smi < self._prev_signal and k >= signal
        cross_down = self._prev_smi > self._prev_signal and k <= signal
        if cross_up and self.Position == 0:
            self.BuyMarket()
        elif cross_down and self.Position == 0:
            self.SellMarket()
        self._prev_smi = k
        self._prev_signal = signal

    def CreateClone(self):
        return smi_correct_strategy()
