import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RateOfChange, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class dynamic_ticks_oscillator_model_strategy(Strategy):
    """
    Dynamic Ticks Oscillator Model strategy.
    Built for the NYSE Down Ticks index as the strategy security. The RocLength rate of change is compared with the standard
    deviation of that rate over VolatilityLookback candles: a long opens when the ROC drops below -StdDev * EntryStdDevMultiplier
    and closes when it rises above StdDev * ExitStdDevMultiplier.
    """

    def __init__(self):
        super(dynamic_ticks_oscillator_model_strategy, self).__init__()
        self._roc_length = self.Param("RocLength", 5).SetGreaterThanZero().SetDisplay("ROC Length", "Rate of change length", "Indicators")
        self._volatility_lookback = self.Param("VolatilityLookback", 24).SetGreaterThanZero().SetDisplay("Volatility Lookback", "Standard deviation length of the ROC", "Indicators")
        self._entry_std_dev_multiplier = self.Param("EntryStdDevMultiplier", 1.6).SetGreaterThanZero().SetDisplay("Entry StdDev Mult", "Standard deviation multiplier of the entry threshold", "Signals")
        self._exit_std_dev_multiplier = self.Param("ExitStdDevMultiplier", 1.4).SetGreaterThanZero().SetDisplay("Exit StdDev Mult", "Standard deviation multiplier of the exit threshold", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._roc_std_dev = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dynamic_ticks_oscillator_model_strategy, self).OnReseted()
        self._roc_std_dev = None

    def OnStarted2(self, time):
        super(dynamic_ticks_oscillator_model_strategy, self).OnStarted2(time)

        roc = RateOfChange()
        roc.Length = self._roc_length.Value
        self._roc_std_dev = StandardDeviation()
        self._roc_std_dev.Length = self._volatility_lookback.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(roc, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, roc)

    def _process_candle(self, candle, roc_value):
        if candle.State != CandleStates.Finished:
            return

        # The deviation is measured on the ROC series itself.
        std_result = process_float(self._roc_std_dev, roc_value, candle.OpenTime, True)

        if not std_result.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        roc = float(roc_value)
        std = float(std_result)

        if self.Position == 0 and roc < -std * float(self._entry_std_dev_multiplier.Value):
            self.BuyMarket(self.Volume)
        elif self.Position > 0 and roc > std * float(self._exit_std_dev_multiplier.Value):
            self.SellMarket(self.Position)

    def CreateClone(self):
        return dynamic_ticks_oscillator_model_strategy()
