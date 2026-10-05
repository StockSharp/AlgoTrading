import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class candle245_breakout_strategy(Strategy):
    """
    Candle 2:45 Breakout Strategy.
    The candle covering TargetHour:TargetMinute (UTC) sets the reference high and low. During the next LookForwardBars candles a
    close above the high goes long and a close below the low goes short, reversing an opposite position. Any position is
    closed when the observation window ends.
    """

    def __init__(self):
        super(candle245_breakout_strategy, self).__init__()
        self._target_hour = self.Param("TargetHour", 2).SetRange(0, 23).SetDisplay("Target Hour", "Hour of the reference candle (UTC)", "Session")
        self._target_minute = self.Param("TargetMinute", 45).SetRange(0, 59).SetDisplay("Target Minute", "Minute of the reference candle", "Session")
        self._look_forward_bars = self.Param("LookForwardBars", 2).SetGreaterThanZero().SetDisplay("Look Forward Bars", "Candles after the reference candle during which breakouts are traded", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(45))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ref_high = None
        self._ref_low = None
        self._bars_left = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(candle245_breakout_strategy, self).OnReseted()
        self._ref_high = None
        self._ref_low = None
        self._bars_left = 0

    def OnStarted2(self, time):
        super(candle245_breakout_strategy, self).OnStarted2(time)

        self._ref_high = None
        self._ref_low = None
        self._bars_left = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # With frames that do not start exactly at the target time, the candle covering it is the reference.
        open_time = candle.OpenTime
        target = open_time.Date.Add(TimeSpan(self._target_hour.Value, self._target_minute.Value, 0))
        arg = self.candle_type.Arg
        frame = arg if isinstance(arg, TimeSpan) else TimeSpan.Zero
        is_reference = open_time == target or (open_time < target and target < open_time.Add(frame))

        if is_reference:
            self._ref_high = float(candle.HighPrice)
            self._ref_low = float(candle.LowPrice)
            self._bars_left = self._look_forward_bars.Value
            return

        if self._bars_left <= 0 or self._ref_high is None or self._ref_low is None:
            return

        self._bars_left -= 1

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._bars_left == 0:
            # The observation window ends with this candle.
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
            return

        close = float(candle.ClosePrice)

        if close > self._ref_high and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < self._ref_low and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return candle245_breakout_strategy()
