import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, WeightedMovingAverage, SmoothedMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class MaTypes:
    """Moving average types of the MACDs."""
    Ema = 0
    Sma = 1
    Wma = 2
    Smma = 3


class double_macd_strategy(Strategy):
    """
    Double MACD strategy.
    Two MACDs of different speeds are built from moving averages of the selected types: each MACD line is the fast average of the
    close minus the slow one, and its signal line is an average of the same type over the MACD line. The strategy goes long when both
    MACD lines are above their signal lines and short when both are below, reversing an opposite position. A percent stop loss limits
    the loss.
    """

    def __init__(self):
        super(double_macd_strategy, self).__init__()
        self._fast_length1 = self.Param("FastLength1", 12).SetGreaterThanZero().SetDisplay("Fast Length 1", "Fast period of the first MACD", "MACD 1")
        self._slow_length1 = self.Param("SlowLength1", 26).SetGreaterThanZero().SetDisplay("Slow Length 1", "Slow period of the first MACD", "MACD 1")
        self._signal_length1 = self.Param("SignalLength1", 9).SetGreaterThanZero().SetDisplay("Signal Length 1", "Signal period of the first MACD", "MACD 1")
        self._ma_type1 = self.Param("MaType1", MaTypes.Ema).SetDisplay("MA Type 1", "Moving average type of the first MACD", "MACD 1")
        self._fast_length2 = self.Param("FastLength2", 24).SetGreaterThanZero().SetDisplay("Fast Length 2", "Fast period of the second MACD", "MACD 2")
        self._slow_length2 = self.Param("SlowLength2", 52).SetGreaterThanZero().SetDisplay("Slow Length 2", "Slow period of the second MACD", "MACD 2")
        self._signal_length2 = self.Param("SignalLength2", 9).SetGreaterThanZero().SetDisplay("Signal Length 2", "Signal period of the second MACD", "MACD 2")
        self._ma_type2 = self.Param("MaType2", MaTypes.Ema).SetDisplay("MA Type 2", "Moving average type of the second MACD", "MACD 2")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._signal1 = None
        self._signal2 = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @staticmethod
    def _create_ma(ma_type, length):
        if ma_type == MaTypes.Sma:
            ma = SimpleMovingAverage()
        elif ma_type == MaTypes.Wma:
            ma = WeightedMovingAverage()
        elif ma_type == MaTypes.Smma:
            ma = SmoothedMovingAverage()
        else:
            ma = ExponentialMovingAverage()
        ma.Length = length
        return ma

    def OnStarted2(self, time):
        super(double_macd_strategy, self).OnStarted2(time)

        ma_type1 = self._ma_type1.Value
        ma_type2 = self._ma_type2.Value
        fast1 = self._create_ma(ma_type1, self._fast_length1.Value)
        slow1 = self._create_ma(ma_type1, self._slow_length1.Value)
        fast2 = self._create_ma(ma_type2, self._fast_length2.Value)
        slow2 = self._create_ma(ma_type2, self._slow_length2.Value)
        self._signal1 = self._create_ma(ma_type1, self._signal_length1.Value)
        self._signal2 = self._create_ma(ma_type2, self._signal_length2.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast1, slow1, fast2, slow2, self._process_candle).Start()

        stop_percent = Decimal(self._stop_loss_percent.Value)
        stop_loss = Unit(stop_percent, UnitTypes.Percent) if stop_percent > 0 else Unit()
        self.StartProtection(Unit(), stop_loss, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_signal(self, indicator, value, candle):
        input_value = DecimalIndicatorValue(indicator, value, candle.OpenTime)
        input_value.IsFinal = True
        return indicator.Process(input_value)

    def _process_candle(self, candle, fast1_value, slow1_value, fast2_value, slow2_value):
        if candle.State != CandleStates.Finished:
            return

        if not fast1_value.IsFormed or not slow1_value.IsFormed or not fast2_value.IsFormed or not slow2_value.IsFormed:
            return

        macd1 = fast1_value.GetValue[Decimal](None) - slow1_value.GetValue[Decimal](None)
        macd2 = fast2_value.GetValue[Decimal](None) - slow2_value.GetValue[Decimal](None)

        signal1_value = self._process_signal(self._signal1, macd1, candle)
        signal2_value = self._process_signal(self._signal2, macd2, candle)

        if not signal1_value.IsFormed or not signal2_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        signal1 = signal1_value.GetValue[Decimal](None)
        signal2 = signal2_value.GetValue[Decimal](None)

        bullish = macd1 > signal1 and macd2 > signal2
        bearish = macd1 < signal1 and macd2 < signal2

        if bullish and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif bearish and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return double_macd_strategy()
