# Aeron JJN Scalper EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein StockSharp High-Level-API-Port des Expert Advisors **Aeron JJN Scalper**. Sie beobachtet abgeschlossene Kerzen, identifiziert spezifische Zwei-Bar-Umkehrsituationen und platziert simulierte Stop-Orders an der Eröffnung der letzten entgegengesetzten Kerze. Wenn der Markt das gespeicherte Stop-Niveau erreicht, tritt die Strategie mit einer Marktorder ein, wendet ATR-basierte Risikovorgaben an und verwaltet den Trade mit einem pip-basierten Trailing-Stop.

Kernideen:

* Die Handelsrichtung wird durch ein bullisches/bärisches Zwei-Kerzen-Umkehrmuster entschieden.
* Einstiegsniveaus kommen vom Eröffnungspreis der letzten starken Kerze in der entgegengesetzten Richtung.
* Ein ATR(8)-Wert, gemessen auf der Signalkerze, setzt sowohl Stop-Loss- als auch Take-Profit-Abstände.
* Die Trailing-Stop-Logik bewegt den Schutzniveau, sobald der Preis um die konfigurierten Pip-Offsets vorgerückt ist.
* Ausstehende Niveaus laufen nach der konfigurierten Anzahl von Minuten automatisch ab.

## Handelsregeln
### Signalerkennung
1. Nur mit abgeschlossenen Kerzen des konfigurierten Zeitrahmens arbeiten (Standard: 1 Minute).
2. Pip-Größe aus dem Preisschritt des Instruments berechnen und für 3- oder 5-Dezimal-Preise mit 10 multiplizieren, um das MetaTrader-Pip-Verhalten zu imitieren.
3. Ein rollendes Fenster der letzten 120 Kerzen zur Suche nach Referenzbars pflegen.
4. Ein **Long-Setup** erkennen, wenn:
   * Die aktuelle Kerze über ihrer Eröffnung schließt (bullisch), und
   * Die vorherige Kerze bärisch ist mit einer Körpergröße größer als `DojiDiff1Pips`.
   * Rückwärts nach der letzten bärischen Kerze suchen, deren Körper `DojiDiff2Pips` überschreitet; ihr Eröffnungspreis wird zum Buy-Stop-Niveau.
5. Ein **Short-Setup** erkennen, wenn:
   * Die aktuelle Kerze unter ihrer Eröffnung schließt (bärisch), und
   * Die vorherige Kerze bullisch ist mit einer Körpergröße größer als `DojiDiff1Pips`.
   * Rückwärts nach der letzten bullischen Kerze suchen, deren Körper `DojiDiff2Pips` überschreitet; ihr Eröffnungspreis wird zum Sell-Stop-Niveau.
6. Neue Setups ignorieren, wenn es bereits ein austehendes Niveau in derselben Richtung gibt, oder wenn der ATR-Wert für die Kerze noch nicht verfügbar ist.

### Management ausstehender Niveaus
* Das gespeicherte Niveau wird als ausstehende Stop-Order behandelt. Es wird verworfen, wenn der Preis unterhalb (Long) oder oberhalb (Short) des Auslösers bleibt, bis die Ablaufzeit `ResetMinutes` verstrichen ist.
* Wenn der Preis das Niveau auf einer späteren Kerze berührt (Hoch ≥ Kaufniveau oder Tief ≤ Verkaufsniveau), sendet die Strategie eine Marktorder, die so dimensioniert ist, um eine bestehende Exposure umzukehren und `Volume` Kontrakte hinzuzufügen.
* Das Eintreten in eine Long-Position löscht jedes ausstehende Short-Niveau und umgekehrt.

### Stop-Loss, Take-Profit und Trailing
* Beim Einstieg zeichnet die Strategie den ATR(8)-Wert der Signalkerze auf.
  * Long-Trades: Stop-Loss = `entry - ATR`, Take-Profit = `entry + ATR`.
  * Short-Trades: Stop-Loss = `entry + ATR`, Take-Profit = `entry - ATR`.
* Bei jeder abgeschlossenen Kerze:
  * Prüft, ob der Preis den Stop-Loss oder Take-Profit erreicht hat, und schließt mit einer Marktorder, wenn berührt.
  * Wendet Trailing an, wenn der Preis mindestens `TrailingStopPips + TrailingStepPips` zugunsten der Position vorgerückt ist. Der neue Stop liegt `TrailingStopPips` hinter dem letzten Schlusskurs. Der Stop bewegt sich nie rückwärts.
* Wird die Position manuell geschlossen, setzt sich der interne Zustand automatisch zurück.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `Volume` | 0.1 | Nettopositionsgröße für Einstiege; die Strategie fügt die absolute aktuelle Position hinzu, um die Richtung umzukehren, wenn erforderlich. |
| `TrailingStopPips` | 5 | Basisabstand des Trailing-Stops (in Preiseinheiten umgerechnet). |
| `TrailingStepPips` | 5 | Zusätzlicher Vorschub, der erforderlich ist, bevor der Trailing-Stop wieder bewegt wird. |
| `ResetMinutes` | 10 | Ablaufzeit für ein gespeichertes austehendes Niveau (Minuten). |
| `DojiDiff1Pips` | 10 | Minimale Körpergröße (in Pips) für die Umkehrkerze, die dem Signal vorausgeht. |
| `DojiDiff2Pips` | 4 | Minimale Körpergröße (in Pips) für die Kerze, die als Eintrittsniveaureferenz verwendet wird. |
| `CandleType` | 1 Minute Zeitrahmen | Für Berechnungen verwendeter Kerzen-Datentyp. |

## Implementierungshinweise
* Die Strategie operiert rein auf abgeschlossenen Kerzen und verwendet speicherinterne Niveaus anstelle von echten Stop-Orders; wenn das Niveau durchbrochen wird, wird sofort eine Marktorder gesendet. Dies spiegelt das ursprüngliche EA-Verhalten innerhalb der StockSharp High-Level-API wider.
* ATR(8) wird mit `AverageTrueRange` berechnet und zwischengespeichert, damit die ursprünglichen Stop-/Ziel-Abstände für jeden Trade konstant bleiben.
* Die Pip-Konvertierung reproduziert die MetaTrader-Anpassung für 3- und 5-stellige Kursnotierungen. Fehlt dem Wertpapier `PriceStep`, wird ein Standardschritt von `1` verwendet.
* Bis zu 120 historische Kerzen werden gespeichert, um das ursprüngliche `CopyRates`-Look-back von 100 Bars mit etwas Sicherheitsmarge zu replizieren.
* Kein Python-Port ist für diese Strategie vorgesehen.

## Verwendung
1. Die Strategie an das gewünschte Wertpapier und Portfolio anhängen.
2. Den Kerzen-Zeitrahmen, Pip-Offsets und ATR-basierte Filter an das Instrument anpassen.
3. Die Strategie starten; sie wird Signale verfolgen, Marktorders senden, wenn Auslöseniveaus berührt werden, und Ausstiege automatisch verwalten.
