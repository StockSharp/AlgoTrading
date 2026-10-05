import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class combo_2_20_ema_bandpass_filter_strategy(Strategy):
    """
    Combo 2/20 EMA and Bandpass Filter strategy.
    An Ehlers bandpass filter of the median price is combined with a fast/slow EMA trend: long when the fast EMA is above the slow
    one and the filter is above BpfSellZone, short when the fast EMA is below the slow one and the filter is below BpfBuyZone.
    The position closes when neither signal holds and nothing is traded before StartDate.
    """

    def __init__(self):
        super(combo_2_20_ema_bandpass_filter_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 2).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "EMA")
        self._slow_ema_length = self.Param("SlowEmaLength", 20).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "EMA")
        self._bpf_length = self.Param("BpfLength", 20).SetGreaterThanZero().SetDisplay("BPF Length", "Bandpass filter period", "Bandpass")
        self._bpf_delta = self.Param("BpfDelta", 0.5).SetGreaterThanZero().SetDisplay("BPF Delta", "Bandpass filter bandwidth", "Bandpass")
        self._bpf_sell_zone = self.Param("BpfSellZone", 5.0).SetDisplay("BPF Sell Zone", "Filter level the long signal has to exceed", "Bandpass")
        self._bpf_buy_zone = self.Param("BpfBuyZone", -5.0).SetDisplay("BPF Buy Zone", "Filter level the short signal has to fall below", "Bandpass")
        self._start_date = self.Param("StartDate", DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Start Date", "Date trading starts from", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._price1 = None
        self._price2 = None
        self._bp1 = 0.0
        self._bp2 = 0.0

    def OnReseted(self):
        super(combo_2_20_ema_bandpass_filter_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(combo_2_20_ema_bandpass_filter_strategy, self).OnStarted2(time)

        self._reset_state()

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_ema, slow_ema):
        if candle.State != CandleStates.Finished:
            return

        price = float((candle.HighPrice + candle.LowPrice) / 2)
        bp = None

        if self._price2 is not None:
            length = self._bpf_length.Value
            beta = math.cos(math.pi * (360.0 / length) / 180.0)
            gamma = 1.0 / math.cos(math.pi * (720.0 * float(self._bpf_delta.Value) / length) / 180.0)
            alpha = gamma - math.sqrt(gamma * gamma - 1.0)

            bp = 0.5 * (1.0 - alpha) * (price - self._price2) + beta * (1.0 + alpha) * self._bp1 - alpha * self._bp2
            self._bp2 = self._bp1
            self._bp1 = bp

        self._price2 = self._price1
        self._price1 = price

        if bp is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if candle.OpenTime < self._start_date.Value.UtcDateTime:
            self._close_position()
            return

        fast = float(fast_ema)
        slow = float(slow_ema)
        long_signal = fast > slow and bp > float(self._bpf_sell_zone.Value)
        short_signal = fast < slow and bp < float(self._bpf_buy_zone.Value)

        if long_signal:
            if self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal:
            if self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
        else:
            self._close_position()

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return combo_2_20_ema_bandpass_filter_strategy()
