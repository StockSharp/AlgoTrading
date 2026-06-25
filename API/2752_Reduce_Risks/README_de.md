# Risikominimierungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Risikominimierungs-Strategie ist ein Multi-Timeframe-Trendfolgesystem, das aus dem MetaTrader Expert Advisor "Reduce_risks.mq5" konvertiert wurde. Es analysiert 1-Minuten-Kerzen, um Einstiege auszulösen, und filtert das Marktregime mit 15-Minuten- und 1-Stunden-Durchschnitten. Der ursprüngliche Algorithmus wurde für hochliquide Forex-Major-Paare (EURUSD, USDCHF, USDJPY) entwickelt und konzentriert sich darauf, Trends nur dann einzusteigen, wenn die Volatilität gedämpft ist und die Struktur die Fortsetzung bestätigt.

## Markt und Zeitrahmen
- **Primärer Zeitrahmen:** 1-Minuten-Kerzen für die Signalerzeugung.
- **Bestätigungszeitrahmen:** 15-Minuten-Kerzen für Momentum-Validierung und Wellenpositionierung.
- **Trendfilter:** 1-Stunden-Kerzen zur Sicherstellung des Handels in der breiteren Trendrichtung.
- **Empfohlene Instrumente:** EURUSD, USDCHF, USDJPY oder Instrumente mit ähnlicher Pip-Struktur (4 oder 5 Dezimalstellen).

## Indikatoren und Daten
- Vier einfache gleitende Durchschnitte (SMA) auf M1: Perioden 5, 8, 13 und 60, berechnet auf den typischen Preis.
- Drei SMAs auf M15: Perioden 4, 5 und 8, berechnet auf den typischen Preis.
- Ein SMA auf H1: Periode 24, berechnet auf den typischen Preis.
- Kerzensignalstatistiken (Körpergröße, Bereich, Schatten) für M1 und M15.
- Interne Zähler verfolgen den höchsten oder niedrigsten Preis seit dem Einstieg, um die MQL-Trailing-Logik zu emulieren.

## Einstiegsregeln
### Long-Setup
1. Jüngste M1- und M15-Kerzen müssen geringe Volatilität aufweisen: Drei vorherige Balken auf jedem Zeitrahmen haben Bereiche unter 20 bzw. 30 Pips, und die 15-Minuten-Kanalbreite ist auf 30 Pips begrenzt.
2. Die zuletzt abgeschlossene M1-Kerze ist aktiver als ihr Vorgänger, aber nicht dreimal größer, und der aktuelle Preis bricht sowohl die jüngsten M1- als auch M15-Hochs (lokaler Widerstand geräumt).
3. Die SMA-Hierarchie zeigt nach oben: SMA5 > SMA8 > SMA13 und SMA60 steigend; der Schlusskurs liegt über allen vier Durchschnitten.
4. SMA4 auf M15 steigt und liegt über SMA8, während der Schlusskurs über den M15- und H1-Durchschnitten liegt.
5. Wellenbestätigung: SMA8 auf M1 kreuzte innerhalb einer der drei vorherigen Kerzen, und SMA5 auf M15 liegt innerhalb des vorherigen M15-Kerzenbereichs.
6. Kerzenstrukturfilter: Vorherige M1- und M15-Kerzen haben bullische Körper, die mehr als die Hälfte ihrer Bereiche übersteigen, höhere Hochs beibehalten, akzeptable Rücksetzer zeigen (<25% des vorherigen Kerzenbereichs) und Intrabar-Schatten enthalten (kein Marubozu).
7. Alle obigen Bedingungen müssen gleichzeitig erfüllt sein, ohne offene Position, bevor eine Market-Kauforder ausgestellt wird.

### Short-Setup
1. Dieselben Volatilitätsfilter gelten, aber der Ausbruch muss unterhalb jüngster Tiefs erfolgen (Unterstützungsverletzung).
2. Die SMA-Hierarchie kehrt sich um: SMA5 < SMA8 < SMA13 mit fallendem SMA60; der Schlusskurs liegt unter allen vier Durchschnitten.
3. SMA4 auf M15 fällt und liegt unter SMA8; der Schlusskurs liegt unter den M15- und H1-Durchschnitten.
4. Wellenvalidierung: SMA8 auf M1 liegt innerhalb eines der drei vorherigen M1-Kerzenbereiche, SMA5 auf M15 liegt innerhalb der letzten M15-Kerze, und jüngste Kerzen zeigen anhaltende bearische Struktur (tiefere Tiefs, bearische Körper, begrenzte Rücksetzer, Schatten vorhanden).
5. Ohne aktive Position wird eine Market-Verkauforder gesendet, sobald alle Bedingungen ausgerichtet sind.

## Ausstiegsregeln
- Schützende Stop-Loss- und Take-Profit-Orders werden automatisch mit den konfigurierten Pip-Distanzen angehängt (spiegelt das ursprüngliche EA-Verhalten wider).
- Zusätzliche diskretionäre Exits replizieren die MQL-Logik:
  - Longs schließen, wenn die aktuelle M1-Kerze mindestens 10 Pips von ihrer Eröffnung fällt oder wenn eine starke bearische M1-Kerze erscheint, nachdem der Trade mehr als eine Minute offen war.
  - Früh Gewinn nehmen, wenn der Preis mindestens 10 Pips vorrückt, oder wenn eine Trailing-Umkehr auftritt: Nach dem ersten Balken nach dem Einstieg, wenn der Preis 20 Pips vom höchsten seit Einstieg erreichten Level zurückfällt, während dieses Hoch über dem Einstiegspreis liegt.
  - Longs bei einer 20-Pip-Gegenausweitung schließen oder wann immer das Portfolio-Eigenkapital unter den konfigurierten Drawdown-Schwellenwert fällt. Short-Positionen verwenden symmetrische Logik mit invertierten Vergleichen.

## Risikomanagement
- Der Handel stoppt automatisch, wenn das Portfolio-Eigenkapital unter `(InitialDeposit * (100% - RiskPercent))` fällt. Das Limit wird bei jedem Signalversuch geprüft und zurückgesetzt, sobald das Eigenkapital sich über den Schwellenwert erholt.
- Das ursprüngliche MQL-Skript enthielt umfangreiche Terminal-Checks; diese werden weggelassen, da StockSharp Konnektivität und Berechtigungen nativ handhabt.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `StopLossPips` | Schutz-Stop-Distanz in Pips (von der Trailing-Logik gespiegelt). | `30` |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. | `60` |
| `InitialDeposit` | Referenz-Eigenkapital für die Berechnung des Drawdown-Stops. | `10000` |
| `RiskPercent` | Maximaler Prozentsatz des Anfangsdepots, der verloren gehen kann, bevor neue Trades blockiert und aktive Positionen zwangsgeschlossen werden. | `5` |
| `M1CandleType` | Datentyp für das 1-Minuten-Kerzen-Abonnement. | `1-Minuten`-Zeitrahmen |
| `M15CandleType` | Datentyp für das 15-Minuten-Bestätigungs-Abonnement. | `15-Minuten`-Zeitrahmen |
| `H1CandleType` | Datentyp für das 1-Stunden-Trendfilter-Abonnement. | `1-Stunden`-Zeitrahmen |

## Hinweise
- Die Strategie erwartet Instrumente, die mit ähnlichen Pip-Größen wie wichtige Forex-Paare notiert werden. Passen Sie die pip-basierten Parameter an, wenn andere Märkte genutzt werden.
- Es wird nur die C#-Implementierung bereitgestellt; die Python-Version wird gemäß Anforderungen absichtlich weggelassen.
