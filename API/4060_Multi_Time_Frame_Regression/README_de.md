# Multi-Time-Frame-Regressionsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Strategie mit mehreren Zeitrahmen, die lineare Regressionskanäle für M1-, M5- und H1-Kerzen kombiniert. Die Regressionssteigung des H1-Kanals definiert den vorherrschenden Trend, während die M5- und M1-Kanäle präzise Einstiegspunkte in der Nähe von Unterstützung und Widerstand bieten.

## Handelslogik

- **Datenfeeds**: neun Zeitrahmen von Standardkerzen (M1, M5, M15, M30, H1, H4, D1, W1, MN1).
- **Indikatoren**: Jeder Feed wird von einem linearen Regressionskanal konfigurierbarer Länge verarbeitet. Der Kanal bietet eine Mittellinie und symmetrische obere/untere Bänder basierend auf der maximalen Abweichung der letzten Schlusskurse.
- **Trendfilter**: Die Strategie berücksichtigt nur Short-Trades, wenn die Steigung des H1-Kanals negativ ist, und Long-Trades, wenn sie positiv ist.
- **Eintrag**:
  - **Kurzfristig** – das jüngste M5-Hoch und M1-Hoch durchdringen beide ihre oberen Kanalbänder, während die H1-Steigung negativ ist.
  - **Long** – das jüngste M5-Tief und das M1-Tief erreichen beide ihre unteren Kanalbänder, während die H1-Steigung positiv ist.
- **Auftragsabwicklung**: Eingaben werden mit Marktaufträgen unter Verwendung des konfigurierten Volumens ausgeführt. Stop-Loss- und Take-Profit-Ziele werden aus der Halbwertsbreite bzw. Mittellinie des M5-Kanals abgeleitet.
- **Ausstieg**: Positionen auf den M1-Kerzen werden geschlossen, wenn der Preis den Schutzstopp oder das Mittellinienziel erreicht.
- **Positionsverwaltung**: Es ist immer maximal eine Marktposition offen.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `EnableTrading` | Ermöglicht der Strategie, Bestellungen aufzugeben, wenn sie aktiviert ist. |
| `BarsToCount` | Anzahl der in jedem Regressionskanal verwendeten Balken (Standard: 50). |
| `Volume` | Market-Order-Volumen in Lots. |

## Notizen

- Längere Regressionsfenster sorgen für glattere Kanalsteigungen, aber langsamere Reaktionen.
- Die Multi-Timeframe-Steigungsanzeige ist nützlich für die Überwachung der Ausrichtung über höhere Intervalle hinweg, auch wenn nur die H1-Steigung Einträge abschließt.
- Die Schutzstufen werden jedes Mal neu berechnet, wenn sich eine neue M5-Kerze bildet. Durch häufige Neukalibrierung bleibt das Risiko eng an die aktuelle Kanalgeometrie gekoppelt.
