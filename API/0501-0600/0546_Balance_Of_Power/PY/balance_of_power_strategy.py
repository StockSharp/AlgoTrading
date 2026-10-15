import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BalanceOfPower
from StockSharp.Algo.Strategies import Strategy


class balance_of_power_strategy(Strategy):
    """
    Balance of Power strategy.
    Long only: buys when Balance of Power crosses above Threshold and closes the long when it crosses below -Threshold.
    """

    def __init__(self):
        super(balance_of_power_strategy, self).__init__()
        self._threshold = self.Param("Threshold", 0.8).SetNotNegative().SetDisplay("Threshold", "Balance of Power level whose upward cross opens a long", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_bop = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(balance_of_power_strategy, self).OnReseted()
        self._prev_bop = None

    def OnStarted2(self, time):
        super(balance_of_power_strategy, self).OnStarted2(time)

        self._prev_bop = None

        bop = BalanceOfPower()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bop, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, bop)

    def _process_candle(self, candle, bop_value):
        if candle.State != CandleStates.Finished:
            return

        # A candle without range has no Balance of Power value.
        if not bop_value.IsFormed or bop_value.IsEmpty:
            return

        bop = bop_value.GetValue[Decimal](None)
        prev = self._prev_bop
        self._prev_bop = bop

        if prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        threshold = Decimal(self._threshold.Value)

        if self.Position == 0 and prev <= threshold and bop > threshold:
            self.BuyMarket(self.Volume)
        elif self.Position > 0 and prev >= -threshold and bop < -threshold:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return balance_of_power_strategy()
