import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class adx_volume_strategy(Strategy):
    """
    ADX Volume strategy.
    While ADX is above AdxThreshold, a candle with volume above the average of the previous VolumeAvgPeriod candles goes long when +DI
    is above -DI and short when -DI is above +DI, reversing an opposite position. The position closes once ADX falls below AdxThreshold.
    The stop lies StopLossAtr ATR from the entry close and is checked on candle closes.
    """

    def __init__(self):
        super(adx_volume_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero().SetDisplay("ADX Period", "Period of ADX", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 25.0).SetDisplay("ADX Threshold", "ADX level of a strong trend", "Indicators")
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20).SetGreaterThanZero().SetDisplay("Volume Average Period", "Previous candles the volume is averaged over", "Indicators")
        self._stop_loss_atr = self.Param("StopLossAtr", 2.0).SetNotNegative().SetDisplay("Stop Loss ATR", "Stop distance from the entry in ATRs", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the stop ATR", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volumes = []
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(adx_volume_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(adx_volume_strategy, self).OnStarted2(time)

        self._reset_state()

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)

    def _process_candle(self, candle, adx_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        # Volume is compared with the candles before this one.
        period = self._volume_avg_period.Value
        average = None
        if len(self._volumes) == period:
            total = Decimal(0)
            for volume in self._volumes:
                total += volume
            average = total / Decimal(period)

        self._volumes.append(candle.TotalVolume)
        if len(self._volumes) > period:
            self._volumes.pop(0)

        if not adx_value.IsFormed or not atr_value.IsFormed or average is None:
            return
        if adx_value.MovingAverage is None or adx_value.Dx.Plus is None or adx_value.Dx.Minus is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        strength = adx_value.MovingAverage
        plus_di = adx_value.Dx.Plus
        minus_di = adx_value.Dx.Minus
        threshold = Decimal(self._adx_threshold.Value)
        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        active = strength > threshold and candle.TotalVolume > average

        stop_atr = Decimal(self._stop_loss_atr.Value)
        if active and plus_di > minus_di and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif active and minus_di > plus_di and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and (strength < threshold or (stop_atr > 0 and close <= self._stop_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (strength < threshold or (stop_atr > 0 and close >= self._stop_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return adx_volume_strategy()
