import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ChandeMomentumOscillator
from StockSharp.Algo.Strategies import Strategy


class chande_momentum_oscillator_strategy(Strategy):
    """
    Chande Momentum Oscillator strategy.
    Long only: buys when CMO(CmoPeriod) is below LowerThreshold and closes the long when CMO rises above UpperThreshold or after
    MaxBarsInPosition candles.
    """

    def __init__(self):
        super(chande_momentum_oscillator_strategy, self).__init__()
        self._cmo_period = self.Param("CmoPeriod", 9).SetGreaterThanZero().SetDisplay("CMO Period", "CMO period", "CMO")
        self._lower_threshold = self.Param("LowerThreshold", -50.0).SetDisplay("Lower Threshold", "CMO level below which the strategy buys", "CMO")
        self._upper_threshold = self.Param("UpperThreshold", 50.0).SetDisplay("Upper Threshold", "CMO level above which the long closes", "CMO")
        self._max_bars_in_position = self.Param("MaxBarsInPosition", 5).SetGreaterThanZero().SetDisplay("Max Bars In Position", "Candles a position is held at most", "Exit")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bars_in_position = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(chande_momentum_oscillator_strategy, self).OnReseted()
        self._bars_in_position = 0

    def OnStarted2(self, time):
        super(chande_momentum_oscillator_strategy, self).OnStarted2(time)

        self._bars_in_position = 0

        cmo = ChandeMomentumOscillator()
        cmo.Length = self._cmo_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(cmo, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, cmo)

    def _process_candle(self, candle, cmo_value):
        if candle.State != CandleStates.Finished:
            return

        if self.Position > 0:
            self._bars_in_position += 1

        if not cmo_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cmo = cmo_value.GetValue[Decimal](None)

        if self.Position > 0:
            if cmo > Decimal(self._upper_threshold.Value) or self._bars_in_position >= self._max_bars_in_position.Value:
                self.SellMarket(self.Position)
            return

        if self.Position == 0 and cmo < Decimal(self._lower_threshold.Value):
            self.BuyMarket(self.Volume)
            self._bars_in_position = 0

    def CreateClone(self):
        return chande_momentum_oscillator_strategy()
