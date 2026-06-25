# Expert Ichimoku Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Expert Ichimoku-Strategie repliziert die Logik des ursprünglichen MQL5-Expert-Advisors "Expert Ichimoku" unter Verwendung der High-Level-API von StockSharp. Das System ist ein direktionales Trendfolgemodell, das mehrere Komponenten des Ichimoku Kinko Hyo-Indikators mit Preisaktion-Filtern und einem optionalen Martingal-artigen Positionierungsmodul kombiniert.

Die Strategie bewertet Signale auf abgeschlossenen Kerzen eines konfigurierbaren Zeitrahmens. Long- und Short-Trades schließen sich gegenseitig aus — die Strategie hält eine einzige Netto-Position und wechselt die Richtung erst nach Schließen des bestehenden Engagements. Alle Indikatorwerte werden auf der abonnierten Kerzenserie berechnet; externe Daten sind nicht erforderlich.

## Kernlogik

### Indikatorkonfiguration

* **Tenkan-sen (Konversionslinie):** Schneller gleitender Durchschnitt zur Kreuzungserkennung.
* **Kijun-sen (Basislinie):** Langsamer gleitender Durchschnitt als Kreuzungspartner.
* **Senkou Span A / Senkou Span B:** Wolkengrenzen, die am vorherigen Balken bewertet werden, um bullische oder bärische Marktstruktur zu bestätigen.
* **Chikou Span (Verzögerungslinie):** Momentum-Bestätigung über verzögerte Preis-Ausbruchsbedingungen.

Die Indikatorlängen sind vom Benutzer konfigurierbar und stimmen mit den Standardwerten des MQL5-Experten überein (9 / 26 / 52).

### Einstiegsregeln

Eine Long-Position wird eröffnet, wenn alle folgenden Bedingungen erfüllt sind:

1. **Momentum-Trigger:** Entweder
   * Tenkan-sen kreuzte am zuletzt geschlossenen Balken über Kijun-sen (Tenkan<sub>t-1</sub> ≤ Kijun<sub>t-1</sub> und Tenkan<sub>t</sub> > Kijun<sub>t</sub>), oder
   * Der Chikou Span brach über den historischen Preis (Chikou<sub>t-1</sub> ≤ Close<sub>t-11</sub> und Chikou<sub>t</sub> > Close<sub>t-10</sub>),
2. **Wolkenfilter:** Der aktuelle Schluss liegt über beiden Senkou-Spans des vorherigen Balkens (Preis vollständig über der Wolke),
3. **Preisaktionsfilter:** Die vorherige Kerze schloss bullisch (Close<sub>t-1</sub> > Open<sub>t-1</sub>),
4. **Positionsfilter:** Kein Long-Engagement ist derzeit aktiv. Wenn eine Short-Position besteht, wird sie zuerst geschlossen; das neue Long wird erst nach Schließen des Short eingereicht.

Eine Short-Position wird unter symmetrischen Bedingungen eröffnet:

1. **Momentum-Trigger:** Entweder
   * Tenkan-sen kreuzte unter Kijun-sen (Tenkan<sub>t-1</sub> ≥ Kijun<sub>t-1</sub> und Tenkan<sub>t</sub> < Kijun<sub>t</sub>), oder
   * Der Chikou Span brach unter den historischen Preis (Chikou<sub>t-1</sub> ≥ Open<sub>t-11</sub> und Chikou<sub>t</sub> < Open<sub>t-10</sub>),
2. **Wolkenfilter:** Der aktuelle Schluss liegt unter beiden Senkou-Spans des vorherigen Balkens,
3. **Preisaktionsfilter:** Die vorherige Kerze schloss bärisch (Close<sub>t-1</sub> < Open<sub>t-1</sub>),
4. **Positionsfilter:** Bestehendes Long-Engagement wird vor dem Eröffnen des Short geschlossen.

### Positionierung und Martingal-Option

