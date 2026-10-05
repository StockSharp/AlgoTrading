import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class rejection_candle_strategy(Strategy):
    """
    Rejection Candle strategy.
    A bullish rejection probes below the previous candle's low, closes up, and has a lower wick longer than WickRatio bodies;
    a bearish rejection mirrors it above the previous high. While flat the strategy trades against the wick.
    The stop lies StopLossPercent beyond the rejected low or high, and a close beyond it closes the position.
    """

    def __init__(self):
        super(rejection_candle_strategy, self).__init__()
        self._wick_ratio = self.Param("WickRatio", 1.5).SetGreaterThanZero().SetDisplay("Wick Ratio", "How many bodies long the rejecting wick must be", "Pattern")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Distance of the stop beyond the rejected extreme, in percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_candle = None
        self._stop_price = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rejection_candle_strategy, self).OnReseted()
        self._prev_candle = None
        self._stop_price = Decimal(0)

    def OnStarted2(self, time):
        super(rejection_candle_strategy, self).OnStarted2(time)

        self._prev_candle = None
        self._stop_price = Decimal(0)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        previous = self._prev_candle
        self._prev_candle = candle

        if previous is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        if self.Position > 0:
            if close <= self._stop_price:
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if close >= self._stop_price:
                self.BuyMarket(-self.Position)
            return

        body = Math.Abs(close - candle.OpenPrice)
        upper_wick = candle.HighPrice - Math.Max(candle.OpenPrice, close)
        lower_wick = Math.Min(candle.OpenPrice, close) - candle.LowPrice
        ratio = Decimal(self._wick_ratio.Value)
        percent = Decimal(self._stop_loss_percent.Value) / Decimal(100)

        if candle.LowPrice < previous.LowPrice and close > candle.OpenPrice and lower_wick > body * ratio:
            self.BuyMarket(self.Volume)
            self._stop_price = candle.LowPrice * (Decimal(1) - percent)
        elif candle.HighPrice > previous.HighPrice and close < candle.OpenPrice and upper_wick > body * ratio:
            self.SellMarket(self.Volume)
            self._stop_price = candle.HighPrice * (Decimal(1) + percent)

    def CreateClone(self):
        return rejection_candle_strategy()
