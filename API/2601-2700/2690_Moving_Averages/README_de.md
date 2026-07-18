# Gleitende-Durchschnitte-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Gleitende-Durchschnitte-Strategie repliziert den klassischen MetaTrader-Experten, der Kreuzungen des Preises relativ zu einem verschobenen einfachen gleitenden Durchschnitt (SMA) handelt. Der Algorithmus verarbeitet nur abgeschlossene Kerzen und stellt sicher, dass alle Handelsentscheidungen auf vollständig geformten Bars basieren. Die Positionsgröße folgt einem dynamischen Risikomodell, das an das Kontoeigenkapital gebunden ist und sich an Verluststrähnen anpasst, ähnlich der ursprünglichen MQL-Implementierung.

## Handelslogik
- Ein einfacher gleitender Durchschnitt wird mit einer konfigurierbaren Periode und einer zusätzlichen Vorwärtsverschiebung berechnet, die in abgeschlossenen Bars gemessen wird.
- Bei jeder abgeschlossenen Kerze prüft die Strategie, ob die Bar über dem verschobenen SMA eröffnet und darunter geschlossen hat (bärische Kreuzung) oder darunter eröffnet und darüber geschlossen hat (bullishe Kreuzung).
- Das System verwaltet immer nur eine Position. Wenn eine Kreuzung gegen die aktive Position auftritt, wird die Position zuerst geschlossen und es werden keine Umkehrorders auf derselben Bar gesendet.
- Wenn keine Position offen ist:
  - Eine bullishe Kreuzung öffnet eine Long-Position.
  - Eine bärische Kreuzung öffnet eine Short-Position.

## Positionsmanagement
- Long-Positionen werden bei einer bärischen Kreuzung geschlossen.
- Short-Positionen werden bei einer bullishen Kreuzung geschlossen.
- Die Trade-Ausführung verwendet Marktorders auf dem Strategie-Instrument.
- Die Trade-Historie wird verfolgt, um den effektiven Einstiegspreis zu berechnen, damit Gewinn und Verlust beim Schließen der Position gemessen werden können.

## Risikomanagement und Positionsgrößenbestimmung
- Das Basis-Ordervolumen wird aus dem Portfolioeigenkapital multipliziert mit dem **Maximum Risk**-Parameter und dividiert durch den aktuellen Schlusskurs abgeleitet. Wenn das Eigenkapital nicht verfügbar ist, fällt die Strategie auf das Standard-Strategievolumen zurück.
- Ein **Decrease Factor**-Parameter reduziert das berechnete Ordervolumen, wenn aufeinanderfolgende Verlusttrades erkannt werden. Die Reduktion ist proportional zur Verluststrähne und reproduziert die adaptive Größenlogik der MQL-Version.
- Das Ordervolumen ist nie negativ; wenn die Reduktion den Basisbetrag vollständig kompensiert, wird der Trade übersprungen.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `MaximumRisk` | Anteil des Kontoeigenkapitals, der bei jedem Trade riskiert wird. | `0.02` |
| `DecreaseFactor` | Divisor zur Volumenreduzierung nach aufeinanderfolgenden Verlusten. | `3` |
| `MovingPeriod` | Periode des SMA für Signale. | `12` |
| `MovingShift` | Anzahl abgeschlossener Bars zur Vorwärtsverschiebung des SMA. | `6` |
| `CandleType` | Kerzenserie für Berechnungen (Zeitrahmen). | `5m`-Kerzen |

## Hinweise
- Die gleitende Durchschnittsverschiebung wird durch einen internen Ringpuffer implementiert, sodass die Strategie den SMA-Wert von mehreren Bars zuvor verwendet, ähnlich wie der MetaTrader-Indikator-Shift-Parameter.
- Orders werden nur generiert, wenn sowohl der SMA als auch der verschobene Puffer vollständig geformt sind, um vorzeitige Trades während der Aufwärmphase zu verhindern.
- Protokollnachrichten dokumentieren Einstiege, Ausstiege und Trade-Ergebnisse zur Unterstützung der Fehlersuche und Leistungsanalyse.
