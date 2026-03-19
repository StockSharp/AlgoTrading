import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class burg_extrapolator_forecast_strategy(Strategy):
    """
    Burg extrapolator forecast strategy.
    Predicts future price path with Burg linear prediction coefficients and trades on forecasted extremes.
    """

    def __init__(self):
        super(burg_extrapolator_forecast_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Primary timeframe used for forecasting", "General")
        self._max_risk = self.Param("MaxRisk", 0.5) \
            .SetDisplay("Max Risk", "Risk factor controlling position scaling", "Money Management")
        self._max_trades = self.Param("MaxTrades", 5) \
            .SetDisplay("Max Trades", "Maximum stacked trades per direction", "Money Management")
        self._min_profit = self.Param("MinProfit", 160) \
            .SetDisplay("Min Profit", "Forecasted profit in points required to open trades", "Signals")
        self._max_loss = self.Param("MaxLoss", 130) \
            .SetDisplay("Max Loss", "Forecasted adverse excursion closing existing trades", "Signals")
        self._take_profit = self.Param("TakeProfit", 0) \
            .SetDisplay("Take Profit", "Optional fixed take profit in points", "Protection")
        self._stop_loss = self.Param("StopLoss", 180) \
            .SetDisplay("Stop Loss", "Optional fixed stop loss in points", "Protection")
        self._trailing_stop = self.Param("TrailingStop", 10) \
            .SetDisplay("Trailing Stop", "Trailing distance in points", "Protection")
        self._past_bars = self.Param("PastBars", 200) \
            .SetDisplay("Past Bars", "History length used for Burg model", "Forecast")
        self._model_order = self.Param("ModelOrder", 0.37) \
            .SetDisplay("Model Order", "Fraction of past bars used as Burg order", "Forecast")
        self._use_momentum = self.Param("UseMomentum", True) \
            .SetDisplay("Use Momentum", "Use logarithmic momentum instead of raw prices", "Forecast")
        self._use_rate_of_change = self.Param("UseRateOfChange", False) \
            .SetDisplay("Use ROC", "Use percentage rate of change instead of raw prices", "Forecast")

        self._open_history = []
        self._np = 0
        self._no = 0
        self._nf = 0
        self._average_price = 0.0
        self._is_first_run = True
        self._samples = []
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_high = None
        self._short_low = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(burg_extrapolator_forecast_strategy, self).OnReseted()
        self._open_history = []
        self._np = 0
        self._no = 0
        self._nf = 0
        self._average_price = 0.0
        self._is_first_run = True
        self._samples = []
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_high = None
        self._short_low = None

    def OnStarted(self, time):
        super(burg_extrapolator_forecast_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _get_open(self, shift):
        idx = len(self._open_history) - 1 - shift
        if 0 <= idx < len(self._open_history):
            return self._open_history[idx]
        return 0.0

    def _ensure_model(self):
        np_val = self._past_bars.Value
        if np_val < 3:
            return False

        mo = self._model_order.Value
        no = int(mo * np_val)
        if no < 1:
            no = 1
        if no >= np_val - 1:
            no = np_val - 2
        nf = np_val - no - 1
        if nf < 1:
            nf = 1

        if self._np != np_val or self._no != no or self._nf != nf:
            self._np = np_val
            self._no = no
            self._nf = nf
            self._samples = [0.0] * np_val
            self._average_price = 0.0
            self._is_first_run = True

        return True

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._open_history.append(float(candle.OpenPrice))

        if not self._ensure_model():
            return

        max_hist = self._np + 1
        while len(self._open_history) > max_hist:
            self._open_history.pop(0)

        if len(self._open_history) < self._np + 1:
            return

        if not self._update_samples():
            return

        predictions = self._compute_predictions()
        if predictions is None:
            return

        open_signal, close_signal = self._evaluate_signals(predictions)

        if self._manage_protection(candle):
            return

        self._handle_signal_closures(open_signal, close_signal)

        if open_signal == 1:
            self._try_open_long(candle)
        elif open_signal == -1:
            self._try_open_short(candle)

    def _update_samples(self):
        use_mom = self._use_momentum.Value
        use_roc = not use_mom and self._use_rate_of_change.Value

        if use_mom or use_roc:
            if not self._is_first_run:
                for i in range(self._np - 1):
                    self._samples[i] = self._samples[i + 1]
                current = self._get_open(0)
                previous = self._get_open(1)
                if previous == 0:
                    return False
                ratio = current / previous
                self._samples[self._np - 1] = math.log(ratio) if use_mom else ratio - 1.0
            else:
                for i in range(self._np):
                    current = self._get_open(i)
                    previous = self._get_open(i + 1)
                    if previous == 0:
                        return False
                    ratio = current / previous
                    self._samples[self._np - 1 - i] = math.log(ratio) if use_mom else ratio - 1.0
                self._average_price = 0.0
                self._is_first_run = False
        else:
            if self._is_first_run:
                total = sum(self._get_open(i) for i in range(self._np))
                self._average_price = total / self._np
                for i in range(self._np):
                    self._samples[self._np - 1 - i] = self._get_open(i) - self._average_price
                self._is_first_run = False
            else:
                newest = self._get_open(0)
                leaving = self._get_open(self._np)
                self._average_price += (newest - leaving) / self._np
                for i in range(self._np):
                    self._samples[self._np - 1 - i] = self._get_open(i) - self._average_price

        return True

    def _compute_predictions(self):
        coefficients = [0.0] * (self._no + 1)
        pred_len = self._nf + 1
        predictions = [0.0] * pred_len

        den = sum(v * v for v in self._samples) * 2.0
        df = list(self._samples)
        db = list(self._samples)
        r = 0.0

        for k in range(1, self._no + 1):
            num = sum(df[i] * db[i - 1] for i in range(k, self._np))
            denom = (1.0 - r * r) * den - df[k - 1] * df[k - 1] - db[self._np - 1] * db[self._np - 1]
            if abs(denom) < 1e-12:
                return None
            r = -2.0 * num / denom
            coefficients[k] = r

            half = k // 2
            for i in range(1, half + 1):
                ki = k - i
                tmp = coefficients[i]
                coefficients[i] += r * coefficients[ki]
                if i != ki:
                    coefficients[ki] += r * tmp

            if k < self._no:
                for i in range(self._np - 1, k - 1, -1):
                    tmp = df[i]
                    df[i] += r * db[i - 1]
                    db[i] = db[i - 1] + r * tmp
            den = denom

        for n in range(self._np - 1, self._np + self._nf):
            s = 0.0
            for i in range(1, self._no + 1):
                if n - i < self._np:
                    s -= coefficients[i] * self._samples[n - i]
                else:
                    s -= coefficients[i] * predictions[n - i - self._np + 1]
            idx = n - self._np + 1
            if idx < len(predictions):
                predictions[idx] = s

        use_mom = self._use_momentum.Value
        use_roc = not use_mom and self._use_rate_of_change.Value

        if use_mom or use_roc:
            start_price = self._get_open(0)
            predictions[0] = start_price
            for i in range(1, len(predictions)):
                if use_mom:
                    predictions[i] = predictions[i - 1] * math.exp(predictions[i])
                else:
                    predictions[i] = predictions[i - 1] * (1.0 + predictions[i])
        else:
            for i in range(len(predictions)):
                predictions[i] += self._average_price

        return predictions

    def _evaluate_signals(self, predictions):
        step = self.Security.PriceStep if self.Security.PriceStep is not None else 1.0
        max_loss_delta = self._max_loss.Value * step
        min_profit_delta = self._min_profit.Value * step

        ymax = predictions[0]
        ymin = ymax
        imax = 0
        imin = 0
        open_signal = 0
        close_signal = 0
        limit = min(self._np, len(predictions))

        for i in range(1, limit):
            value = predictions[i]
            if value > ymax and open_signal == 0:
                ymax = value
                imax = i
                if imin == 0 and ymax - ymin >= max_loss_delta:
                    close_signal = 1
                if imin == 0 and ymax - ymin >= min_profit_delta:
                    open_signal = 1
            if value < ymin and open_signal == 0:
                ymin = value
                imin = i
                if imax == 0 and ymax - ymin >= max_loss_delta:
                    close_signal = -1
                if imax == 0 and ymax - ymin >= min_profit_delta:
                    open_signal = -1

        return open_signal, close_signal

    def _manage_protection(self, candle):
        step = self.Security.PriceStep if self.Security.PriceStep is not None else 1.0
        stop_dist = self._stop_loss.Value * step
        take_dist = self._take_profit.Value * step
        trail_dist = self._trailing_stop.Value * step

        if self.Position > 0:
            if self._long_entry_price is None:
                self._long_entry_price = float(candle.ClosePrice)
            self._long_high = max(self._long_high, float(candle.HighPrice)) if self._long_high is not None else float(candle.HighPrice)

            if self._stop_loss.Value > 0 and float(candle.LowPrice) <= self._long_entry_price - stop_dist:
                self.SellMarket()
                self._long_entry_price = None
                self._long_high = None
                return True
            if self._take_profit.Value > 0 and float(candle.HighPrice) >= self._long_entry_price + take_dist:
                self.SellMarket()
                self._long_entry_price = None
                self._long_high = None
                return True
            if self._trailing_stop.Value > 0 and self._stop_loss.Value > 0 and self._long_high is not None:
                if float(candle.LowPrice) <= self._long_high - trail_dist:
                    self.SellMarket()
                    self._long_entry_price = None
                    self._long_high = None
                    return True
        else:
            self._long_entry_price = None
            self._long_high = None

        if self.Position < 0:
            if self._short_entry_price is None:
                self._short_entry_price = float(candle.ClosePrice)
            self._short_low = min(self._short_low, float(candle.LowPrice)) if self._short_low is not None else float(candle.LowPrice)

            if self._stop_loss.Value > 0 and float(candle.HighPrice) >= self._short_entry_price + stop_dist:
                self.BuyMarket()
                self._short_entry_price = None
                self._short_low = None
                return True
            if self._take_profit.Value > 0 and float(candle.LowPrice) <= self._short_entry_price - take_dist:
                self.BuyMarket()
                self._short_entry_price = None
                self._short_low = None
                return True
            if self._trailing_stop.Value > 0 and self._stop_loss.Value > 0 and self._short_low is not None:
                if float(candle.HighPrice) >= self._short_low + trail_dist:
                    self.BuyMarket()
                    self._short_entry_price = None
                    self._short_low = None
                    return True
        else:
            self._short_entry_price = None
            self._short_low = None

        return False

    def _handle_signal_closures(self, open_signal, close_signal):
        if self.Position > 0 and (close_signal == -1 or open_signal == -1):
            self.SellMarket()
            self._long_entry_price = None
            self._long_high = None
        elif self.Position < 0 and (close_signal == 1 or open_signal == 1):
            self.BuyMarket()
            self._short_entry_price = None
            self._short_low = None

    def _try_open_long(self, candle):
        base_vol = self.Volume
        if base_vol <= 0:
            return
        trade_count = int(math.ceil(abs(self.Position) / base_vol - 1e-8)) if base_vol > 0 else 0
        if trade_count >= self._max_trades.Value:
            return
        self.BuyMarket()
        self._long_entry_price = float(candle.ClosePrice)
        self._long_high = float(candle.ClosePrice)
        self._short_entry_price = None
        self._short_low = None

    def _try_open_short(self, candle):
        base_vol = self.Volume
        if base_vol <= 0:
            return
        trade_count = int(math.ceil(abs(self.Position) / base_vol - 1e-8)) if base_vol > 0 else 0
        if trade_count >= self._max_trades.Value:
            return
        self.SellMarket()
        self._short_entry_price = float(candle.ClosePrice)
        self._short_low = float(candle.ClosePrice)
        self._long_entry_price = None
        self._long_high = None

    def CreateClone(self):
        return burg_extrapolator_forecast_strategy()
