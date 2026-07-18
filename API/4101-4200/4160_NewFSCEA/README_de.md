# Neue FSCEA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die neue FSCEA-Strategie ist ein auf MACD basierendes Trendfolgesystem, das vom ursprünglichen MetaTrader 4 Expert Advisor `new_fscea.mq4` portiert wurde. Die Strategie kombiniert eine klassische MACD-Crossover-Bestätigung mit einem EMA-Steigungsfilter, statischen Take-Profit-Zielen und einem Trailing-Stop zur Verwaltung offener Positionen. Es handelt jeweils ein einzelnes Symbol und eröffnet nur eine Position auf dem Markt.

## Handelslogik
### Langer Eintrag
- Die Hauptlinie von MACD liegt unter Null, kreuzt jedoch über der Signallinie der aktuell geschlossenen Kerze.
- Die vorherige Kerze hatte immer noch die MACD-Linie unterhalb der Signallinie (bestätigt den Crossover).
- Der absolute Wert der Zeile MACD überschreitet den Schwellenwert `OpenLevelPoints` (skaliert nach Preisschritt).
- Die verschobene EMA-Steigung ist positiv (`EMA_shifted_now > EMA_shifted_previous`).
- Derzeit ist keine Position offen.

### Kurzer Eintrag
- Die Hauptlinie von MACD liegt über Null, kreuzt jedoch unterhalb der Signallinie der aktuell geschlossenen Kerze.
- Die vorherige Kerze hatte immer noch die MACD-Linie über der Signallinie.
- Die Hauptlinie MACD überschreitet den Schwellenwert `OpenLevelPoints` (skaliert nach Preisschritt).
- Die verschobene EMA-Steigung ist negativ (`EMA_shifted_now < EMA_shifted_previous`).
- Derzeit ist keine Position offen.

### Langer Ausgang
- Wird ausgelöst, wenn MACD die Signallinie unterschreitet, aber über Null bleibt und der MACD-Wert den Schwellenwert `CloseLevelPoints` überschreitet.
- Oder wenn das Kerzenhoch das virtuelle Take-Profit-Niveau (`entry + TakeProfitPoints * priceStep`) berührt.
- Oder wenn das Kerzentief das Trailing-Stop-Niveau erreicht (wird dynamisch aktualisiert, wenn sich der Preis zu seinen Gunsten bewegt).

### Kurzer Ausgang
- Wird ausgelöst, wenn MACD die Signallinie überschreitet, dabei jedoch unter Null bleibt und der absolute MACD-Wert den Schwellenwert `CloseLevelPoints` überschreitet.
- Oder wenn das Kerzentief das virtuelle Take-Profit-Niveau (`entry - TakeProfitPoints * priceStep`) berührt.
- Oder wenn das Kerzenhoch das Trailing-Stop-Niveau erreicht (wird dynamisch aktualisiert, wenn sich der Preis zu seinen Gunsten bewegt).

## Risikomanagement
- Der Take-Profit wird in Instrumentenpunkten ausgedrückt und durch Multiplikation mit `Security.PriceStep` in einen Preis umgewandelt.
- Der Trailing-Stop funktioniert in Punkten und verschärft sich, sobald der variable Gewinn größer als die Trailing-Distanz ist.
- Es kann immer nur eine Position offen sein, was dem Verhalten des MT4-Expertenberaters entspricht.
- Der Positionsschutz wird durch den integrierten `StartProtection()`-Helfer aktiviert.

## Indikatoren
- **MACD (12, 26, 9)** – der wichtigste Crossover-Motor. Die Histogrammgröße liefert die Eintritts- und Austrittsschwellenwerte.
- **EMA (TrendPeriod)** – wird auf Schlusskurse angewendet. Der Steigungsvergleich verwendet eine konfigurierbare Verschiebung (`TrendShift`), um den MT4-Parameter `ma_shift` zu emulieren.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `TakeProfitPoints` | 300 | Abstand zum Gewinnziel in Punkten. Mithilfe des Symbols „Preisschritt“ in einen Preis umgerechnet. |
| `TrailingStopPoints` | 20 | Trailing-Stop-Größe in Punkten. Wird erst aktiviert, wenn sich der Handel um mehr als diesen Abstand zu seinen Gunsten bewegt. |
| `OpenLevelPoints` | 3 | Mindestens MACD Betrag (Punkte) erforderlich, bevor ein neuer Handel zulässig ist. |
| `CloseLevelPoints` | 2 | MACD Betrag (Punkte), der erforderlich ist, um einen Trade über einen MACD Crossover abzuschließen. |
| `TrendPeriod` | 10 | Länge des Trendfilters EMA. |
| `TrendShift` | 2 | Horizontale Verschiebung (in Balken), die bei der Auswertung seiner Steigung auf EMA angewendet wird. Höhere Werte verzögern die Trendbestätigung. |
| `TradeVolume` | 0,1 | Standard-Auftragsvolumen, das mit Marktaufträgen gesendet wird. |
| `CandleType` | 1-stündiger Zeitrahmen | Kerzentyp, der für Indikatorberechnungen verwendet wird; kann an den gewünschten Zeitrahmen angepasst werden. |

## Implementierungshinweise
- Die Strategie verarbeitet nur fertige Kerzen, um die Logik nahe an der MT4-Version zu halten.
- Die EMA-Verschiebung wird durch die Pufferung der Indikatorausgaben und den Vergleich von Werten im Abstand von `TrendShift` Balken emuliert.
- Trailing-Stop und Take-Profit werden virtuell implementiert (keine tatsächlichen Stop-/Limit-Orders), um die hohen API-Anforderungen einzuhalten.
- Der Code basiert ausschließlich auf dem High-Level-Kerzenabonnement API (`SubscribeCandles().BindEx(...)`), um den Repository-Richtlinien zu entsprechen.
