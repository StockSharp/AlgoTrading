import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class vinin_i_trend_strategy(Strategy):
    """CCI crossing upper/lower levels for trend entry."""
    def __init__(self):
        super(vinin_i_trend_strategy, self).__init__()
        self._period = self.Param("Period", 20).SetGreaterThanZero().SetDisplay("CCI Period", "CCI indicator period", "Parameters")
        self._up_level = self.Param("UpLevel", 10).SetDisplay("Upper Level", "Upper threshold for buy", "Parameters")
        self._down_level = self.Param("DownLevel", -10).SetDisplay("Lower Level", "Lower threshold for sell", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vinin_i_trend_strategy, self).OnReseted()
        self._prev_cci = None

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

        if self._prev_cci is not None:
            buy_signal = self._prev_cci <= up and cci_val > up
            sell_signal = self._prev_cci >= down and cci_val < down

            if buy_signal and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif sell_signal and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_cci = cci_val

    def CreateClone(self):
        return vinin_i_trend_strategy()