* Die Basis-Ordergröße entspricht der `Volume`-Eigenschaft der Strategie.
* Wenn **Use Martingale** aktiviert ist, verdoppelt sich die nächste Einstiegsgröße, wenn der vorherige abgeschlossene Trade mit Verlust schloss. Profitable oder Gewinnschwellen-Trades setzen den Multiplikator zurück.
* Die resultierende Ordergröße ist durch `Volume × Max Position Multiplier` begrenzt, was dem Maximum-Positions-Schutz im ursprünglichen EA entspricht.

### Risikomanagement

* **Statischer Stop-Loss / Take-Profit:** Optionale absolute Preisabstände werden auf jede neue Position angewendet. Wenn der Schlusskurs den Stop oder das Ziel erreicht, wird die Position zum Markt geschlossen.
* **Trailing Stop:** Wenn sowohl `Trailing Stop Offset` als auch `Trailing Step` positiv sind, wird das Stop-Level nur gestrafft, nachdem der Preis über `offset + step` vom Einstieg hinausgeht, was die inkrementelle Trailing-Logik der MQL5-Version emuliert.
* Die Strategie handelt eine Netto-Position. Nach dem Ausstieg (via Stop, Ziel, Trailing oder Umkehrung) wird das realisierte PnL bewertet, um die Martingal-Flag für das nächste Signal zu aktualisieren.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| Tenkan Period | Länge der Tenkan-sen-Linie. | 9 |
| Kijun Period | Länge der Kijun-sen-Linie. | 26 |
| Senkou Span B Period | Länge der Senkou Span B-Linie. | 52 |
| Stop Loss Offset | Absoluter Abstand zwischen Einstiegspreis und Schutz-Stop. Auf 0 setzen zum Deaktivieren. | 0 |
| Take Profit Offset | Absoluter Abstand zwischen Einstiegspreis und Gewinnziel. Auf 0 setzen zum Deaktivieren. | 0 |
| Trailing Stop Offset | Basis-Trailing-Abstand nach Aktivierung. | 0 |
| Trailing Step | Zusätzliche Bewegung vor Straffung des Trailing Stops. | 0 |
| Max Position Multiplier | Obergrenze für die effektive Ordergröße (relativ zu `Volume`). | 5 |
| Use Martingale | Ob die nächste Trade-Größe nach einem Verlust-Trade verdoppelt werden soll. | true |
| Candle Type | Für Berechnungen verwendete Kerzenserie. | 1-Stunden-Zeitrahmen |

## Praktische Hinweise

* Die Strategie benötigt mindestens 12 abgeschlossene Kerzen, bevor alle Bedingungen bewertet werden können (Chikou-Vergleiche referenzieren Preise bis zu 11 Balken zurück).
* Da StockSharp-Strategien auf Netto-Positionen operieren, begrenzt der Parameter `Max Position Multiplier` die maximale Kontraktgröße anstatt mehrere unabhängige Tickets zu verwalten. Dies hält das Verhalten im Einklang mit dem Expositionslimit der MQL5-Implementierung.
* Die Trailing-Stop-Logik spiegelt den EA: Der Stop wird nur bewegt, wenn der Preis um `Trailing Stop Offset + Trailing Step` fortgeschritten ist. Einen der Parameter auf null zu setzen, deaktiviert Trailing-Anpassungen.
* Protokollierungsanweisungen berichten jeden Einstieg und Ausstieg, was die Überprüfung von Entscheidungspunkten bei der Wiedergabe von Marktdaten erleichtert.

## Verwendungsworkflow

1. Konfigurieren Sie den gewünschten Kerzentyp und das Instrument in einem `StrategyContainer` oder einer Designer-Vorlage.
2. Setzen Sie das Basis-`Volume` und passen Sie Risikoparameter entsprechend der Symbol-Volatilität an (z.B. konvertieren Sie Pip-basierte Abstände des ursprünglichen EA in Preiseinheiten für den ausgewählten Markt).
3. Starten Sie die Strategie. Sobald der Indikator ausreichend Historie hat, wird er Kreuzungen und Verzögerungslinie-Bestätigungen auf jedem abgeschlossenen Balken auswerten und Exits und Martingal-Sizing automatisch verwalten.
