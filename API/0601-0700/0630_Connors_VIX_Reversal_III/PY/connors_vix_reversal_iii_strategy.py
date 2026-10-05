import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class connors_vix_reversal_iii_strategy(Strategy):
    """
    Connors VIX Reversal III strategy.
    The candles are the VIX series of the traded security. A bar whose low is above the LengthMA simple average and whose close is
    PercentThreshold percent above it buys; a bar whose high is below the average and whose close is PercentThreshold percent below
    it sells short. A long closes when the close falls below the previous bar's average and a short when it rises above it.
    """

    def __init__(self):
        super(connors_vix_reversal_iii_strategy, self).__init__()
        self._length_ma = self.Param("LengthMA", 10).SetGreaterThanZero().SetDisplay("MA Length", "Moving average length", "Indicators")
        self._percent_threshold = self.Param("PercentThreshold", 10.0).SetNotNegative().SetDisplay("Percent Threshold", "Distance from the average in percent that confirms a spike", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_ma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(connors_vix_reversal_iii_strategy, self).OnReseted()
        self._prev_ma = None

    def OnStarted2(self, time):
        super(connors_vix_reversal_iii_strategy, self).OnStarted2(time)

        self._prev_ma = None

        sma = SimpleMovingAverage()
        sma.Length = self._length_ma.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not sma_value.IsFormed:
            return

        ma = sma_value.GetValue[Decimal](None)
        prev_ma = self._prev_ma
        self._prev_ma = ma

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        factor = Decimal(self._percent_threshold.Value) / Decimal(100)

        if candle.LowPrice > ma and close > ma * (Decimal(1) + factor) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif candle.HighPrice < ma and close < ma * (Decimal(1) - factor) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif prev_ma is not None:
            if self.Position > 0 and close < prev_ma:
                self.SellMarket(self.Position)
            elif self.Position < 0 and close > prev_ma:
                self.BuyMarket(-self.Position)

    def CreateClone(self):
        return connors_vix_reversal_iii_strategy()
