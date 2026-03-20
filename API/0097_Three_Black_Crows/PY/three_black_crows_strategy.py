import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class three_black_crows_strategy(Strategy):
    """
    Three Black Crows strategy.
    Enters short when three consecutive bearish candles with falling closes are detected.
    Enters long when three consecutive bullish candles with rising closes are detected.
    Uses SMA for exit confirmation.
    """

    def __init__(self):
        super(three_black_crows_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 20).SetDisplay("MA Length", "Period of SMA for exit", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._candle1 = None
        self._candle2 = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_black_crows_strategy, self).OnReseted()
        self._candle1 = None
        self._candle2 = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(three_black_crows_strategy, self).OnStarted(time)

        self._candle1 = None
        self._candle2 = None
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

        prev2 = self._candle1
        prev1 = self._candle2
        self._candle1 = self._candle2
        self._candle2 = candle

        if prev2 is None or prev1 is None:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        cd = self._cooldown_bars.Value
        sv = float(sma_val)

        # Three Black Crows: 3 bearish candles with falling closes
        three_black = (
            prev2.ClosePrice < prev2.OpenPrice and
            prev1.ClosePrice < prev1.OpenPrice and
            candle.ClosePrice < candle.OpenPrice and
            prev1.ClosePrice < prev2.ClosePrice and
            candle.ClosePrice < prev1.ClosePrice
        )

        # Three White Soldiers: 3 bullish candles with rising closes
        three_white = (
            prev2.ClosePrice > prev2.OpenPrice and
            prev1.ClosePrice > prev1.OpenPrice and
            candle.ClosePrice > candle.OpenPrice and
            prev1.ClosePrice > prev2.ClosePrice and
            candle.ClosePrice > prev1.ClosePrice
        )

        if self.Position == 0 and three_black:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position == 0 and three_white:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position < 0 and float(candle.ClosePrice) > sv:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position > 0 and float(candle.ClosePrice) < sv:
            self.SellMarket()
            self._cooldown = cd

    def CreateClone(self):
        return three_black_crows_strategy()
