import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class cci_breakout_strategy(Strategy):
    """
    Strategy based on CCI (Commodity Channel Index) breakout.
    Buys when CCI crosses above +100, sells when CCI crosses below -100.
    """

    def __init__(self):
        super(cci_breakout_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "Period for CCI calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_cci = 0.0
        self._has_prev_values = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_breakout_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._has_prev_values = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(cci_breakout_strategy, self).OnStarted(time)

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(cci, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, cci_ind):
        if candle.State != CandleStates.Finished:
            return

        if cci_ind.IsEmpty:
            return

        try:
            cci_value = float(cci_ind)
        except:
            return

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_cci = cci_value
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_cci = cci_value
            return

        if self._prev_cci <= 100 and cci_value > 100 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._cooldown = 2
        elif self._prev_cci >= -100 and cci_value < -100 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._cooldown = 2

        self._prev_cci = cci_value

    def CreateClone(self):
        return cci_breakout_strategy()
