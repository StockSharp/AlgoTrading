import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class vela_superada_strategy(Strategy):
    """
    Vela Superada Strategy.
    A bearish candle followed by a bullish one that closes above the prior open, with both closes above the EMA, RSI below 65
    and a rising MACD line, is a long signal. The mirrored pattern below the EMA with RSI above 35 and a falling MACD line is
    a short signal. ShowLong and ShowShort enable each side; an opposite signal closes or reverses the position. A trailing
    SlPercent stop and a TpPercent take profit protect the trade.
    """

    def __init__(self):
        super(vela_superada_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 10).SetGreaterThanZero().SetDisplay("EMA Length", "EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Indicators")
        self._show_long = self.Param("ShowLong", True).SetDisplay("Long Trades", "Allow long trades", "Trading")
        self._show_short = self.Param("ShowShort", False).SetDisplay("Short Trades", "Allow short trades", "Trading")
        self._tp_percent = self.Param("TpPercent", 1.2).SetNotNegative().SetDisplay("TP %", "Take profit percentage from entry price", "Risk")
        self._sl_percent = self.Param("SlPercent", 1.8).SetNotNegative().SetDisplay("SL %", "Trailing stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_candle = None
        self._prev_macd = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vela_superada_strategy, self).OnReseted()
        self._prev_candle = None
        self._prev_macd = None

    def OnStarted2(self, time):
        super(vela_superada_strategy, self).OnStarted2(time)

        self._prev_candle = None
        self._prev_macd = None

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        macd = MovingAverageConvergenceDivergence()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, rsi, macd, self._process_candle).Start()

        tp = float(self._tp_percent.Value)
        sl = float(self._sl_percent.Value)
        self.StartProtection(
            Unit(Decimal(tp), UnitTypes.Percent) if tp > 0 else Unit(),
            Unit(Decimal(sl), UnitTypes.Percent) if sl > 0 else Unit(),
            isStopTrailing=True,
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, ema_value, rsi_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        prev_candle = self._prev_candle
        self._prev_candle = candle

        if not macd_value.IsFormed:
            return

        macd = float(macd_value.GetValue[Decimal](None))
        prev_macd = self._prev_macd
        self._prev_macd = macd

        if prev_candle is None or prev_macd is None or not ema_value.IsFormed or not rsi_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema = float(ema_value.GetValue[Decimal](None))
        rsi = float(rsi_value.GetValue[Decimal](None))
        open_price = float(candle.OpenPrice)
        close = float(candle.ClosePrice)
        prev_open = float(prev_candle.OpenPrice)
        prev_close = float(prev_candle.ClosePrice)

        long_signal = (prev_close < prev_open and close > open_price and close > prev_open
                       and close > ema and prev_close > ema and rsi < 65 and macd > prev_macd)

        short_signal = (prev_close > prev_open and close < open_price and close < prev_open
                        and close < ema and prev_close < ema and rsi > 35 and macd < prev_macd)

        if long_signal:
            if self._show_long.Value and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
        elif short_signal:
            if self._show_short.Value and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
            elif self.Position > 0:
                self.SellMarket(self.Position)

    def CreateClone(self):
        return vela_superada_strategy()
