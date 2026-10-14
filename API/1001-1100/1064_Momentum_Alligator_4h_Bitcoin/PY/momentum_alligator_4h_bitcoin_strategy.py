import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AwesomeOscillator, SimpleMovingAverage, SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

SIGNALS_TO_SKIP = 2


class momentum_alligator_4h_bitcoin_strategy(Strategy):
    """
    Momentum Alligator 4h Bitcoin strategy.
    Goes long when the Awesome Oscillator crosses above its 5-period SMA and the close is above the jaw, teeth and lips
    of the daily Alligator, only between TradeStart and TradeStop. The position is closed by a stop at the higher of the
    percent stop below entry and the daily jaw. After a profitable exit the next two entry signals are skipped.
    """

    def __init__(self):
        super(momentum_alligator_4h_bitcoin_strategy, self).__init__()
        self._stop_loss_percent = self.Param("StopLossPercent", 0.02).SetNotNegative().SetDisplay("Stop Loss %", "Percent stop below entry as a fraction", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._trade_start = self.Param("TradeStart", DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Trade Start", "Entries are allowed from this time", "General")
        self._trade_stop = self.Param("TradeStop", DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Trade Stop", "Entries are allowed until this time", "General")
        self._ao_sma = None
        self._jaw = None
        self._teeth = None
        self._lips = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.TimeFrame(TimeSpan.FromDays(1)))]

    def _reset_state(self):
        self._prev_ao = None
        self._prev_ao_sma = None
        self._daily_jaw = None
        self._daily_teeth = None
        self._daily_lips = None
        self._entry_price = 0.0
        self._skip_count = 0

    def OnReseted(self):
        super(momentum_alligator_4h_bitcoin_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(momentum_alligator_4h_bitcoin_strategy, self).OnStarted2(time)

        self._reset_state()

        ao = AwesomeOscillator()
        self._ao_sma = SimpleMovingAverage()
        self._ao_sma.Length = 5
        self._jaw = SmoothedMovingAverage()
        self._jaw.Length = 13
        self._teeth = SmoothedMovingAverage()
        self._teeth.Length = 8
        self._lips = SmoothedMovingAverage()
        self._lips.Length = 5

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ao, self._process_candle).Start()

        self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromDays(1))).Bind(self._process_daily_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, ao)
                self.DrawIndicator(oscillators, self._ao_sma)

    def _process_daily_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # The Alligator lines are smoothed averages of the median price.
        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        jaw = float(process_float(self._jaw, median, candle.OpenTime, True))
        teeth = float(process_float(self._teeth, median, candle.OpenTime, True))
        lips = float(process_float(self._lips, median, candle.OpenTime, True))

        if self._jaw.IsFormed:
            self._daily_jaw = jaw
        if self._teeth.IsFormed:
            self._daily_teeth = teeth
        if self._lips.IsFormed:
            self._daily_lips = lips

    def _process_candle(self, candle, ao_value):
        if candle.State != CandleStates.Finished:
            return

        ao = float(ao_value)
        ao_sma = float(process_float(self._ao_sma, ao, candle.OpenTime, True))
        if not self._ao_sma.IsFormed:
            self._prev_ao = ao
            return

        prev_ao = self._prev_ao
        prev_ao_sma = self._prev_ao_sma
        self._prev_ao = ao
        self._prev_ao_sma = ao_sma

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            stop = self._entry_price * (1.0 - float(self._stop_loss_percent.Value))
            if self._daily_jaw is not None and self._daily_jaw > stop:
                stop = self._daily_jaw

            if float(candle.LowPrice) <= stop:
                exit_price = min(stop, float(candle.OpenPrice))
                if exit_price > self._entry_price:
                    self._skip_count = SIGNALS_TO_SKIP
                self.SellMarket(self.Position)
            return

        if prev_ao is None or prev_ao_sma is None:
            return

        if self._daily_jaw is None or self._daily_teeth is None or self._daily_lips is None:
            return

        cross_up = prev_ao <= prev_ao_sma and ao > ao_sma
        if not cross_up:
            return

        close = float(candle.ClosePrice)
        if close <= self._daily_jaw or close <= self._daily_teeth or close <= self._daily_lips:
            return

        open_time = candle.OpenTime
        if open_time < self._trade_start.Value.UtcDateTime or open_time >= self._trade_stop.Value.UtcDateTime:
            return

        if self._skip_count > 0:
            self._skip_count -= 1
            return

        self._entry_price = close
        self.BuyMarket(self.Volume)

    def CreateClone(self):
        return momentum_alligator_4h_bitcoin_strategy()
