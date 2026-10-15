# Strategie für neuronale Netzwerkvorlagen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert das Verhalten der Expert Advisor-Vorlage MQL5, die RSI- und MACD-Funktionen in ein neuronales Netzwerk einspeist. Da StockSharp nicht mit dem benutzerdefinierten Netzwerklader des Originalprojekts ausgeliefert wird, ersetzt die Strategie das Black-Box-Netzwerk durch ein deterministisches Bewertungsmodell und behält dabei die gleiche Marktstruktur und Risikokontrolle bei. Das Ziel besteht darin, die Dynamik zu erfassen, wenn sich sowohl RSI als auch MACD über die Richtung einig sind und die prognostizierte Bewegung groß genug ist, um einen Handel zu rechtfertigen.

## Indikatoren und Daten
- **Relative Strength Index (RSI, 12 Perioden)** wird bei Kerzenschluss berechnet und spiegelt die ursprüngliche typische Preiseingabe wider.
- **Moving Average Convergence Divergence (MACD 12/48/12)** wird als Momentum-Histogramm und Konfidenz-Proxy verwendet.
- **Zeitrahmen** konfigurierbar; Der Standardwert sind 5-Minuten-Kerzen, um mit dem Quellenexperten übereinzustimmen.

## Handelslogik
1. Bei jeder fertigen Kerze aktualisiert die Strategie fortlaufende Warteschlangen mit Histogrammwerten von RSI und MACD, wobei das Fenster von `BarsToPattern` gesteuert wird.
2. Die RSI-Abweichung von 50 und die MACD-Histogrammabweichung vom gleitenden Mittelwert werden mithilfe eines hyperbolischen Tangens zu einem Konfidenzwert kombiniert, um die Squashing-Funktion des Netzwerks zu emulieren.
3. Wenn das absolute Konfidenzniveau `TradeLevel` übersteigt und die in Punkte umgerechnete prognostizierte Bewegung über `MinTargetPoints` liegt, erteilt die Strategie eine Marktorder in die vom Score vorgeschlagene Richtung.
4. Für die manuelle Ausstiegsbehandlung wird ein dynamischer Take-Profit gespeichert, der der prognostizierten Bewegung multipliziert mit `ProfitMultiply` und begrenzt durch `MaxTakeProfitPoints` entspricht. Ein symmetrischer Stop-Loss in Punkten spiegelt das ursprüngliche Verhalten wider.
5. Während eine Position offen ist, prüft die Strategie jede fertige Kerze: Wenn der Preis den gespeicherten Stop oder das gespeicherte Ziel erreicht, wird die Position zum Marktwert geschlossen und der interne Status zurückgesetzt.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `BarsToPattern` | Anzahl der im rollierenden Fenster gespeicherten Kerzen, die zur Berechnung der RSI- und MACD-Statistiken verwendet werden. |
| `TradeLevel` | Mindestkonfidenz (0-1), die zum Öffnen einer Position erforderlich ist. |
| `ProfitMultiply` | Der Multiplikator wird auf die geplante Bewegung angewendet, bevor sie mit `MaxTakeProfitPoints` begrenzt wird. |
| `MinTargetPoints` | Mindestanzahl an Preispunkten, die aus der Prognose erforderlich sind, um einen Handel einzugehen. |
| `MaxTakeProfitPoints` | Maximal zulässige Distanz in Punkten für den Take-Profit. |
| `StopLossPoints` | Abstand des Schutzstopps relativ zum Einstiegspreis in Punkten. |
| `TradeVolume` | Mit jeder Market-Order gesendetes Volumen. |
| `CandleType` | Kerzendatentyp oder Zeitrahmen, den Sie abonnieren möchten. |

## Notizen
- Das Konfidenzmodell ist absichtlich deterministisch, um das Verhalten transparent zu halten und gleichzeitig die Struktur des ursprünglichen neuronalen Netzwerkansatzes beizubehalten.
- Take-Profit- und Stop-Loss-Level werden manuell verwaltet, sodass jeder Trade seine eigenen dynamischen Ziele behält, ähnlich wie die MQL5-Version die Netzwerkausgabe nutzt.
- Die Strategie wertet neue Einträge nur aus, wenn keine Position offen ist, und repliziert damit die Einzelpositionsbeschränkung des Quell-Expert Advisors.
