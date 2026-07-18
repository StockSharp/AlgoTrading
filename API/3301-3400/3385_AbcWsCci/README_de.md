# AbcWsCci-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **AbcWsCci-Strategie** kombiniert zwei klassische japanische Candlestick-Umkehrmuster – **Three White Soldiers** und **Three Black Crows** – mit dem **Commodity Channel Index (CCI)**-Indikator zur Bestätigung. Das System scannt fertige Kerzen, misst die Körpergröße im Verhältnis zu einer gleitenden Durchschnittsbasislinie und eröffnet Geschäfte nur, wenn ein starkes Multi-Kerzen-Momentum mit CCI-Extremen übereinstimmt. Positionsausstiege werden ausgelöst, wenn der CCI die Extremzonen verlässt, was signalisiert, dass die Dynamik nachlässt.

## Handelslogik
- Behalten Sie einen gleitenden Durchschnitt der Kerzenkörpergrößen bei, um „lange“ Kerzen zu qualifizieren.
- Erkennen Sie das Muster der drei weißen Soldaten (drei aufeinanderfolgende starke bullische Kerzen mit steigenden Mittelpunkten).
- Erkennen Sie das Three Black Crows-Muster (drei aufeinanderfolgende starke bärische Kerzen mit fallenden Mittelpunkten).
- Bestätigen Sie bullische Einstiege, wenn CCI unter **-50** fällt, und bärische Einstiege, wenn CCI über **50** steigt.
- Schließen Sie Long-Positionen, wenn CCI die Werte **-80** oder **80** überschreitet, und schließen Sie Short-Positionen zu den entsprechenden Bedingungen.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `CciPeriod` | Länge des zur Bestätigung verwendeten Indikators CCI. | 37 |
| `BodyAveragePeriod` | Anzahl der Kerzen im gleitenden Durchschnitt, die die minimale „starke“ Körpergröße definieren. | 13 |
| `CandleType` | Kerzenzeitrahmen, der zur Mustererkennung verwendet wird. | 1 Stunde |

## Indikatoren
- **Commodity Channel Index (CCI)**: Bewertet Momentum-Extreme für Bestätigungs- und Ausstiegssignale.
- **Einfacher gleitender Durchschnitt der Kerzenkörper**: Legt die Mindestkerzengröße fest, die für ein gültiges Muster erforderlich ist.

## Positionsmanagement
- Geben Sie **long** ein, wenn sich drei weiße Soldaten bilden und CCI unter -50 liegt, während keine Long-Position aktiv ist.
- Geben Sie **short** ein, wenn sich Three Black Crows bilden und CCI über 50 liegt, während keine Short-Position aktiv ist.
- Verlassen Sie **Long-Positionen**, wenn CCI das -80/80-Band verlässt, was darauf hinweist, dass der Aufwärtsimpuls erschöpft ist.
- Verlassen Sie **Short-Positionen**, wenn CCI das +80/-80-Band verlässt, was einen rückläufigen Momentumverlust signalisiert.

## Nutzungshinweise
- Die Strategie ist ereignisgesteuert: Es werden nur vollständig abgeschlossene Kerzen verarbeitet.
- Funktioniert am besten bei Trendinstrumenten, bei denen Multi-Candle-Momentum in Kombination mit Oszillator-Extremen zuverlässige Signale liefert.
- Erwägen Sie die Kombination mit zusätzlichen Risikomanagementregeln (Stop-Loss, Positionsgrößenbestimmung), abhängig von Ihrer Handelsumgebung.
