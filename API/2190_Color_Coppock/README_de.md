# Color Coppock-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Color Coppock Strategy** implementiert ein Handelssystem auf Basis eines modifizierten Coppock-Oszillators. Der Oszillator summiert zwei Rate-of-Change-Werte (ROC) und glättet das Ergebnis mit einem gleitenden Durchschnitt. Steigendes Momentum erzeugt Long-Signale, fallendes Momentum erzeugt Short-Signale.

## Funktionsweise

1. Berechnung von zwei ROC-Werten mit unterschiedlichen Perioden.
2. Summierung beider ROC-Werte und Anwendung eines einfachen gleitenden Durchschnitts zur Glättung.
3. Vergleich des aktuellen Oszillatorwerts mit den zwei vorherigen Werten:
   - Wenn der Oszillator nach einem Rückgang nach oben dreht, eröffnet die Strategie eine Long-Position oder schließt eine bestehende Short-Position.
   - Wenn der Oszillator nach einem Anstieg nach unten dreht, eröffnet die Strategie eine Short-Position oder schließt eine bestehende Long-Position.
4. Das Positionsvolumen wird aus der `Volume`-Eigenschaft der Strategie entnommen.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `Roc1Period` | Periode für die erste ROC-Berechnung. |
| `Roc2Period` | Periode für die zweite ROC-Berechnung. |
| `SmoothingPeriod` | SMA-Periode für die Summe beider ROC-Werte. |
| `CandleType` | Kerzentyp für die Indikatorberechnungen. |

## Verwendung

1. Strategie einem Wertpapier zuweisen und gewünschte Parameter setzen.
2. Die Strategie abonniert die angegebenen Kerzen und verarbeitet nur abgeschlossene Kerzen.
3. Trades werden mit Marktorders unter Verwendung des Standardvolumens ausgeführt.

## Hinweise

- Die Strategie verwendet nur High-Level-API-Aufrufe wie `SubscribeCandles` und Marktorder-Helfer.
- Alle Kommentare im Code sind auf Englisch verfasst.
