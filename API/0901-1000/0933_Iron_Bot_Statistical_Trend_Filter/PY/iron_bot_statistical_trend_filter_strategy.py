import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation, Highest, Lowest, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class iron_bot_statistical_trend_filter_strategy(Strategy):
    """
    Iron Bot statistical trend filter strategy.
    The highest high and lowest low of the last AnalysisWindow candles define a range. The trend line sits at its middle, the high
    trend level HighTrendLimit of the range below the top and the low trend level LowTrendLimit of the range below the top. A close
    crossing above the high trend level (and so above the trend line) with a non-negative Z-score of the close goes long, a close
    crossing below the low trend level (and the trend line) with a non-positive Z-score goes short, reversing an opposite position.
    A stop at SlRatio and a take profit at the TP ratio picked by TakeProfitLevel, both fractions of the entry price, close trades.
    """

    def __init__(self):
        super(iron_bot_statistical_trend_filter_strategy, self).__init__()
        self._z_length = self.Param("ZLength", 40).SetGreaterThanZero().SetDisplay("Z Length", "Length of the Z-score mean and deviation", "Indicators")
        self._analysis_window = self.Param("AnalysisWindow", 44).SetGreaterThanZero().SetDisplay("Analysis Window", "Candles the trend range spans", "Indicators")
        self._high_trend_limit = self.Param("HighTrendLimit", 0.236).SetDisplay("High Trend Limit", "Fibonacci fraction below the top for the high trend level", "Indicators")
        self._low_trend_limit = self.Param("LowTrendLimit", 0.786).SetDisplay("Low Trend Limit", "Fibonacci fraction below the top for the low trend level", "Indicators")
        self._ema_length = self.Param("EmaLength", 200).SetGreaterThanZero().SetDisplay("EMA Length", "EMA length shown on the chart", "Indicators")
        self._sl_ratio = self.Param("SlRatio", 0.008).SetNotNegative().SetDisplay("SL Ratio", "Stop loss as a fraction of the entry price", "Risk")
        self._tp1_ratio = self.Param("Tp1Ratio", 0.0075).SetNotNegative().SetDisplay("TP1 Ratio", "First take profit as a fraction of the entry price", "Risk")
        self._tp2_ratio = self.Param("Tp2Ratio", 0.011).SetNotNegative().SetDisplay("TP2 Ratio", "Second take profit as a fraction of the entry price", "Risk")
        self._tp3_ratio = self.Param("Tp3Ratio", 0.015).SetNotNegative().SetDisplay("TP3 Ratio", "Third take profit as a fraction of the entry price", "Risk")
        self._tp4_ratio = self.Param("Tp4Ratio", 0.02).SetNotNegative().SetDisplay("TP4 Ratio", "Fourth take profit as a fraction of the entry price", "Risk")
        self._take_profit_level = self.Param("TakeProfitLevel", 1).SetRange(1, 4).SetDisplay("Take Profit Level", "Which take profit ratio (1-4) closes the trade", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(iron_bot_statistical_trend_filter_strategy, self).OnReseted()
        self._prev_close = None

    def OnStarted2(self, time):
        super(iron_bot_statistical_trend_filter_strategy, self).OnStarted2(time)

        self._prev_close = None

        mean = SimpleMovingAverage()
        mean.Length = self._z_length.Value
        deviation = StandardDeviation()
        deviation.Length = self._z_length.Value
        highest = Highest()
        highest.Length = self._analysis_window.Value
        lowest = Lowest()
        lowest.Length = self._analysis_window.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(mean, deviation, highest, lowest, ema, self._process_candle).Start()

        level = self._take_profit_level.Value
        if level == 2:
            take_ratio = float(self._tp2_ratio.Value)
        elif level == 3:
            take_ratio = float(self._tp3_ratio.Value)
        elif level == 4:
            take_ratio = float(self._tp4_ratio.Value)
        else:
            take_ratio = float(self._tp1_ratio.Value)
        sl_ratio = float(self._sl_ratio.Value)

        take = Unit(Decimal(take_ratio * 100.0), UnitTypes.Percent) if take_ratio > 0 else Unit()
        stop = Unit(Decimal(sl_ratio * 100.0), UnitTypes.Percent) if sl_ratio > 0 else Unit()
        self.StartProtection(take, stop, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, mean_value, deviation_value, highest_value, lowest_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        prev = self._prev_close
        close = candle.ClosePrice
        self._prev_close = close

        if prev is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        rng = highest_value - lowest_value
        if rng <= 0:
            return

        z_score = Decimal(0) if deviation_value == 0 else (close - mean_value) / deviation_value

        high_trend_level = highest_value - rng * Decimal(self._high_trend_limit.Value)
        trend_line = highest_value - rng * Decimal(0.5)
        low_trend_level = highest_value - rng * Decimal(self._low_trend_limit.Value)

        cross_up = close > trend_line and close > high_trend_level and (prev <= trend_line or prev <= high_trend_level)
        cross_down = close < trend_line and close < low_trend_level and (prev >= trend_line or prev >= low_trend_level)

        if cross_up and z_score >= 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and z_score <= 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return iron_bot_statistical_trend_filter_strategy()
