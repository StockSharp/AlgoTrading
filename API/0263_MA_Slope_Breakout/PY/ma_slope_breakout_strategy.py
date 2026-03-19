import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_slope_breakout_strategy(Strategy):
    """
    MA slope breakout. Enters when slope exceeds avg + k*stddev.
    """

    def __init__(self):
        super(ma_slope_breakout_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 20).SetDisplay("MA Length", "SMA period", "Indicators")
        self._lookback = self.Param("LookbackPeriod", 20).SetDisplay("Lookback", "Slope stats period", "Strategy")
        self._dev_mult = self.Param("DeviationMultiplier", 2.0).SetDisplay("Dev Mult", "Stddev multiplier", "Strategy")
        self._sl_pct = self.Param("StopLossPercent", 2.0).SetDisplay("SL %", "Stop loss percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_ma = 0.0
        self._slopes = []
        self._is_init = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_slope_breakout_strategy, self).OnReseted()
        self._prev_ma = 0.0
        self._slopes = []
        self._is_init = False

    def OnStarted(self, time):
        super(ma_slope_breakout_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self._ma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()
        self.StartProtection(None, Unit(self._sl_pct.Value, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return
        ma = float(ma_val)
        if not self._is_init:
            self._prev_ma = ma
            self._is_init = True
            return
        slope = ma - self._prev_ma
        lb = self._lookback.Value
        self._slopes.append(slope)
        if len(self._slopes) > lb:
            self._slopes.pop(0)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_ma = ma
            return
        if len(self._slopes) < lb:
            self._prev_ma = ma
            return
        avg = sum(self._slopes) / lb
        var = sum((s - avg) ** 2 for s in self._slopes) / lb
        std = math.sqrt(var)
        dm = self._dev_mult.Value
        if abs(avg) > 0:
            if slope > 0 and slope > avg + dm * std and self.Position <= 0:
                self.BuyMarket()
            elif slope < 0 and slope < avg - dm * std and self.Position >= 0:
                self.SellMarket()
            if self.Position > 0 and slope < avg:
                self.SellMarket()
            elif self.Position < 0 and slope > avg:
                self.BuyMarket()
        self._prev_ma = ma

    def CreateClone(self):
        return ma_slope_breakout_strategy()
