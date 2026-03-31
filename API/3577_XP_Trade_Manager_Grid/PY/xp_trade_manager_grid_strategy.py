import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class xp_trade_manager_grid_strategy(Strategy):
    def __init__(self):
        super(xp_trade_manager_grid_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._rsi_upper = self.Param("RsiUpper", 70.0)
        self._rsi_lower = self.Param("RsiLower", 30.0)

        self._prev_rsi = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiUpper(self):
        return self._rsi_upper.Value

    @RsiUpper.setter
    def RsiUpper(self, value):
        self._rsi_upper.Value = value

    @property
    def RsiLower(self):
        return self._rsi_lower.Value

    @RsiLower.setter
    def RsiLower(self, value):
        self._rsi_lower.Value = value

    def OnReseted(self):
        super(xp_trade_manager_grid_strategy, self).OnReseted()
        self._prev_rsi = None

    def OnStarted2(self, time):
        super(xp_trade_manager_grid_strategy, self).OnStarted2(time)
        self._prev_rsi = None

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)

        if self._prev_rsi is None:
            self._prev_rsi = rsi_val
            return

        rsi_lower = float(self.RsiLower)
        rsi_upper = float(self.RsiUpper)

        # RSI crosses below oversold -> buy
        cross_down = self._prev_rsi > rsi_lower and rsi_val <= rsi_lower
        # RSI crosses above overbought -> sell
        cross_up = self._prev_rsi < rsi_upper and rsi_val >= rsi_upper

        if cross_down:
            if self.Position <= 0:
                self.BuyMarket()
        elif cross_up:
            if self.Position >= 0:
                self.SellMarket()

        self._prev_rsi = rsi_val

    def CreateClone(self):
        return xp_trade_manager_grid_strategy()
