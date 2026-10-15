import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class breakout_nifty_bn_strategy(Strategy):
    def __init__(self):
        super(breakout_nifty_bn_strategy, self).__init__()
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "General")
        self._atr_multiplier = self.Param("AtrMultiplier", 2) \
            .SetDisplay("ATR Multiplier", "ATR stop multiplier", "General")
        self._channel_length = self.Param("ChannelLength", 20) \
            .SetDisplay("Channel Length", "Donchian channel period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._trail_sl = 0.0
        self._entry_price = 0.0

    @property
    def atr_length(self):
        return self._atr_length.Value

    @property
    def atr_multiplier(self):
        return self._atr_multiplier.Value

    @property
    def channel_length(self):
        return self._channel_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(breakout_nifty_bn_strategy, self).OnReseted()
        self._trail_sl = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(breakout_nifty_bn_strategy, self).OnStarted2(time)
        highest = Highest()
        highest.Length = self.channel_length
        lowest = Lowest()
        lowest.Length = self.channel_length
        atr = AverageTrueRange()
        atr.Length = self.atr_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, atr, self.on_process).Start()
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, high, low, atr):
        if candle.State != CandleStates.Finished:
            return
        if self.Position == 0:
            if candle.ClosePrice >= high:
                self.BuyMarket()
            elif candle.ClosePrice <= low:
                self.SellMarket()

    def CreateClone(self):
        return breakout_nifty_bn_strategy()
