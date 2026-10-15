import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class averaging_down2_strategy(Strategy):
    """
    Averaging down strategy.
    Every candle that closes with RSI below RsiBuyThreshold buys another Volume, averaging the entry price of the long. The whole
    long closes when a close exceeds the previous candle's high. Long only.
    """

    def __init__(self):
        super(averaging_down2_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_buy_threshold = self.Param("RsiBuyThreshold", 33.0) \
            .SetDisplay("RSI Buy Threshold", "RSI level below which the strategy buys", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_high = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(averaging_down2_strategy, self).OnReseted()
        self._prev_high = None

    def OnStarted2(self, time):
        super(averaging_down2_strategy, self).OnStarted2(time)

        self._prev_high = None

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        prev_high = self._prev_high
        self._prev_high = float(candle.HighPrice)

        if prev_high is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0 and float(candle.ClosePrice) > prev_high:
            self.SellMarket(self.Position)
        elif float(rsi_value) < float(self._rsi_buy_threshold.Value):
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return averaging_down2_strategy()
