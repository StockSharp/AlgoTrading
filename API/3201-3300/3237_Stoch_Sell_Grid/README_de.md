# Stoch Sell-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert das Verhalten des ursprünglichen MetaTrader-Experten **stochSell**. Sie hört auf einen einzelnen Kerzenstream und wartet auf eine dreifache stochastische Bestätigung kombiniert mit einem Volatilitätsfilter, bevor eine anfängliche Market-Sell-Order gesendet wird. Unmittelbar nach dem Short-Einstieg wird eine Leiter ausstehender Sell-Stops eingesetzt, um die Bewegung zu skalieren, wenn der Preis weiter fällt.

## Handelslogik
- **Volatilitätsfilter** – eine Average True Range (ATR) mit konfigurierbarer Länge muss unter dem angegebenen Schwellenwert bleiben.
- **Langsame stochastische Bestätigung** – der längste stochastische Oszillator muss unter dem langfristigen Überverkauf-Niveau bleiben, bevor Trades erlaubt sind.
- **Kreuzungsbestätigung** – sowohl der mittlere als auch der schnelle stochastische Oszillator müssen während derselben abgeschlossenen Kerze durch den Überverkauf-Trigger nach unten kreuzen.
- **Positionsprüfung** – neue Einstiege werden nur dann platziert, wenn die Strategie keine aktiven Orders hat und die Position flat ist.

Sobald alle Bedingungen erfüllt sind, sendet die Strategie eine Market-Sell-Order mit dem konfigurierten Volumen und plant sofort eine Reihe von Sell-Stop-Orders gemäß den Grid-Einstellungen. Ausstehende Orders sind optional und können deaktiviert werden, indem der Grid-Order-Zähler auf null gesetzt wird.

## Ausstiegsregeln
- **Gewinnziel** – wenn der Short-Korb den gewünschten Gewinn in Pips akkumuliert (berechnet aus dem volumengewichteten Einstiegspreis), kauft die Strategie die gesamte Position zurück und entfernt alle verbleibenden ausstehenden Orders.
- **Manueller Stop** – Grid-Orders respektieren eine konfigurierbare Lebensdauer. Wenn eine Stop-Order ohne Ausführung abläuft, wird sie automatisch storniert.
- **Vollständiges Schließen** – jeder Kauftrade, der die Position auf null zurückbringt, löscht die internen Einstiegsstatistiken und storniert das ausstehende Grid.

## Grid-Verwaltung
- Ausstehende Orders werden unterhalb des Referenzpreises mit dem Start-Offset und Schritt in Pips platziert.
- Jede ausstehende Order verwendet den Grid-Volumen-Multiplikator, wodurch die Korbgröße vom anfänglichen Market-Einstieg abweichen kann.
- Die Ablaufzeit (in Minuten) wird auf jede ausstehende Order angewendet; null deaktiviert das Timeout.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Primärer Zeitrahmen für jeden Indikator und jede Handelsentscheidung. |
| `AtrPeriod` / `AtrThreshold` | Volatilitätsfilter, der steuert, wann die Strategie handeln darf. |
| `FastKPeriod`, `FastDPeriod`, `FastSlowing` | Konfiguration des schnellen stochastischen Oszillators. |
| `MediumKPeriod`, `MediumDPeriod`, `MediumSlowing` | Konfiguration des mittleren stochastischen Oszillators. |
| `SlowKPeriod`, `SlowDPeriod`, `SlowSlowing` | Konfiguration des langsamen stochastischen Oszillators. |
| `OversoldLevel` | Niveau, durch das die schnellen und mittleren stochastischen Werte nach unten kreuzen müssen. |
| `LongTermOversoldLevel` | Obere Grenze für den langsamen Stochastik beim Einstieg. |
| `ProfitTargetPips` | Nettogewinn in Pips zum Schließen des Short-Korbs. |
| `GridOrdersCount` | Anzahl der nach dem Einstieg erstellten ausstehenden Sell-Stops. |
| `GridStartOffsetPips` | Offset in Pips zwischen Einstiegspreis und erster ausstehender Order. |
| `GridStepPips` | Abstand in Pips zwischen aufeinanderfolgenden ausstehenden Orders. |
| `GridVolume` | Volumen für jede ausstehende Order. |
| `GridExpirationMinutes` | Lebensdauer ausstehender Orders in Minuten. |
| `MarketVolume` | Volumen für den anfänglichen Market-Verkauf. |

## Hinweise
- Indikatorwerte werden über die High-Level-`BindEx`-API verarbeitet und nur fertige Kerzen lösen Handelsentscheidungen aus.
- Die Positionsverfolgungslogik hält einen volumengewichteten Einstiegspreis, um das Rohgewinnziel in Pips zu übersetzen.
- Um die Skalierung zu deaktivieren, setzen Sie den Grid-Order-Zähler einfach auf null; die Strategie verlässt sich dann immer noch auf die stochastische Bestätigung und den ATR-Filter für einzelne Trades.
