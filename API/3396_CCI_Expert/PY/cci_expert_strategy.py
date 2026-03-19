import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class cci_expert_strategy(Strategy):
    """
    CCI Expert strategy: CCI crossover with level-based signals.
    Buys when CCI stays above +100 for 2 bars, sells when below -100 for 2 bars.
    """

    def __init__(self):
        super(cci_expert_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI period", "Indicators")
        self._prev_cci = None
        self._prev_prev_cci = None

    @property
    def candle_type(self):
        return self._candle_type.Value
    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(cci_expert_strategy, self).OnReseted()
        self._prev_cci = None
        self._prev_prev_cci = None

    def OnStarted(self, time):
        super(cci_expert_strategy, self).OnStarted(time)
        self._prev_cci = None
        self._prev_prev_cci = None
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self.on_process).Start()

    def on_process(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_cci is not None and self._prev_prev_cci is not None:
            long_signal = cci_value > 100 and self._prev_cci > 100 and self._prev_prev_cci < 100
            short_signal = cci_value < -100 and self._prev_cci < -100 and self._prev_prev_cci > -100

            if long_signal and self.Position <= 0:
                self.BuyMarket()
            elif short_signal and self.Position >= 0:
                self.SellMarket()

        self._prev_prev_cci = self._prev_cci
        self._prev_cci = cci_value

    def CreateClone(self):
        return cci_expert_strategy()
