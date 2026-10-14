import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class enhanced_bar_up_dn_strategy(Strategy):
    """
    Enhanced BarUpDn strategy.
    A bullish candle that opens above the previous close goes long when it closes above the trend SMA and above the lower
    Bollinger band; a bearish candle that opens below the previous close goes short when it closes below the trend SMA and below
    the upper band. An opposite signal reverses the position. The stop lies AtrMultiplierSl ATR and the target AtrMultiplierTp ATR
    from the entry.
    """

    def __init__(self):
        super(enhanced_bar_up_dn_strategy, self).__init__()
        self._bb_length = self.Param("BbLength", 20).SetGreaterThanZero().SetDisplay("BB Length", "Bollinger Bands length", "Indicators")
        self._bb_multiplier = self.Param("BbMultiplier", 2.0).SetGreaterThanZero().SetDisplay("BB Multiplier", "Bollinger Bands deviation multiplier", "Indicators")
        self._ma_length = self.Param("MaLength", 50).SetGreaterThanZero().SetDisplay("MA Length", "Trend SMA length", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR length", "Risk")
        self._atr_multiplier_sl = self.Param("AtrMultiplierSl", 2.0).SetNotNegative().SetDisplay("ATR Stop", "Stop loss distance in ATR", "Risk")
        self._atr_multiplier_tp = self.Param("AtrMultiplierTp", 3.0).SetNotNegative().SetDisplay("ATR Target", "Take profit distance in ATR", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(enhanced_bar_up_dn_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(enhanced_bar_up_dn_strategy, self).OnStarted2(time)

        self._reset_state()

        bollinger = BollingerBands()
        bollinger.Length = self._bb_length.Value
        bollinger.Width = Decimal(float(self._bb_multiplier.Value))
        ma = SimpleMovingAverage()
        ma.Length = self._ma_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, ma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bollinger_value, ma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        prev_close = self._prev_close
        close = float(candle.ClosePrice)
        self._prev_close = close

        if not bollinger_value.IsFormed or not ma_value.IsFormed or not atr_value.IsFormed:
            return

        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        sl_mult = float(self._atr_multiplier_sl.Value)
        tp_mult = float(self._atr_multiplier_tp.Value)

        if self.Position > 0 and ((sl_mult > 0 and low <= self._stop_price) or (tp_mult > 0 and high >= self._take_price)):
            self.SellMarket(self.Position)
            return

        if self.Position < 0 and ((sl_mult > 0 and high >= self._stop_price) or (tp_mult > 0 and low <= self._take_price)):
            self.BuyMarket(-self.Position)
            return

        if prev_close is None:
            return

        upper = float(bollinger_value.UpBand)
        lower = float(bollinger_value.LowBand)
        ma = float(ma_value.GetValue[Decimal](None))
        atr = float(atr_value.GetValue[Decimal](None))
        open_price = float(candle.OpenPrice)

        long_signal = close > open_price and open_price > prev_close and close > ma and close > lower
        short_signal = close < open_price and open_price < prev_close and close < ma and close < upper

        if long_signal and self.Position <= 0:
            self._stop_price = close - atr * sl_mult
            self._take_price = close + atr * tp_mult
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self._stop_price = close + atr * sl_mult
            self._take_price = close - atr * tp_mult
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return enhanced_bar_up_dn_strategy()
