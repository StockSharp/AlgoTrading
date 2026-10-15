# Pinbar Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert aus dem ursprünglichen MQL-Expert Advisor `PINBAR.mq4` (Ordner `MQL/22269`). Die Strategie erkennt Pin-Bar-Umkehrungen auf dem primären Zeitrahmen und bestätigt diese mit höherzeitigen Momentum- und MACD-Filtern. Sie reproduziert den Geist des Quellsystems unter Verwendung der StockSharp High-Level-API-Features.

## Handelslogik

- **Primärer Zeitrahmen** – konfigurierbarer Kerzentyp zur Identifizierung von Kursaktions-Mustern.
- **Höherer Zeitrahmen** – konfigurierbarer Kerzentyp zur Bestätigung von Momentum und MACD-Trendbias.
- **Pin-Bar-Erkennung** – eine Bar wird akzeptiert, wenn der echte Kerzenkörper klein relativ zum Gesamtbereich ist und ein Docht die Kerze dominiert (konfigurierbare Körper- und Dochtverhältnisse).
- **Trendfilter** – der schnelle EMA muss über (oder unter) dem langsamen EMA für Long- (oder Short-)Setups liegen und spiegelt die LWMA-Filter aus dem Originalcode wider.
- **Momentum-Bestätigung** – das Momentum auf dem höheren Zeitrahmen muss für mindestens einen der letzten drei höherzeitigen Bars über (Long) oder unter (Short) einem konfigurierbaren Schwellenwert liegen.
- **MACD-Bestätigung** – der MACD-Wert muss für Long-Trades über seiner Signallinie und für Shorts unter der Signallinie liegen, entsprechend der monatlichen MACD-Bestätigung im MQL-Expert.
- **Fraktal-Bestätigung** – die Strategie pflegt ein rollendes Fünf-Bar-Fenster und erfordert das Vorhandensein des neuesten bullischen/bärischen Fraktals vor dem Annehmen eines neuen Trades, ähnlich dem `FindFractals()`-Gate in der Quelle.
- **Risikomanagement** – konfigurierbarer Stop-Loss, Take-Profit, Break-Even-Trigger/Offset und Trailing-Stop-Logik verfolgen die offene Position. Der Trade wird geschlossen, wenn ein Niveau berührt wird oder das Trailing-Niveau verletzt wird.

## Einstiegsregeln

### Long-Setup
1. Letzte Kerze auf dem primären Zeitrahmen bildet einen bullischen Pin Bar (langer unterer Docht, kleiner Körper).
2. Schneller EMA > langsamer EMA.
3. Letztes Momentum des höheren Zeitrahmens (oder einer der zwei vorherigen Werte) liegt über dem Schwellenwert.
4. Höherzeitiger MACD liegt über seiner Signallinie.
5. Ein bullisches Fraktal wurde kürzlich erkannt und der Preis hat es nicht ungültig gemacht.
6. Strategie ist flat oder short (Shorts werden umgekehrt).

### Short-Setup
1. Letzte Kerze auf dem primären Zeitrahmen bildet einen bärischen Pin Bar (langer oberer Docht, kleiner Körper).
2. Schneller EMA < langsamer EMA.
3. Letztes Momentum des höheren Zeitrahmens (oder einer der zwei vorherigen Werte) liegt unter dem negativen Schwellenwert.
4. Höherzeitiger MACD liegt unter seiner Signallinie.
5. Ein bärisches Fraktal wurde kürzlich erkannt und der Preis hat es nicht ungültig gemacht.
6. Strategie ist flat oder long (Longs werden umgekehrt).

## Ausstiegsregeln

- Stop-Loss und Take-Profit werden in Prozent relativ zum Einstiegspreis ausgedrückt.
- Break-Even aktiviert sich, sobald sich der Preis um den Trigger-Prozentsatz bewegt; der Stop wird zu Einstieg plus/minus einem Offset verschoben.
- Trailing-Stop aktiviert sich nach Erreichen des Aktivierungsprozentsatzes und folgt dem Preis bei der konfigurierten Distanz.
- Entgegengesetzte Signale kehren ebenfalls die Position um.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 15-Minuten-Kerzen | Primärer Zeitrahmen für die Mustererkennung. |
| `TrendCandleType` | 1-Stunden-Kerzen | Höherer Zeitrahmen für Momentum/MACD-Filter. |
| `FastMaLength` | 6 | Schnelle EMA-Länge (ersetzt schnellen LWMA). |
| `SlowMaLength` | 85 | Langsame EMA-Länge (ersetzt langsamen LWMA). |
| `MomentumLength` | 14 | Momentum-Indikatorlänge auf höherem Zeitrahmen. |
| `MomentumThreshold` | 0.1 | Minimaler absoluter Momentum-Wert zur Bestätigung. |
| `MacdFastLength` | 12 | MACD schnelle EMA-Länge. |
| `MacdSlowLength` | 26 | MACD langsame EMA-Länge. |
| `MacdSignalLength` | 9 | MACD Signal-EMA-Länge. |
| `BodyToRangeRatio` | 0.3 | Maximale Körpergröße relativ zum Kerzenbereich. |
| `WickRatio` | 0.6 | Minimales dominantes Dochtverhältnis zur Pin-Bar-Definition. |
| `StopLossPercent` | 2 | Schutz-Stop-Größe in Prozent. |
| `TakeProfitPercent` | 4 | Gewinnzielgröße in Prozent. |
| `BreakEvenTriggerPercent` | 1.5 | Erforderlicher Gewinn zum Verschieben des Stops auf Break-Even. |
| `BreakEvenOffsetPercent` | 0.2 | Zusätzlicher Offset für den Break-Even-Stop. |
| `TrailingActivationPercent` | 2.5 | Gewinnschwelle zur Aktivierung des Trailing Stops. |
| `TrailingDistancePercent` | 1 | Trailing-Stop-Distanz nach Aktivierung. |

## Hinweise

- Das Volumen ist standardmäßig auf 1 Kontrakt fixiert; passen Sie das Strategie-Volumen für unterschiedliche Positionsgrößen an.
- Die Fraktals-Erkennung setzt sich zurück, wenn der Preis das aufgezeichnete Fraktal-Niveau verletzt, was ein neues Muster vor einem neuen Trade erfordert.
- Optimierungsbereiche sind für wichtige Parameter enthalten, um das Backtesting und die Abstimmung in StockSharp Designer zu erleichtern.
