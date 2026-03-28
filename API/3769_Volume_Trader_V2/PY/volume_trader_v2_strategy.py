import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class volume_trader_v2_strategy(Strategy):
    """Volume Trader V2: compares volume of the last two finished candles and trades
    only during configured hours. Goes long when previous volume < two-bars-ago volume,
    short when previous volume > two-bars-ago volume."""

    def __init__(self):
        super(volume_trader_v2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))) \
            .SetDisplay("Candle Type", "Time frame used to request candles", "Data")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "First hour (inclusive) when trading is allowed", "Trading")
        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("End Hour", "Last hour (inclusive) when trading is allowed", "Trading")
        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Trade Volume", "Order volume replicated from the original EA", "Trading")

        self._previous_volume = None
        self._two_bars_ago_volume = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def EndHour(self):
        return self._end_hour.Value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    def OnReseted(self):
        super(volume_trader_v2_strategy, self).OnReseted()
        self._previous_volume = None
        self._two_bars_ago_volume = None

    def OnStarted(self, time):
        super(volume_trader_v2_strategy, self).OnStarted(time)

        self.Volume = float(self.TradeVolume)

        self.StartProtection(None, None)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        current_volume = candle.TotalVolume

        if self._previous_volume is None:
            self._previous_volume = current_volume
            return

        if self._two_bars_ago_volume is None:
            self._two_bars_ago_volume = self._previous_volume
            self._previous_volume = current_volume
            return

        volume1 = self._previous_volume
        volume2 = self._two_bars_ago_volume

        hour = candle.OpenTime.Hour
        hour_valid = hour >= self.StartHour and hour <= self.EndHour

        should_go_long = hour_valid and volume1 < volume2
        should_go_short = hour_valid and volume1 > volume2

        if not should_go_long and not should_go_short:
            # Exit position when no direction is active
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
        elif should_go_long:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif should_go_short:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._two_bars_ago_volume = self._previous_volume
        self._previous_volume = current_volume

    def CreateClone(self):
        return volume_trader_v2_strategy()
