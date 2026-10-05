import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class bollinger_bands_modified_strategy(Strategy):
    """
    Bollinger Bands Modified strategy.
    A close above the upper Bollinger band goes long and a close below the lower band goes short, reversing an opposite position.
    With CrossoverCheck (CrossunderCheck) the close also has to have been at or below the upper band (at or above the lower band) on
    the previous candle, and with EmaTrend a long needs the close above EMA(EmaLength) and a short below it. Each entry freezes a
    stop at the lowest low of the last LowestLength candles (highest high of the last HighestLength candles for shorts) and a
    target TargetFactor times that risk away.
    """

    def __init__(self):
        super(bollinger_bands_modified_strategy, self).__init__()
        self._bollinger_length = self.Param("BollingerLength", 20).SetGreaterThanZero().SetDisplay("Bollinger Length", "Bollinger period", "Bollinger")
        self._bollinger_deviation = self.Param("BollingerDeviation", 0.38).SetGreaterThanZero().SetDisplay("Bollinger Deviation", "Bollinger standard deviation multiplier", "Bollinger")
        self._ema_length = self.Param("EmaLength", 80).SetGreaterThanZero().SetDisplay("EMA Length", "EMA period of the trend filter", "Trend")
        self._highest_length = self.Param("HighestLength", 7).SetGreaterThanZero().SetDisplay("Highest Length", "Candles of the highest high for the short stop", "Risk")
        self._lowest_length = self.Param("LowestLength", 7).SetGreaterThanZero().SetDisplay("Lowest Length", "Candles of the lowest low for the long stop", "Risk")
        self._target_factor = self.Param("TargetFactor", 1.6).SetGreaterThanZero().SetDisplay("Target Factor", "Target distance as a multiple of the risk", "Risk")
        self._ema_trend = self.Param("EmaTrend", True).SetDisplay("EMA Trend", "Require the EMA trend filter", "Trend")
        self._crossover_check = self.Param("CrossoverCheck", False).SetDisplay("Crossover Check", "Require an actual cross above the upper band for longs", "Signals")
        self._crossunder_check = self.Param("CrossunderCheck", False).SetDisplay("Crossunder Check", "Require an actual cross below the lower band for shorts", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_upper = None
        self._prev_lower = None
        self._stop_price = None
        self._target_price = None

    def OnReseted(self):
        super(bollinger_bands_modified_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(bollinger_bands_modified_strategy, self).OnStarted2(time)

        self._reset_state()

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_length.Value
        bollinger.Width = Decimal(self._bollinger_deviation.Value)
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        highest = Highest()
        highest.Length = self._highest_length.Value
        lowest = Lowest()
        lowest.Length = self._lowest_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, ema, highest, lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bollinger_value, ema_value, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed or not ema_value.IsFormed or not highest_value.IsFormed or not lowest_value.IsFormed:
            return

        upper = bollinger_value.UpBand
        lower = bollinger_value.LowBand
        if upper is None or lower is None:
            return

        close = candle.ClosePrice
        prev_close = self._prev_close
        prev_upper = self._prev_upper
        prev_lower = self._prev_lower
        self._prev_close = close
        self._prev_upper = upper
        self._prev_lower = lower

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

        ema = ema_value.GetValue[Decimal](None)
        crossed_up = prev_close is not None and prev_upper is not None and prev_close <= prev_upper
        crossed_down = prev_close is not None and prev_lower is not None and prev_close >= prev_lower
        long_signal = close > upper and (not self._crossover_check.Value or crossed_up) and (not self._ema_trend.Value or close > ema)
        short_signal = close < lower and (not self._crossunder_check.Value or crossed_down) and (not self._ema_trend.Value or close < ema)
        factor = Decimal(self._target_factor.Value)

        if long_signal and self.Position <= 0:
            stop = lowest_value.GetValue[Decimal](None)
            risk = close - stop
            if risk <= 0:
                return
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = stop
            self._target_price = close + risk * factor
        elif short_signal and self.Position >= 0:
            stop = highest_value.GetValue[Decimal](None)
            risk = stop - close
            if risk <= 0:
                return
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = stop
            self._target_price = close - risk * factor

    def CreateClone(self):
        return bollinger_bands_modified_strategy()
