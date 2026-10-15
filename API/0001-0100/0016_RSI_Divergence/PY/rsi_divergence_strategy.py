import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_divergence_strategy(Strategy):
    """Trades confirmed three-bar price pivots unconfirmed by RSI, without future bars."""

    def __init__(self):
        super(rsi_divergence_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero()
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetRange(0.0, 100.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._reset_pivots()

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(rsi_divergence_strategy, self).OnReseted()
        self._reset_pivots()

    def _reset_pivots(self):
        self._bars = []
        self._previous_low = None
        self._previous_high = None

    def OnStarted2(self, time):
        super(rsi_divergence_strategy, self).OnStarted2(time)
        self._reset_pivots()
        self.StartProtection(Unit(), Unit(float(self._stop_loss_percent.Value), UnitTypes.Percent),
                             useMarketOrders=True, isLocalStop=True)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.Bind(rsi, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished or not self.IsFormedAndOnlineAndAllowTrading():
            return

        self._bars.append((candle.HighPrice, candle.LowPrice, rsi_value))
        if len(self._bars) < 3:
            return
        left, pivot, right = self._bars
        bullish = False
        bearish = False

        if pivot[1] < left[1] and pivot[1] < right[1]:
            bullish = (self._previous_low is not None and
                       pivot[1] < self._previous_low[0] and pivot[2] > self._previous_low[1])
            self._previous_low = (pivot[1], pivot[2])

        if pivot[0] > left[0] and pivot[0] > right[0]:
            bearish = (self._previous_high is not None and
                       pivot[0] > self._previous_high[0] and pivot[2] < self._previous_high[1])
            self._previous_high = (pivot[0], pivot[2])
        self._bars.pop(0)

        if bullish and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif bearish and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return rsi_divergence_strategy()
