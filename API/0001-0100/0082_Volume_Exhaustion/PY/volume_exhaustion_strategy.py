import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

# Period of the ATR that sizes the stop.
ATR_PERIOD = 14

class volume_exhaustion_strategy(Strategy):
    """
    Volume Exhaustion strategy.
    A volume spike is a candle whose volume exceeds VolumeMultiplier times the average of the previous VolumePeriod candles.
    While flat, a bullish spike against a falling moving average buys and a bearish spike against a rising one sells.
    The only exit is a stop that trails the close by AtrMultiplier ATRs.
    """

    def __init__(self):
        super(volume_exhaustion_strategy, self).__init__()
        self._volume_period = self.Param("VolumePeriod", 20).SetGreaterThanZero().SetDisplay("Volume Period", "Previous candles the volume is averaged over", "Indicators")
        self._volume_multiplier = self.Param("VolumeMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Volume Multiplier", "How many times the average volume a spike must exceed", "Indicators")
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Moving average that defines the trend", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "Distance of the trailing stop in ATR multiples", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._volumes = []
        self._prev_ma = None
        self._stop_price = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_exhaustion_strategy, self).OnReseted()
        self._volumes = []
        self._prev_ma = None
        self._stop_price = Decimal(0)

    def OnStarted2(self, time):
        super(volume_exhaustion_strategy, self).OnStarted2(time)

        self._volumes = []
        self._prev_ma = None
        self._stop_price = Decimal(0)

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        atr = AverageTrueRange()
        atr.Length = ATR_PERIOD

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        # The spike is measured against the candles before this one.
        period = self._volume_period.Value
        average = None
        if len(self._volumes) == period:
            total = Decimal(0)
            for volume in self._volumes:
                total += volume
            average = total / Decimal(period)

        self._volumes.append(candle.TotalVolume)
        if len(self._volumes) > period:
            self._volumes.pop(0)

        if not sma_value.IsFormed or not atr_value.IsFormed:
            return

        ma = sma_value.GetValue[Decimal](None)
        prev_ma = self._prev_ma
        self._prev_ma = ma

        if average is None or prev_ma is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        distance = Decimal(self._atr_multiplier.Value) * atr_value.GetValue[Decimal](None)

        if self.Position > 0:
            if close <= self._stop_price:
                self.SellMarket(self.Position)
            else:
                self._stop_price = Math.Max(self._stop_price, close - distance)
            return

        if self.Position < 0:
            if close >= self._stop_price:
                self.BuyMarket(-self.Position)
            else:
                self._stop_price = Math.Min(self._stop_price, close + distance)
            return

        spike = candle.TotalVolume > average * Decimal(self._volume_multiplier.Value)
        if not spike:
            return

        if close > candle.OpenPrice and ma < prev_ma:
            self.BuyMarket(self.Volume)
            self._stop_price = close - distance
        elif close < candle.OpenPrice and ma > prev_ma:
            self.SellMarket(self.Volume)
            self._stop_price = close + distance

    def CreateClone(self):
        return volume_exhaustion_strategy()
