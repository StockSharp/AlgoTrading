# Color Bears-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie erstellt einen doppelt geglätteten Bears-Power-Oszillator und handelt bei Änderungen seiner Steigung.

## Idee
1. Einen exponentiellen gleitenden Durchschnitt (MA1) der Schlusskurse berechnen.
2. Bears Power als Differenz zwischen dem Kerzentiefst und MA1 berechnen.
3. Bears Power mit einem weiteren exponentiellen gleitenden Durchschnitt (MA2) glätten.
4. Verfolgen, ob der geglättete Wert steigt oder fällt, und auf Steigungsumkehrungen reagieren.

## Handelsregeln
- Wenn der Indikator von steigend zu fallend wechselt (Farbe 0 → 2), Short-Positionen schließen und eine Long-Position eröffnen.
- Wenn der Indikator von fallend zu steigend wechselt (Farbe 2 → 0), Long-Positionen schließen und eine Short-Position eröffnen.
- Jede Position verwendet die `Volume`-Eigenschaft der Strategie als Ordergröße.

## Parameter
| Name | Beschreibung |
|------|--------------|
| `Ma1Period` | Periode des ersten EMA zur Berechnung von Bears Power. |
| `Ma2Period` | Periode des glättenden EMA. |
| `CandleType` | Kerzen-Zeitrahmen für Berechnungen. |

## Hinweise
Diese C#-Implementierung ist aus dem MQL-Experten „ColorBears" (Ordner `MQL/14314`) adaptiert.
Der Algorithmus basiert auf Standard-StockSharp-Indikatoren und High-Level-API-Bindings.
