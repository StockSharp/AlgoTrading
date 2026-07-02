# Strategie zum Ausbruch von Unterstützung und Widerstand
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert den MetaTrader-Experten von „SupportResistTrade“, indem sie einen Ausbruch der jüngsten Unterstützung und des Widerstands mit einem langfristigen EMA-Trendfilter kombiniert. Trades werden nur eröffnet, wenn der Preis die Kanalgrenze Donchian durchbricht **und** die Kerze auf der gleichen Seite eines langen exponentiellen gleitenden Durchschnitts öffnet. Das Risiko wird durch sofortige Schutzstopps und eine dreistufige Trailing-Routine gesteuert, die Gewinne bei +10, +20 und +30 Punkten festlegt.

## Daten und Indikatoren
- **Primärer Feed:** Einzelkerzenabonnement (Standardzeitrahmen von 1 Minute, konfigurierbar über `CandleType`).
- **Unterstützung/Widerstand:** `DonchianChannels` mit der Länge `RangeLength` (Standard 55), um das höchste Hoch und das niedrigste Tief des aktuellen Bereichs zu verfolgen.
- **Trendfilter:** `ExponentialMovingAverage` über Kerzeneröffnungen mit Zeitraum `EmaPeriod` (Standard 500). Es werden nur Long-Positionen mit einem Preis über EMA und Short-Positionen mit einem Preis unter EMA akzeptiert.

## Handelslogik
1. **Marktanalyse:** Bei jeder fertigen Kerze werden der Bereich Donchian und EMA aktualisiert. Das obere Band wird als Widerstand und das untere Band als Unterstützung behandelt.
2. **Eintrittsbedingungen:**
   - **Long:** Kerze schließt über dem Widerstand *und* ihre Eröffnung lag über EMA. Ein eventuell bestehender Short wird geschlossen und eine Long-Market-Order wird gesendet.
   - **Short:** Kerze schließt unter der Unterstützung *und* ihre Eröffnung lag unter EMA. Eventuell bestehende Long-Positionen werden geschlossen und eine Short-Market-Order gesendet.
3. **Anfänglicher Stop:** Nach einer Füllung wird eine Stop-Order an der letzten Unterstützung (für Long-Positionen) oder Widerstand (für Short-Positionen) platziert, was das Stop-Loss-Verhalten von MQL widerspiegelt.
4. **Exit-Logik:**
   - Wenn der Handel profitabel ist und der Schlusskurs über das aktualisierte Unterstützungs-/Widerstandsband hinausgeht, wird die Position zum Marktwert geschlossen, was der manuellen Ausstiegsbedingung von EA entspricht.
   - Der Schutzstopp bleibt aktiv, so dass plötzliche Umkehrungen automatisch abgefangen werden.

## Trailing Stop
Ein abgestufter Trailing-Mechanismus reproduziert die drei `OrderModify`-Aufrufe von EA:
| Gewinnschwelle (Punkte) | Neue Stoppdistanz (Punkte) | Beschreibung |
| --- | --- | --- |
| `>= 20` | `10` | Langer Stopp springt zum Einstieg + 10 Punkte (kurzer Stopp zum Einstieg − 10). |
| `>= 40` | `20` | Stop bewegt sich zum Einstieg +/− 20 Punkte. |
| `>= 60` | `30` | Der letzte Schritt sichert 30 Gewinnpunkte. |
Die Logik lockert niemals den Stopp: Bei Long-Positionen kann sich der Stop nur nach oben bewegen, während er sich bei Short-Positionen nur nach unten bewegen kann.

## Risikomanagement
- Alle Stops werden als native Stop-Orders (`SellStop`/`BuyStop`) implementiert, sodass der Broker die Ausführung auch dann übernimmt, wenn die Strategie kurzzeitig unterbrochen wird.
- Die Strategie funktioniert auf Nettopositionsbasis; Jedes neue Signal schließt in die entgegengesetzte Richtung, bevor ein neuer Handel aufgebaut wird.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `RangeLength` | `55` | Anzahl der Kerzen, die zur Berechnung der Unterstützung (tief) und des Widerstands (hoch) verwendet werden. |
| `EmaPeriod` | `500` | Zeitraum des EMA-Trendfilters, der auf Kerzeneröffnungen angewendet wird. |
| `CandleType` | `1 Minute` | Für alle Berechnungen verwendete Kerzenserie (kann auf jeden anderen Zeitrahmen umgestellt werden). |

## Notizen
- Der Code wird für die übergeordnete Ebene StockSharp API nur mit Indikatorbindung und Kerzenabonnements geschrieben.
- Es wird kein Python-Port bereitgestellt. Der Ordner `CS` enthält die einzige Implementierung.
