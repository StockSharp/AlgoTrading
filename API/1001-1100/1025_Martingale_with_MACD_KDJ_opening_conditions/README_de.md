# Martingale-Strategie mit MACD- und KDJ-Eröffnungsbedingungen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet Trades, wenn sowohl die MACD-Linie als auch die KDJ %K-Linie ihre Signallinien in dieselbe Richtung kreuzen. Positionen werden nach einem Martingale-Ansatz pyramidisiert: Es wird hinzugefügt, wenn sich der Preis um einen konfigurierten Prozentsatz gegen den Trade bewegt und dann zurückprallt.

Positionen werden geschlossen, wenn eine Take-Profit-, Stop-Loss- oder Trailing-Stop-Bedingung erfüllt ist.

## Details

- **Einstieg**: MACD-Linie und KDJ %K-Linie kreuzen ihre Signallinien in dieselbe Richtung.
- **Hinzufügungen**: Bis zu `Max Additions` Mal, wenn sich der Preis um `Add Position Percent` bewegt und um `Rebound Percent` zurückprallt. Jede Hinzufügungsgröße wird mit `Add Multiplier` multipliziert.
- **Ausstieg**: Schließen bei `Take Profit Trigger`, `Stop Loss Percent` oder Trailing-Stop-Auslösung.
- **Richtung**: Long und Short.

