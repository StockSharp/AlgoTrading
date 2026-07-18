# Exp-TrendValue-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Strategie basierend auf dem TrendValue-Indikator. Sie baut dynamische Unterstützungs- und Widerstandsbänder mit gewichteten gleitenden Durchschnitten von Hoch- und Tiefpreisen auf, die um ATR verschoben werden. Ein neuer Auf- oder Abwärtstrend wird erkannt, wenn der Preis das entgegengesetzte Band kreuzt.

## Einstieg und Ausstieg
- **Long-Einstieg**: Wenn ein neuer Aufwärtstrend beginnt.
- **Short-Einstieg**: Wenn ein neuer Abwärtstrend beginnt.
- **Long-Ausstieg**: Bei einem Abwärtssignal oder Trendlinie.
- **Short-Ausstieg**: Bei einem Aufwärtssignal oder Trendlinie.

## Parameter
- `BuyPosOpen` / `SellPosOpen` – Long/Short-Einstiege aktivieren.
- `BuyPosClose` / `SellPosClose` – Schließen von Long/Short-Positionen erlauben.
- `StopLossPips` – Stop-Loss in Preispunkten.
- `TakeProfitPips` – Take-Profit in Preispunkten.
- `MaPeriod` – Periode des gewichteten gleitenden Durchschnitts.
- `ShiftPercent` – prozentuale Verschiebung der Durchschnitte.
- `AtrPeriod` – ATR-Periode.
- `AtrSensitivity` – Multiplikator für ATR.
- `CandleType` – Kerzenzeitrahmen.

## Hinweise
Die Strategie abonniert Kerzendaten und aktualisiert Indikatoren bei jeder abgeschlossenen Kerze. Marktorders werden bei erfüllten Bedingungen platziert und Schutz-Stop- sowie Take-Profit-Niveaus werden intern verfolgt.
