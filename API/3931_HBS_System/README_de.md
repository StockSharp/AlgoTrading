# HBS-Systemstrategie (StockSharp Version)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **HBS-Systemstrategie** ist eine High-Level-StockSharp-Konvertierung des MetaTrader 4-Expertenberaters „HBS system.mq4“ (ForTrader.ru). Das Original EA kombiniert die exponentielle Filterung des gleitenden Durchschnitts mit ausstehenden Stop-Orders, die auf feste Preisniveaus gerundet werden. In Trendrichtung werden zwei Stop-Orders eingesetzt: Die erste zielt auf ein nahegelegenes abgerundetes Niveau und die zweite auf einen längeren Ausbruch. Beide Trades nutzen die gleiche schützende Stop- und Trailing-Logik, wodurch eine mehrschichtige Breakout-Struktur entsteht.

Dieser StockSharp-Port behält das Multi-Order-Verhalten bei und umfasst gleichzeitig das High-Level-API. Aufträge werden über die Helfer für ausstehende Aufträge (`BuyStop`, `SellStop`, `SellLimit`, `BuyLimit`) übermittelt und das Risiko wird über dynamisch verwaltete Schutzstopps kontrolliert. Der Code ist zur einfacheren Wartung vollständig auf Englisch kommentiert.

## Handelslogik

1. **Trendfilter** – Ein exponentieller gleitender Durchschnitt (EMA), der auf dem Medianpreis (`(High + Low) / 2`) der abgeschlossenen Kerzen berechnet wird, definiert den aktiven Trend. Es werden nur vollständig geformte Kerzen verarbeitet, was das Verhalten von `iMA(..., shift=1)` von MetaTrader widerspiegelt.
2. **Stufenrundung** – Der Schlusskurs der vorherigen Kerze wird mit einem konfigurierbaren Multiplikator (Standard `100`, d. h. zwei Dezimalstellen) auf- und abgerundet. Diese gerundeten Werte emulieren die ursprünglichen `MathCeil`/`MathFloor`-Aufrufe.
3. **Einstiegskonstruktion** – Wenn die vorherige Kerze über EMA öffnet und schließt, werden zwei Kauf-Stopp-Orders platziert:
   - **Primärorder** bei `roundedHigh - entryOffset` mit einem Take-Profit in Höhe des gerundeten Niveaus.
   - **Sekundärorder** zum gleichen Einstiegspreis, aber mit um `secondaryTakeProfitPoints` weiter verschobenem Take-Profit.
   - Beide Orders haben einen gemeinsamen Stop-Loss (`entry - stopLossPoints`).

Die Logik spiegelt sich bei Shorts wider, wenn die Kerze unter EMA öffnet und schließt. Gegenüberliegende ausstehende Orders werden automatisch storniert, um Überschneidungen zu vermeiden.
4. **Positionsverwaltung** – Wenn eine ausstehende Order ausgeführt wird, registriert die Strategie eine dedizierte Take-Profit-Limit-Order und aktualisiert den gemeinsamen Stop-Loss. Die Trailing-Stop-Logik verschärft den Stop, wenn sich der Preis zugunsten der offenen Position bewegt, und respektiert dabei die konfigurierten Trailing-Distanzen.
5. **Bereinigung** – Abgeschlossene oder stornierte Bestellungen werden aus der internen Registrierung entfernt. Wenn die Nettoposition wieder flach ist, werden alle Schutzanordnungen aufgehoben, um den Zustand zurückzusetzen.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `EMA Period` | Länge des exponentiellen gleitenden Durchschnittsfilters. | 200 |
| `Buy Stop-Loss (points)` | Abstand (in Punkten) zwischen dem langen Einstieg und seinem Schutzanschlag. | 50 |
| `Buy Trailing (points)` | Nachlaufdistanz für Long-Positionen. | 10 |
| `Sell Stop-Loss (points)` | Abstand (in Punkten) zwischen dem kurzen Einstieg und seinem Schutzstopp. | 50 |
| `Sell Trailing (points)` | Nachlaufdistanz für Short-Positionen. | 10 |
| `Order Volume` | Das Volumen gilt für **jede** ausstehende Bestellung. Bei den standardmäßig zwei Aufträgen entspricht die maximale Belichtung dem Doppelten dieses Wertes. | 0,1 |
| `Entry Offset (points)` | Offset (in Punkten), der vom gerundeten Niveau abgezogen/addiert wird, um den ausstehenden Einstiegspreis zu erhalten. | 15 |
| `Second Take-Profit (points)` | Zusätzliche Distanz, die vom sekundären Take-Profit-Ziel genutzt wird. | 15 |
| `Rounding Factor` | Für die Rundungslogik verwendeter Multiplikator (z. B. 100 → zwei Dezimalstellen). | 100 |
| `Candle Type` | Datentyp für die Kerzenaggregation. Standardmäßig ist ein Zeitrahmen von 1 Stunde eingestellt. | `TimeFrame(1h)` |

## Hinweise zur Nutzung

- Stellen Sie sicher, dass `Security.PriceStep` (oder `Security.Decimals`) konfiguriert ist. andernfalls fällt die Strategie auf einen Punktwert von 0,0001 zurück.
- Jeder ausstehende Auftrag verwaltet seinen eigenen Take-Profit, sodass die Gesamtposition in zwei Stufen skaliert werden kann.
- Trailing Stops werden erst aktiviert, nachdem sich der Preis um die konfigurierte Distanz (`TrailingStop{Buy/Sell}Points`) nach oben bewegt hat.
- Die Strategie geht von einer traditionellen Forex-Preisgestaltung aus, bei der eine Rundung auf zwei Dezimalstellen sinnvoll ist. Passen Sie `RoundingFactor` an, wenn eine andere Genauigkeit erforderlich ist.
- Es sind keine automatisierten Geldverwaltungsregeln enthalten; Legen Sie `OrderVolume` entsprechend den Risikopräferenzen fest.

## Conversion-Highlights

- Alle Kommentare wurden in Englisch umgeschrieben und die Struktur folgt dem Repository-Styleguide (Tabs, Namespace, Benennung).
- Hochrangige StockSharp-Helfer werden für Datenabonnements, die Verwaltung ausstehender Bestellungen und die Bearbeitung von Schutzbestellungen verwendet.
- Die Trailing-Stop- und Take-Profit-Koordination reproduziert die Dual-Order-Architektur des ursprünglichen MetaTrader-Experten, bleibt aber idiomatisch für StockSharp.

## Referenzen

- Ursprüngliches MT4-Skript: `MQL/8134/HBS_system.mq4`
- StockSharp-Dokumentation: [https://doc.stocksharp.com/](https://doc.stocksharp.com/)
