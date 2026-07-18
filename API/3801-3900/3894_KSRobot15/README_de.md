# KSRobot 1.5 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **KSRobot 1.5-Strategie** ist eine C#-Konvertierung des MetaTrader 4 Expert Advisors `KSRobot_1_5_h1_v1.mq4`. Die StockSharp-Version behält die ursprüngliche Idee des Handels mit Kijun-sen-Preisbrüchen bei, die durch einen linear gewichteten gleitenden Durchschnitt (LWMA) über 20 Perioden bestätigt werden, und erzwingt gleichzeitig ein striktes Handelsfenster und mehrschichtige Risikokontrollen. Alle Berechnungen werden standardmäßig für 30-Minuten-Kerzen durchgeführt, der Zeitrahmen kann jedoch über einen Parameter geändert werden.

## Marktdaten und Indikatoren
- **Ichimoku** Indikator mit Tenkan/Kijun/Senkou Span B-Perioden standardmäßig 6/12/24.
- **Linearer gewichteter gleitender Durchschnitt (LWMA)** mit einer Länge von 20 zur Messung der Steigung und des Mindestabstandsfilters.
- **Zeitgesteuerte Kerzen**, definiert durch `CandleType` (standardmäßig M30) für die Signalgenerierung.

## Handelslogik
### Langer Arbeitsablauf
1. Eine Kerze muss von unten mit der Kijun-Linie interagieren. Eine der folgenden Bedingungen ist ausreichend: Die Kerze öffnet unten und schließt darüber, der vorherige Schlusskurs lag darunter, während der neue Schlusskurs darüber liegt, oder das Tief der Kerze durchbricht das Niveau.
2. Der neueste Kijun-Wert ist flach oder höher als zwei Balken zurück, was Trades gegen eine sofortige Abwärtsbewegung der Grundlinie verhindert.
3. Der LWMA liegt mindestens `MaFilterPips` (umgerechnet in Preiseinheiten) unter Kijun. Dies entspricht der Anforderung, dass der gleitende Durchschnitt einige Pips unter der Grundlinie liegen muss.
4. Die LWMA-Steigung ist positiv (aktueller LWMA größer als der vorherige Balken).
5. Das Setup wird lange als ausstehend gespeichert, bis die Steigungsbedingung erfüllt ist. Es kann jeweils nur eine Seite ausstehend sein, was die `longcross`/`shortcross`-Flags von MQL nachahmt.
6. Wenn alle Kriterien übereinstimmen und kein Netto-Long-Engagement besteht, wird eine Market-Buy-Order übermittelt. Der von der Strategie zwischengespeicherte Einstiegspreis wird zur Grundlage für das Stop-, Break-Even- und Trailing-Management.

### Kurzer Arbeitsablauf
Es gelten Spiegelbedingungen:
1. Die Kerze interagiert mit Kijun von oben (eröffnet oben und schließt unten, vorheriger Schluss darüber und aktueller Schluss darunter oder das Hoch berührt das Niveau).
2. Kijun ist flach oder tiefer als zwei Balken zurück.
3. Die LWMA liegt `MaFilterPips` über Kijun.
4. Die LWMA-Steigung ist im Vergleich zum vorherigen Balken negativ.
5. Es wird nur ein ausstehender Short verfolgt und gelöscht, sobald ein Long-Signal erscheint, genau wie beim ursprünglichen Experten.
6. Wenn Sie zufrieden sind und das Konto nicht bereits leer ist, wird ein Marktverkaufsauftrag gesendet.

