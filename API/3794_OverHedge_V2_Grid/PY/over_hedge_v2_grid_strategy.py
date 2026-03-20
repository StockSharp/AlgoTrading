import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class over_hedge_v2_grid_strategy(Strategy):
    """OverHedge V2 Grid strategy using RSI mean reversion.
    Buy when RSI is oversold, sell when RSI is overbought."""

    def __init__(self):
        super(over_hedge_v2_grid_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._oversold = self.Param("Oversold", 30.0) \
            .SetDisplay("Oversold", "RSI oversold level", "Indicators")
        self._overbought = self.Param("Overbought", 70.0) \
            .SetDisplay("Overbought", "RSI overbought level", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def Oversold(self):
        return self._oversold.Value

    @property
    def Overbought(self):
        return self._overbought.Value

    def OnReseted(self):
        super(over_hedge_v2_grid_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(over_hedge_v2_grid_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)

        if rsi_val <= float(self.Oversold) and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif rsi_val >= float(self.Overbought) and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return over_hedge_v2_grid_strategy()
