# SV Tages-Ausbruch Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **SV Tages-Ausbruch Strategie** ist eine direkte C#-Konvertierung des MetaTrader 5 Expert Advisors "SV v.4.2.5". Das System bewertet die Kursbewegung einmal pro abgeschlossener Bar und erlaubt maximal einen Trade pro Börsentag. Der Handel beginnt erst nach der konfigurierten Startzeit und basiert auf der Beziehung zwischen dem jüngsten Hoch/Tief-Bereich und zwei geglätteten gleitenden Durchschnitten. Eine Long-Position wird eröffnet, wenn der gesamte analysierte Bereich unter beiden Durchschnitten bleibt, was eine erwartete Erholung aus überverkauften Bedingungen signalisiert. Umgekehrt wird eine Short-Position eröffnet, wenn der Bereich über beiden Durchschnitten bleibt, was eine mögliche Umkehr aus überkauftem Gebiet signalisiert.

## Handelsregeln
### Einstiegsbedingungen
- **Tagesfilter** – Es werden keine Trades bewertet, bis die aktuelle Serverzeit später als *Start Hour*/*Start Minute* ist. Pro Tag ist nur ein Einstieg erlaubt.
- **Datenfenster** – Die Strategie überspringt die neuesten `Shift` Bars und analysiert die nächsten `Interval` Bars. Ihre höchsten und niedrigsten Preise werden mit den verschobenen gleitenden Durchschnitten verglichen.
- **Long-Einstieg** – Wenn der höchste Preis im analysierten Fenster strikt unter dem langsamen MA liegt **und** der niedrigste Preis strikt unter dem schnellen MA liegt, Long einsteigen (zuerst jede bestehende Short-Position schließen).
- **Short-Einstieg** – Wenn der niedrigste Preis im analysierten Fenster strikt über dem langsamen MA liegt **und** der höchste Preis strikt über dem schnellen MA liegt, Short einsteigen (zuerst jede bestehende Long-Position schließen).

### Exit-Management
- **Anfänglicher Stop-Loss** – wird `Stop Loss (pips)` vom Einstiegspreis entfernt platziert. Wenn das Niveau erreicht wird, wird die Position geschlossen.
- **Take-Profit** – wird `Take Profit (pips)` vom Einstiegspreis entfernt platziert. Wenn das Niveau erreicht wird, wird die Position geschlossen.
- **Trailing-Stop** – wenn aktiviert (sowohl Trailing-Abstand als auch -Schritt sind größer als null), bewegt sich der Stop in die Gewinnrichtung. Bei Longs wird der Stop auf `Schluss − Trailing Stop` angehoben, sobald der Preis mehr als `Trailing Stop + Trailing Step` vorgerückt ist; Shorts spiegeln die Logik.
- **Tagessperre** – Unabhängig davon, wie ein Trade ausläuft, wird die Strategie bis zum nächsten Handelstag keine neue Position öffnen.

### Positionsgrößenbestimmung
- **Manueller Modus** – wenn *Use Manual Volume* `true` ist, sendet die Strategie den festen *Volume*-Wert (angepasst an den Volumenschritt des Instruments).
- **Risikobasierter Modus** – wenn *Use Manual Volume* `false` ist, schätzt die Strategie die Handelsgröße aus dem Kontokapital und `Risk %`. Es teilt das Risikokapital durch den Geldwert des konfigurierten Stop-Abstands unter Verwendung von Instrument-Schrittinformationen, wenn verfügbar.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| Use Manual Volume | `false` | Den festen `Volume`-Wert anstelle der risikobasierten Größenbestimmung verwenden. |
| Volume | `0.1` | Handelsvolumen, wenn manuelle Größenbestimmung aktiviert ist. |
| Risk % | `5` | Prozentsatz des Kontokapitals, der pro Trade riskiert wird, wenn manuelle Größenbestimmung aktiv ist. |
| Stop Loss (pips) | `50` | Stop-Loss-Abstand in Pips. Auf `0` setzen zum Deaktivieren. |
| Take Profit (pips) | `50` | Take-Profit-Abstand in Pips. Auf `0` setzen zum Deaktivieren. |
| Trailing Stop (pips) | `5` | Trailing-Stop-Abstand in Pips. Erfordert, dass `Trailing Step` größer als null ist. |
| Trailing Step (pips) | `5` | Minimaler Gewinnzuwachs, bevor der Trailing-Stop bewegt wird. |
| Start Hour | `19` | Stunde (Börsenzeit), wenn Einstiege beginnen dürfen. |
| Start Minute | `0` | Minute (Börsenzeit), wenn Einstiege beginnen dürfen. |
| Shift | `6` | Anzahl der neuesten Bars, die vor der Analyse des Bereichs ausgeschlossen werden. |
| Interval | `27` | Anzahl der historischen Bars, die zur Berechnung des Hoch/Tief-Fensters verwendet werden. |
| Fast MA Period | `14` | Länge des schnellen gleitenden Durchschnitts. |
| Fast MA Shift | `0` | Horizontale Verschiebung (Bars zurück) für den Wert des schnellen MA. |
| Fast MA Method | `Smma` | Gleitender Durchschnitt-Methode für den schnellen MA. |
| Fast Applied Price | `Median` | Preisquelle für den schnellen MA. |
| Slow MA Period | `41` | Länge des langsamen gleitenden Durchschnitts. |
| Slow MA Shift | `0` | Horizontale Verschiebung (Bars zurück) für den Wert des langsamen MA. |
| Slow MA Method | `Smma` | Gleitender Durchschnitt-Methode für den langsamen MA. |
| Slow Applied Price | `Median` | Preisquelle für den langsamen MA. |
| Candle Type | `1 hour` | Für Berechnungen verwendete Kerzenserie. |

## Zusätzliche Hinweise
- Die Konvertierung behält das ursprüngliche Verhalten bei, ein verzögertes Preis-Fenster (`Shift` + `Interval`) zu analysieren, um die aktuellsten Bars bei der Bestimmung von Ausbrüchen zu vermeiden.
- Die Trailing-Logik verwendet den Kerzenschlusskurs, um MetaTrader's tick-basierte Trailing-Updates zu approximieren. Passen Sie die Pip-Abstände an, wenn Ihr Instrument eine andere Präzision erfordert.
- Die risikobasierte Größenbestimmung basiert auf `Security.PriceStep`, `Security.StepPrice` und `Security.VolumeStep`. Geben Sie diese Werte in Ihren Instrumenteinstellungen an für eine genaue Lot-Größenbestimmung.
- Die Strategie ruft `StartProtection()` auf, sodass Sie bei Bedarf zusätzliche globale Risikoregeln anhängen können.
- Um den ursprünglichen EA zu spiegeln, stellen Sie sicher, dass Ihr Datenfeed und Ihr Trading-Konto in derselben Serverzeitzone betrieben werden, auf die die Parameter *Start Hour* und *Start Minute* verweisen.
