import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class one_ma_channel_breakout_strategy(Strategy):
    def __init__(self):
        super(one_ma_channel_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._ma_period = self.Param("MaPeriod", 44) \
            .SetDisplay("MA Period", "Moving average length", "Indicators")
        self._channel_offset = self.Param("ChannelOffset", 0.005) \
            .SetDisplay("Channel Offset", "Percentage offset for channel", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @property
    def ChannelOffset(self):
        return self._channel_offset.Value

    def OnStarted2(self, time):
        super(one_ma_channel_breakout_strategy, self).OnStarted2(time)

        sma = SimpleMovingAverage()
        sma.Length = self.MaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        mv = float(ma_value)
        offset = float(self.ChannelOffset)
        upper = mv * (1.0 + offset)
        lower = mv * (1.0 - offset)
        close = float(candle.ClosePrice)

        # Bullish breakout above upper channel
        if close > upper and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Bearish breakdown below lower channel
        elif close < lower and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return one_ma_channel_breakout_strategy()
