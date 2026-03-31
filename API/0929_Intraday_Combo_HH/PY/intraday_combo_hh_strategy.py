import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator, MovingAverageConvergenceDivergenceSignal, BollingerBands
from StockSharp.Algo.Strategies import Strategy


class intraday_combo_hh_strategy(Strategy):
    def __init__(self):
        super(intraday_combo_hh_strategy, self).__init__()
        self._min_signals = self.Param("MinSignals", 2) \
            .SetDisplay("Min Signals", "Minimum required conditions", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_macd_signal = 0.0
        self._macd_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(intraday_combo_hh_strategy, self).OnReseted()
        self._prev_macd_signal = 0.0
        self._macd_initialized = False

    def OnStarted2(self, time):
        super(intraday_combo_hh_strategy, self).OnStarted2(time)
        stoch = StochasticOscillator()
        stoch.K.Length = 3
        stoch.D.Length = 3
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = 12
        macd.Macd.LongMa.Length = 26
        macd.SignalMa.Length = 9
        bb = BollingerBands()
        bb.Length = 20
        bb.Width = 2.0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stoch, macd, bb, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, stoch_val, macd_val, bb_val):
        if candle.State != CandleStates.Finished:
            return
        buy_count = 0
        sell_count = 0
        close = float(candle.ClosePrice)
        min_signals = self._min_signals.Value
        k_val = stoch_val.K
        if k_val is not None:
            k = float(k_val)
            if k < 20:
                buy_count += 1
            if k > 80:
                sell_count += 1
        signal_val = macd_val.Signal
        if signal_val is not None:
            signal = float(signal_val)
            if self._macd_initialized:
                if signal > self._prev_macd_signal:
                    buy_count += 1
                elif signal < self._prev_macd_signal:
                    sell_count += 1
            else:
                self._macd_initialized = True
            self._prev_macd_signal = signal
        lower = bb_val.LowBand
        upper = bb_val.UpBand
        if lower is not None and upper is not None:
            if close < float(lower):
                buy_count += 1
            if close > float(upper):
                sell_count += 1
        if buy_count >= min_signals and self.Position <= 0:
            self.BuyMarket()
        elif sell_count >= min_signals and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return intraday_combo_hh_strategy()