## Ausgangsregeln und Risikokontrolle
- **Zeitfenster** – neue Trades werden nur berücksichtigt, während die Kerzeneröffnungszeit innerhalb von `[TradingStartHour, TradingEndHour)` liegt, standardmäßig 07:00–19:00 Uhr Börsenzeit.
- **Anfänglicher Stop-Loss** – Setzen Sie `StopLossPips` unter/über dem Einstiegspreis (umgerechnet anhand der Pip-Größe des Instruments). Bei Null wird kein anfänglicher Stopp verfolgt.
- **Break-even-Bewegung** – sobald der nicht realisierte Gewinn `BreakEvenPips` übersteigt, wird der Stop auf den Einstiegspreis plus einen Pip für Long-Positionen (minus eins für Short-Positionen) verschoben. Dieses Verhalten wird von `_breakEvenStep` gesteuert, um die MT4-Logik „Move to BE+1“ zu emulieren.
- **Trailing Stop** – sobald der Preis um `TrailingStopPips` ansteigt, läuft der Stop um diesen Abstand nur noch in die günstige Richtung.
- **Take-Profit** – optionale feste Zielentfernung, definiert durch `TakeProfitPips`. Zum Deaktivieren auf Null setzen.
- **Slope Exit** – wenn sich der LWMA gegen den Handel dreht, bevor der Stop den Einstieg gekreuzt hat, wird die Position sofort geschlossen. Dadurch wird der Exit „MA ist falsch gedreht“ aus dem Skript MQL erfasst.
- **Priorität** – Wenn sowohl Stop-Loss als auch Take-Profit innerhalb derselben Kerze berührt würden, hat der Stop-Loss Vorrang, um bei Kerzendaten konservativ zu bleiben.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `TenkanPeriod` | 6 | Tenkan-sen-Länge des Ichimoku-Indikators. |
| `KijunPeriod` | 12 | Kijun-sen-Länge (Hauptauslöser). |
| `SenkouSpanBPeriod` | 24 | Senkou Span B-Länge. |
| `LwmaPeriod` | 20 | Zeitraum der Bestätigung LWMA. |
| `MaFilterPips` | 6 | Mindestpunktabstand zwischen LWMA und Kijun. |
| `StopLossPips` | 50 | Anfänglicher Schutzstoppabstand. |
| `BreakEvenPips` | 9 | Erforderlicher Gewinn, bevor der Stop auf die Gewinnschwelle verschoben wird. |
| `TrailingStopPips` | 10 | Trailing-Stop-Distanz, nachdem der Preis in die Gewinnzone gelangt. |
| `TakeProfitPips` | 120 | Optionale feste Take-Profit-Distanz. |
| `TradingStartHour` | 7 | Inklusive Stunde, um mit der Bearbeitung neuer Trades zu beginnen. |
| `TradingEndHour` | 19 | Exklusive Stunde zum Stoppen neuer Einträge. |
| `CandleType` | 30-minütiger Zeitrahmen | Datentyp, der für das Kerzenabonnement verwendet wird. |

Alle Pip-basierten Parameter werden mit `Security.PriceStep` (oder `MinPriceStep`) in Preiseinheiten umgewandelt. Instrumente, die mit drei oder fünf Dezimalstellen notiert werden, erhalten einen automatischen x10-Multiplikator, um die Standard-FX-Pip-Größe wiederherzustellen.

## Hinweise zur Implementierung
- Die Strategie bindet sowohl Ichimoku- als auch LWMA-Indikatoren über `SubscribeCandles().BindEx(...)` und stellt so sicher, dass Werte ohne manuelle Erfassung direkt aus der Indikatorpipeline stammen.
- Das Positionsmanagement spiegelt den MT4-Experten wider: Ausstehende Level ersetzen die Flags `longcross`/`shortcross` und werden gelöscht, sobald ein Trade ausgelöst wird.
- Schutzebenen werden nach der Eingabe zwischengespeichert, sodass Break-Even- und Trailing-Entscheidungen auch ohne individuelle Auftragsaktualisierungen mit Daten auf Kerzenebene funktionieren.
- `StartProtection` wird mit Nullabständen aufgerufen, da alle Schutzmaßnahmen innerhalb des Strategiecodes gehandhabt werden, was der maßgeschneiderten MT4-Logik entspricht.
- Es werden ausschließlich Marktaufträge verwendet. Die ursprüngliche Auswahl zwischen Limit und Markt basierte auf Geld-/Brief-Ticks, die bei kerzenbasierten Backtests nicht verfügbar sind.

## Nutzung
1. Erstellen Sie die Strategieinstanz, weisen Sie `Security`, `Portfolio`, `Volume` zu und starten Sie sie in der Umgebung StockSharp.
2. Passen Sie optional Pip-basierte Parameter für das spezifische Instrument an. Optimierte Voreinstellungen aus den MQL-Kommentaren (GBPUSD, EURUSD) können reproduziert werden, indem die Standardeinstellungen vor der Ausführung geändert werden.
3. Behalten Sie die Protokollausgabe im Auge: Einträge, Break-Even-Bewegungen, nachlaufende Anpassungen und Notausgänge werden über `LogInfo`-Aufrufe gemeldet.
4. Hängen Sie den generierten Diagrammbereich (Kerzen, Ichimoku, LWMA, eigene Trades) im Designer oder Backtester an, um den Handelsfluss zu visualisieren.

Es wird nur die C#-Version bereitgestellt. Gemäß den Anforderungen wird kein Python-Ordner erstellt.
