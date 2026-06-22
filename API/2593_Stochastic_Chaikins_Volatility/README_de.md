# Stochastic Chaikin's Volatilität Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein StockSharp-Port des MetaTrader-Expertenberaters `Exp_Stochastic_Chaikins_Volatility`. Sie analysiert die Spanne zwischen Hoch- und Tiefpreisen, glättet diese Volatilität mit einem konfigurierbaren gleitenden Durchschnitt und normalisiert das Ergebnis dann mit einem Stochastic-ähnlichen Oszillator. Die Handelsentscheidungen folgen der ursprünglichen Gegentrend-Logik: Die Strategie sucht nach Wendepunkten im Oszillator, um kurzfristige Extreme zu handeln, während bestehende Positionen optional geschlossen werden, wenn der Schwung sich umkehrt.

## Indikatoraufbau
1. **Chaikin-Volatilität** – die Differenz zwischen Kerzenhoch und -tief wird mit dem *primären* gleitenden Durchschnitt geglättet. Unterstützte Glättungsmethoden sind:
   - Einfach (SMA)
   - Exponentiell (EMA)
   - Geglättet/Wilder (SMMA)
   - Linear gewichtet (LWMA)
   - Jurik (JMA-Approximation)
2. **Stochastische Normalisierung** – die letzten `Stochastic Length` geglätteten Werte definieren den höchsten und niedrigsten Bereich. Der aktuelle geglättete Wert wird mit diesem Fenster in einen Bereich von 0–100 normalisiert.
3. **Sekundäre Glättung** – ein zweiter gleitender Durchschnitt (Methode aus derselben Liste wählbar) wird auf den normalisierten Wert angewendet, um die Haupt-Oszillatorlinie zu erhalten. Intern ist die Signallinie einfach der Oszillatorwert der vorherigen abgeschlossenen Kerze, was das MQL-Indikator-Buffer-Verhalten repliziert.

## Handelsstrategie
- **Einstieg**
  - *Kaufen*: wenn der Hauptoszillator ein niedrigeres Hoch gebildet hat (vorheriger Wert größer als sein eigener Vorgängerwert, aktueller Wert kreuzt unter diesen vorherigen Wert). Dies spiegelt den ursprünglichen konträren Long-Auslöser des EA wider.
  - *Verkaufen*: wenn der Oszillator ein höheres Tief gebildet hat (vorheriger Wert kleiner als sein eigener Vorgängerwert, aktueller Wert kreuzt über diesen vorherigen Wert).
- **Ausstieg**
  - Long-Positionen schließen, wenn der vorherige Oszillatorwert unter seinen älteren Wert fällt (Abwärtsschwung taucht wieder auf).
  - Short-Positionen schließen, wenn der vorherige Oszillatorwert über seinen älteren Wert steigt.
- Die Signalauswertung verwendet den Parameter `Signal Shift`, um abgeschlossene Kerzen zu inspizieren. Die Standardwerte emulieren die MQL-Einstellung von 1 Bar.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `Candle Type` | Zeitrahmen für alle Berechnungen (Standard: 4-Stunden-Zeitkerzen). |
| `Primary Method` / `Primary Length` | Gleitender Durchschnittstyp und -länge zur Glättung der Hoch-Tief-Spanne. |
| `Secondary Method` / `Secondary Length` | Gleitender Durchschnittstyp und -länge zur Glättung des normalisierten Oszillators. |
| `Stochastic Length` | Rückblickfenster für den höchsten/niedrigsten Bereich im Normalisierungsschritt. |
| `Signal Shift` | Anzahl der abgeschlossenen Kerzen zwischen der aktuellen Bar und der für die Signalauswertung verwendeten Bar. Muss ≥1 bleiben. |
| `Allow Long/Short Entry` | Long- oder Short-Trades öffnen oder deaktivieren. |
| `Allow Long/Short Exit` | Positionsschließung bei Oszillatorumkehr aktivieren oder deaktivieren. |
| `High/Middle/Low Level` | Visuelle Referenzlevel aus dem ursprünglichen Indikator (kein direkter Handelseffekt). |

## Verwendungshinweise
- Der StockSharp-Port behält das ursprüngliche Gegentrend-Verhalten bei, verwendet aber StockSharp-gleitende Durchschnitte. Exotische Methoden aus der MQL-Bibliothek (ParMA, VIDYA, AMA usw.) werden zur nächsten verfügbaren Glättungsoption zugeordnet; wählen Sie Jurik für eine genauere Annäherung, wenn nötig.
- Die Positionsgröße folgt der `Volume`-Eigenschaft der Basisstrategie. Stop-Loss- und Take-Profit-Management aus der MQL-Hilfsbibliothek wird nicht repliziert; Exits stützen sich auf Oszillatorumkehrungen oder externes Risikomanagement wie `StartProtection`.
- Signale werden nur bei abgeschlossenen Kerzen berechnet. Stellen Sie sicher, dass der Datenfeed den ausgewählten `Candle Type` mit ausreichend Geschichte bereitstellt, damit sich beide Glättungsstufen und das Stochastic-Fenster aufwärmen können.
