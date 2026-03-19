import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import OnBalanceVolume, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class obv_atr_strategy(Strategy):
    def __init__(self):
        super(obv_atr_strategy, self).__init__()
        self._lookback = self.Param("LookbackLength", 60) \
            .SetGreaterThanZero() \
            .SetDisplay("OBV Lookback", "Lookback length for OBV highs and lows", "Parameters")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "Parameters")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(obv_atr_strategy, self).OnReseted()
        self._prev_obv = None
        self._prev_high = None
        self._prev_low = None
        self._mode = 0
        self._prev_mode = 0

    def OnStarted(self, time):
        super(obv_atr_strategy, self).OnStarted(time)
        self._prev_obv = None
        self._prev_high = None
        self._prev_low = None
        self._mode = 0
        self._prev_mode = 0

        self._obv = OnBalanceVolume()
        self._highest = Highest()
        self._highest.Length = self._lookback.Value
        self._lowest = Lowest()
        self._lowest.Length = self._lookback.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.BindEx(self._obv, self.OnProcess).Start()

        self.StartProtection(
            takeProfit=Unit(5, UnitTypes.Percent),
            stopLoss=Unit(3, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._obv)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, obv_value):
        if candle.State != CandleStates.Finished:
            return

        obv_val = float(obv_value)

        prev_high = self._prev_high
        prev_low = self._prev_low
        prev_obv = self._prev_obv

        high = float(self._highest.Process(obv_value))
        low = float(self._lowest.Process(obv_value))

        self._prev_obv = obv_val
        self._prev_high = high
        self._prev_low = low

        if prev_high is not None and prev_obv is not None and obv_val > prev_high and prev_obv <= prev_high:
            self._mode = 1
        elif prev_low is not None and prev_obv is not None and obv_val < prev_low and prev_obv >= prev_low:
            self._mode = -1

        bull_signal = self._mode == 1 and self._prev_mode != 1
        bear_signal = self._mode == -1 and self._prev_mode != -1
        self._prev_mode = self._mode

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if bull_signal and self.Position <= 0:
            self.BuyMarket()
        if bear_signal and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return obv_atr_strategy()
