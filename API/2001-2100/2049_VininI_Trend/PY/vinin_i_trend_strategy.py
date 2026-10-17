import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class vinin_i_trend_strategy(Strategy):
    """CCI crossing upper/lower levels for trend entry."""
    def __init__(self):
        super(vinin_i_trend_strategy, self).__init__()
        self._mode = self.Param("Mode", 0).SetRange(0, 1).SetDisplay("Entry Mode", "0: Breakdown, 1: Twist", "Parameters")
        self._period = self.Param("Period", 20).SetGreaterThanZero().SetDisplay("CCI Period", "CCI indicator period", "Parameters")
        self._up_level = self.Param("UpLevel", 10).SetDisplay("Upper Level", "Upper threshold for buy", "Parameters")
        self._down_level = self.Param("DownLevel", -10).SetDisplay("Lower Level", "Lower threshold for sell", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_cci = None
        self._older_cci = None
        self._pending = None

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vinin_i_trend_strategy, self).OnReseted()
        self._prev_cci = None
        self._older_cci = None
        self._pending = None

    def OnStarted2(self, time):
        super(vinin_i_trend_strategy, self).OnStarted2(time)
        self._prev_cci = None

        cci = CommodityChannelIndex()
        cci.Length = self._period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.BindEx(cci, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, cci_ind_val):
        if candle.State != CandleStates.Finished:
            return

        if not cci_ind_val.IsFormed:
            return

        cci_val = float(cci_ind_val)
        up = self._up_level.Value
        down = self._down_level.Value

        buy = sell = False
        if self._prev_cci is not None:
            if int(self._mode.Value) == 0:
                buy = self._prev_cci <= up and cci_val > up
                sell = self._prev_cci >= down and cci_val < down
            elif self._older_cci is not None:
                buy = self._prev_cci < self._older_cci and cci_val > self._prev_cci
                sell = self._prev_cci > self._older_cci and cci_val < self._prev_cci
        if self.IsFormedAndOnlineAndAllowTrading() and (self._pending is None or self._pending.State in (OrderStates.Done, OrderStates.Failed)):
            if buy and self.Position <= 0:
                self._pending = self.BuyMarket(self.Volume - min(0, self.Position))
            elif sell and self.Position >= 0:
                self._pending = self.SellMarket(self.Volume + max(0, self.Position))
        self._older_cci, self._prev_cci = self._prev_cci, cci_val

    def CreateClone(self):
        return vinin_i_trend_strategy()
