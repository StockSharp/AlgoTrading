# IBS RSI CCI v4-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **IBS RSI CCI v4-Strategie** ist ein konträres Handelssystem, das drei Momentum-Oszillatoren kombiniert:

- **Internal Bar Strength (IBS)** – misst die relative Schlussposition innerhalb des Hoch-Tief-Bereichs der Kerze und wird mit einem konfigurierbaren gleitenden Durchschnitt geglättet.
- **Relative Strength Index (RSI)** – erfasst das Markt-Momentum um das neutrale 50er-Niveau.
- **Commodity Channel Index (CCI)** – bewertet die Kursabweichung von einer gleitenden Durchschnittslinie.

Die drei Komponenten werden skaliert und zu einem zusammengesetzten Oszillator gemischt. Das zusammengesetzte Signal wird durch einen konfigurierbaren Schritt-Schwellenwert eingeschränkt und durch einen Donchian-artigen Hoch/Tief-Umschlag gefiltert. Kreuzungen zwischen dem zusammengesetzten Signal und seiner Mittellinie erzeugen Umkehrmöglichkeiten.

## Handelslogik
1. Kerzen mit dem gewählten Zeitrahmen abonnieren (Standard: 4 Stunden).
2. Den IBS-Wert für jede abgeschlossene Kerze berechnen und mit dem gewählten Moving-Average-Typ glätten.
3. RSI- und CCI-Werte mit ihren jeweiligen Lookback-Längen ermitteln.
4. Den zusammengesetzten Oszillator mit der ursprünglichen Gewichtung aus dem MetaTrader-Skript aufbauen:
   - IBS-Beitrag × 700
   - RSI-Abweichung von 50 × 9
   - Roh-CCI-Wert × 1
5. Einen Schritt-Schwellenwert anwenden, um plötzliche Sprünge im zusammengesetzten Signal zu vermeiden.
6. Das rollende Maximum und Minimum des zusammengesetzten Signals verfolgen und beide Kanten glätten, um ein dynamisches Band zu bilden. Die Mittellinie des Bands wird als „Basislinie" verwendet (entspricht dem zweiten Indikatorpuffer in der MQL-Version).
7. **Positionsmanagement**
   - Long-Positionen schließen, wenn das zusammengesetzte Signal auf der bestätigten Kerze unter der Basislinie liegt.
   - Short-Positionen schließen, wenn das zusammengesetzte Signal auf der bestätigten Kerze über der Basislinie liegt.
   - Long-Positionen öffnen, wenn die zuvor bestätigte Kerze über der Basislinie war und das neueste Signal durch die Basislinie nach unten kreuzt (konträrer Einstieg).
   - Short-Positionen öffnen, wenn die zuvor bestätigte Kerze unter der Basislinie war und das neueste Signal durch die Basislinie nach oben kreuzt.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Kerzenserie für Indikatorrechnungen. |
| `IbsPeriod` | Lookback-Länge zum Glätten der IBS-Komponente. |
| `IbsAverageType` | Gleitender-Durchschnitt-Typ für IBS-Glättung (Einfach, Exponentiell, Geglättet, Linear Gewichtet). |
| `RsiPeriod` | RSI-Lookback-Länge. |
| `CciPeriod` | CCI-Lookback-Länge. |
| `RangePeriod` | Fenstergröße für das rollende Hoch/Tief-Band des zusammengesetzten Signals. |
| `SmoothPeriod` | Länge des gleitenden Durchschnitts zum Glätten der Hoch/Tief-Band-Kanten. |
| `RangeAverageType` | Gleitender-Durchschnitt-Typ für die Bandglättung (Einfach, Exponentiell, Geglättet, Linear Gewichtet). |
| `StepThreshold` | Maximale Anpassung bei scharfen Sprüngen des zusammengesetzten Signals zwischen Kerzen. |
| `SignalBar` | Anzahl bereits geschlossener Kerzen zur Bestätigung (Standard 1 repliziert das ursprüngliche Verhalten). |
| `EnableLongOpen` | Öffnen neuer Long-Positionen erlauben. |
| `EnableShortOpen` | Öffnen neuer Short-Positionen erlauben. |
| `EnableLongClose` | Schließen bestehender Long-Positionen erlauben. |
| `EnableShortClose` | Schließen bestehender Short-Positionen erlauben. |
| `OrderVolume` | Basis-Marktorder-Volumen bei Einstiegen. |

## Implementierungshinweise
- Die Schritt-Einschränkung repliziert die Puffer-Begrenzungslogik des MQL-Indikators. Ein höherer `StepThreshold` erlaubt größere Sprünge im zusammengesetzten Oszillator.
- Für IBS und Umschlag-Glättung werden nur die vier gängigsten gleitenden Durchschnittsfamilien unterstützt, da die StockSharp-Standardbibliothek die benutzerdefinierten Filter aus der MetaTrader-Ressourcendatei nicht enthält.
- Die Strategie verwendet `SignalBar`, um Signale um eine vollständig geschlossene Kerze zu verzögern, was dem ursprünglichen Expertenberater-Verhalten entspricht.
- Standardmäßig ist die Strategie vollständig konträr: Signale werden gegen die Richtung der letzten Kreuzung generiert. Schalten Sie die Ein-/Ausstiegs-Booleans um, um die Strategie auf eine einzelne Richtung zu beschränken.

## Verwendung
1. `CandleType` konfigurieren, um dem Zielinstrument-Zeitrahmen zu entsprechen.
2. Indikatslängen und den Schritt-Schwellenwert an die Volatilität des Instruments anpassen.
3. Long/Short-Einstiege und -Ausstiege nach Handelspräferenz aktivieren oder deaktivieren.
4. Den `OrderVolume`-Parameter zur Ordergrößensteuerung setzen und die Strategie starten. `StartProtection()` ist standardmäßig aktiviert und kann angepasst werden, wenn zusätzliche Risikoregeln erforderlich sind.
5. Das Diagramm-Panel (falls verfügbar) überprüfen, um Kerzenpreise, den zusammengesetzten Oszillator und aufgezeichnete Trades zu überwachen.

## Unterschiede zur MetaTrader-Version
- Geldmanagement- und Orderabweichungsparameter des ursprünglichen EA werden durch StockSharp's `OrderVolume`-Parameter und High-Level-Marktorders ersetzt.
- Die StockSharp-Konvertierung behält die ursprünglichen Indikatorgewichtungen und Umkehrlogik bei, konzentriert sich aber auf die am häufigsten verwendeten gleitenden Durchschnittsfilter.
- Schutz-Stops sind nicht vorkonfiguriert; kombinieren Sie die Strategie mit StockSharp-Risikomodulen, wenn feste Stops oder Take-Profits erforderlich sind.
