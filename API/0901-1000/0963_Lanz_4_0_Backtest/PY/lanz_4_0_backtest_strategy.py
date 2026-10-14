import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class lanz_4_0_backtest_strategy(Strategy):
    """
    LANZ Strategy 4.0 Backtest.
    A pivot high (low) is a candle whose high (low) is the extreme of the SwingLength candles on each side of it. A close crossing above
    the last pivot high goes long and a close crossing below the last pivot low goes short, reversing an opposite position. The stop sits
    SlBufferPoints price steps beyond the opposite pivot, the target RiskReward times the stop distance away, and the volume risks
    RiskPercent of equity with PipValueUsd per price step and lot.
    """

    def __init__(self):
        super(lanz_4_0_backtest_strategy, self).__init__()
        self._swing_length = self.Param("SwingLength", 180).SetGreaterThanZero().SetDisplay("Swing Length", "Candles on each side of a pivot", "General")
        self._sl_buffer_points = self.Param("SlBufferPoints", 50.0).SetNotNegative().SetDisplay("SL Buffer", "Stop buffer beyond the pivot in price steps", "Risk")
        self._risk_reward = self.Param("RiskReward", 1.0).SetGreaterThanZero().SetDisplay("Risk Reward", "Take profit as a multiple of the stop distance", "Risk")
        self._risk_percent = self.Param("RiskPercent", 1.0).SetGreaterThanZero().SetDisplay("Risk %", "Percent of equity risked per trade", "Risk")
        self._pip_value_usd = self.Param("PipValueUsd", 10.0).SetGreaterThanZero().SetDisplay("Pip Value USD", "Money value of one price step for one lot", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._highs = []
        self._lows = []
        self._pivot_high = None
        self._pivot_low = None
        self._prev_close = None
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(lanz_4_0_backtest_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(lanz_4_0_backtest_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        self._update_pivots(high, low)

        prev_close = self._prev_close
        self._prev_close = close

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0 and self._stop_price is not None and self._take_price is not None:
            if low <= self._stop_price or high >= self._take_price:
                self.SellMarket(self.Position)
                self._stop_price = None
                self._take_price = None
                return

        if self.Position < 0 and self._stop_price is not None and self._take_price is not None:
            if high >= self._stop_price or low <= self._take_price:
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._take_price = None
                return

        if prev_close is None or self._pivot_high is None or self._pivot_low is None:
            return

        step = float(self.Security.PriceStep) if self.Security.PriceStep is not None else 1.0
        buffer = float(self._sl_buffer_points.Value) * step
        rr = float(self._risk_reward.Value)
        pivot_high = self._pivot_high
        pivot_low = self._pivot_low

        if prev_close <= pivot_high and close > pivot_high and self.Position <= 0:
            stop = pivot_low - buffer
            if stop >= close:
                return
            self.BuyMarket(self._calculate_volume(close - stop, step) + abs(self.Position))
            self._stop_price = stop
            self._take_price = close + (close - stop) * rr
        elif prev_close >= pivot_low and close < pivot_low and self.Position >= 0:
            stop = pivot_high + buffer
            if stop <= close:
                return
            self.SellMarket(self._calculate_volume(stop - close, step) + abs(self.Position))
            self._stop_price = stop
            self._take_price = close - (stop - close) * rr

    def _update_pivots(self, high, low):
        length = self._swing_length.Value
        window = length * 2 + 1

        self._highs.append(high)
        self._lows.append(low)

        if len(self._highs) > window:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < window:
            return

        # The candle in the middle of the window is a pivot once SwingLength candles have closed after it.
        mid_high = self._highs[length]
        if mid_high == max(self._highs):
            self._pivot_high = mid_high

        mid_low = self._lows[length]
        if mid_low == min(self._lows):
            self._pivot_low = mid_low

    def _calculate_volume(self, stop_distance, step):
        portfolio = self.Portfolio
        equity = float(portfolio.CurrentValue) if portfolio is not None and portfolio.CurrentValue is not None else 0.0
        steps = stop_distance / step
        volume = equity * float(self._risk_percent.Value) / 100.0 / (steps * float(self._pip_value_usd.Value)) if steps > 0.0 else 0.0

        volume_step = self.Security.VolumeStep
        if volume_step is not None and float(volume_step) > 0.0:
            vs = float(volume_step)
            volume = math.floor(volume / vs) * vs

        return Decimal(volume) if volume > 0.0 else self.Volume

    def CreateClone(self):
        return lanz_4_0_backtest_strategy()
