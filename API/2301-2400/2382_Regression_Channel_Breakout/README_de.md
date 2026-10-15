# Regressions-Kanal-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein Regressionskanal-Handelssystem auf Basis des MQL-Skripts `e-Regr`.
Es wird eine lineare Regressionslinie über eine konfigurierbare Anzahl aktueller Kerzen aufgebaut und
obere sowie untere Bänder im Abstand einer bestimmten Standardabweichung hinzugefügt. Handelsregeln:

- **Long-Einstieg:** wenn das Kerzentief das untere Band berührt oder darunter bricht.
- **Short-Einstieg:** wenn das Kerzenhoch das obere Band berührt oder darüber bricht.
- **Ausstieg:** wenn der Schlusskurs die Regressionslinie in entgegengesetzter Richtung kreuzt.
- **Trailing-Stop:** optionale Trailing-Logik verschiebt das Stop-Level, nachdem der Trade
  einen konfigurierten Gewinn erreicht hat.

## Parameter

| Name            | Beschreibung                                                    |
|-----------------|-----------------------------------------------------------------|
| `CandleType`    | Kerzentyp für Berechnungen.                                     |
| `Length`        | Anzahl der Kerzen für Regression und Standardabweichung.        |
| `Deviation`     | Standardabweichungs-Multiplikator für die Kanalbreite.          |
| `UseTrailing`   | Aktiviert die Trailing-Stop-Logik.                              |
| `TrailingStart` | Erforderlicher Gewinn, bevor Trailing beginnt.                  |
| `TrailingStep`  | Abstand zwischen Preis und Trailing-Stop.                       |

Die Strategie verwendet die High-Level-StockSharp-API über `SubscribeCandles` und `Bind`,
um Kerzdaten und Indikatorwerte zu empfangen.
