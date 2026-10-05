import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class btc_future_gamma_weighted_momentum_model_strategy(Strategy):
    """
    BTC Future Gamma-Weighted Momentum Model strategy.
    The gamma-weighted average price (GWAP) averages the last Length closes with weight GammaFactor^i for the close i bars ago.
    A close above GWAP after three consecutively rising closes goes long, a close below GWAP after three consecutively falling closes
    goes short, and the opposite signal reverses the position.
    """

    def __init__(self):
        super(btc_future_gamma_weighted_momentum_model_strategy, self).__init__()
        self._length = self.Param("Length", 60).SetGreaterThanZero().SetDisplay("Length", "Closes in the GWAP window", "GWAP")
        self._gamma_factor = self.Param("GammaFactor", 0.75).SetRange(0.01, 1.0).SetDisplay("Gamma Factor", "Decay of the weight per bar back", "GWAP")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._closes = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(btc_future_gamma_weighted_momentum_model_strategy, self).OnReseted()
        self._closes = []

    def OnStarted2(self, time):
        super(btc_future_gamma_weighted_momentum_model_strategy, self).OnStarted2(time)

        self._closes = []

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        length = self._length.Value
        self._closes.append(candle.ClosePrice)
        keep = max(length, 3)
        while len(self._closes) > keep:
            self._closes.pop(0)

        if len(self._closes) < keep:
            return

        last = len(self._closes) - 1
        gamma = Decimal(self._gamma_factor.Value)

        weighted = Decimal(0)
        weights = Decimal(0)
        weight = Decimal(1)
        for i in range(length):
            weighted += self._closes[last - i] * weight
            weights += weight
            weight *= gamma

        gwap = weighted / weights

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = self._closes[last]
        rising = close > self._closes[last - 1] and self._closes[last - 1] > self._closes[last - 2]
        falling = close < self._closes[last - 1] and self._closes[last - 1] < self._closes[last - 2]

        if close > gwap and rising and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < gwap and falling and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return btc_future_gamma_weighted_momentum_model_strategy()
