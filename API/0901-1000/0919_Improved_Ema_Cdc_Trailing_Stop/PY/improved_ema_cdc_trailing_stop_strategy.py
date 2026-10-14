import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class improved_ema_cdc_trailing_stop_strategy(Strategy):
    """
    Improved EMA and CDC trailing stop strategy.
    A long opens when the close is above EMA60, EMA60 is above EMA90 and the MACD (12, 26, 9) line is above its signal line; a short
    mirrors these rules and an opposite signal reverses the position. A CDC ATR trailing stop follows the close at Multiplier ATRs and only
    moves in the trade's favour, and a profit target sits ProfitTargetMultiplier ATRs from the entry.
    """

    def __init__(self):
        super(improved_ema_cdc_trailing_stop_strategy, self).__init__()
        self._ema60_period = self.Param("Ema60Period", 60).SetGreaterThanZero().SetDisplay("EMA 60 Period", "Length of the fast EMA", "Parameters")
        self._ema90_period = self.Param("Ema90Period", 90).SetGreaterThanZero().SetDisplay("EMA 90 Period", "Length of the slow EMA", "Parameters")
        self._atr_period = self.Param("AtrPeriod", 24).SetGreaterThanZero().SetDisplay("ATR Period", "Period for ATR calculation", "Parameters")
        self._multiplier = self.Param("Multiplier", 4.0).SetNotNegative().SetDisplay("ATR Multiplier", "ATR multiplier for the trailing stop", "Parameters")
        self._profit_target_multiplier = self.Param("ProfitTargetMultiplier", 2.0).SetNotNegative().SetDisplay("Profit Target Multiplier", "ATR multiplier for the profit target", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_price = None
        self._take_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(improved_ema_cdc_trailing_stop_strategy, self).OnReseted()
        self._stop_price = None
        self._take_price = None

    def OnStarted2(self, time):
        super(improved_ema_cdc_trailing_stop_strategy, self).OnStarted2(time)

        self._stop_price = None
        self._take_price = None

        ema60 = ExponentialMovingAverage()
        ema60.Length = self._ema60_period.Value
        ema90 = ExponentialMovingAverage()
        ema90.Length = self._ema90_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = 12
        macd.Macd.LongMa.Length = 26
        macd.SignalMa.Length = 9

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, ema60, ema90, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema60)
            self.DrawIndicator(area, ema90)
            self.DrawOwnTrades(area)
            macd_area = self.CreateChartArea()
            if macd_area is not None:
                self.DrawIndicator(macd_area, macd)

    def _process_candle(self, candle, macd_value, ema60_value, ema90_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not atr_value.IsFormed:
            return

        atr = atr_value.GetValue[Decimal](None)

        if self._manage_exits(candle, atr):
            return

        if not macd_value.IsFormed or macd_value.Macd is None or macd_value.Signal is None:
            return

        if not ema60_value.IsFormed or not ema90_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd = macd_value.Macd
        signal = macd_value.Signal
        ema60 = ema60_value.GetValue[Decimal](None)
        ema90 = ema90_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        long_condition = close > ema60 and ema60 > ema90 and macd > signal
        short_condition = close < ema60 and ema60 < ema90 and macd < signal

        if long_condition and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._set_levels(close, atr, Decimal(1))
        elif short_condition and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._set_levels(close, atr, Decimal(-1))

    def _set_levels(self, entry, atr, sign):
        multiplier = Decimal(self._multiplier.Value)
        target = Decimal(self._profit_target_multiplier.Value)
        self._stop_price = entry - sign * atr * multiplier if multiplier > 0 else None
        self._take_price = entry + sign * atr * target if target > 0 else None

    # Returns True when the trailing stop or profit target closed the position on this candle.
    def _manage_exits(self, candle, atr):
        if self.Position == 0:
            self._stop_price = None
            self._take_price = None
            return False

        is_long = self.Position > 0
        sl = self._stop_price
        tp = self._take_price

        if is_long:
            stop_hit = sl is not None and candle.LowPrice <= sl
            take_hit = tp is not None and candle.HighPrice >= tp
        else:
            stop_hit = sl is not None and candle.HighPrice >= sl
            take_hit = tp is not None and candle.LowPrice <= tp

        if stop_hit or take_hit:
            if is_long:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(-self.Position)
            self._stop_price = None
            self._take_price = None
            return True

        if sl is not None:
            distance = atr * Decimal(self._multiplier.Value)
            if is_long:
                self._stop_price = max(sl, candle.ClosePrice - distance)
            else:
                self._stop_price = min(sl, candle.ClosePrice + distance)

        return False

    def CreateClone(self):
        return improved_ema_cdc_trailing_stop_strategy()
