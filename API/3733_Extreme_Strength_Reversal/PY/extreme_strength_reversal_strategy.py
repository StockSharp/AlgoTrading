import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class extreme_strength_reversal_strategy(Strategy):
    """BB + RSI extreme reversal with pip-based SL/TP."""

    def __init__(self):
        super(extreme_strength_reversal_strategy, self).__init__()

        self._risk_percent = self.Param("RiskPercent", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Risk Percent", "Risk per trade as percentage of equity", "Risk Management")
        self._stop_loss_pips = self.Param("StopLossPips", 150) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk Management")
        self._take_profit_pips = self.Param("TakeProfitPips", 300) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk Management")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Number of candles used for Bollinger Bands", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Number of candles used for RSI", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 65.0) \
            .SetDisplay("RSI Overbought", "RSI level that marks extreme overbought conditions", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 35.0) \
            .SetDisplay("RSI Oversold", "RSI level that marks extreme oversold conditions", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle timeframe used for analysis", "General")

        self._stop_loss_price = None
        self._take_profit_price = None
        self._entry_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def BollingerPeriod(self):
        return self._bollinger_period.Value

    @property
    def BollingerDeviation(self):
        return self._bollinger_deviation.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def RsiOverbought(self):
        return self._rsi_overbought.Value

    @property
    def RsiOversold(self):
        return self._rsi_oversold.Value

    def OnReseted(self):
        super(extreme_strength_reversal_strategy, self).OnReseted()
        self._stop_loss_price = None
        self._take_profit_price = None
        self._entry_price = None

    def OnStarted(self, time):
        super(extreme_strength_reversal_strategy, self).OnStarted(time)

        bollinger = BollingerBands()
        bollinger.Length = self.BollingerPeriod
        bollinger.Width = self.BollingerDeviation

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, rsi, self._process_candle).Start()

    def _process_candle(self, candle, bb_value, rsi_ind):
        if candle.State != CandleStates.Finished:
            return

        upper_band = bb_value.UpBand
        lower_band = bb_value.LowBand

        if upper_band is None or lower_band is None:
            return

        upper = float(upper_band)
        lower = float(lower_band)
        rsi_value = float(rsi_ind)

        self._manage_open_position(candle)

        if self.Position != 0:
            return

        close_price = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        bullish_reversal = (rsi_value < float(self.RsiOversold) and rsi_value > 0
                            and float(candle.LowPrice) < lower and close_price > open_price)
        if bullish_reversal:
            self._try_enter_long(close_price)
            return

        bearish_reversal = (rsi_value > float(self.RsiOverbought)
                            and float(candle.HighPrice) > upper and close_price < open_price)
        if bearish_reversal:
            self._try_enter_short(close_price)

    def _try_enter_long(self, close_price):
        if self.Position < 0:
            self.BuyMarket(abs(self.Position))

        self.BuyMarket()

        pip_size = self._get_pip_size()
        self._entry_price = close_price
        self._stop_loss_price = close_price - self.StopLossPips * pip_size if self.StopLossPips > 0 else None
        self._take_profit_price = close_price + self.TakeProfitPips * pip_size if self.TakeProfitPips > 0 else None

    def _try_enter_short(self, close_price):
        if self.Position > 0:
            self.SellMarket(self.Position)

        self.SellMarket()

        pip_size = self._get_pip_size()
        self._entry_price = close_price
        self._stop_loss_price = close_price + self.StopLossPips * pip_size if self.StopLossPips > 0 else None
        self._take_profit_price = close_price - self.TakeProfitPips * pip_size if self.TakeProfitPips > 0 else None

    def _manage_open_position(self, candle):
        if self.Position > 0:
            if self._stop_loss_price is not None and float(candle.LowPrice) <= self._stop_loss_price:
                self.SellMarket(self.Position)
                self._reset_trade_state()
                return
            if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket(self.Position)
                self._reset_trade_state()
        elif self.Position < 0:
            short_position = abs(self.Position)
            if self._stop_loss_price is not None and float(candle.HighPrice) >= self._stop_loss_price:
                self.BuyMarket(short_position)
                self._reset_trade_state()
                return
            if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket(short_position)
                self._reset_trade_state()
        elif self._stop_loss_price is not None or self._take_profit_price is not None or self._entry_price is not None:
            self._reset_trade_state()

    def _get_pip_size(self):
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
            if step > 0:
                return step
        return 0.0001

    def _reset_trade_state(self):
        self._stop_loss_price = None
        self._take_profit_price = None
        self._entry_price = None

    def CreateClone(self):
        return extreme_strength_reversal_strategy()
