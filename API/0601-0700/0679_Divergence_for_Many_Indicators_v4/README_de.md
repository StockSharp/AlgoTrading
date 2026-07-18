# Divergenz-Strategie für viele Indikatoren v4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erkennt Divergenzen zwischen Preis und mehreren Momentum-Indikatoren (MACD, RSI, Stochastic, CCI, Momentum, OBV, MFI).
Eine Position wird eröffnet, wenn mindestens eine bestimmte Anzahl von Indikatoren eine Divergenz in dieselbe Richtung zeigt.

## Details
- **Einstiegskriterien**: Long eingehen, wenn der Preis fällt, während die meisten Indikatoren steigen (positive Divergenz). Short eingehen, wenn der Preis steigt, während die meisten Indikatoren fallen (negative Divergenz).
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetzte Divergenz oder Positionsschutz
- **Stops**: Konfigurierbare Take-Profit- und Stop-Loss-Prozentsätze
- **Standardwerte**: 5m-Kerzen, 2 Bestätigungen, 4% Take-Profit, 2% Stop-Loss
- **Filter**: Verwendet mehrere Momentum-Indikatoren zur Bestätigung
