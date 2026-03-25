import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_zero_filter_take_profit_strategy(Strategy):
    def __init__(self):
        super(macd_zero_filter_take_profit_strategy, self).__init__()

        self._macd_fast = self.Param("MacdFast", 12)
        self._macd_slow = self.Param("MacdSlow", 26)
        self._macd_signal = self.Param("MacdSignal", 9)
        self._take_profit_points = self.Param("TakeProfitPoints", 300)
        self._volume_per_trade = self.Param("VolumePerTrade", 1.0)
        self._minimum_capital_per_volume = self.Param("MinimumCapitalPerVolume", 1000.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._previous_macd = None
        self._previous_signal = None

    @property
    def MacdFast(self):
        return self._macd_fast.Value

    @MacdFast.setter
    def MacdFast(self, value):
        self._macd_fast.Value = value

    @property
    def MacdSlow(self):
        return self._macd_slow.Value

    @MacdSlow.setter
    def MacdSlow(self, value):
        self._macd_slow.Value = value

    @property
    def MacdSignal(self):
        return self._macd_signal.Value

    @MacdSignal.setter
    def MacdSignal(self, value):
        self._macd_signal.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def VolumePerTrade(self):
        return self._volume_per_trade.Value

    @VolumePerTrade.setter
    def VolumePerTrade(self, value):
        self._volume_per_trade.Value = value

    @property
    def MinimumCapitalPerVolume(self):
        return self._minimum_capital_per_volume.Value

    @MinimumCapitalPerVolume.setter
    def MinimumCapitalPerVolume(self, value):
        self._minimum_capital_per_volume.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(macd_zero_filter_take_profit_strategy, self).OnStarted(time)

        self._previous_macd = None
        self._previous_signal = None

        self.Volume = self._volume_per_trade.Value

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.MacdFast
        macd.Macd.LongMa.Length = self.MacdSlow
        macd.SignalMa.Length = self.MacdSignal
        self._macd_ind = macd

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, self.ProcessCandle).Start()

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        tp_distance = int(self.TakeProfitPoints) * step

        self.StartProtection(
            takeProfit=Unit(tp_distance, UnitTypes.Absolute),
            stopLoss=Unit(0),
            useMarketOrders=True)

    def ProcessCandle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd_val = macd_value.Macd
        signal_val = macd_value.Signal

        if macd_val is None or signal_val is None:
            return

        macd_f = float(macd_val)
        signal_f = float(signal_val)

        if self._previous_macd is None or self._previous_signal is None:
            self._previous_macd = macd_f
            self._previous_signal = signal_f
            return

        prev_macd = self._previous_macd
        prev_signal = self._previous_signal

        crossed_up = prev_macd <= prev_signal and macd_f > signal_f
        crossed_down = prev_macd >= prev_signal and macd_f < signal_f

        if self.Position > 0 and crossed_down:
            self.SellMarket()
        elif self.Position < 0 and crossed_up:
            self.BuyMarket()

        if self.Position == 0:
            required_capital = float(self.MinimumCapitalPerVolume) * float(self.VolumePerTrade)
            portfolio = self.Portfolio
            current_value = float(portfolio.CurrentValue) if portfolio is not None and portfolio.CurrentValue is not None else 0.0
            has_capital = current_value >= required_capital or portfolio is None or portfolio.CurrentValue is None

            if has_capital:
                if crossed_up and macd_f < 0.0 and signal_f < 0.0:
                    self.BuyMarket()
                elif crossed_down and macd_f > 0.0 and signal_f > 0.0:
                    self.SellMarket()

        self._previous_macd = macd_f
        self._previous_signal = signal_f

    def OnReseted(self):
        super(macd_zero_filter_take_profit_strategy, self).OnReseted()
        self._previous_macd = None
        self._previous_signal = None

    def CreateClone(self):
        return macd_zero_filter_take_profit_strategy()
