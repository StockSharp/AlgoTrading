import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class doji_trader_strategy(Strategy):
    def __init__(self):
        super(doji_trader_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 50.0)
        self._take_profit_pips = self.Param("TakeProfitPips", 50.0)
        self._start_hour = self.Param("StartHour", 8)
        self._end_hour = self.Param("EndHour", 17)
        self._maximum_doji_height = self.Param("MaximumDojiHeight", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._previous_candle = None
        self._two_ago_candle = None
        self._three_ago_candle = None
        self._pip_size = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def EndHour(self):
        return self._end_hour.Value

    @property
    def MaximumDojiHeight(self):
        return self._maximum_doji_height.Value

    def OnStarted2(self, time):
        super(doji_trader_strategy, self).OnStarted2(time)

        self._pip_size = self._calculate_pip_size()

        tp_unit = Unit(self.TakeProfitPips * self._pip_size, UnitTypes.Absolute) if self.TakeProfitPips > 0 and self._pip_size > 0 else Unit()
        sl_unit = Unit(self.StopLossPips * self._pip_size, UnitTypes.Absolute) if self.StopLossPips > 0 and self._pip_size > 0 else Unit()

        self.StartProtection(tp_unit, sl_unit)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        next_hour = candle.CloseTime.Hour
        if next_hour < self.StartHour or next_hour >= self.EndHour:
            self._shift_history(candle)
            return

        if self._two_ago_candle is None:
            self._shift_history(candle)
            return

        pip_size = self._pip_size if self._pip_size > 0 else self._calculate_pip_size()
        if pip_size <= 0:
            pip_size = 0.0001
        self._pip_size = pip_size
        doji_height = self.MaximumDojiHeight * pip_size

        doji_high = 0.0
        doji_low = 0.0

        if self._is_doji(self._two_ago_candle, doji_height):
            doji_high = float(self._two_ago_candle.HighPrice)
            doji_low = float(self._two_ago_candle.LowPrice)
        elif self._three_ago_candle is not None and self._is_doji(self._three_ago_candle, doji_height):
            doji_high = float(self._three_ago_candle.HighPrice)
            doji_low = float(self._three_ago_candle.LowPrice)
        else:
            self._shift_history(candle)
            return

        direction = 0
        if float(candle.ClosePrice) > doji_high:
            direction = 1
        elif float(candle.ClosePrice) < doji_low:
            direction = -1

        if direction != 0 and self.Volume > 0:
            if direction > 0:
                self.BuyMarket()
            else:
                self.SellMarket()

        self._shift_history(candle)

    def _calculate_pip_size(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        digits = 0
        value = step
        while value < 1.0 and digits < 10:
            value *= 10.0
            digits += 1
        multiplier = 10.0 if (digits == 3 or digits == 5) else 1.0
        return step * multiplier

    def _is_doji(self, candle, threshold):
        body = abs(float(candle.OpenPrice) - float(candle.ClosePrice))
        return body <= threshold

    def _shift_history(self, candle):
        self._three_ago_candle = self._two_ago_candle
        self._two_ago_candle = self._previous_candle
        self._previous_candle = candle

    def OnReseted(self):
        super(doji_trader_strategy, self).OnReseted()
        self._previous_candle = None
        self._two_ago_candle = None
        self._three_ago_candle = None
        self._pip_size = 0.0

    def CreateClone(self):
        return doji_trader_strategy()
