import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Highest, Lowest, SuperTrend
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class elliott_wave_supertrend_exit_strategy(Strategy):
    """
    Elliott Wave Supertrend Exit strategy.
    A candle whose low is the lowest low of the last WaveLength candles is a local low and goes long; a candle whose high
    is the highest high of the last WaveLength candles is a local high and goes short, reversing an opposite position.
    A long closes when the Supertrend turns down and a short when it turns up, and a percent stop limits the loss.
    """

    def __init__(self):
        super(elliott_wave_supertrend_exit_strategy, self).__init__()
        self._wave_length = self.Param("WaveLength", 4).SetGreaterThanZero().SetDisplay("Wave Length", "Candles that define a local high or low", "Indicators")
        self._supertrend_length = self.Param("SupertrendLength", 10).SetGreaterThanZero().SetDisplay("Supertrend Length", "Supertrend ATR length", "Indicators")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0).SetGreaterThanZero().SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 10.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._highest = None
        self._lowest = None
        self._prev_is_up_trend = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(elliott_wave_supertrend_exit_strategy, self).OnReseted()
        self._prev_is_up_trend = None

    def OnStarted2(self, time):
        super(elliott_wave_supertrend_exit_strategy, self).OnStarted2(time)

        self._prev_is_up_trend = None

        self._highest = Highest()
        self._highest.Length = self._wave_length.Value
        self._lowest = Lowest()
        self._lowest.Length = self._wave_length.Value
        supertrend = SuperTrend()
        supertrend.Length = self._supertrend_length.Value
        supertrend.Multiplier = Decimal(self._supertrend_multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(supertrend, self._process_candle).Start()

        stop_percent = Decimal(self._stop_loss_percent.Value)
        stop = Unit(stop_percent, UnitTypes.Percent) if stop_percent > 0 else Unit()
        self.StartProtection(Unit(), stop, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, supertrend_value):
        if candle.State != CandleStates.Finished:
            return

        high_result = process_float(self._highest, candle.HighPrice, candle.OpenTime, True)
        low_result = process_float(self._lowest, candle.LowPrice, candle.OpenTime, True)

        if not supertrend_value.IsFormed or not high_result.IsFormed or not low_result.IsFormed:
            return

        is_up_trend = bool(supertrend_value.IsUpTrend)
        prev_is_up_trend = self._prev_is_up_trend
        self._prev_is_up_trend = is_up_trend

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        local_high = float(candle.HighPrice) >= float(high_result)
        local_low = float(candle.LowPrice) <= float(low_result)

        # A candle that is both the highest and the lowest of the window gives no direction.
        if local_low and not local_high and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif local_high and not local_low and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and prev_is_up_trend is True and not is_up_trend:
            self.SellMarket(self.Position)
        elif self.Position < 0 and prev_is_up_trend is False and is_up_trend:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return elliott_wave_supertrend_exit_strategy()
