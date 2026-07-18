# Forex-Paar-Rendite-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt ein ausgewähltes Forex-Paar auf Basis des Momentums der 2-Jahres-Renditespanne zwischen seinen Währungen. Das Momentum wird als Differenz zwischen der Spanne und ihrem gleitenden Durchschnitt gemessen. Bollinger-Bänder auf dem Momentum definieren überkaufte und überverkaufte Zonen. Positionen werden nach einer festen Anzahl von Kerzen geschlossen.

## Hauptmerkmale

- Verwendet das 2-Jahres-Renditespannen-Momentum für Signale.
- Bollinger-Bänder auf dem Momentum identifizieren extreme Bedingungen.
- Optionale Umkehrung der Einstiegslogik.
- Schließt Positionen nach einer angegebenen Anzahl von Kerzen.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `YieldASecurity` | Erstes Renditewertpapier. |
| `YieldBSecurity` | Zweites Renditewertpapier. |
| `CandleType` | Kerzen-Zeitrahmen für die Analyse. |
| `MomentumLength` | Periode für den Renditespannen-Durchschnitt. |
| `BollingerLength` | Periode für Bollinger-Bänder. |
| `BollingerStdDev` | Standardabweichungsmultiplikator für Bänder. |
| `HoldPeriods` | Kerzen zum Halten einer Position. |
| `ReverseLogic` | Long- und Short-Bedingungen umkehren. |

## Komplexität

Anfänger

