import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class macd_volume_xauusd_strategy(Strategy):
    """
    MACD Volume XAUUSD strategy.
    The volume oscillator is 100 * (EMA(volume, ShortLength) - EMA(volume, LongLength)) / EMA(volume, LongLength). The MACD line
    crossing above zero with a positive oscillator and volume above the previous candle's volume goes long; crossing below zero
    under the same volume conditions goes short, reversing an opposite position. The stop is StopLoss price steps from the entry
    and the take profit is StopLoss * TakeProfitMultiplier steps. Leverage scales the order volume.
    """

    def __init__(self):
        super(macd_volume_xauusd_strategy, self).__init__()
        self._short_length = self.Param("ShortLength", 5).SetGreaterThanZero().SetDisplay("Short Length", "Short volume EMA length", "Volume")
        self._long_length = self.Param("LongLength", 8).SetGreaterThanZero().SetDisplay("Long Length", "Long volume EMA length", "Volume")
        self._fast_length = self.Param("FastLength", 16).SetGreaterThanZero().SetDisplay("Fast Length", "MACD fast EMA length", "MACD")
        self._slow_length = self.Param("SlowLength", 26).SetGreaterThanZero().SetDisplay("Slow Length", "MACD slow EMA length", "MACD")
        self._signal_length = self.Param("SignalLength", 9).SetGreaterThanZero().SetDisplay("Signal Length", "MACD signal line length", "MACD")
        self._leverage = self.Param("Leverage", 1.0).SetGreaterThanZero().SetDisplay("Leverage", "Multiplier of the order volume", "Risk")
        self._stop_loss = self.Param("StopLoss", 10100.0).SetNotNegative().SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk")
        self._take_profit_multiplier = self.Param("TakeProfitMultiplier", 1.1).SetNotNegative().SetDisplay("Take Profit Multiplier", "Take profit distance as a multiple of the stop loss", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._short_volume_ema = None
        self._long_volume_ema = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_macd = None
        self._prev_volume = None

    def OnReseted(self):
        super(macd_volume_xauusd_strategy, self).OnReseted()
        self._short_volume_ema = None
        self._long_volume_ema = None
        self._reset_state()

    def OnStarted2(self, time):
        super(macd_volume_xauusd_strategy, self).OnStarted2(time)

        self._reset_state()

        self._short_volume_ema = ExponentialMovingAverage()
        self._short_volume_ema.Length = self._short_length.Value
        self._long_volume_ema = ExponentialMovingAverage()
        self._long_volume_ema.Length = self._long_length.Value

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_length.Value
        macd.Macd.LongMa.Length = self._slow_length.Value
        macd.SignalMa.Length = self._signal_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

        step = Decimal(1)
        if self.Security is not None and self.Security.PriceStep is not None:
            step = self.Security.PriceStep
        stop = Decimal(self._stop_loss.Value)
        take = stop * Decimal(self._take_profit_multiplier.Value)
        self.StartProtection(Unit(take * step, UnitTypes.Absolute), Unit(stop * step, UnitTypes.Absolute), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        volume = candle.TotalVolume
        short_value = process_value(self._short_volume_ema, volume, candle.OpenTime, True)
        long_value = process_value(self._long_volume_ema, volume, candle.OpenTime, True)

        prev_volume = self._prev_volume
        self._prev_volume = volume

        if not macd_value.IsFormed:
            return
        macd_line = macd_value.Macd
        if macd_line is None:
            return

        pm = self._prev_macd
        self._prev_macd = macd_line

        if not self._short_volume_ema.IsFormed or not self._long_volume_ema.IsFormed or pm is None or prev_volume is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        zero = Decimal(0)
        long_ema = long_value.GetValue[Decimal](None)
        if long_ema == zero:
            return

        oscillator = Decimal(100) * (short_value.GetValue[Decimal](None) - long_ema) / long_ema
        volume_ok = oscillator > zero and volume > prev_volume
        order_volume = self.Volume * Decimal(self._leverage.Value)

        if volume_ok and pm <= zero and macd_line > zero and self.Position <= 0:
            self.BuyMarket(order_volume + abs(self.Position))
        elif volume_ok and pm >= zero and macd_line < zero and self.Position >= 0:
            self.SellMarket(order_volume + abs(self.Position))

    def CreateClone(self):
        return macd_volume_xauusd_strategy()
