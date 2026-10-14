import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, ExponentialMovingAverage, Aroon
from StockSharp.Algo.Strategies import Strategy


class bollinger_winner_pro_strategy(Strategy):
    """
    Bollinger Winner Pro strategy.
    Goes long when a candle closes below the lower band and short when it closes above the upper band, provided every
    enabled filter agrees: RSI beyond its oversold/overbought level, Aroon up/down dominance in the trade direction and
    the middle band on the trade side of the moving average, which confirms the trend direction. A position closes when price returns to the middle band (or reaches
    the opposite one), and an optional percent stop-loss caps the risk.
    """

    def __init__(self):
        super(bollinger_winner_pro_strategy, self).__init__()
        self._bb_length = self.Param("BBLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
        self._use_rsi = self.Param("UseRSI", True) \
            .SetDisplay("Use RSI", "Require RSI confirmation", "RSI Filter")
        self._rsi_length = self.Param("RSILength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "RSI Filter")
        self._rsi_oversold = self.Param("RSIOversold", 40.0) \
            .SetDisplay("RSI Oversold", "RSI level below which longs are allowed", "RSI Filter")
        self._rsi_overbought = self.Param("RSIOverbought", 60.0) \
            .SetDisplay("RSI Overbought", "RSI level above which shorts are allowed", "RSI Filter")
        self._use_aroon = self.Param("UseAroon", False) \
            .SetDisplay("Use Aroon", "Require Aroon confirmation", "Aroon Filter")
        self._aroon_length = self.Param("AroonLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Aroon Length", "Aroon period", "Aroon Filter")
        self._use_ma = self.Param("UseMA", True) \
            .SetDisplay("Use MA", "Require the middle band on the trade side of the moving average", "Moving Average")
        self._ma_length = self.Param("MALength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Moving average period", "Moving Average")
        self._use_sl = self.Param("UseSL", True) \
            .SetDisplay("Use Stop Loss", "Enable the percent stop-loss", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage from the entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        super(bollinger_winner_pro_strategy, self).OnStarted2(time)

        bollinger = BollingerBands()
        bollinger.Length = self._bb_length.Value
        bollinger.Width = Decimal(self._bb_multiplier.Value)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        aroon = Aroon()
        aroon.Length = self._aroon_length.Value
        ma = ExponentialMovingAverage()
        ma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, rsi, aroon, ma, self._process_candle).Start()

        if self._use_sl.Value:
            self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bollinger_value, rsi_value, aroon_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed or not rsi_value.IsFormed or not aroon_value.IsFormed or not ma_value.IsFormed:
            return

        if bollinger_value.UpBand is None or bollinger_value.LowBand is None or bollinger_value.MovingAverage is None:
            return

        if aroon_value.Up is None or aroon_value.Down is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upper = float(bollinger_value.UpBand)
        lower = float(bollinger_value.LowBand)
        middle = float(bollinger_value.MovingAverage)
        aroon_up = float(aroon_value.Up)
        aroon_down = float(aroon_value.Down)
        rsi = float(rsi_value.GetValue[Decimal](None))
        ma = float(ma_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)

        use_rsi = self._use_rsi.Value
        use_aroon = self._use_aroon.Value
        use_ma = self._use_ma.Value

        long_signal = close < lower \
            and (not use_rsi or rsi < float(self._rsi_oversold.Value)) \
            and (not use_aroon or aroon_up > aroon_down) \
            and (not use_ma or middle > ma)

        short_signal = close > upper \
            and (not use_rsi or rsi > float(self._rsi_overbought.Value)) \
            and (not use_aroon or aroon_down > aroon_up) \
            and (not use_ma or middle < ma)

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close >= middle:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close <= middle:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return bollinger_winner_pro_strategy()
