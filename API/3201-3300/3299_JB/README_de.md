# JB-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung

Die JB-Strategie stammt aus einem fxDreema-Expert-Advisor und kombiniert langfristige Trendfilter, Momentum-Bestätigung und Volatilitätsausbrüche:

- **Trendfilter:** Der Schlusskurs der vorherigen Kerze muss über (Long) oder unter (Short) einem einfachen gleitenden Durchschnitt über 100 Perioden liegen.
- **Momentum-Filter:** Die Richtung wird mit einem Force Index über 100 Perioden bestätigt (positiv für Longs, negativ für Shorts).
- **Volatilitätsauslöser:** Einstieg, wenn der vorherige Schlusskurs das entsprechende Bollinger-Band (20 Perioden, Abweichung 2,0) durchbricht.
- **Positionsverwaltung:** Erhöht das Ordervolumen nach einem Verlustzyklus mit einem martingalartigen Multiplikator und setzt es nach profitablen Zyklen auf die Basisgröße zurück.
- **Ausstiegsregel:** Schließt alle offenen Positionen, sobald der durchschnittliche nicht realisierte Gewinn pro Kontrakt ein konfigurierbares Geldziel erreicht.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `SmaPeriod` | Länge des SMA-Trendfilters. Standard: 100. |
| `ForcePeriod` | Länge des Force-Index-Indikators. Standard: 100. |
| `BollingerPeriod` | Länge der Bollinger-Bänder. Standard: 20. |
| `BollingerDeviation` | Standardabweichungsmultiplikator für Bollinger-Bänder. Standard: 2,0. |
| `BaseVolume` | Anfangsvolumen der Order vor Martingal-Anpassungen. Standard: 0,1. |
| `LossMultiplier` | Multiplikator für das nächste Ordervolumen nach einem Verlustzyklus. Standard: 1,55. |
| `AverageProfitTarget` | Durchschnittlicher nicht realisierter Gewinn pro Kontrakt, der zum Schließen aller Positionen erforderlich ist. Standard: 2,8. |
| `CandleType` | Für Berechnungen verwendeter Kerzentyp (standardmäßig 1-Minuten-Zeitrahmen). |

## Signale

### Long-Einstieg
1. Der Schlusskurs der vorherigen Kerze liegt unter oder auf dem unteren Bollinger-Band.
2. Der vorherige Schlusskurs liegt über der SMA über 100 Perioden (Aufwärtstrend).
3. Der Force-Index-Wert ist positiv.

### Short-Einstieg
1. Der Schlusskurs der vorherigen Kerze liegt über oder auf dem oberen Bollinger-Band.
2. Der vorherige Schlusskurs liegt unter der SMA über 100 Perioden (Abwärtstrend).
3. Der Force-Index-Wert ist negativ.

### Ausstiege
- Wenn der durchschnittliche nicht realisierte Gewinn pro Kontrakt über alle offenen Positionen `AverageProfitTarget` erreicht, werden alle Positionen zum Marktpreis geschlossen.
- Nach jeder flachen Position passt die Strategie das nächste Ordervolumen an: Multiplikation mit `LossMultiplier` nach einem Verlustzyklus, Rücksetzung auf `BaseVolume` nach einem profitablen Zyklus.

## Hinweise

- Die Martingal-Anpassung nutzt realisierten PnL, um Verlustserien zu erkennen; verwenden Sie die Strategie nur auf Instrumenten, bei denen steigendes Volumen akzeptabel ist.
- Da StockSharp-Strategien mit Nettopositionen arbeiten, wird das Hedging der MQL-Version (gleichzeitige Long- und Short-Körbe) über aggregierte Positionen angenähert.
