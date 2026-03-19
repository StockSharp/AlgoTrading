import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageDirectionalIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class hull_ma_adx_strategy(Strategy):
    """
    Hull MA + ADX trend strategy. Enters on HMA slope turn with ADX confirmation.
    """

    def __init__(self):
        super(hull_ma_adx_strategy, self).__init__()
        self._hma_period = self.Param("HmaPeriod", 9).SetDisplay("HMA Period", "Hull MA period", "Indicators")
        self._adx_period = self.Param("AdxPeriod", 14).SetDisplay("ADX Period", "ADX period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 80).SetDisplay("Cooldown", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_hma = 0.0
        self._has_prev_slope = False
        self._prev_slope_up = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hull_ma_adx_strategy, self).OnReseted()
        self._prev_hma = 0.0
        self._has_prev_slope = False
        self._prev_slope_up = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(hull_ma_adx_strategy, self).OnStarted(time)
        hma = HullMovingAverage()
        hma.Length = self._hma_period.Value
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        atr = AverageTrueRange()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(hma, adx, atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, hma_val, adx_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        hma = float(hma_val.ToDecimal())
        hma_increasing = hma > self._prev_hma
        hma_decreasing = hma < self._prev_hma
        if not self._has_prev_slope:
            self._has_prev_slope = True
            self._prev_slope_up = hma_increasing
            self._prev_hma = hma
            return
        if self._cooldown > 0:
            self._cooldown -= 1
        slope_turned_up = not self._prev_slope_up and hma_increasing
        slope_turned_down = self._prev_slope_up and hma_decreasing
        if self._cooldown == 0 and slope_turned_up and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self._cooldown == 0 and slope_turned_down and self.Position >= 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position != 0 and (slope_turned_up or slope_turned_down):
            if self.Position > 0:
                self.SellMarket()
            else:
                self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        self._prev_hma = hma
        self._prev_slope_up = hma_increasing

    def CreateClone(self):
        return hull_ma_adx_strategy()
