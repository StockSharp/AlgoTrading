# ScalpWiz 9001 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
ScalpWiz 9001 ist ein mehrschichtiges Ausbruchs-Scalping-System, das das Verhalten des gleichnamigen MetaTrader-Experten repliziert. Die Strategie misst, wie weit die neueste Kerze über den Bollinger-Bänder-Umschlag hinausschließt und, wenn die Volatilität scharf expandiert, setzt sie ein Grid von ausstehenden Stop-Orders über oder unter dem Markt ein. Das originale Geldverwaltungsmodul wird beibehalten: Jede ausstehende Order kann entweder ein festes Lot oder einen konfigurierbaren Prozentsatz des Kontokapitals riskieren.

Sobald eine der Stop-Orders ausgeführt wird, werden die verbleibenden Orders storniert, während die aktive Position mit einem traditionellen Stop-Loss, Take-Profit und einer Trailing-Komponente geschützt wird, die erst nach dem Erreichen eines zusätzlichen Puffers mit dem Trailing beginnt. Die Strategie ist für Hochfrequenz-Scalping auf niedrigeren Zeitrahmen gedacht, kann aber auf jedem von StockSharp unterstützten Instrument ausgeführt werden.

## Signallogik
1. Den konfigurierten Zeitrahmen abonnieren und 20-Perioden-Bollinger-Bänder mit dem Abweichungsfaktor `BandsDeviation` (Standard 2) berechnen.
2. Prüfen, wie weit der Schlusskurs vom oberen oder unteren Band entfernt ist. Wenn der Schluss das Band um mindestens den vierten Level-Abstand überschreitet (`Level3Pips` in Preis umgerechnet), bereitet sich die Strategie darauf vor, die Bewegung zu faden:
   - Schluss über dem oberen Band → Sell-Stop-Orders unterhalb des Marktes platzieren.
   - Schluss unter dem unteren Band → Buy-Stop-Orders oberhalb des Marktes platzieren.
3. Vier ausstehende Orders werden mit zunehmenden Abständen platziert (`Level0Pips` … `Level3Pips`). Jede Order verwendet entweder das feste Volumen oder den der Schicht zugewiesenen Risikoprozentsatz. Orders verfallen nach `ExpirationMinutes`, wenn sie unberührt bleiben.
4. Wenn eine Einstiegsorder ausgeführt wird, werden alle ausstehenden Orders storniert. Die ausgeführte Position wird mit Stop-Loss (`StopLossPips`), Take-Profit (`TakeProfitPips`) und Trailing-Parametern (`TrailingStopPips`, `TrailingStepPips`) verwaltet. Trailing bewegt den Schutz-Stop nur, wenn der Preis mindestens `TrailingStopPips + TrailingStepPips` vom Einstieg entfernt ist.
5. Ausstiege werden mit Marktorders ausgeführt, sobald der Trailing-Stop oder das Gewinnziel auf einer abgeschlossenen Kerze erreicht wird.

## Parameter
- **Candle Type** – Zeitrahmen für Bollinger-Berechnungen.
- **Bands Period / Bands Deviation** – Bollinger-Konfiguration.
- **Stop Loss (pips)** – Schutz-Stop-Abstand in Pips.
- **Take Profit (pips)** – Gewinnziel-Abstand in Pips.
- **Trailing Stop (pips)** – Trailing-Stop-Abstand, der der Bewegung nach dem Extra-Puffer folgt.
- **Trailing Step (pips)** – Zusätzlicher Abstand, der vor der Aktivierung des Trailings erforderlich ist.
- **Expiration (minutes)** – Lebensdauer ausstehender Stop-Orders. Auf 0 setzen, um Orders unbegrenzt zu halten.
- **Management Mode** – Zwischen `FixedVolume` und `RiskPercent` wählen.
- **Level 0-3 Value** – Festes Lot oder Risikoprozentsatz für jede ausstehende Schicht.
- **Level 0-3 Pips** – Einstiegsoffsets für jede ausstehende Schicht.

## Geldverwaltung
Wenn `ManagementMode` gleich `RiskPercent` ist, berechnet die Strategie das Ordervolumen aus dem Kontokapital und dem konfigurierten Stop-Loss-Abstand:

```
Ordervolumen = (equity × riskPercent / 100) / (stopOffset / priceStep × stepPrice)
```

Wenn Markt-Metadaten (Preisschritt, Schrittpreis oder Volumenschritt) nicht verfügbar sind, fällt die Ordergröße aus Sicherheitsgründen auf null zurück. Mit `FixedVolume` werden die Schichtwerte direkt verwendet und auf den Instrumenten-Volumenschritt und -grenzen gerundet.

## Trailing und Schutz
- Stop-Loss und Take-Profit werden mit Pip-Abständen relativ zum tatsächlichen Ausführungspreis initialisiert.
- Die Trailing-Logik spiegelt die MetaTrader-Implementierung wider: Der Stop wird erst bewegt, wenn der Preis `TrailingStop + TrailingStep` vorrückt, und hält danach einen Abstand von `TrailingStop`.
- Ausstiege werden als Marktorders ausgegeben, was die Kompatibilität mit Handelsplätzen sicherstellt, die keine serverseitigen Schutzorders unterstützen.

## Praktische Hinweise
- Die Pip-Abstände entsprechend der Tick-Größe des Instruments konfigurieren. Bei fünfstelligen FX-Symbolen entspricht jeder Pip zehn Preisschritten und die Strategie passt sich automatisch durch Inspektion der Wertpapier-Dezimalstellen an.
- Da die Strategie von Stop-Orders abhängt, broker-spezifische Stop-Level-Anforderungen prüfen und Level-Abstände bei Bedarf anpassen.
- Risikoprozent-Sizing erfordert eine gültige Portfolio-Bewertung und Sicherheitsschritt-Metadaten; andernfalls wird das Ordervolumen zu null ausgewertet.
- Die Strategie operiert auf abgeschlossenen Kerzen und reagiert daher einmal pro Balken, was das Rauschen im Vergleich zum ursprünglichen tick-basierten Experten glättet.
