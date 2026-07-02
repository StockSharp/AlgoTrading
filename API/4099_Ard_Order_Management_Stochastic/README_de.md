# ARD Order Management Stochastic Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Ard Order Management**-Strategie ist eine StockSharp-Umsetzung des MetaTrader-Experten `ARD_ORDER_MANAGEMENT_EA-BETA_1`. Das ursprüngliche Skript konzentrierte sich auf das wiederholte Schließen bestehender Positionen vor der Platzierung neuer Aufträge und bot Hilfsroutinen für manuelle Stop-Loss- und Take-Profit-Updates. Die StockSharp-Version behält diese Disziplin bei und fügt gleichzeitig eine indikatorgesteuerte Automatisierung basierend auf dem Stochastic-Oszillator hinzu.

Das Standard-Setup zielt auf den Intraday-Forex-Handel auf einem 5-Minuten-Chart ab, der Kerzentyp ist jedoch vollständig konfigurierbar. Die gesamte Handelslogik läuft auf fertigen Kerzen ab, um dem Ausführungsstil des Quellexperten am Ende des Balkens treu zu bleiben.

## Handelslogik
- Ein Stochastic-Oszillator mit konfigurierbaren **Lookback**-, **Signal**- und **Verlangsamungsperioden** erzeugt Richtungssignale (Standard: 5/3/3).
- Wenn %K **über der Kaufschwelle** (standardmäßig 80) schließt, storniert die Strategie ausstehende Aufträge, schließt alle offenen Short-Positionen und geht eine Long-Position mit dem konfigurierten Volumen ein.
- Wenn %K **unter der Verkaufsschwelle** (standardmäßig 20) schließt, werden alle ausstehenden Aufträge storniert, offene Long-Positionen geschlossen und ein neuer Short eröffnet.
- Die Strategie bleibt in der neuen Position, bis das entgegengesetzte Signal ertönt oder ein Schutzausgang ausgelöst wird.

## Auftrags- und Risikomanagement
- Vor jedem neuen Einstieg gibt die Strategie Marktaufträge aus, die die aktuelle Position vollständig abflachen und das `open_order(CLOSE)`-Verhalten von EA reproduzieren.
- `StartProtection` sendet automatisch erste Stop-Loss- und Take-Profit-Orders gemäß den Parametern `StopLossPips` und `TakeProfitPips`.
- Die optionale Trailing-Logik emuliert den `MODIFY`-Zweig von EA: Jede fertige Kerze kann ein dynamisches Stop-Level (`ModifyStopLossPips`) und ein schwebendes Gewinnziel (`ModifyTakeProfitPips`) aktualisieren. Wenn der Preis eines der unteren Niveaus erreicht, wird die Position geschlossen, um Gewinne zu sichern oder das Risiko zu begrenzen.
- Die Pip-Konvertierung verwendet den `PriceStep` des Instruments (mit einer 10-fachen Anpassung für Forex-Symbole mit Bruchteilen von Pip), sodass distanzbasierte Parameter über alle Märkte hinweg konsistent bleiben.

## Parameter
- **Volumen** – Handelsvolumen für Neuzugänge; Zusätzliche Größe wird automatisch hinzugefügt, um gegensätzliche Positionen zu schließen.
- **TakeProfitPips / StopLossPips** – anfängliche Schutzdistanzen, die an das eingebaute Schutzmodul übergeben werden. Auf Null setzen, um eine der beiden Reihenfolgen zu deaktivieren.
- **ModifyTakeProfitPips / ModifyStopLossPips** – nachlaufende Offsets (in Pips), die nach jeder Kerze neu berechnet werden. Auf Null setzen, um nachlaufende Aktualisierungen zu deaktivieren.
- **StochasticPeriod / SignalPeriod / SlowingPeriod** – Oszillatorkonfiguration, die den `iStochastic`-Aufruf des ursprünglichen Experten widerspiegelt.
- **BuyThreshold / SellThreshold** – überkaufte/überverkaufte Niveaus, die Long/Short-Umkehrungen auslösen.
- **CandleType** – Zeitrahmen oder benutzerdefinierte Kerzendatenquelle, die den Indikator antreibt.

Jeder Parameter stellt sinnvolle Optimierungsbereiche bereit, sodass Sie alternative Einstellungen im StockSharp-Optimierer erneut testen können.

## Nutzungshinweise
- Funktioniert am besten bei liquiden Instrumenten, bei denen Pip-basierte Stopps sinnvoll sind (wichtige Devisenpaare, Index-CFDs, liquide Futures).
- Erhöhen Sie den Zeitrahmen beim Handel mit langsameren Märkten, um Lärm und falsche Umkehrungen zu reduzieren.
- Stellen Sie bei der Ausführung auf Live-Konten sicher, dass das konfigurierte Volumen die Mindest- und Schrittgrößen des Brokers berücksichtigt.
- Die nachgestellte Logik kann deaktiviert werden, indem die Parameter `Modify*` auf Null belassen werden, wodurch die Beibehaltung der statischen Reihenfolge der Quelle EA effektiv reproduziert wird.
- Kombinieren Sie es mit zusätzlichen Filtern (Trend, Volatilität, Sitzungen), wenn Sie selektivere Einträge wünschen – der Code stellt Eigenschaften bereit, die erweitert werden können.

## Konvertierungsdetails
- Quelldatei: `MQL/9041/ARD_ORDER_MANAGEMENT_EA-BETA_1.mq4`.
- Die in der kommentierten `start()`-Routine angedeutete Stochastic-Triggerlogik wurde neu erstellt.
- Durch StockSharps hochrangiges API konnten die Disziplinarmaßnahmen kurz vor Beginn und die Erteilung einer Schutzanordnung aufrechterhalten werden.
- Optionale Trailing-Exits hinzugefügt, um den manuellen `MODIFY`-Block von EA widerzuspiegeln, während die Implementierung indikatorgesteuert und ereignisbasiert bleibt.
