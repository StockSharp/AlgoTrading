import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, RelativeStrengthIndex, AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

TREND_FAST_LENGTH = 50
TREND_SLOW_LENGTH = 200
RSI_UPPER = 70
RSI_LOWER = 30


class multi_factor_strategy(Strategy):
    """
    Multi-factor strategy.
    Goes long when MACD is above its signal, RSI is below 70, the close is above the 50-period SMA and the 50 SMA is above the 200 SMA;
    goes short on the mirrored conditions (RSI above 30), reversing an opposite position. Each position is closed by a stop loss and a
    take profit placed StopAtrMultiplier and ProfitAtrMultiplier ATRs away from the entry price.
    """

    def __init__(self):
        super(multi_factor_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "MACD fast EMA length", "MACD")
        self._slow_length = self.Param("SlowLength", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "MACD slow EMA length", "MACD")
        self._signal_length = self.Param("SignalLength", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "MACD signal EMA length", "MACD")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "RSI")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Risk")
        self._stop_atr_multiplier = self.Param("StopAtrMultiplier", 2.0).SetNotNegative().SetDisplay("Stop ATR Mult", "ATR multiple for the stop loss", "Risk")
        self._profit_atr_multiplier = self.Param("ProfitAtrMultiplier", 3.0).SetNotNegative().SetDisplay("Profit ATR Mult", "ATR multiple for the take profit", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(multi_factor_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(multi_factor_strategy, self).OnStarted2(time)

        self._reset_state()

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_length.Value
        macd.Macd.LongMa.Length = self._slow_length.Value
        macd.SignalMa.Length = self._signal_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        sma50 = SimpleMovingAverage()
        sma50.Length = TREND_FAST_LENGTH
        sma200 = SimpleMovingAverage()
        sma200.Length = TREND_SLOW_LENGTH

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, rsi, atr, sma50, sma200, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma50)
            self.DrawIndicator(area, sma200)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, macd_value, rsi_value, atr_value, sma50_value, sma200_value):
        if candle.State != CandleStates.Finished:
            return

        # The ATR levels are checked against the candle range, so exits happen before new signals.
        if self.Position > 0 and self._stop_price is not None and self._take_price is not None:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
                self._reset_state()
                return
        elif self.Position < 0 and self._stop_price is not None and self._take_price is not None:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket(-self.Position)
                self._reset_state()
                return

        if not macd_value.IsFormed or not rsi_value.IsFormed or not atr_value.IsFormed or not sma50_value.IsFormed or not sma200_value.IsFormed:
            return

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal
        rsi = rsi_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        sma50 = sma50_value.GetValue[Decimal](None)
        sma200 = sma200_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        long_signal = macd_line > signal_line and rsi < Decimal(RSI_UPPER) and close > sma50 and sma50 > sma200
        short_signal = macd_line < signal_line and rsi > Decimal(RSI_LOWER) and close < sma50 and sma50 < sma200

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._set_levels(close, atr, True)
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._set_levels(close, atr, False)

    def _set_levels(self, entry, atr, is_long):
        stop_mult = Decimal(self._stop_atr_multiplier.Value)
        take_mult = Decimal(self._profit_atr_multiplier.Value)
        stop = atr * stop_mult
        take = atr * take_mult

        # A zero multiplier disables that level.
        if stop_mult > Decimal(0):
            self._stop_price = entry - stop if is_long else entry + stop
        else:
            self._stop_price = Decimal.MinValue if is_long else Decimal.MaxValue
        if take_mult > Decimal(0):
            self._take_price = entry + take if is_long else entry - take
        else:
            self._take_price = Decimal.MaxValue if is_long else Decimal.MinValue

    def CreateClone(self):
        return multi_factor_strategy()
