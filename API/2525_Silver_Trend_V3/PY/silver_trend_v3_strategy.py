import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class silver_trend_v3_strategy(Strategy):
    def __init__(self):
        super(silver_trend_v3_strategy, self).__init__()
        self._count_bars = self.Param("CountBars", 150)
        self._ssp = self.Param("Ssp", 9)
        self._jtpo_length = self.Param("JtpoLength", 14)
        self._history_capacity = self.Param("HistoryCapacity", 220)
        self._risk = self.Param("Risk", 3)
        self._trailing_points = self.Param("TrailingStopPoints", 50.0)
        self._tp_points = self.Param("TakeProfitPoints", 50.0)
        self._sl_points = self.Param("InitialStopLossPoints", 0.0)
        self._friday_cutoff = self.Param("FridayCutoffHour", 16)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self.Volume = 1

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(silver_trend_v3_strategy, self).OnReseted()
        self._close_hist = []
        self._high_hist = []
        self._low_hist = []
        self._long_trailing = None
        self._short_trailing = None
        self._entry_price = 0.0
        self._prev_signal = 0
        self._point_value = 0.0

    def OnStarted2(self, time):
        super(silver_trend_v3_strategy, self).OnStarted2(time)
        self._close_hist = []
        self._high_hist = []
        self._low_hist = []
        self._long_trailing = None
        self._short_trailing = None
        self._entry_price = 0.0
        self._prev_signal = 0

        pv = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and float(self.Security.PriceStep) > 0:
            pv = float(self.Security.PriceStep)
        self._point_value = pv

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_history(candle)

        cb = int(self._count_bars.Value)
        ssp = int(self._ssp.Value)
        if len(self._close_hist) < cb + ssp + 1:
            return

        jtpo = self._calc_jtpo(int(self._jtpo_length.Value))
        signal = self._calc_signal()

        long_signal = self._prev_signal != signal and signal > 0 and jtpo > 0
        short_signal = self._prev_signal != signal and signal < 0 and jtpo < 0

        exit_long = self._prev_signal < 0
        exit_short = self._prev_signal > 0

        self._manage_position(candle, exit_long, exit_short)

        if self.Position <= 0 and long_signal and not self._is_friday_blocked(candle):
            self._enter_long(candle)
        elif self.Position >= 0 and short_signal and not self._is_friday_blocked(candle):
            self._enter_short(candle)

        self._prev_signal = signal

    def _update_history(self, candle):
        self._close_hist.append(float(candle.ClosePrice))
        self._high_hist.append(float(candle.HighPrice))
        self._low_hist.append(float(candle.LowPrice))
        cap = int(self._history_capacity.Value)
        if len(self._close_hist) > cap:
            self._close_hist.pop(0)
            self._high_hist.pop(0)
            self._low_hist.pop(0)

    def _manage_position(self, candle, exit_long_signal, exit_short_signal):
        if self.Position > 0:
            self._update_long_trailing(candle)

            initial_stop = None
            if float(self._sl_points.Value) > 0:
                initial_stop = self._entry_price - self._get_distance(float(self._sl_points.Value))
            trailing_stop = self._long_trailing
            stop = self._combine_long_stops(initial_stop, trailing_stop)
            take_profit = None
            if float(self._tp_points.Value) > 0:
                take_profit = self._entry_price + self._get_distance(float(self._tp_points.Value))

            if exit_long_signal or \
               (take_profit is not None and float(candle.HighPrice) >= take_profit) or \
               (stop is not None and float(candle.LowPrice) <= stop):
                self.SellMarket()
                self._reset_stops()

        elif self.Position < 0:
            self._update_short_trailing(candle)

            initial_stop = None
            if float(self._sl_points.Value) > 0:
                initial_stop = self._entry_price + self._get_distance(float(self._sl_points.Value))
            trailing_stop = self._short_trailing
            stop = self._combine_short_stops(initial_stop, trailing_stop)
            take_profit = None
            if float(self._tp_points.Value) > 0:
                take_profit = self._entry_price - self._get_distance(float(self._tp_points.Value))

            if exit_short_signal or \
               (take_profit is not None and float(candle.LowPrice) <= take_profit) or \
               (stop is not None and float(candle.HighPrice) >= stop):
                self.BuyMarket()
                self._reset_stops()
        else:
            self._reset_stops()

    def _enter_long(self, candle):
        volume = float(self.Volume)
        if self.Position < 0:
            volume += abs(float(self.Position))
        self.BuyMarket(volume)
        self._entry_price = float(candle.ClosePrice)
        self._long_trailing = None
        self._short_trailing = None

    def _enter_short(self, candle):
        volume = float(self.Volume)
        if self.Position > 0:
            volume += float(self.Position)
        self.SellMarket(volume)
        self._entry_price = float(candle.ClosePrice)
        self._long_trailing = None
        self._short_trailing = None

    def _update_long_trailing(self, candle):
        tp = float(self._trailing_points.Value)
        if tp <= 0:
            return
        dist = self._get_distance(tp)
        trigger = self._entry_price + dist
        close = float(candle.ClosePrice)
        if close > trigger:
            new_stop = close - dist
            if self._long_trailing is None or new_stop > self._long_trailing:
                self._long_trailing = new_stop

    def _update_short_trailing(self, candle):
        tp = float(self._trailing_points.Value)
        if tp <= 0:
            return
        dist = self._get_distance(tp)
        trigger = self._entry_price - dist
        close = float(candle.ClosePrice)
        if close < trigger:
            new_stop = close + dist
            if self._short_trailing is None or new_stop < self._short_trailing:
                self._short_trailing = new_stop

    def _combine_long_stops(self, initial, trailing):
        if initial is None and trailing is None:
            return None
        if initial is None:
            return trailing
        if trailing is None:
            return initial
        return max(initial, trailing)

    def _combine_short_stops(self, initial, trailing):
        if initial is None and trailing is None:
            return None
        if initial is None:
            return trailing
        if trailing is None:
            return initial
        return min(initial, trailing)

    def _reset_stops(self):
        if self.Position == 0:
            self._entry_price = 0.0
        self._long_trailing = None
        self._short_trailing = None

    def _is_friday_blocked(self, candle):
        cutoff = int(self._friday_cutoff.Value)
        if cutoff <= 0:
            return False
        t = candle.OpenTime
        return t.DayOfWeek == 5 and t.Hour > cutoff

    def _calc_signal(self):
        k = 33 - int(self._risk.Value)
        ssp = int(self._ssp.Value)
        cb = int(self._count_bars.Value)
        uptrend = False
        val = 0
        for i in range(cb - ssp, -1, -1):
            ss_max = self._get_high(i)
            ss_min = self._get_low(i)
            for i2 in range(i, i + ssp):
                h = self._get_high(i2)
                if ss_max < h:
                    ss_max = h
                lo = self._get_low(i2)
                if ss_min >= lo:
                    ss_min = lo
            smin_val = ss_min + (ss_max - ss_min) * k / 100.0
            smax_val = ss_max - (ss_max - ss_min) * k / 100.0
            c = self._get_close(i)
            if c < smin_val:
                uptrend = False
            if c > smax_val:
                uptrend = True
            val = 1 if uptrend else -1
        return val

    def _calc_jtpo(self, length):
        if len(self._close_hist) < 200:
            return 0.0

        f8 = 0.0
        f10 = 0.0
        f18 = 0.0
        f20 = 0.0
        f30 = 0.0
        f40 = 0.0
        k = 0.0
        var14 = 0.0
        var18 = 0.0
        var1C = 0.0
        var20 = 0.0
        var24 = 0.0
        value = 0.0
        f38 = 0
        f48 = 0
        arr0 = [0.0] * 400
        arr1 = [0.0] * 400
        arr2 = [0.0] * 400
        arr3 = [0.0] * 400

        for i in range(200 - length - 100, -1, -1):
            var14 = 0.0
            var1C = 0.0

            if f38 == 0:
                f38 = 1
                f40 = 0.0
                f30 = float(length - 1) if length - 1 >= 2 else 2.0
                f48 = int(f30) + 1
                f10 = self._get_close(i)
                arr0[f38] = f10
                k = float(f48)
                f18 = 12.0 / (k * (k - 1.0) * (k + 1.0))
                f20 = (f48 + 1) * 0.5
            else:
                if f38 <= f48:
                    f38 += 1
                else:
                    f38 = f48 + 1

                f8 = f10
                f10 = self._get_close(i)

                if f38 > f48:
                    for var6 in range(2, f48 + 1):
                        arr0[var6 - 1] = arr0[var6]
                    arr0[f48] = f10
                else:
                    arr0[f38] = f10

                if f30 >= f38 and f8 != f10:
                    f40 = 1.0

                if f30 == f38 and f40 == 0.0:
                    f38 = 0

            if f38 >= f48:
                for varA in range(1, f48 + 1):
                    arr2[varA] = float(varA)
                    arr3[varA] = float(varA)
                    arr1[varA] = arr0[varA]

                for varA in range(1, f48):
                    var24 = arr1[varA]
                    var12 = varA
                    for var6 in range(varA + 1, f48 + 1):
                        if arr1[var6] < var24:
                            var24 = arr1[var6]
                            var12 = var6
                    var20 = arr1[varA]
                    arr1[varA] = arr1[var12]
                    arr1[var12] = var20
                    var20 = arr2[varA]
                    arr2[varA] = arr2[var12]
                    arr2[var12] = var20

                varIndex = 1
                while f48 > varIndex:
                    var6 = varIndex + 1
                    var14 = 1.0
                    var1C = arr3[varIndex]
                    while var14 != 0.0 and var6 < len(arr3):
                        if arr1[varIndex] != arr1[var6]:
                            if (var6 - varIndex) > 1:
                                var1C /= float(var6 - varIndex)
                                for varE in range(varIndex, var6):
                                    arr3[varE] = var1C
                            var14 = 0.0
                        else:
                            var1C += arr3[var6]
                            var6 += 1
                            if var6 > f48 + 1:
                                break
                    varIndex = var6

                var1C = 0.0
                for varA in range(1, f48 + 1):
                    var1C += (arr3[varA] - f20) * (arr2[varA] - f20)
                var18 = f18 * var1C
            else:
                var18 = 0.0

            value = var18
            if value == 0.0:
                value = 0.00001

        return value

    def _get_close(self, shift):
        idx = len(self._close_hist) - 1 - shift
        if idx < 0:
            idx = 0
        return self._close_hist[idx]

    def _get_high(self, shift):
        idx = len(self._high_hist) - 1 - shift
        if idx < 0:
            idx = 0
        return self._high_hist[idx]

    def _get_low(self, shift):
        idx = len(self._low_hist) - 1 - shift
        if idx < 0:
            idx = 0
        return self._low_hist[idx]

    def _get_distance(self, points):
        return points * self._point_value

    def CreateClone(self):
        return silver_trend_v3_strategy()
