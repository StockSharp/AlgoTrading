import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import EhlersFisherTransform
from StockSharp.Algo.Strategies import Strategy


class fisher_org_sign_strategy(Strategy):
    """Fisher level crossings with documented defaults and exact-volume reversal."""

    def __init__(self):
        super(fisher_org_sign_strategy, self).__init__()
        self._length = self.Param("Length", 7).SetDisplay("Fisher Length", "Period for Fisher Transform", "General")
        self._up_level = self.Param("UpLevel", 1.5).SetDisplay("Upper Level", "Sell signal level", "General")
        self._down_level = self.Param("DownLevel", -1.5).SetDisplay("Lower Level", "Buy signal level", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles", "General")
        self._previous_fisher = None

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def UpLevel(self):
        return self._up_level.Value

    @UpLevel.setter
    def UpLevel(self, value):
        self._up_level.Value = value

    @property
    def DownLevel(self):
        return self._down_level.Value

    @DownLevel.setter
    def DownLevel(self, value):
        self._down_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(fisher_org_sign_strategy, self).OnReseted()
        self._previous_fisher = None

    def OnStarted2(self, time):
        super(fisher_org_sign_strategy, self).OnStarted2(time)
        self._previous_fisher = None
        fisher = EhlersFisherTransform()
        fisher.Length = self.Length
        self.SubscribeCandles(self.CandleType).BindEx(fisher, self.process_candle).Start()

    def process_candle(self, candle, fisher_value):
        if candle.State != CandleStates.Finished:
            return
        current = fisher_value.MainLine
        if current is None:
            return
        current = float(current)
        previous = self._previous_fisher
        self._previous_fisher = current
        if previous is None or not self.IsFormedAndOnlineAndAllowTrading():
            return
        position = self.Position
        if previous <= float(self.DownLevel) < current and position <= 0:
            if position < 0:
                self.BuyMarket(-position)
            self.BuyMarket(self.Volume)
        elif previous >= float(self.UpLevel) > current and position >= 0:
            if position > 0:
                self.SellMarket(position)
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return fisher_org_sign_strategy()
