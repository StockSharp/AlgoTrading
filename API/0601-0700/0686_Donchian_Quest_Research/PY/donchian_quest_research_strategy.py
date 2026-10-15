import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy


class donchian_quest_research_strategy(Strategy):
    """
    Donchian Quest Research strategy.
    A close above the upper band of the OpenPeriod Donchian channel goes long and a close below its lower band goes short,
    reversing an opposite position. A long closes when price touches the lower band of the ClosePeriod channel and a short
    when price touches its upper band. Both channels are measured on the candles before the current one.
    """

    def __init__(self):
        super(donchian_quest_research_strategy, self).__init__()
        self._open_period = self.Param("OpenPeriod", 50).SetGreaterThanZero().SetDisplay("Open Period", "Period of the entry Donchian channel", "Indicators")
        self._close_period = self.Param("ClosePeriod", 50).SetGreaterThanZero().SetDisplay("Close Period", "Period of the exit Donchian channel", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_open_upper = None
        self._prev_open_lower = None
        self._prev_close_upper = None
        self._prev_close_lower = None

    def OnReseted(self):
        super(donchian_quest_research_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(donchian_quest_research_strategy, self).OnStarted2(time)

        self._reset_state()

        open_channel = DonchianChannels()
        open_channel.Length = self._open_period.Value
        close_channel = DonchianChannels()
        close_channel.Length = self._close_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(open_channel, close_channel, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, open_channel)
            self.DrawIndicator(area, close_channel)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, open_value, close_value):
        if candle.State != CandleStates.Finished:
            return

        open_upper = self._prev_open_upper
        open_lower = self._prev_open_lower
        close_upper = self._prev_close_upper
        close_lower = self._prev_close_lower

        if open_value.IsFormed and open_value.UpperBand is not None and open_value.LowerBand is not None:
            self._prev_open_upper = open_value.UpperBand
            self._prev_open_lower = open_value.LowerBand

        if close_value.IsFormed and close_value.UpperBand is not None and close_value.LowerBand is not None:
            self._prev_close_upper = close_value.UpperBand
            self._prev_close_lower = close_value.LowerBand

        if open_upper is None or open_lower is None or close_upper is None or close_lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        if close > open_upper and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < open_lower and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and candle.LowPrice <= close_lower:
            self.SellMarket(self.Position)
        elif self.Position < 0 and candle.HighPrice >= close_upper:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return donchian_quest_research_strategy()
