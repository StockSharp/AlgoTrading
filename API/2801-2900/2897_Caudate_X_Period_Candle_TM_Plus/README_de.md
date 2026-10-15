# Caudate X Periode Kerzen TM Plus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Strategie repliziert die Logik des Expertenberaters Caudate X Period Candle TM Plus. Sie glättet die Eröffnungs-, Hoch-, Tief- und Schlusskurse der Kerze mit einem konfigurierbaren gleitenden Durchschnitt, erstellt einen Donchian-ähnlichen Bereich und klassifiziert jede abgeschlossene Kerze in einen von sechs Farbcodes, abhängig von der Position des Körpers innerhalb des Bereichs. Long-Einstiege werden durch die bullischen Unterhalt-Farben (0 oder 1) ausgelöst, während Short-Einstiege durch die bärischen Oberhalt-Farben (5 oder 6) ausgelöst werden. Entgegengesetzte Farbgruppen werden verwendet, um bestehende Positionen zu beenden.

## Handelsregeln
1. Die ausgewählte Kerzenserie abonnieren und jede Komponente mit dem gewählten gleitenden Durchschnitt glätten.
2. Das höchste Hoch und das niedrigste Tief der geglätteten Hochs und Tiefs über den angegebenen `Donchian Period` berechnen, dann den Bereich erweitern, damit er immer die geglättete Eröffnung und den Schluss enthält.
3. Die Kerzenfarbe bestimmen:
   * Farben **0/1** – Körper nahe der Oberseite des Bereichs (unterer Schatten).
   * Farben **2/4** – Körper zentriert innerhalb des Bereichs.
   * Farben **5/6** – Körper nahe der Unterseite des Bereichs (oberer Schatten).
4. Die Farbe des durch `Signal Bar` versetzten Balkens auswerten (Standard `1` verwendet die vorherige abgeschlossene Kerze).
5. Positionen öffnen, wenn die Farbe zur Einstiegsgruppe gehört und die entgegengesetzte Position nicht aktiv ist.
6. Positionen schließen, wenn die Farbe zur Ausstiegsgruppe gehört oder die maximale Haltezeit abläuft.
7. Optionale Stop-Loss- und Take-Profit-Versätze werden über das integrierte Schutzmodul eingestellt.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `Candle Type` | Zeitrahmen für Signalberechnungen. |
| `Donchian Period` | Anzahl der Kerzen für den geglätteten Hoch-/Tief-Bereich. |
| `Signal Bar` | Anzahl der Balken zur Verzögerung der Signalauswertung (0 = aktueller Balken). |
| `Smoothing Method` | Gleitender Durchschnitt auf OHLC-Kurse angewendet (SMA, EMA, SMMA, LWMA, Jurik JJMA-Annäherung, Kaufman AMA). |
| `MA Length` | Länge des Glättungsfilters. |
| `MA Phase` | Reserviert für JJMA-Kompatibilität (nicht von StockSharp-Durchschnitten verwendet). |
| `Enable Long/Short Entries` | Öffnen neuer Long- oder Short-Positionen umschalten. |
| `Enable Long/Short Exits` | Schließen bestehender Long- oder Short-Positionen bei Signalen umschalten. |
| `Enable Time Exit` | Maximalen Haltezeitfilter aktivieren. |
| `Time Exit (minutes)` | Haltedauer vor einem erzwungenen Ausstieg. |
| `Stop Loss (points)` | Stop-Loss-Abstand in Kursschritten (multipliziert mit `Security.PriceStep`). |
| `Take Profit (points)` | Take-Profit-Abstand in Kursschritten. |

## Hinweise
- `Signal Bar = 1` entspricht dem MQL5-Expertenverhalten, indem auf der letzten vollständig geschlossenen Kerze agiert wird.
- Wenn Stop- oder Zielabstände größer als null sind, ruft die Strategie `StartProtection` mit absoluten Versätzen basierend auf dem Instrumentenpreisschritt auf.
- `MA Phase` wird für Kompatibilität beibehalten, wird aber nicht von den StockSharp-gleitenden Durchschnittsimplementierungen verbraucht.
- Die Basis-Ordergröße durch die geerbte `Strategy.Volume`-Eigenschaft einstellen; die Implementierung schließt immer entgegengesetzte Positionen, bevor eine neue eröffnet wird.
