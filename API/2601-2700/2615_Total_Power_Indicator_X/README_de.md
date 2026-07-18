# Total Power Indikator X-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie recreiert das Verhalten des MetaTrader-Experten "Exp_TotalPowerIndicatorX" mit den High-Level-APIs von StockSharp. Sie basiert auf einer benutzerdefinierten Implementierung des Total Power Indikators, der die Dominanz von Bullen und Bären misst, indem er zählt, wie viele Kerzen in einem gleitenden Fenster über oder unter einer internen EMA-Basislinie schließen. Handelsentscheidungen werden getroffen, wenn sich die Stärkelinien von Bullen und Bären kreuzen.

Der Indikator funktioniert auf jedem Symbol und Zeitrahmen. Standardmäßig abonniert die Strategie 4-Stunden-Kerzen, entsprechend der ursprünglichen Expert-Advisor-Konfiguration, aber der Zeitrahmen kann über einen Parameter angepasst werden.

## Handelslogik
1. Für jede abgeschlossene Kerze füttert die Strategie den Total Power Indikator mit den Kerzendaten. Der Indikator:
   - Berechnet eine EMA mit dem Zeitraum **Power Period**.
   - Zählt, wie viele Kerzen innerhalb der **Lookback Period** `High > EMA` (Bullen) und `Low < EMA` (Bären) hatten.
   - Konvertiert die Zählungen in Stärkewerte im prozentualen Stil im Bereich 0–100.
2. Ein **bullischer Crossover** (Bullenstärke steigt über Bärenstärke) löst einen Long-Einstieg aus, wenn Long-Trading aktiviert ist und keine offenen Positionen vorhanden sind.
3. Ein **bearischer Crossover** (Bärenstärke steigt über Bullenstärke) löst einen Short-Einstieg aus, wenn Short-Trading aktiviert ist und keine offenen Positionen vorhanden sind.
4. Entgegengesetzte Crossover schließen bestehende Positionen, wenn die relevanten Exit-Schalter aktiviert sind.
5. Ein optionaler Trading-Session-Filter erzwingt das Schließen aller Positionen außerhalb des konfigurierten Zeitfensters und deaktiviert neue Einstiege während dieses Zeitraums.
6. Optionale Stop-Loss- und Take-Profit-Level werden als Vielfache des Sicherheits-Preisschritts ausgedrückt. Sie werden nach jedem Einstieg neu berechnet und werden ausgelöst, sobald das Hoch oder Tief der Kerze das Niveau durchbricht.

## Parameter
- **Candle Type** – Zeitrahmen für Indikatorberechnungen. Standard: 4-Stunden-Kerzen.
- **Power Period** – EMA-Länge innerhalb des Indikators; spiegelt den MQL-Input wider. Standard: 10.
- **Lookback** – Anzahl der Kerzen zur Zählung bullischer und bearischer Dominanz. Standard: 45.
- **Volume** – Ordergröße, die an die Börse oder den Simulator gesendet wird. Standard: 1.
- **Enable Long Entry / Enable Short Entry** – neue Positionen in der entsprechenden Richtung erlauben oder verbieten.
- **Enable Long Exit / Enable Short Exit** – Positionen bei entgegengesetzten Signalen schließen. Deaktivieren, um Positionen offen zu halten, bis sie manuell geschlossen oder gestoppt werden.
- **Use Trading Hours** – Zeitfilter aktivieren. Wenn aktiv, handelt die Strategie nur zwischen **Start Hour/Minute** und **End Hour/Minute** und schließt alle offenen Positionen außerhalb dieses Intervalls. Übernacht-Fenster (Start später als Ende) werden unterstützt.
- **Stop Loss Points / Take Profit Points** – Abstände vom Eintrittspreis, gemessen in Preisschritten. Auf null setzen, um das Level zu deaktivieren. Die Berechnung verwendet `Security.PriceStep`, daher sicherstellen, dass die Sicherheitsmetadaten verfügbar sind.

## Hinweise
- Die Strategie öffnet eine neue Position nur, wenn keine bestehende Position auf der Sicherheit aktiv ist, was das Verhalten des ursprünglichen Experten emuliert.
- Da Stop-Loss- und Take-Profit-Berechnungen vom Preisschritt des Instruments abhängen, bleiben die Schutzlevel beim Ausführen der Strategie ohne diese Metadaten automatisch deaktiviert.
- Der Indikatorwert wird auf dem Diagrammbereich aufgezeichnet, wenn die Benutzeroberfläche verfügbar ist, was hilft, die Kreuzungen zwischen Bullen- und Bärenstärke zu visualisieren.
