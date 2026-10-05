import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class mema_bb_rsi_strategy(Strategy):
    """
    Multi EMA + Bollinger Bands + RSI strategy.
    Goes long when the close is above the fast EMA on a candle whose low pierced the lower band, and short when the
    close is below the fast EMA on a candle whose high pierced the upper band while RSI is above 50. A long closes when
    RSI rises above RSIOversold and a short when the close drops below the lower band. After XBars bars a position that
    is in profit is closed as well. Each side can be switched off. The slow EMA is plotted as a trend reference.
    """

    def __init__(self):
        super(mema_bb_rsi_strategy, self).__init__()
        self._ma1_period = self.Param("Ma1Period", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("MA1 Period", "Fast EMA period", "Moving Average")
        self._ma2_period = self.Param("Ma2Period", 55) \
            .SetGreaterThanZero() \
            .SetDisplay("MA2 Period", "Slow EMA period", "Moving Average")
        self._bb_length = self.Param("BBLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Multiplier", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
        self._rsi_length = self.Param("RSILength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._rsi_oversold = self.Param("RSIOversold", 71.0) \
            .SetDisplay("RSI Exit Level", "RSI level above which a long is closed", "RSI")
        self._x_bars = self.Param("XBars", 12) \
            .SetNotNegative() \
            .SetDisplay("X Bars", "Bars after which a profitable position is closed, 0 disables", "Exit")
        self._enable_long = self.Param("EnableLong", True) \
            .SetDisplay("Enable Long", "Allow long trades", "Trading")
        self._enable_short = self.Param("EnableShort", True) \
            .SetDisplay("Enable Short", "Allow short trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

        self._entry_price = 0.0
        self._bars_in_position = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(mema_bb_rsi_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._bars_in_position = 0

    def OnStarted2(self, time):
        super(mema_bb_rsi_strategy, self).OnStarted2(time)

        self._entry_price = 0.0
        self._bars_in_position = 0

        ma1 = ExponentialMovingAverage()
        ma1.Length = self._ma1_period.Value
        ma2 = ExponentialMovingAverage()
        ma2.Length = self._ma2_period.Value
        bollinger = BollingerBands()
        bollinger.Length = self._bb_length.Value
        bollinger.Width = Decimal(self._bb_multiplier.Value)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ma1, ma2, bollinger, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma1)
            self.DrawIndicator(area, ma2)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)

    def _process_candle(self, candle, ma1_value, ma2_value, bollinger_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if self.Position != 0:
            self._bars_in_position += 1

        if not ma1_value.IsFormed or not bollinger_value.IsFormed or not rsi_value.IsFormed:
            return

        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upper = float(bollinger_value.UpBand)
        lower = float(bollinger_value.LowBand)
        ma1 = float(ma1_value.GetValue[Decimal](None))
        rsi = float(rsi_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)

        long_signal = self._enable_long.Value and close > ma1 and float(candle.LowPrice) < lower
        short_signal = self._enable_short.Value and close < ma1 and float(candle.HighPrice) > upper and rsi > 50.0

        x_bars = int(self._x_bars.Value)
        time_up = x_bars > 0 and self._bars_in_position >= x_bars

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._entry_price = close
            self._bars_in_position = 0
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._entry_price = close
            self._bars_in_position = 0
        elif self.Position > 0 and (rsi > float(self._rsi_oversold.Value) or (time_up and close > self._entry_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (close < lower or (time_up and close < self._entry_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return mema_bb_rsi_strategy()
