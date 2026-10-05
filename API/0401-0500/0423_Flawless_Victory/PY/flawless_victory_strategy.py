import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy

RSI_OVERSOLD = 30.0
RSI_OVERBOUGHT = 70.0
MFI_OVERSOLD = 20.0
MFI_OVERBOUGHT = 80.0


class flawless_victory_strategy(Strategy):
    """
    Flawless Victory strategy.
    Goes long when the close is below the lower Bollinger band with RSI under 30 and short when it is above the upper band
    with RSI over 70; the opposite signal reverses the position. Version 2 adds percent take-profit and stop-loss exits,
    and Version 3 additionally requires MFI under 20 for longs and over 80 for shorts.
    """

    def __init__(self):
        super(flawless_victory_strategy, self).__init__()
        self._version = self.Param("Version", 1) \
            .SetRange(1, 3) \
            .SetDisplay("Version", "1: RSI signals, 2: adds take-profit/stop-loss, 3: adds MFI confirmation", "General")
        self._rsi_length = self.Param("RSI_length", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._mfi_length = self.Param("MFI_length", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("MFI Length", "MFI period", "Indicators")
        self._bb_length = self.Param("BBLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bb_multiplier = self.Param("BBMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Multiplier", "Bollinger Bands standard deviation multiplier", "Indicators")
        self._take_profit_pct = self.Param("TakeProfitPct", 1.5) \
            .SetNotNegative() \
            .SetDisplay("Take Profit %", "Take-profit percentage for version 2", "Risk")
        self._stop_loss_pct = self.Param("StopLossPct", 1.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage for version 2", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        super(flawless_victory_strategy, self).OnStarted2(time)

        bollinger = BollingerBands()
        bollinger.Length = self._bb_length.Value
        bollinger.Width = Decimal(self._bb_multiplier.Value)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        mfi = MoneyFlowIndex()
        mfi.Length = self._mfi_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, rsi, mfi, self._process_candle).Start()

        if int(self._version.Value) == 2:
            tp = float(self._take_profit_pct.Value)
            sl = float(self._stop_loss_pct.Value)
            self.StartProtection(
                Unit(Decimal(tp), UnitTypes.Percent) if tp > 0 else Unit(),
                Unit(Decimal(sl), UnitTypes.Percent) if sl > 0 else Unit(),
                useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)
                self.DrawIndicator(oscillators, mfi)

    def _process_candle(self, candle, bollinger_value, rsi_value, mfi_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed or not rsi_value.IsFormed or not mfi_value.IsFormed:
            return

        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upper = float(bollinger_value.UpBand)
        lower = float(bollinger_value.LowBand)
        rsi = float(rsi_value.GetValue[Decimal](None))
        mfi = float(mfi_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)
        use_mfi = int(self._version.Value) == 3

        long_signal = close < lower and rsi < RSI_OVERSOLD and (not use_mfi or mfi < MFI_OVERSOLD)
        short_signal = close > upper and rsi > RSI_OVERBOUGHT and (not use_mfi or mfi > MFI_OVERBOUGHT)

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return flawless_victory_strategy()
