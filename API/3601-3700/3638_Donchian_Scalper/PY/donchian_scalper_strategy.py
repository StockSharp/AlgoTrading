import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class donchian_scalper_strategy(Strategy):
    def __init__(self):
        super(donchian_scalper_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._channel_period = self.Param("ChannelPeriod", 50)

        self._highs = []
        self._lows = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def ChannelPeriod(self):
        return self._channel_period.Value

    @ChannelPeriod.setter
    def ChannelPeriod(self, value):
        self._channel_period.Value = value

    def OnReseted(self):
        super(donchian_scalper_strategy, self).OnReseted()
        self._highs = []
        self._lows = []

    def OnStarted2(self, time):
        super(donchian_scalper_strategy, self).OnStarted2(time)
        self._highs = []
        self._lows = []

        ema = ExponentialMovingAverage()
        ema.Length = self.ChannelPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._process_candle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        ema_val = float(ema_value)
        period = self.ChannelPeriod

        self._highs.append(high)
        self._lows.append(low)
        while len(self._highs) > period:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < period:
            return

        upper = max(self._highs)
        lower = min(self._lows)

        # Long: close breaks above upper Donchian and is above EMA
        if self.Position == 0 and close >= upper and close > ema_val:
            self.BuyMarket()
        # Short: close breaks below lower Donchian and is below EMA
        elif self.Position == 0 and close <= lower and close < ema_val:
            self.SellMarket()

    def CreateClone(self):
        return donchian_scalper_strategy()
