import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class hull_ma_volume_strategy(Strategy):
    """
    Hull MA Volume strategy.
    A rising Hull average on a candle whose volume exceeds VolumeMultiplier times the average of the previous VolumePeriod candles goes long,
    a falling one on such volume goes short, reversing an opposite position. A long closes when the Hull average turns down and a short
    when it turns up. The stop lies StopLossAtr ATR from the entry close and is checked on candle closes.
    """

    def __init__(self):
        super(hull_ma_volume_strategy, self).__init__()
        self._hull_period = self.Param("HullPeriod", 9).SetGreaterThanZero().SetDisplay("Hull Period", "Period of the Hull moving average", "Indicators")
        self._volume_period = self.Param("VolumePeriod", 20).SetGreaterThanZero().SetDisplay("Volume Period", "Previous candles the volume is averaged over", "Indicators")
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.5).SetGreaterThanZero().SetDisplay("Volume Multiplier", "How many times the average volume a candle must exceed", "Indicators")
        self._stop_loss_atr = self.Param("StopLossAtr", 2.0).SetNotNegative().SetDisplay("Stop Loss ATR", "Stop distance from the entry in ATRs", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the stop ATR", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volumes = []
        self._prev_hull = None
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(hull_ma_volume_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(hull_ma_volume_strategy, self).OnStarted2(time)

        self._reset_state()

        hull = HullMovingAverage()
        hull.Length = self._hull_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(hull, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, hull_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        # Volume is compared with the candles before this one.
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

        if not hull_value.IsFormed or not atr_value.IsFormed:
            return

        hull = hull_value.GetValue[Decimal](None)
        prev_hull = self._prev_hull
        self._prev_hull = hull

        if prev_hull is None or average is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        rising = hull > prev_hull
        falling = hull < prev_hull
        surge = candle.TotalVolume > average * Decimal(self._volume_multiplier.Value)

        stop_atr = Decimal(self._stop_loss_atr.Value)
        if rising and surge and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif falling and surge and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and (falling or (stop_atr > 0 and close <= self._stop_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (rising or (stop_atr > 0 and close >= self._stop_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return hull_ma_volume_strategy()
