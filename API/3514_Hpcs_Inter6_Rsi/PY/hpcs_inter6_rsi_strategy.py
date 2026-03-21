import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class hpcs_inter6_rsi_strategy(Strategy):
    """
    HPCS Inter6 RSI: trades RSI reversals at configurable levels.
    Sells when RSI crosses above upper level, buys when crossing below lower level.
    Manages SL/TP based on pip offset with signal cooldown.
    """

    def __init__(self):
        super(hpcs_inter6_rsi_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 7) \
            .SetDisplay("RSI Length", "Lookback period for RSI", "Parameters")
        self._upper_level = self.Param("UpperLevel", 65.0) \
            .SetDisplay("Upper RSI", "Upper RSI level for shorts", "Parameters")
        self._lower_level = self.Param("LowerLevel", 35.0) \
            .SetDisplay("Lower RSI", "Lower RSI level for longs", "Parameters")
        self._offset_in_pips = self.Param("OffsetInPips", 30.0) \
            .SetDisplay("Offset (pips)", "Target and stop distance in pips", "Risk")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4) \
            .SetDisplay("Signal Cooldown", "Bars to wait between entries", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("Candle Type", "Time frame for RSI evaluation", "General")

        self._prev_rsi = None
        self._target_price = None
        self._stop_price = None
        self._is_long_position = False
        self._candles_since_trade = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hpcs_inter6_rsi_strategy, self).OnReseted()
        self._prev_rsi = None
        self._target_price = None
        self._stop_price = None
        self._is_long_position = False
        self._candles_since_trade = self._signal_cooldown_candles.Value

    def OnStarted(self, time):
        super(hpcs_inter6_rsi_strategy, self).OnStarted(time)

        self._candles_since_trade = self._signal_cooldown_candles.Value

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)

    def _process_candle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        rsi = float(rsi_val)

        if self._candles_since_trade < self._signal_cooldown_candles.Value:
            self._candles_since_trade += 1

        self._update_position_targets(candle)

        prev_rsi = self._prev_rsi
        self._prev_rsi = rsi

        if prev_rsi is None:
            return

        cooldown = self._signal_cooldown_candles.Value

        if self._candles_since_trade >= cooldown:
            if rsi > self._upper_level.Value and prev_rsi <= self._upper_level.Value:
                self._enter_short(candle)
                self._candles_since_trade = 0
                return

        if self._candles_since_trade >= cooldown:
            if rsi < self._lower_level.Value and prev_rsi >= self._lower_level.Value:
                self._enter_long(candle)
                self._candles_since_trade = 0

    def _update_position_targets(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if not self._is_long_position:
                self._target_price = None
                self._stop_price = None
                return
            should_exit = False
            if self._target_price is not None and high >= self._target_price:
                should_exit = True
            if self._stop_price is not None and low <= self._stop_price:
                should_exit = True
            if should_exit:
                self.SellMarket()
                self._target_price = None
                self._stop_price = None
        elif self.Position < 0:
            if self._is_long_position:
                self._target_price = None
                self._stop_price = None
                return
            should_exit = False
            if self._target_price is not None and low <= self._target_price:
                should_exit = True
            if self._stop_price is not None and high >= self._stop_price:
                should_exit = True
            if should_exit:
                self.BuyMarket()
                self._target_price = None
                self._stop_price = None
        else:
            self._target_price = None
            self._stop_price = None
            self._is_long_position = False

    def _enter_short(self, candle):
        if self.Position > 0:
            self.SellMarket()
        self.SellMarket()

        offset = self._calculate_offset()
        if offset > 0:
            entry = float(candle.ClosePrice)
            self._target_price = entry - offset
            self._stop_price = entry + offset
            self._is_long_position = False
        else:
            self._target_price = None
            self._stop_price = None
            self._is_long_position = False

    def _enter_long(self, candle):
        if self.Position < 0:
            self.BuyMarket()
        self.BuyMarket()

        offset = self._calculate_offset()
        if offset > 0:
            entry = float(candle.ClosePrice)
            self._target_price = entry + offset
            self._stop_price = entry - offset
            self._is_long_position = True
        else:
            self._target_price = None
            self._stop_price = None
            self._is_long_position = True

    def _calculate_offset(self):
        step = 0.01
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 0.01

        decimals = 0
        if self.Security is not None and self.Security.Decimals is not None:
            decimals = int(self.Security.Decimals)
        factor = 10.0 if decimals in (3, 5) else 1.0

        return self._offset_in_pips.Value * step * factor

    def CreateClone(self):
        return hpcs_inter6_rsi_strategy()
