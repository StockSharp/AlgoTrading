import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Lowest
from StockSharp.Algo.Strategies import Strategy


class doji_trading_strategy(Strategy):
    """
    Doji trading strategy.
    A doji is a candle whose body is at most Tolerance of its range. A doji closing above the EMA opens a long.
    The stop sits at the lowest low of the last StopBars candles at entry. Once price has risen TrailTriggerPercent above the entry,
    a trailing stop follows the highest high at TrailOffsetPercent below it.
    """

    def __init__(self):
        super(doji_trading_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_length = self.Param("EmaLength", 60).SetGreaterThanZero().SetDisplay("EMA Length", "EMA length", "Indicators")
        self._tolerance = self.Param("Tolerance", 0.05).SetNotNegative().SetDisplay("Tolerance", "Largest body as a fraction of the range for a doji", "Pattern")
        self._stop_bars = self.Param("StopBars", 450).SetGreaterThanZero().SetDisplay("Stop Bars", "Candles whose lowest low sets the stop", "Risk")
        self._trail_trigger_percent = self.Param("TrailTriggerPercent", 1.0).SetNotNegative().SetDisplay("Trail Trigger %", "Profit percent that activates the trailing stop", "Risk")
        self._trail_offset_percent = self.Param("TrailOffsetPercent", 0.5).SetNotNegative().SetDisplay("Trail Offset %", "Trailing stop distance below the highest high", "Risk")
        self._clear_trade()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _clear_trade(self):
        self._entry_price = None
        self._stop_price = None
        self._highest_since_entry = None
        self._trail_active = False

    def OnReseted(self):
        super(doji_trading_strategy, self).OnReseted()
        self._clear_trade()

    def OnStarted2(self, time):
        super(doji_trading_strategy, self).OnStarted2(time)

        self._clear_trade()

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        lowest = Lowest()
        lowest.Length = self._stop_bars.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        if not ema_value.IsFormed or not lowest_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self.Position > 0 and self._entry_price is not None:
            entry = self._entry_price
            self._highest_since_entry = high if self._highest_since_entry is None else max(self._highest_since_entry, high)

            if not self._trail_active and self._highest_since_entry >= entry * (1 + float(self._trail_trigger_percent.Value) / 100.0):
                self._trail_active = True

            stop = self._stop_price
            if self._trail_active:
                trail = self._highest_since_entry * (1 - float(self._trail_offset_percent.Value) / 100.0)
                stop = trail if stop is None else max(stop, trail)

            if stop is not None and low <= stop:
                self.SellMarket(self.Position)
                self._clear_trade()
            return

        candle_range = high - low
        body = abs(close - open_price)
        is_doji = candle_range > 0 and body <= candle_range * float(self._tolerance.Value)

        if self.Position == 0 and is_doji and close > float(ema_value.GetValue[Decimal](None)):
            self.BuyMarket()
            self._entry_price = close
            self._stop_price = float(lowest_value.GetValue[Decimal](None))
            self._highest_since_entry = None
            self._trail_active = False

    def CreateClone(self):
        return doji_trading_strategy()
