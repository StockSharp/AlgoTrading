import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class rejection_candle_strategy(Strategy):
    """
    Rejection Candle (Pin Bar) strategy.
    Enters long on bullish rejection (lower low + bullish close + long lower wick).
    Enters short on bearish rejection (higher high + bearish close + long upper wick).
    Uses SMA for exit confirmation.
    """

    def __init__(self):
        super(rejection_candle_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 20).SetDisplay("MA Length", "Period of SMA for exit", "Indicators")
        self._wick_ratio = self.Param("WickRatio", 1.5).SetDisplay("Wick Ratio", "Min wick to body ratio for rejection", "Pattern")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_candle = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rejection_candle_strategy, self).OnReseted()
        self._prev_candle = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(rejection_candle_strategy, self).OnStarted(time)

        self._prev_candle = None
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._prev_candle is None:
            self._prev_candle = candle
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_candle = candle
            return

        cd = self._cooldown_bars.Value
        sv = float(sma_val)
        wr = self._wick_ratio.Value

        body_size = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        if body_size == 0:
            body_size = 0.01

        upper_wick = float(candle.HighPrice) - max(float(candle.OpenPrice), float(candle.ClosePrice))
        lower_wick = min(float(candle.OpenPrice), float(candle.ClosePrice)) - float(candle.LowPrice)

        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice

        # Bullish rejection: made lower low, bullish close, long lower wick
        bullish_rejection = (
            candle.LowPrice < self._prev_candle.LowPrice and
            is_bullish and
            lower_wick > body_size * wr
        )

        # Bearish rejection: made higher high, bearish close, long upper wick
        bearish_rejection = (
            candle.HighPrice > self._prev_candle.HighPrice and
            is_bearish and
            upper_wick > body_size * wr
        )

        if self.Position == 0 and bullish_rejection:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and bearish_rejection:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and float(candle.ClosePrice) < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and float(candle.ClosePrice) > sv:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_candle = candle

    def CreateClone(self):
        return rejection_candle_strategy()
