import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class heatmap_macd_strategy(Strategy):
    """
    Heatmap MACD strategy.
    A MACD histogram (MACD minus signal) is calculated on each of five timeframes. When all five turn positive after not all
    being positive the strategy goes long; when all five turn negative after not all being negative it goes short, reversing an
    opposite position. With CloseOnOpposite a position is also closed as soon as any histogram flips against it.
    """

    def __init__(self):
        super(heatmap_macd_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 9).SetGreaterThanZero().SetDisplay("Fast Length", "Fast EMA length of MACD", "MACD")
        self._slow_length = self.Param("SlowLength", 26).SetGreaterThanZero().SetDisplay("Slow Length", "Slow EMA length of MACD", "MACD")
        self._signal_length = self.Param("SignalLength", 9).SetGreaterThanZero().SetDisplay("Signal Length", "Signal line length", "MACD")
        self._time_frame1 = self.Param("TimeFrame1", DataType.TimeFrame(TimeSpan.FromMinutes(60))).SetDisplay("Timeframe 1", "First timeframe", "Timeframes")
        self._time_frame2 = self.Param("TimeFrame2", DataType.TimeFrame(TimeSpan.FromMinutes(120))).SetDisplay("Timeframe 2", "Second timeframe", "Timeframes")
        self._time_frame3 = self.Param("TimeFrame3", DataType.TimeFrame(TimeSpan.FromMinutes(240))).SetDisplay("Timeframe 3", "Third timeframe", "Timeframes")
        self._time_frame4 = self.Param("TimeFrame4", DataType.TimeFrame(TimeSpan.FromMinutes(240))).SetDisplay("Timeframe 4", "Fourth timeframe", "Timeframes")
        self._time_frame5 = self.Param("TimeFrame5", DataType.TimeFrame(TimeSpan.FromMinutes(480))).SetDisplay("Timeframe 5", "Fifth timeframe", "Timeframes")
        self._close_on_opposite = self.Param("CloseOnOpposite", False).SetDisplay("Close On Opposite", "Close the position when any histogram flips against it", "Trading")
        self._reset_state()

    def _time_frames(self):
        return [self._time_frame1.Value, self._time_frame2.Value, self._time_frame3.Value, self._time_frame4.Value, self._time_frame5.Value]

    def GetWorkingSecurities(self):
        result = []
        for dt in self._time_frames():
            if dt not in [x[1] for x in result]:
                result.append((self.Security, dt))
        return result

    def _reset_state(self):
        self._histograms = [None] * 5
        self._was_all_positive = False
        self._was_all_negative = False

    def OnReseted(self):
        super(heatmap_macd_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(heatmap_macd_strategy, self).OnStarted2(time)

        self._reset_state()

        for i, tf in enumerate(self._time_frames()):
            macd = MovingAverageConvergenceDivergenceSignal()
            macd.Macd.ShortMa.Length = self._fast_length.Value
            macd.Macd.LongMa.Length = self._slow_length.Value
            macd.SignalMa.Length = self._signal_length.Value

            subscription = self.SubscribeCandles(tf)
            subscription.BindEx(macd, self._make_handler(i)).Start()

            if i == 0:
                area = self.CreateChartArea()
                if area is not None:
                    self.DrawCandles(area, subscription)
                    self.DrawOwnTrades(area)

    def _make_handler(self, index):
        return lambda candle, value: self._process_candle(index, candle, value)

    def _process_candle(self, index, candle, value):
        if candle.State != CandleStates.Finished:
            return

        if not value.IsFormed or value.Macd is None or value.Signal is None:
            return

        self._histograms[index] = float(value.Macd) - float(value.Signal)

        if any(h is None for h in self._histograms):
            return

        all_positive = all(h > 0 for h in self._histograms)
        all_negative = all(h < 0 for h in self._histograms)
        any_positive = any(h > 0 for h in self._histograms)
        any_negative = any(h < 0 for h in self._histograms)

        was_all_positive = self._was_all_positive
        was_all_negative = self._was_all_negative
        self._was_all_positive = all_positive
        self._was_all_negative = all_negative

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if all_positive and not was_all_positive and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif all_negative and not was_all_negative and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self._close_on_opposite.Value:
            if self.Position > 0 and any_negative:
                self.SellMarket(self.Position)
            elif self.Position < 0 and any_positive:
                self.BuyMarket(-self.Position)

    def CreateClone(self):
        return heatmap_macd_strategy()
