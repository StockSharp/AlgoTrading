import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import EhlersFisherTransform
from StockSharp.Algo.Strategies import Strategy


class fisher_org_sign_strategy(Strategy):
    def __init__(self):
        super(fisher_org_sign_strategy, self).__init__()
        self._length = self.Param("Length", 7) \
            .SetDisplay("Fisher Length", "Period for Fisher Transform", "General")
        self._up_level = self.Param("UpLevel", 0.1) \
            .SetDisplay("Upper Level", "Sell signal level", "General")
        self._down_level = self.Param("DownLevel", -0.1) \
            .SetDisplay("Lower Level", "Buy signal level", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_fisher = None

    @property
    def length(self):
        return self._length.Value

    @property
    def up_level(self):
        return self._up_level.Value

    @property
    def down_level(self):
        return self._down_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fisher_org_sign_strategy, self).OnReseted()
        self._prev_fisher = None

    def OnStarted(self, time):
        super(fisher_org_sign_strategy, self).OnStarted(time)
        self._prev_fisher = None
        fisher = EhlersFisherTransform()
        fisher.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fisher, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fisher)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fisher_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        main_line = fisher_val.MainLine
        if main_line is None:
            return
        fisher_value = float(main_line)
        if self._prev_fisher is None:
            self._prev_fisher = fisher_value
            return
        down_level = float(self.down_level)
        up_level = float(self.up_level)
        long_condition = self._prev_fisher <= down_level and fisher_value > down_level
        short_condition = self._prev_fisher >= up_level and fisher_value < up_level
        if long_condition and self.Position <= 0:
            self.BuyMarket()
        if short_condition and self.Position >= 0:
            self.SellMarket()
        self._prev_fisher = fisher_value

    def CreateClone(self):
        return fisher_org_sign_strategy()
