import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, AverageTrueRange, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class chaikin_momentum_scalper_strategy(Strategy):
    """
    Chaikin Momentum Scalper strategy.
    The Chaikin oscillator is EMA(FastLength) minus EMA(SlowLength) of the accumulation/distribution line. A cross above zero with the
    close above SMA(SmaLength) goes long, a cross below zero with the close below the SMA goes short, reversing an opposite position.
    Each entry freezes a stop AtrMultiplierSL ATRs and a target AtrMultiplierTP ATRs away.
    """

    def __init__(self):
        super(chaikin_momentum_scalper_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 3).SetGreaterThanZero().SetDisplay("Fast Length", "Fast EMA period of the oscillator", "Chaikin")
        self._slow_length = self.Param("SlowLength", 10).SetGreaterThanZero().SetDisplay("Slow Length", "Slow EMA period of the oscillator", "Chaikin")
        self._sma_length = self.Param("SmaLength", 200).SetGreaterThanZero().SetDisplay("SMA Length", "Period of the trend SMA", "Trend")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Risk")
        self._atr_multiplier_sl = self.Param("AtrMultiplierSL", 1.5).SetGreaterThanZero().SetDisplay("ATR Multiplier SL", "ATR multiple of the stop", "Risk")
        self._atr_multiplier_tp = self.Param("AtrMultiplierTP", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier TP", "ATR multiple of the target", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._fast_adl = None
        self._slow_adl = None
        self._adl = Decimal(0)
        self._prev_oscillator = None
        self._stop_price = None
        self._target_price = None

    def OnReseted(self):
        super(chaikin_momentum_scalper_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(chaikin_momentum_scalper_strategy, self).OnStarted2(time)

        self._reset_state()

        self._fast_adl = ExponentialMovingAverage()
        self._fast_adl.Length = self._fast_length.Value
        self._slow_adl = ExponentialMovingAverage()
        self._slow_adl.Length = self._slow_length.Value
        sma = SimpleMovingAverage()
        sma.Length = self._sma_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _feed(self, indicator, value, time):
        indicator_input = DecimalIndicatorValue(indicator, value, time)
        indicator_input.IsFinal = True
        return indicator.Process(indicator_input)

    def _process_candle(self, candle, sma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        price_range = candle.HighPrice - candle.LowPrice
        if price_range > 0:
            self._adl += ((candle.ClosePrice - candle.LowPrice) - (candle.HighPrice - candle.ClosePrice)) / price_range * candle.TotalVolume

        fast = self._feed(self._fast_adl, self._adl, candle.OpenTime)
        slow = self._feed(self._slow_adl, self._adl, candle.OpenTime)

        if not fast.IsFormed or not slow.IsFormed:
            return

        oscillator = fast.GetValue[Decimal](None) - slow.GetValue[Decimal](None)
        prev = self._prev_oscillator
        self._prev_oscillator = oscillator

        if not sma_value.IsFormed or not atr_value.IsFormed or prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Stop and target frozen at entry.
        if self.Position > 0 and self._stop_price is not None and self._target_price is not None and (candle.LowPrice <= self._stop_price or candle.HighPrice >= self._target_price):
            self.SellMarket(self.Position)
            self._stop_price = None
            self._target_price = None
            return

        if self.Position < 0 and self._stop_price is not None and self._target_price is not None and (candle.HighPrice >= self._stop_price or candle.LowPrice <= self._target_price):
            self.BuyMarket(-self.Position)
            self._stop_price = None
            self._target_price = None
            return

        close = candle.ClosePrice
        sma = sma_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        sl = Decimal(self._atr_multiplier_sl.Value)
        tp = Decimal(self._atr_multiplier_tp.Value)

        if prev <= 0 and oscillator > 0 and close > sma and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - atr * sl
            self._target_price = close + atr * tp
        elif prev >= 0 and oscillator < 0 and close < sma and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + atr * sl
            self._target_price = close - atr * tp

    def CreateClone(self):
        return chaikin_momentum_scalper_strategy()
