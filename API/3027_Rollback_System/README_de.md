# Rollback System Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Konvertierung des MetaTrader 5-Expertenberaters **"Rollback system"**. Sie bewahrt die ursprüngliche Idee,
ganz am Anfang eines neuen Handelstages zu handeln, indem die letzten 24 Stundenkerzen ausgewertet werden, um zu erkennen, ob der Markt einen
ausgedehnten Zug geliefert hat, der wahrscheinlich zurücklaufen wird.

## Handelslogik

1. Die Strategie arbeitet auf einem stündlichen Zeitrahmen (`CandleType`, Standard 1 Stunde).
2. Signale werden nur einmal täglich ausgewertet, wenn der neue Tag beginnt (`00:00` – `00:03`). Der Filter überspringt Montag- und Freitagssitzungen
   genau wie die MQL-Version.
3. Vor dem Eröffnen einer Position stellt der Algorithmus sicher, dass keine anderen Trades aktiv sind.
4. Für jeden Handelstag werden die folgenden Werte aus den letzten 24 geschlossenen Kerzen berechnet:
   - `Open_24_minus_Close_1` – Abstand zwischen dem Eröffnungspreis vor 24 Bars und dem letzten Schlusskurs.
   - `Close_1_minus_Open_24` – umgekehrter Abstand, der die Nettotagsveränderung zeigt.
   - `Close_1_minus_Lowest` – wie weit der Schlusskurs vom tiefsten Tief des Tages entfernt ist.
   - `Highest_minus_Close_1` – wie weit der Schlusskurs vom höchsten Hoch des Tages entfernt ist.
5. Einstiegsregeln (in Preiseinheiten ausgedrückt, aus Pip-Parametern umgerechnet):
   - **Long #1** – Vortag fiel (`Open_24_minus_Close_1` über dem Schwellenwert `ChannelOpenClosePips`) und der Schlusskurs ist immer noch
     nahe dem extremen Tief (`Close_1_minus_Lowest` unter `RollbackPips - ChannelRollbackPips`).
   - **Long #2** – Vortag stieg (`Close_1_minus_Open_24` über dem Kanalschwellenwert), aber der Markt schloss weit unter dem
     Tageshoch (`Highest_minus_Close_1` größer als `RollbackPips + ChannelRollbackPips`).
   - **Short #1** – Vortag stieg und der Schlusskurs endete nahe dem Tageshoch (`Highest_minus_Close_1` unter
     `RollbackPips - ChannelRollbackPips`).
   - **Short #2** – Vortag fiel und der Schlusskurs erholte sich weit über das Tagestief (`Close_1_minus_Lowest` über
     `RollbackPips + ChannelRollbackPips`).
6. Aufträge werden mit `BuyMarket`/`SellMarket` mit dem konfigurierten Handelsvolumen ausgeführt. Stop-Loss- und Take-Profit-Niveaus werden
   aus `StopLossPips` und `TakeProfitPips` abgeleitet (beide null deaktivieren den jeweiligen Schutz).
7. Schutzniveaus werden auf jeder abgeschlossenen Kerze überwacht. Wenn der Preis intrabar ein Niveau verletzt, schließt die Strategie die Position
   mit einer Marktorder, was das Verhalten des originalen MQL-Expertenberaters repliziert, der harte Stops sendete.

## Pip-Parameterkonvertierung

MetaTrader 5 multipliziert Pip-Werte bei 3- und 5-stelligen Symbolen mit 10. Die Konvertierungslogik wird beibehalten: Die Strategie nimmt den
`PriceStep` des Instruments und wendet einen zehnfachen Multiplikator an, wenn die erkannte Anzahl der Dezimalstellen 3 oder 5 beträgt. Dies hält die
Einstiegsschwellen, Stop-Loss- und Take-Profit-Abstände konsistent mit der MQL-Implementierung über typische FX-Symbole.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Handelsgröße für Marktorders. |
| `StopLossPips` | Stop-Loss-Abstand in Pips. Auf null setzen, um zu deaktivieren. |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. Auf null setzen, um zu deaktivieren. |
| `RollbackPips` | Basis-Rollback-Anforderung für alle Signale. |
| `ChannelOpenClosePips` | Mindestdifferenz zwischen Eröffnung und Schluss des Vortages. |
| `ChannelRollbackPips` | Toleranz, die zur Rollback-Prüfung addiert/subtrahiert wird. |
| `CandleType` | Arbeits-Kerzentyp, standardmäßig Stundenkerzen. |

## Hinweise

- Die MQL-Version zeichnete Rechtecke im Chart zur visuellen Referenz. Der StockSharp-Port behält nur die Handelslogik.
- Risikomanagement wird mit strategie-interner Überwachung statt serverseitiger Schutzorders implementiert, da die High-Level-API
  Positionen direkt verwaltet.
- Passen Sie bei der Optimierung die Pip-Schwellenwerte und das Volumen an das Zielinstrument und die Tick-Größe des Brokers an.
