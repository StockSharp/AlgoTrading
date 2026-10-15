import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class dual_keltner_channels_strategy(Strategy):
    """
    Dual Keltner Channels strategy.
    Both channels are EMA(EmaPeriod) plus and minus a multiple of ATR(EmaPeriod): InnerMultiplier for the inner channel and
    OuterMultiplier for the outer one. A low below the lower outer band arms a long, which is taken when the close then crosses back
    above the lower inner band; a high above the upper outer band arms a short, taken when the close crosses back below the upper inner
    band. An opposite signal reverses the position. The stop is MaxStopPercent from the entry and the take profit SlTpRatio times that.
    """

    def __init__(self):
        super(dual_keltner_channels_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 50).SetGreaterThanZero().SetDisplay("EMA Period", "EMA and ATR period of the channels", "Indicators")
        self._inner_multiplier = self.Param("InnerMultiplier", 2.75).SetGreaterThanZero().SetDisplay("Inner Multiplier", "ATR multiplier of the inner channel", "Indicators")
        self._outer_multiplier = self.Param("OuterMultiplier", 3.75).SetGreaterThanZero().SetDisplay("Outer Multiplier", "ATR multiplier of the outer channel", "Indicators")
        self._max_stop_percent = self.Param("MaxStopPercent", 10.0).SetNotNegative().SetDisplay("Max Stop %", "Stop loss percentage from entry price", "Risk")
        self._sl_tp_ratio = self.Param("SlTpRatio", 1.0).SetNotNegative().SetDisplay("SL/TP Ratio", "Take profit as a multiple of the stop distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_inner_upper = None
        self._prev_inner_lower = None
        self._long_armed = False
        self._short_armed = False

    def OnReseted(self):
        super(dual_keltner_channels_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(dual_keltner_channels_strategy, self).OnStarted2(time)

        self._reset_state()

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._ema_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, atr, self._process_candle).Start()

        stop_percent = Decimal(self._max_stop_percent.Value)
        take_percent = stop_percent * Decimal(self._sl_tp_ratio.Value)
        stop = Unit(stop_percent, UnitTypes.Percent) if stop_percent > 0 else Unit()
        take = Unit(take_percent, UnitTypes.Percent) if take_percent > 0 else Unit()
        self.StartProtection(take, stop, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not ema_value.IsFormed or not atr_value.IsFormed:
            return

        ema = ema_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        inner = Decimal(self._inner_multiplier.Value)
        outer = Decimal(self._outer_multiplier.Value)
        inner_upper = ema + atr * inner
        inner_lower = ema - atr * inner
        outer_upper = ema + atr * outer
        outer_lower = ema - atr * outer

        if candle.LowPrice < outer_lower:
            self._long_armed = True

        if candle.HighPrice > outer_upper:
            self._short_armed = True

        pc = self._prev_close
        piu = self._prev_inner_upper
        pil = self._prev_inner_lower

        self._prev_close = close
        self._prev_inner_upper = inner_upper
        self._prev_inner_lower = inner_lower

        if pc is None or piu is None or pil is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        long_signal = self._long_armed and pc <= pil and close > inner_lower
        short_signal = self._short_armed and pc >= piu and close < inner_upper

        if long_signal:
            self._long_armed = False
            if self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal:
            self._short_armed = False
            if self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return dual_keltner_channels_strategy()
