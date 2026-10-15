# AH HM MFI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung

Die AH HM MFI-Strategie handelt mit Hammer- und Hängemann-Kerzenmustern, die durch den Money Flow Index (MFI) bestätigt werden. Wenn in einem kurzfristigen Abwärtstrend ein bullischer Hammer erscheint und der MFI unter einem überverkauften Schwellenwert bleibt, eröffnet die Strategie eine Long-Position. Wenn sich in einem Aufwärtstrend ein bärischer hängender Mann bildet, während der MFI über einer überkauften Schwelle liegt, eröffnet er eine Short-Position. Schutzausgänge werden ausgelöst, wenn der MFI vordefinierte Ober- oder Untergrenzen überschreitet.

## Kernlogik

1. Abonnieren Sie die konfigurierten Zeitrahmenkerzen und berechnen Sie zwei Indikatoren:
   - **Geldflussindex** mit konfigurierbarem Zeitraum (Standard: 47).
   - **Einfacher gleitender Durchschnitt** der Schlusskurse zur Annäherung an den Trendfilter der ursprünglichen MQL-Strategie (Standardlänge: 5).
2. **Hammer**- und **Hanging Man**-Muster erkennen:
   - Der Kerzenkörper liegt im oberen Drittel des Sortiments.
   - Langer unterer Schatten im Verhältnis zum realen Körper.
   - Lücke in der Trendrichtung im Vergleich zur vorherigen Kerze.
   - Trendbestätigung anhand des Mittelpunkts der vorherigen Kerze im Vergleich zum gleitenden Durchschnitt.
3. Eingaben mit MFI-Schwellenwerten bestätigen:
   - Geben Sie „long“ ein, wenn ein Hammer erkannt wird und der MFI auf oder unter dem konfigurierten Überverkauft-Niveau liegt (Standard: 40).
   - Geben Sie Short ein, wenn ein hängender Mann erkannt wird und der MFI auf oder über dem konfigurierten Überkaufniveau liegt (Standard: 60).
4. Ausgänge über MFI-Kreuzungen verwalten:
   - Schließen Sie Short-Positionen, wenn der MFI das untere oder obere Ausgangsniveau nach oben überschreitet (Standardwerte: 30 und 70).
   - Schließen Sie Long-Positionen, wenn der MFI die obere Ausstiegsebene nach oben oder die untere Ausstiegsebene nach unten kreuzt.
5. Starten Sie das integrierte Risikoschutzmodul, um Notstopps durchzuführen.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Kerzendatentyp und Zeitrahmen, die zur Mustererkennung verwendet werden. | 30-minütiger Zeitrahmen |
| `MfiPeriod` | Rückblickzeitraum für die MFI-Berechnung. | 47 |
| `MaPeriod` | Länge des SMA, der zur Trendbestätigung auf die Schlusskurse angewendet wird. | 5 |
| `HammerEntryThreshold` | Maximal zulässiger MFI-Wert vor Eingabe eines Hammersignals. | 40 |
| `HangingEntryThreshold` | Erforderlicher Mindest-MFI-Wert vor dem Betreten eines Hängenden-Mann-Signals. | 60 |
| `MfiUpperExitLevel` | Obere MFI-Grenze; Wenn Sie darüber kreuzen, wird jede offene Position geschlossen. | 70 |
| `MfiLowerExitLevel` | Untere MFI-Grenze; Bei einem Schnitt darunter werden Long-Positionen geschlossen, bei einem Schnitt darüber werden Short-Positionen geschlossen. | 30 |

## Notizen

- Die Strategie bewertet nur fertige Kerzen, um zu vermeiden, dass auf unvollständigen Informationen reagiert wird.
- Die Erkennung von Hammer und hängendem Mann ist konservativ: Es sind sowohl ein langer unterer Schatten als auch ein Körper in der Nähe der Kerzenhöhe erforderlich.
- Der gleitende Durchschnitt ersetzt den Filter MetaTrader 5 `CloseAvg` des ursprünglichen Expertenberaters und stellt sicher, dass die Einträge mit dem breiteren Trend übereinstimmen.
