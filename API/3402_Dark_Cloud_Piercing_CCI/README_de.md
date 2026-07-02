# Dark Cloud Piercing CCI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein StockSharp-Port des MetaTrader Expert_ADC_PL_CCI-Advisors. Es durchsucht die Preisbewegung nach Piercing Line- und Dark Cloud Cover-Kerzenumkehrungen und verwendet den Commodity Channel Index (CCI) als Bestätigung. Sobald ein gültiges Muster zusammen mit einem extremen CCI-Wert erkannt wird, eröffnet die Strategie eine Marktposition in Richtung der Umkehr und verlässt sie später, wenn der CCI seinen extremen Bereich verlässt.

## Indikatoren
- **Commodity Channel Index (CCI):** bestätigt Momentum-Extreme und erstellt die Ausstiegsbedingungen.
- **Durchschnittliche Körperlänge (SMA):** misst die Körpergröße der Kerze, um „lange“ Kerzen innerhalb der Musterdefinition zu validieren.
- **Durchschnittlicher Schlusskurs (SMA):** fungiert als einfacher Trendfilter, der den in der ursprünglichen MQL-Logik verwendeten gleitenden Durchschnitt widerspiegelt.

## Handelsregeln
### Entry
- **Bulles Signal (Durchdringungslinie):**
  1. Die vorherige Kerze muss eine lange bärische Kerze sein, die über ihrem Schlusskurs öffnet.
  2. Die letzte Kerze muss eine lange bullische Kerze sein, die unterhalb des vorherigen Tiefs öffnet und innerhalb des vorherigen Körpers schließt, oberhalb ihres Mittelpunkts, aber unterhalb der vorherigen Eröffnung.
  3. Der Mittelpunkt der älteren Kerze muss unter dem gleitenden Durchschnitt liegen, um einen kurzfristigen Abwärtstrend zu bestätigen.
  4. The most recent completed CCI value must be less than or equal to `-EntryConfirmationLevel` (default `50`).
  5. Wenn eine Short-Position besteht, wird diese vollständig geschlossen, bevor eine Long-Position eingegangen wird.
- **Bearisches Signal (dunkle Wolkendecke):** spiegelte die Logik des bullischen Signals mit einer langen bullischen Kerze, gefolgt von einer langen bärischen Kerze, die eine Lücke nach oben bildet, den vorherigen Körper durchdringt und unter seinem Mittelpunkt schließt, während CCI größer oder gleich `EntryConfirmationLevel` ist.

### Ausstieg
- **Long-Positionen:** geschlossen, wenn der CCI unter `ExitLevel` fällt oder von oben unter `-ExitLevel` fällt, was signalisiert, dass sich die Dynamik normalisiert hat.
- **Short-Positionen:** geschlossen, wenn der CCI über `-ExitLevel` oder über `ExitLevel` von unten kreuzt.

### Positionsgrößen
- Verwendet die Basiseigenschaft `Volume`. Wenn das Signal die Umkehrung einer bestehenden Position erfordert, addiert die Strategie automatisch die absolute Größe der aktuellen Position zum Auftragsvolumen und stellt so eine vollständige Umkehrung sicher.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Kerzentyp und Zeitrahmen, die zur Erkennung verwendet werden. | `1H` Zeitrahmen |
| `CciPeriod` | Lookback-Länge des Commodity Channel Index. | `49` |
| `AverageBodyPeriod` | Anzahl der Kerzen für den gleitenden Durchschnitt der Körpergröße. | `11` |
| `EntryConfirmationLevel` | Absoluter CCI-Level, der Mustereinträge validiert. | `50` |
| `ExitLevel` | Absoluter CCI-Level, der Positionsausstiege auslöst. | `80` |

## Notizen
- The strategy processes only finished candles and ignores partial updates.
- No stop-loss or take-profit orders are set automatically; Ausstiege erfolgen ausschließlich signalbasiert wie im ursprünglichen Expert Advisor.
- Ensure the instrument has a price step configured because the equality tolerance of the candlestick logic depends on the security settings.
