import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from collections import deque
from datatype_extensions import *
from indicator_extensions import *
import math

class neural_network_template_strategy(Strategy):
    def __init__(self):
        super(neural_network_template_strategy, self).__init__()
        self._bars_to_pattern = self.Param("BarsToPattern", 3).SetGreaterThanZero().SetDisplay("Bars", "Candles analysed", "Model")
        self._max_tp_points = self.Param("MaxTakeProfitPoints", 500).SetGreaterThanZero().SetDisplay("Max TP", "Maximum take-profit in points", "Risk")
        self._min_target = self.Param("MinTargetPoints", 1).SetGreaterThanZero().SetDisplay("Min Target", "Minimum projected move in points", "Model")
        self._sl_points = self.Param("StopLossPoints", 300).SetGreaterThanZero().SetDisplay("Stop-Loss", "Stop-loss distance in points", "Risk")
        self._profit_mult = self.Param("ProfitMultiply", 0.8).SetNotNegative().SetDisplay("Profit Mult", "Take-profit multiplier", "Model")
        self._trade_level = self.Param("TradeLevel", 0.1).SetDisplay("Trade Level", "Required confidence", "Model")
        self._trade_volume = self.Param("TradeVolume", 0.1).SetGreaterThanZero().SetDisplay("Volume", "Order volume", "Trading")
        self._candle_type = self.Param("CandleType", tf(5)).SetDisplay("TF", "Working timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(neural_network_template_strategy, self).OnReseted()
        self._rsi_history = deque()
        self._macd_history = deque()
        self._rsi_sum = 0
        self._macd_sum = 0
        self._target_price = None
        self._stop_price = None
        self._pos_dir = 0

    def OnStarted(self, time):
        super(neural_network_template_strategy, self).OnStarted(time)
        self._rsi_history = deque()
        self._macd_history = deque()
        self._rsi_sum = 0
        self._macd_sum = 0
        self._target_price = None
        self._stop_price = None
        self._pos_dir = 0

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = 12
        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = 12
        self._macd.Macd.LongMa.Length = 48
        self._macd.SignalMa.Length = 12

        sub = self.SubscribeCandles(self.CandleType)
        sub.BindEx(self._rsi, self._macd, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        self._manage_position(candle)

        if not self._rsi.IsFormed or not self._macd.IsFormed:
            return

        rsi_dec = float(rsi_value)
        macd_line = macd_value.Macd
        signal_line = macd_value.Signal
        if macd_line is None or signal_line is None:
            return
        macd_line = float(macd_line)
        signal_line = float(signal_line)
        histogram = macd_line - signal_line

        self._update_history(rsi_dec, histogram)

        if self.Position != 0:
            return
        self._evaluate_entry(candle, rsi_dec, macd_line, signal_line)

    def _update_history(self, rsi_val, macd_hist):
        bars = self._bars_to_pattern.Value
        self._rsi_history.append(rsi_val)
        self._rsi_sum += rsi_val
        if len(self._rsi_history) > bars:
            self._rsi_sum -= self._rsi_history.popleft()

        self._macd_history.append(macd_hist)
        self._macd_sum += macd_hist
        if len(self._macd_history) > bars:
            self._macd_sum -= self._macd_history.popleft()

    def _evaluate_entry(self, candle, rsi_val, macd_line, signal_line):
        bars = self._bars_to_pattern.Value
        if len(self._rsi_history) < bars or len(self._macd_history) < bars:
            return

        price_step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            price_step = float(self.Security.PriceStep)

        normalized_rsi = max(-1, min(1, (rsi_val - 50) / 50))
        macd_hist = macd_line - signal_line
        macd_avg = self._macd_sum / len(self._macd_history) if len(self._macd_history) > 0 else 0
        macd_dev = macd_hist - macd_avg
        normalized_momentum = math.tanh(macd_dev * 5)

        combined = normalized_rsi * 0.6 + normalized_momentum * 0.4
        confidence = min(1, abs(combined))
        projected_move = macd_dev * bars
        projected_points = projected_move / price_step

        vol = self._trade_volume.Value
        if combined > 0:
            if confidence < self._trade_level.Value:
                return
            if projected_points < self._min_target.Value:
                return
            tp = candle.ClosePrice + min(projected_move * self._profit_mult.Value, self._max_tp_points.Value * price_step)
            sl = candle.ClosePrice - self._sl_points.Value * price_step
            if tp <= candle.ClosePrice:
                return
            self.BuyMarket(vol)
            self._target_price = tp
            self._stop_price = sl
            self._pos_dir = 1
        elif combined < 0:
            if confidence < self._trade_level.Value:
                return
            if projected_points > -self._min_target.Value:
                return
            tp = candle.ClosePrice + max(projected_move * self._profit_mult.Value, -self._max_tp_points.Value * price_step)
            sl = candle.ClosePrice + self._sl_points.Value * price_step
            if tp >= candle.ClosePrice:
                return
            self.SellMarket(vol)
            self._target_price = tp
            self._stop_price = sl
            self._pos_dir = -1

    def _manage_position(self, candle):
        if self.Position > 0:
            if self._stop_price is not None and candle.LowPrice <= self._stop_price:
                self.SellMarket(Math.Abs(self.Position))
                self._reset_targets()
                return
            if self._target_price is not None and candle.HighPrice >= self._target_price:
                self.SellMarket(Math.Abs(self.Position))
                self._reset_targets()
        elif self.Position < 0:
            if self._stop_price is not None and candle.HighPrice >= self._stop_price:
                self.BuyMarket(Math.Abs(self.Position))
                self._reset_targets()
                return
            if self._target_price is not None and candle.LowPrice <= self._target_price:
                self.BuyMarket(Math.Abs(self.Position))
                self._reset_targets()
        elif self._pos_dir != 0:
            self._reset_targets()

    def _reset_targets(self):
        self._target_price = None
        self._stop_price = None
        self._pos_dir = 0

    def CreateClone(self):
        return neural_network_template_strategy()
