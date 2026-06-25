# Triple-MA-Kanal-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Triple-MA-Kanal-Crossover-Strategie** handelt direktionale Ausbrüche, wenn ein schneller gleitender Durchschnitt sowohl
einen mittleren als auch einen langsamen gleitenden Durchschnitt durchkreuzt. Ein Donchian-ähnlicher Preiskanal wird zur
Verwaltung von Ausstiegen und zur Bereitstellung optionaler automatischer Stop-Loss- und Take-Profit-Niveaus verwendet. Die
Konvertierung basiert auf dem originalen MetaTrader "3MACross EA" und behält seine konfigurierbare gleitende
Durchschnittsstruktur, Risikokontrollen und Trailing-Logik bei.

Die Strategie skaliert bis zu einer konfigurierbaren Anzahl von Positionen, unterstützt manuelle pip-basierte Risikovorgaben und
kann dem Kanal für adaptive Ausstiege folgen. Wenn aktiviert, schiebt der Break-Even-Auslöser den Stop-Loss auf den Einstiegspreis
plus einen Sicherheitspuffer.

## Handelslogik
- **Einstiegsbedingungen**
  - *Long:* der schnelle gleitende Durchschnitt kreuzt über beide, den mittleren und langsamen Durchschnitt. Wenn `Trade On
    Close` aktiviert ist, muss der Kreuzer auf einer vollständig geschlossenen Kerze auftreten; andernfalls ist das Long-Signal
    erlaubt, während der schnelle Durchschnitt über beiden langsameren Durchschnitten bleibt.
  - *Short:* der schnelle gleitende Durchschnitt kreuzt unter die mittleren und langsamen Durchschnitte mit derselben
    Bestätigungslogik.
  - Bestehende Positionen auf der entgegengesetzten Seite werden sofort geschlossen und umgekehrt. Das Skalieren in dieselbe
    Richtung ist erlaubt, bis `Max Positions` erreicht ist.
- **Ausstiegsbedingungen**
  - Preis erreicht den konfigurierten Take-Profit oder das kanalbasierte Ziel.
  - Preis berührt das dynamische Stop-Niveau (manuelle Distanz, Trailing Stop, Break-Even-Bewegung oder kanalbasierter Stop).
  - Optionaler Trailing Stop passt sich an, nachdem der Preis mindestens die Trailing-Schritt-Distanz zu Gunsten vorgerückt ist.

## Risikomanagement
- Stops und Ziele können manuell in Pips definiert oder aus dem Preiskanal abgeleitet werden, wenn `Auto SL/TP` aktiviert ist.
- Trailing-Stop- und Break-Even-Logik spiegeln den ursprünglichen Experten-Berater wider. Der Stop bewegt sich nur in der
  günstigen Richtung und wird niemals gelockert.
- Der Donchian-Kanal liefert natürliche Unterstützungs-/Widerstandsgrenzen, die für automatische Stop-Loss- und
  Take-Profit-Platzierung verwendet werden können.
- `Max Positions` begrenzt die Anzahl der Skalierungsschritte und verhindert unkontrolliertes Pyramidisieren.

## Wichtige Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| `Volume` | Ordergröße für jeden Skalierungsschritt. |
| `Stop Loss (pips)` | Feste Distanz für den Schutzstop. Auf `0` setzen, um zu deaktivieren. |
| `Take Profit (pips)` | Feste Distanz für das Gewinnziel. Auf `0` setzen, um zu deaktivieren. |
| `Trailing Stop (pips)` | Distanz, die vom Trailing Stop verwendet wird. `0` deaktiviert Trailing. |
| `Trailing Step (pips)` | Minimaler Vorschub vor dem Aktualisieren des Trailing Stops. |
| `Break Even (pips)` | Gewinn, der vor dem Einrasten eines Break-Even-Stops erforderlich ist. |
| `Auto SL/TP` | Den Donchian-Kanal anstelle fester Distanzen für Stop-Loss- und Take-Profit-Platzierung verwenden. |
| `Trade On Close` | Crossover-Bestätigung auf einer geschlossenen Kerze erfordern. Wenn deaktiviert, wird die Ausrichtung der Durchschnitte jeden Balken geprüft. |
| `Max Positions` | Maximale Anzahl von Skalierungsschritten pro Richtung. |
| `Fast/Middle/Slow MA Period` | Länge der gleitenden Durchschnitte. |
| `Fast/Middle/Slow MA Shift` | Optionale Verschiebung (in Balken) für jeden gleitenden Durchschnitt. |
| `Fast/Middle/Slow MA Type` | Berechnungsmodus des gleitenden Durchschnitts (Einfach, Exponentiell, Geglättet, Gewichtet). |
| `Channel Period` | Lookback für das Hoch/Tief des Donchian-Kanals. |
| `Candle Type` | Zeitrahmen der von der Strategie verarbeiteten Kerzen. |

## Implementierungshinweise
- Pip-Distanzen werden mit `Security.PriceStep` konvertiert. Für Instrumente ohne gültige Tick-Größe fällt die Strategie auf
  eine Distanz von `1` Preiseinheit pro Pip zurück.
- Automatisches Kanalmanagement hält Stop-Loss- und Take-Profit-Niveaus nur näher am aktuellen Preis bewegend; sie werden nie
  erweitert.
- Break-Even-Aktivierung verwendet den Trailing-Schritt als zusätzlichen Puffer und entspricht dem ursprünglichen EA-Verhalten.
- Die Strategie ist für die Verwendung mit StockSharp High-Level-APIs ausgelegt und handhabt Chart-Rendering (MAs und
  Donchian-Kanal) für visuelle Analyse.
- Stellen Sie sicher, dass die historische Datentiefe für den langsamen gleitenden Durchschnitt und den Kanalzeitraum
  ausreichend ist, damit Crossover-Signale gültig sind.

## Verwendung
1. Die Strategie an ein Wertpapier anhängen und den gewünschten Kerzen-Zeitrahmen festlegen.
2. Gleitende Durchschnittsperioden/-methoden konfigurieren, um dem ursprünglichen EA oder Ihrer Anpassung zu entsprechen.
3. Zwischen manuellen pip-basierten Risikoeinstellungen wählen oder automatische Kanalausstiege aktivieren.
4. Die Strategie starten; sie abonniert die konfigurierten Kerzen, berechnet Indikatoren und handelt, wenn die
   Crossover-Bedingungen erfüllt sind.
5. Trailing Stop und Break-Even-Anpassungen über die Logs und Chart-Überlagerungen überwachen.

> **Haftungsausschluss:** Automatisiertes Trading birgt erhebliche Risiken. Testen Sie die Strategie gründlich mit historischen
> Daten und in einer Simulationsumgebung, bevor Sie sie auf Live-Märkten einsetzen.
