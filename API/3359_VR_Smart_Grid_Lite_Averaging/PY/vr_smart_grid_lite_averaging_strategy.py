import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vr_smart_grid_lite_averaging_strategy(Strategy):
    """Bollinger Bands grid averaging: buy below midline, sell above midline."""
    def __init__(self):
        super(vr_smart_grid_lite_averaging_strategy, self).__init__()
        self._bb_period = self.Param("BbPeriod", 20).SetGreaterThanZero().SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vr_smart_grid_lite_averaging_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_mid = None

    def OnStarted(self, time):
        super(vr_smart_grid_lite_averaging_strategy, self).OnStarted(time)
        self._prev_close = None
        self._prev_mid = None

        bb = BollingerBands()
        bb.Length = self._bb_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.BindEx(bb, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_val):
        if candle.State != CandleStates.Finished:
            return

        if not bb_val.IsFormed:
            return

        # Extract upper and lower bands
        upper = None
        lower = None
        for key in bb_val.InnerValues:
            name = str(key.Key.Name) if hasattr(key.Key, 'Name') else str(key.Key)
            val = float(key.Value)
            name_lower = name.lower()
            if 'up' in name_lower:
                upper = val
            elif 'low' in name_lower or 'down' in name_lower:
                lower = val

        if upper is None or lower is None:
            return

        close = float(candle.ClosePrice)
        mid = (upper + lower) / 2.0

        if self._prev_close is not None and self._prev_mid is not None:
            cross_below = self._prev_close >= self._prev_mid and close < mid
            cross_above = self._prev_close <= self._prev_mid and close > mid

            if cross_below and self.Position <= 0:
                self.BuyMarket()
            elif cross_above and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_mid = mid

    def CreateClone(self):
        return vr_smart_grid_lite_averaging_strategy()
