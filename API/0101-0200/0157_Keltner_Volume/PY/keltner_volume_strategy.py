import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class keltner_volume_strategy(Strategy):
    """
    Keltner Volume strategy.
    The Keltner Channel is the EmaPeriod EMA plus and minus Multiplier times the AtrPeriod ATR. A close below the lower band on volume above
    the average of the previous VolumeAvgPeriod candles goes long and a close above the upper band on such volume goes short, reversing
    an opposite position. The position closes when price crosses the EMA. The stop lies StopLossAtr ATR from the entry close
    and is checked on candle closes.
    """

    def __init__(self):
        super(keltner_volume_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetGreaterThanZero().SetDisplay("EMA Period", "Period of the channel EMA", "Keltner")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the channel and stop ATR", "Keltner")
        self._multiplier = self.Param("Multiplier", 2.0).SetGreaterThanZero().SetDisplay("Multiplier", "ATR multiplier of the channel width", "Keltner")
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20).SetGreaterThanZero().SetDisplay("Volume Average Period", "Previous candles the volume is averaged over", "Volume")
        self._stop_loss_atr = self.Param("StopLossAtr", 2.0).SetNotNegative().SetDisplay("Stop Loss ATR", "Stop distance from the entry in ATRs", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volumes = []
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(keltner_volume_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(keltner_volume_strategy, self).OnStarted2(time)

        self._reset_state()

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value, atr_value):
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

        if not ema_value.IsFormed or not atr_value.IsFormed or average is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        middle = ema_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        multiplier = Decimal(self._multiplier.Value)
        upper = middle + multiplier * atr
        lower = middle - multiplier * atr
        close = candle.ClosePrice
        surge = candle.TotalVolume > average

        stop_atr = Decimal(self._stop_loss_atr.Value)
        if close < lower and surge and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif close > upper and surge and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and (close > middle or (stop_atr > 0 and close <= self._stop_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (close < middle or (stop_atr > 0 and close >= self._stop_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return keltner_volume_strategy()
