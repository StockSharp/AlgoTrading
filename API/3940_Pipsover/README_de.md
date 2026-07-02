# Pipsover 8167 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Pipsover 8167**-Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `Pipsover.mq4`, der mit Build 8167 vertrieben wird. Der Experte sucht nach starken Chaikin-Oszillatorspitzen, die unmittelbar nach einem Rückzug zum einfachen gleitenden 20-Perioden-Durchschnitt der vorherigen Kerze auftreten. Wenn diese Kombination auftritt, eröffnet das Skript eine Position in Richtung des Impulses und schützt sie mit festen Stop-Loss- und Take-Profit-Distanzen (70 bzw. 140 Punkte im ursprünglichen MQL-Code). Diese C#-Version erstellt genau dieselbe Logik mithilfe von StockSharp-Komponenten auf hoher Ebene neu, sodass kein direkter Pufferzugriff erforderlich ist.

Die Implementierung verwendet den Accumulation/Distribution Line (ADL)-Indikator und zwei exponentielle gleitende Durchschnitte, um die von `iCustom("Chaikin", ...)` in MetaTrader erzeugten Chaikin-Oszillatorwerte zu rekonstruieren. Alle Handelsentscheidungen werden verzögert, bis die Kerze vollständig geschlossen ist, wobei die Prüfungen `OrdersTotal()` und `Close[1]` / `Open[1]` aus dem Quellskript repliziert werden.

## Indikatoren und Signale
- **Einfacher gleitender Durchschnitt (SMA 20)** – angewendet auf Kerzenschlüsse. Die vorherige Kerze muss den SMA (unten unten für Long-Positionen, hoch oben für Short-Positionen) durchbrechen und gleichzeitig einen Körper in Richtung des Setups beibehalten.
- **Chaikin-Oszillator (EMA 3 – EMA 10 von ADL)** – intern aus dem ADL-Stream neu erstellt, um `iCustom("Chaikin", 0, 0, 1)`-Messwerte widerzuspiegeln. Ein- und Ausstiegsschwellenwerte werden in absoluten Oszillatoreinheiten ausgedrückt.
- **Preisaktionsfilter** – die Strategie prüft die Richtung des vorherigen Kerzenkörpers: bullische Körper ermöglichen Long-Trades, während bärische Körper Short-Trades ermöglichen.

## Handelsregeln
### Langer Eintrag
1. Die vorherige Kerze schließt bullisch (`Close[1] > Open[1]`).
2. Das vorherige Tief fällt unter den SMA20-Wert dieser Kerze.
3. Der vorherige Chaikin-Wert liegt unter `-OpenLevel` (Standard 55).
4. Derzeit ist keine Position offen.

### Kurzer Eintrag
1. Vorherige Kerze schließt bärisch (`Close[1] < Open[1]`).
2. Das vorherige Hoch liegt über dem SMA20-Wert dieser Kerze.
3. Der vorherige Chaikin-Wert liegt über `OpenLevel`.
4. Derzeit ist keine Position offen.

### Ausstiegsbedingungen
- **Long-Positionen** schließen, wenn die nächste Kerze erfüllt: rückläufiger Körper, hoch über SMA20 und Chaikin über `CloseLevel` (Standard 90).
- **Short-Positionen** schließen, wenn die nächste Kerze einen bullischen Körper hat, Tief unter SMA20 und Chaikin unter `-CloseLevel`.
- Darüber hinaus beinhaltet jeder Trade einen Schutzstopp bei `StopLossPoints` und einen Take-Profit bei `TakeProfitPoints`, beide ausgedrückt in Preisschritten des ausgewählten Instruments.

## Risikomanagement
- Stop-Loss-Distanz: `StopLossPoints × PriceStep` (standardmäßig 70 Punkte).
- Take-Profit-Distanz: `TakeProfitPoints × PriceStep` (standardmäßig 140 Punkte).
- Positionsgröße: konfigurierbar über `TradeVolume`, direkt der Eigenschaft `Volume` der Strategie StockSharp zugeordnet und für alle Marktaufträge verwendet.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `TradeVolume` | 0,1 | Market-Order-Volumen (Lots oder Kontrakte, je nach Wertpapier). |
| `MaLength` | 20 | Zeitraum des SMA, der für die Pullback-Prüfung verwendet wird. |
| `StopLossPoints` | 70 | Stop-Loss-Distanz gemessen in Preisschritten. |
| `TakeProfitPoints` | 140 | Take-Profit-Distanz gemessen in Preisschritten. |
| `OpenLevel` | 55 | Absoluter Chaikin-Oszillator-Schwellenwert, der neue Einträge freischaltet. |
| `CloseLevel` | 90 | Absoluter Chaikin-Oszillator-Schwellenwert, der Ausgänge erzwingt. |
| `ChaikinFastLength` | 3 | Schnelle EMA-Länge in der Chaikin-Rekonstruktion. |
| `ChaikinSlowLength` | 10 | Langsame EMA-Länge in der Chaikin-Rekonstruktion. |
| `CandleType` | H1 | Zeitrahmen, der zum Abonnieren von Kerzen und zum Berechnen von Indikatoren verwendet wird. |

## Implementierungshinweise
- Kerzen und Indikatoren sind über `SubscribeCandles().Bind(...)` verbunden, sodass die Strategie innerhalb der allgemeinen API-Richtlinien bleibt.
- Chaikin-Werte werden im Speicher berechnet, indem ADL-Messwerte in zwei EMA-Objekte eingespeist werden, wodurch verbotene Aufrufe wie `GetValue()` für Indikatorpuffer vermieden werden.
- Frühere Kerzeninformationen werden im Strategiestatus zwischengespeichert, um die MQL-Zugriffsmuster `Close[1]`, `Low[1]`, `High[1]` und `iCustom(...,1)` zu reproduzieren.
- Stop-Loss- und Take-Profit-Level werden manuell verfolgt, da der ursprüngliche Experte einfache Marktaufträge mit statischen Offsets anstelle serverseitiger Schutzaufträge gesendet hat.
