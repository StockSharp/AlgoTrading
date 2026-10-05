import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageDirectionalIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class hull_ma_adx_strategy(Strategy):
    """
    Hull MA ADX strategy.
    The Hull average turns up when it rises after falling and turns down when it falls after rising. While ADX is above AdxThreshold, a turn up
    goes long and a turn down goes short, reversing an opposite position. A long closes when the Hull average falls, a short when it rises,
    and either closes once ADX drops below AdxExitThreshold. The stop lies AtrMultiplier ATR from the entry close and is checked on candle closes.
    """

    def __init__(self):
        super(hull_ma_adx_strategy, self).__init__()
        self._hma_period = self.Param("HmaPeriod", 9).SetGreaterThanZero().SetDisplay("HMA Period", "Period of the Hull moving average", "Indicators")
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero().SetDisplay("ADX Period", "Period of ADX", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 25.0).SetDisplay("ADX Threshold", "ADX level required to enter", "Indicators")
        self._adx_exit_threshold = self.Param("AdxExitThreshold", 20.0).SetDisplay("ADX Exit Threshold", "ADX level below which the position closes", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the stop ATR", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_hull = None
        self._prev_prev_hull = None
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(hull_ma_adx_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(hull_ma_adx_strategy, self).OnStarted2(time)

        self._reset_state()

        hull = HullMovingAverage()
        hull.Length = self._hma_period.Value
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(hull, adx, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)

    def _process_candle(self, candle, hull_value, adx_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not hull_value.IsFormed or not adx_value.IsFormed or not atr_value.IsFormed or adx_value.MovingAverage is None:
            return

        hull = hull_value.GetValue[Decimal](None)
        prev_hull = self._prev_hull
        prev_prev_hull = self._prev_prev_hull
        self._prev_prev_hull = prev_hull
        self._prev_hull = hull

        if prev_hull is None or prev_prev_hull is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        strength = adx_value.MovingAverage
        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        rising = hull > prev_hull
        falling = hull < prev_hull
        turns_up = rising and prev_hull < prev_prev_hull
        turns_down = falling and prev_hull > prev_prev_hull
        strong = strength > Decimal(self._adx_threshold.Value)
        weak = strength < Decimal(self._adx_exit_threshold.Value)

        stop_atr = Decimal(self._atr_multiplier.Value)
        if turns_up and strong and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif turns_down and strong and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and (falling or weak or (stop_atr > 0 and close <= self._stop_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (rising or weak or (stop_atr > 0 and close >= self._stop_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return hull_ma_adx_strategy()
