# 2526 TDI-2 ReOpen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine C#-Konvertierung des MetaTrader 5 Expert Advisors **Exp_TDI-2_ReOpen**. Sie handelt mit dem Trend Direction Index (TDI-2)-Indikator und wendet die ursprüngliche Positions-Wiedereinstiegslogik an. Der C#-Port verwendet die High-Level-StockSharp-API und behält das Kernverhalten der MQL-Version bei: Er reagiert auf Kreuzungen zwischen der TDI-Momentum-Linie und der TDI-Indexlinie, skaliert bei profitablen Positionen nach einem konfigurierbaren Preisanstieg und verwaltet Trades mit optionalen Schutzstops.

## Indikatoren
- **TDI-2-Indikator** – ein benutzerdefinierter momentum-basierter Indikator, der in diesem Repository implementiert ist. Er erstellt zwei Linien:
  - *Direktionale Linie*: `Periode × GeglättetenMomentum`, wobei das Momentum dem angewendeten Preis minus dem Preis `Periode` Bars zuvor entspricht.
  - *Indexlinie*: `|Direktional| − (2 × Periode × Glätten(|Momentum|, 2×Periode) − |Momentum|)`.
- Der Indikator unterstützt folgende Glättungsmethoden: Einfacher, Exponentieller, Geglätteter (RMA) und Lineare Gewichteter gleitender Durchschnitt.
- Die unterstützten angewendeten Preisoptionen replizieren die ursprüngliche MQL-Implementierung, einschließlich der TrendFollow- und Demark-Formeln.

## Handelslogik
1. Bei jeder abgeschlossenen Kerze wertet die Strategie die TDI-2-Werte an der durch **Signal Bar** angegebenen Bar (Standard: vorherige geschlossene Kerze) und einer Bar früher aus.
2. Wenn die Direktionallinie über der Indexlinie war und dann darunter kreuzt:
   - Wenn **Allow Long Entries** aktiviert ist und keine Long-Position aktiv ist, bereitet die Strategie einen neuen Long-Einstieg vor.
   - Wenn eine Short-Position besteht und **Allow Short Exits** aktiviert ist, schließt sie die Short-Position.
3. Wenn die Direktionallinie unter der Indexlinie war und dann darüber kreuzt:
   - Wenn **Allow Short Entries** aktiviert ist und keine Short-Position aktiv ist, bereitet die Strategie einen neuen Short-Einstieg vor.
   - Wenn eine Long-Position besteht und **Allow Long Exits** aktiviert ist, schließt sie die Long-Position.
4. Wiedereinstiegslogik (Skalierung):
   - Beim Halten einer Long-Position verfolgt die Strategie den Ausführungspreis des letzten Long-Trades. Wenn sich der Markt um **Re-entry Step (points)** günstig bewegt und die Anzahl der ausgeführten Long-Trades noch unter **Max Entries** liegt, öffnet sie eine zusätzliche Long-Order mit dem Basisvolumen.
   - Die gleiche Logik gilt für Short-Positionen unter Verwendung des letzten Short-Ausführungspreises.
5. Beim Öffnen einer Position, während eine entgegengesetzte Position besteht, sendet die Strategie eine kombinierte Marktorder, die so dimensioniert ist, dass sie sowohl das entgegengesetzte Exposure schließt als auch die neue Position mit dem konfigurierten Basisvolumen aufbaut.
6. Optionale Stop-Loss- und Take-Profit-Level werden über `StartProtection` unter Verwendung des `PriceStep`-Multiplikators des Instruments aktiviert.

## Parameter
| Name | Beschreibung | Standardwerte |
| --- | --- | --- |
| Money Management | Basis-Ordervolumen. | 0.1 |
| Max Entries | Maximale Anzahl von Einstiegen pro Richtung (Ersthandel + Wiedereinstiege). | 10 |
| Stop Loss (points) | Stop-Loss-Distanz in Instrument-Punkten. | 1000 |
| Take Profit (points) | Take-Profit-Distanz in Instrument-Punkten. | 2000 |
| Slippage (points) | Zur Kompatibilität beibehalten; wird in der StockSharp-Implementierung nicht verwendet. | 10 |
| Re-entry Step (points) | Minimale günstige Bewegung, bevor in eine bestehende Position skaliert wird. | 300 |
| Allow Long Entries / Allow Short Entries | Long-/Short-Positionen öffnen aktivieren. | true |
| Allow Long Exits / Allow Short Exits | Long-/Short-Positionen schließen aktivieren. | true |
| Candle Type | Für Berechnungen verwendete Kerzenreihe. | H4-Kerzen |
| TDI Smoothing | Glättungsmethode für den TDI-2-Indikator. | Einfacher MA |
| TDI Period | Momentum-Lookback-Periode. | 20 |
| TDI Phase | Zur Kompatibilität mit dem MQL-Input reserviert (kein Effekt in unterstützten Glättungsmodi). | 15 |
| Applied Price | Von TDI-2 verwendete Preisquelle. | Close |
| Signal Bar | Anzahl der geschlossenen Kerzen, die bei der Auswertung von Kreuzungen zurückgeschaut werden. | 1 |

## Zusätzliche Hinweise
- Nur die von StockSharp-Indikatoren unterstützten Glättungsmethoden (SMA, EMA, SMMA, LWMA) sind implementiert. Andere MQL-Modi wie JJMA oder T3 sind nicht verfügbar.
- Der Parameter **TDI Phase** wird zur Vollständigkeit beibehalten. Er beeinflusst die unterstützten Glättungsmethoden nicht und kann auf seinem Standardwert belassen werden.
- Der Parameter **Slippage (points)** wird zur Parität mit dem ursprünglichen Expert Advisor bereitgestellt, wird aber nicht von der High-Level-API verwendet.
- Die Skalierungszähler werden automatisch zurückgesetzt, wenn die Nettoposition auf null zurückkehrt.
