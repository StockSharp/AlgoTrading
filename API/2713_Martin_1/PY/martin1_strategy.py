import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class martin1_strategy(Strategy):
    """Martin 1: hedging martingale with pyramid entries on profit and opposite hedges on drawdown."""

    def __init__(self):
        super(martin1_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate conditions", "General")
        self._use_trading_hours = self.Param("UseTradingHours", False) \
            .SetDisplay("Use Trading Hours", "Restrict entries to a time window", "General")
        self._start_hour = self.Param("StartHour", 2) \
            .SetDisplay("Start Hour", "Hour to start monitoring for new trades", "General")
        self._end_hour = self.Param("EndHour", 21) \
            .SetDisplay("End Hour", "Hour to stop opening hedges/pyramids", "General")
        self._lot_multiplier = self.Param("LotMultiplier", 1.6) \
            .SetGreaterThanZero() \
            .SetDisplay("Lot Multiplier", "Factor applied to volume after a loss", "Money Management")
        self._max_multiplications = self.Param("MaxMultiplications", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Multiplications", "Maximum hedging steps", "Money Management")
        # 0=Buy, 1=Sell
        self._start_direction = self.Param("StartDirection", 0) \
            .SetDisplay("Start Direction", "0=Buy, 1=Sell", "Trading")
        self._min_profit = self.Param("MinProfit", 1.5) \
            .SetDisplay("Min Profit", "Floating profit target to flatten", "Risk")
        self._initial_volume = self.Param("InitialVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Initial Volume", "Baseline order size", "Money Management")
        self._stop_loss_pips = self.Param("StopLossPips", 400) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (pips)", "Distance before hedging the opposite side", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 1000) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pips)", "Distance to pyramid in same direction", "Risk")

        self._long_positions = []
        self._short_positions = []
        self._current_volume = 0.0
        self._multiplication_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def UseTradingHours(self):
        return self._use_trading_hours.Value
    @property
    def StartHour(self):
        return int(self._start_hour.Value)
    @property
    def EndHour(self):
        return int(self._end_hour.Value)
    @property
    def LotMultiplier(self):
        return float(self._lot_multiplier.Value)
    @property
    def MaxMultiplications(self):
        return int(self._max_multiplications.Value)
    @property
    def StartDirection(self):
        return int(self._start_direction.Value)
    @property
    def MinProfit(self):
        return float(self._min_profit.Value)
    @property
    def InitialVolume(self):
        return float(self._initial_volume.Value)
    @property
    def StopLossPips(self):
        return int(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return int(self._take_profit_pips.Value)

    def _get_pip_size(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None:
            return 1.0
        step = float(sec.PriceStep)
        if step <= 0:
            return 1.0
        s = step
        digits = 0
        while s < 1.0 and digits < 10:
            s *= 10
            digits += 1
        return step * 10.0 if (digits == 3 or digits == 5) else step

    def _adjust_volume(self, volume):
        if volume <= 0:
            return 0.0
        sec = self.Security
        if sec is not None and sec.VolumeStep is not None:
            step = float(sec.VolumeStep)
            if step > 0:
                volume = math.floor(volume / step) * step
                if volume < step:
                    return 0.0
        return volume

    def OnStarted(self, time):
        super(martin1_strategy, self).OnStarted(time)

        self._long_positions = []
        self._short_positions = []
        self._multiplication_count = 0
        self._current_volume = self._adjust_volume(self.InitialVolume)
        self._pip_size = self._get_pip_size()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close_price = float(candle.ClosePrice)
        total_profit = self._calc_open_profit(close_price)
        within_hours = not self.UseTradingHours or self._is_within_hours(candle.CloseTime)

        if within_hours:
            if len(self._long_positions) > 0:
                self._eval_longs(close_price)
            if len(self._short_positions) > 0:
                self._eval_shorts(close_price)

        if len(self._long_positions) == 0 and len(self._short_positions) == 0:
            self._reset_martingale()
            self._open_initial(close_price)
            return

        if (len(self._long_positions) > 0 or len(self._short_positions) > 0) and total_profit > self.MinProfit:
            self._close_all(close_price)
            self._reset_martingale()

    def _eval_longs(self, close_price):
        tp_dist = self.TakeProfitPips * self._pip_size
        sl_dist = self.StopLossPips * self._pip_size
        for pos in list(self._long_positions):
            price_gain = close_price - pos["entry"]
            profit = self._price_to_money(price_gain, pos["volume"])

            if profit > 0 and price_gain > tp_dist:
                self._execute_order("buy", self._current_volume, close_price)

            if self.StartDirection == 0 and sl_dist > 0:
                loss_dist = pos["entry"] - close_price
                if (loss_dist > sl_dist * (self._multiplication_count + 1)
                        and self._multiplication_count + 1 <= self.MaxMultiplications):
                    new_vol = self._adjust_volume(self._current_volume * self.LotMultiplier)
                    if new_vol > 0:
                        self._multiplication_count += 1
                        self._current_volume = new_vol
                        self._execute_order("sell", new_vol, close_price)

    def _eval_shorts(self, close_price):
        tp_dist = self.TakeProfitPips * self._pip_size
        sl_dist = self.StopLossPips * self._pip_size
        for pos in list(self._short_positions):
            price_gain = pos["entry"] - close_price
            profit = self._price_to_money(price_gain, pos["volume"])

            if profit > 0 and price_gain > tp_dist:
                self._execute_order("sell", self._current_volume, close_price)

            if self.StartDirection == 1 and sl_dist > 0:
                loss_dist = close_price - pos["entry"]
                if (loss_dist > sl_dist * (self._multiplication_count + 1)
                        and self._multiplication_count + 1 <= self.MaxMultiplications):
                    new_vol = self._adjust_volume(self._current_volume * self.LotMultiplier)
                    if new_vol > 0:
                        self._multiplication_count += 1
                        self._current_volume = new_vol
                        self._execute_order("buy", new_vol, close_price)

    def _open_initial(self, price):
        vol = self._current_volume
        if vol <= 0:
            return
        side = "sell" if self.StartDirection == 1 else "buy"
        self._execute_order(side, vol, price)

    def _close_all(self, price):
        long_vol = sum(p["volume"] for p in self._long_positions)
        if long_vol > 0:
            self._execute_order("sell", long_vol, price)
        short_vol = sum(p["volume"] for p in self._short_positions)
        if short_vol > 0:
            self._execute_order("buy", short_vol, price)

    def _execute_order(self, side, volume, price):
        if volume <= 0:
            return
        if side == "buy":
            self.BuyMarket()
        else:
            self.SellMarket()
        self._update_positions(side, volume, price)

    def _update_positions(self, side, volume, price):
        if volume <= 0:
            return
        if side == "buy":
            remaining = volume
            while remaining > 0 and len(self._short_positions) > 0:
                pos = self._short_positions[0]
                qty = min(pos["volume"], remaining)
                pos["volume"] -= qty
                remaining -= qty
                if pos["volume"] <= 0:
                    self._short_positions.pop(0)
            if remaining > 0:
                self._long_positions.append({"volume": remaining, "entry": price})
        else:
            remaining = volume
            while remaining > 0 and len(self._long_positions) > 0:
                pos = self._long_positions[0]
                qty = min(pos["volume"], remaining)
                pos["volume"] -= qty
                remaining -= qty
                if pos["volume"] <= 0:
                    self._long_positions.pop(0)
            if remaining > 0:
                self._short_positions.append({"volume": remaining, "entry": price})

    def _calc_open_profit(self, current_price):
        profit = 0.0
        for pos in self._long_positions:
            diff = current_price - pos["entry"]
            profit += self._price_to_money(diff, pos["volume"])
        for pos in self._short_positions:
            diff = pos["entry"] - current_price
            profit += self._price_to_money(diff, pos["volume"])
        return profit

    def _price_to_money(self, price_diff, volume):
        sec = self.Security
        if sec is not None and sec.PriceStep is not None:
            step = float(sec.PriceStep)
            if step > 0:
                steps = price_diff / step
                return steps * step * volume
        return price_diff * volume

    def _is_within_hours(self, time):
        hour = time.Hour
        return hour >= self.StartHour and hour <= self.EndHour

    def _reset_martingale(self):
        self._multiplication_count = 0
        self._current_volume = self._adjust_volume(self.InitialVolume)

    def OnReseted(self):
        super(martin1_strategy, self).OnReseted()
        self._long_positions = []
        self._short_positions = []
        self._current_volume = 0.0
        self._multiplication_count = 0

    def CreateClone(self):
        return martin1_strategy()
