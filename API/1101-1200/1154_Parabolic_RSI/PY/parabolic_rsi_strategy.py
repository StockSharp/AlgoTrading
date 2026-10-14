import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class parabolic_rsi_strategy(Strategy):
    """
    Parabolic RSI strategy.
    A Parabolic SAR (SarStart, SarIncrement, SarMax) is run on the RSI line instead of on price. When the SAR flips below the RSI and RSI
    is at least LongRsiMin the strategy goes long, when it flips above the RSI and RSI is at most ShortRsiMax it goes short. An opposite
    flip always closes the current position and reverses it when its own RSI condition holds.
    """

    def __init__(self):
        super(parabolic_rsi_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Indicators")
        self._sar_start = self.Param("SarStart", 0.02).SetGreaterThanZero().SetDisplay("SAR Start", "Initial SAR acceleration factor", "Indicators")
        self._sar_increment = self.Param("SarIncrement", 0.02).SetGreaterThanZero().SetDisplay("SAR Increment", "SAR acceleration factor increment", "Indicators")
        self._sar_max = self.Param("SarMax", 0.2).SetGreaterThanZero().SetDisplay("SAR Max", "Maximum SAR acceleration factor", "Indicators")
        self._long_rsi_min = self.Param("LongRsiMin", 50.0).SetDisplay("Long RSI Min", "Minimum RSI for a long entry", "Signals")
        self._short_rsi_max = self.Param("ShortRsiMax", 50.0).SetDisplay("Short RSI Max", "Maximum RSI for a short entry", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_rsi = None
        self._prev_rsi2 = None
        self._sar_ready = False
        self._is_up_trend = False
        self._sar = Decimal(0)
        self._extreme = Decimal(0)
        self._af = Decimal(0)

    def OnReseted(self):
        super(parabolic_rsi_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(parabolic_rsi_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi):
        if candle.State != CandleStates.Finished:
            return

        flip = self._update_sar(rsi)

        if flip == 0 or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if flip > 0:
            if rsi >= Decimal(self._long_rsi_min.Value) and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
        else:
            if rsi <= Decimal(self._short_rsi_max.Value) and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
            elif self.Position > 0:
                self.SellMarket(self.Position)

    # Parabolic SAR on a single series; returns 1 on a flip below the series, -1 on a flip above it, 0 otherwise.
    def _update_sar(self, value):
        prev = self._prev_rsi
        prev2 = self._prev_rsi2
        self._prev_rsi2 = self._prev_rsi
        self._prev_rsi = value

        if prev is None:
            return 0

        start = Decimal(self._sar_start.Value)
        increment = Decimal(self._sar_increment.Value)
        maximum = Decimal(self._sar_max.Value)

        if not self._sar_ready:
            self._is_up_trend = value >= prev
            self._sar = min(prev, value) if self._is_up_trend else max(prev, value)
            self._extreme = max(prev, value) if self._is_up_trend else min(prev, value)
            self._af = start
            self._sar_ready = True
            return 0

        sar = self._sar + self._af * (self._extreme - self._sar)

        if self._is_up_trend:
            sar = min(sar, prev)
            if prev2 is not None:
                sar = min(sar, prev2)

            if value < sar:
                self._is_up_trend = False
                self._sar = self._extreme
                self._extreme = value
                self._af = start
                return -1

            if value > self._extreme:
                self._extreme = value
                self._af = min(self._af + increment, maximum)
        else:
            sar = max(sar, prev)
            if prev2 is not None:
                sar = max(sar, prev2)

            if value > sar:
                self._is_up_trend = True
                self._sar = self._extreme
                self._extreme = value
                self._af = start
                return 1

            if value < self._extreme:
                self._extreme = value
                self._af = min(self._af + increment, maximum)

        self._sar = sar
        return 0

    def CreateClone(self):
        return parabolic_rsi_strategy()
