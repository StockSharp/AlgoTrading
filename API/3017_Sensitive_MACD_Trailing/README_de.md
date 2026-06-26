# Sensitive MACD Trailing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist eine direkte StockSharp-Konvertierung des „Sensitive" MACD-Experten-Advisors für MetaTrader 5. Sie kombiniert MACD-Crossovers mit konfigurierbaren Risikomanagement-Tools (fester Stop-Loss, Take-Profit und Pip-basierte Trailing-Stops). Der Algorithmus arbeitet ausschließlich auf abgeschlossenen Kerzen und verwendet die High-Level-API, um den gewünschten Zeitrahmen zu abonnieren.

## Indikatoren und Daten
- **MACD (Moving Average Convergence Divergence)** – konfiguriert mit unabhängigen Fast-, Slow- und Signal-EMA-Längen.
- **Kerzen** – benutzerwählbarer Zeitrahmen, bereitgestellt über den Parameter `CandleType`.

## Einstiegsbedingungen
1. Eine neue Kerze muss schließen, um Intrabar-Rauschen zu vermeiden.
2. MACD-Werte werden aus der Indikatorbindung verarbeitet:
   - `macd` repräsentiert die MACD-Hauptlinie.
   - `signal` ist die Signallinie (EMA der MACD-Differenz).
3. Anforderungen für **Long-Einstieg**:
   - MACD kreuzt über die Signallinie (`macd > signal`, während die vorherigen Werte `macd < signal` erfüllten).
   - MACD bleibt im negativen Bereich (`macd < 0`).
   - Die absolute MACD-Magnitude ist größer als `MacdOpenLevel * Point`, was eine bedeutungsvolle Verschiebung sicherstellt.
   - Keine aktive Long-Position (Nettoposition ist kleiner oder gleich null). Bestehende Shorts werden durch Senden der erforderlichen Menge umgekehrt.
4. Anforderungen für **Short-Einstieg** spiegeln das Long-Setup wider:
   - MACD kreuzt unter die Signallinie, bleibt dabei positiv.
   - Die absolute MACD-Magnitude übersteigt den konfigurierten Schwellenwert.
   - Keine Short-Position vorhanden (Nettoposition ist größer oder gleich null). Bestehende Longs werden geglättet, bevor der Short eröffnet wird.

## Exit-Management
- **Take-Profit**: Sobald der Trade eröffnet ist, speichert die Strategie ein Zielniveau, das durch `TakeProfitPips` definiert wird. Wenn das Hoch einer Long-Kerze oder das Tief einer Short-Kerze dieses Niveau erreicht, wird die Position zu Marktpreisen geschlossen.
- **Stop-Loss**: Ein Schutz-Stop wird aus `StopLossPips` berechnet. Bei Longs löst ein Kursrückgang auf das Stop-Niveau einen Marktausstieg aus. Shorts reagieren auf Kursanstiege, die den Stop erreichen.
- **Trailing-Stop**: Wenn `TrailingStopPips` ungleich null ist, aktiviert der Algorithmus eine Trailing-Logik, nachdem der Kurs mindestens `TrailingStopPips + TrailingStepPips` Pips vom Einstieg vorgerückt ist. Nachfolgende Bewegungen halten das Stop-Niveau immer in der angegebenen Trailing-Distanz vom letzten Schlusskurs. Der Trailing-Schritt muss positiv sein, wenn der Trailing-Stop aktiviert ist; andernfalls stoppt die Strategie mit einer Fehlermeldung.
- Wenn keine Position aktiv ist, werden interne Tracking-Variablen zurückgesetzt, um sich auf den nächsten Trade vorzubereiten.

## Positionsgröße
Ordermengen werden durch den eingebauten `Volume`-Strategieparameter gesteuert (Standard: 0.1). Umkehrungen fügen automatisch den absoluten Wert der aktuellen Position zum gewünschten Volumen hinzu, um die Richtung in einer einzigen Marktorder zu wechseln.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `FastLength` | Schnelle EMA-Periode, die von der MACD-Hauptlinie verwendet wird. | 12 |
| `SlowLength` | Langsame EMA-Periode, die von der MACD-Hauptlinie verwendet wird. | 26 |
| `SignalLength` | Signal-EMA-Periode für den MACD. | 9 |
| `MacdOpenLevel` | Minimale MACD-Magnitude (in Preispunkten), die zum Auslösen von Trades erforderlich ist. | 3 |
| `StopLossPips` | Abstand des Schutz-Stops in Pips. | 35 |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. | 75 |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips (0 deaktiviert Trailing). | 5 |
| `TrailingStepPips` | Zusätzliche Distanz, die der Kurs bewegen muss, bevor der Trailing-Stop aktualisiert wird. | 5 |
| `CandleType` | Quell-Kerzentyp (Zeitrahmen). | 1-Minuten-Kerzen |
| `Volume` | Order-Volumen, ausgedrückt in Lots/Kontrakten je nach Instrument. | 0.1 |

## Zusätzliche Hinweise
- Pip- und MACD-Punktwerte werden aus dem Preisschritt des Instruments und seiner Dezimalgenauigkeit abgeleitet. Der Code passt sich für 3- und 5-stellige Forex-Symbole an, indem die Pip-Größe entsprechend skaliert wird.
- Alle Kommentare im Quellcode sind in Englisch geschrieben, und die Implementierung verwendet nur High-Level-StockSharp-APIs gemäß den Projektrichtlinien.
- Die Strategie vermeidet absichtlich die Verwaltung von Teilfüllungen und geht davon aus, dass Marktorders beim Ausführen im Simulator oder Echthandel sofort gefüllt werden. Weitere Sicherheitsvorkehrungen können bei Bedarf hinzugefügt werden.
