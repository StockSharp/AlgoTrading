# VR Overturn Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Repliziert den MetaTrader-Experten «VR---Overturn» mit den High-Level-APIs von StockSharp.
- Hält jeweils nur eine offene Position und bewertet den nächsten Trade sofort nach dem Schließen des vorherigen.
- Entwickelt für diskretionäre Trader, die eine automatische Positionsumkehr mit Martingale- oder Anti-Martingale-Sizing wünschen.

## Handelslogik
1. **Einstiegsposition** – die Strategie eröffnet den ersten Trade in der konfigurierten Richtung (`FirstPositionDirection`) mit dem Basisvolumen (`BaseVolume`).
2. **Stop-Loss / Take-Profit** – Schutzausstiegsorders werden automatisch mit `StopLossPips` und `TakeProfitPips` verknüpft. Die Engine konvertiert Pips in absolute Preisoffsets durch Analyse des Kursschritts des Instruments (Instrumente mit 3 und 5 Dezimalstellen erhalten die 10-fache Anpassung wie beim Original-Experten).
3. **Verarbeitung des Positionsschlusses** – wenn eine Position durch eine Schutzorder geschlossen wird, erfasst die Strategie:
   - Seite des geschlossenen Trades (Long oder Short).
   - Ausgeführtes Volumen.
   - Realisierter PnL (Differenz zwischen Ein- und Ausstiegspreis).
4. **Sizing der nächsten Einstiegsposition** – das gespeicherte Ergebnis bestimmt die Seite und die Lotgröße der nächsten Order.
   - Gewinnende Trades behalten dieselbe Richtung bei, verlierende Trades drehen die Richtung um.
   - Der Martingale-Modus multipliziert die Positionsgröße nach einem Verlust und setzt sie nach einem Gewinn auf das Basisvolumen zurück.
   - Der Anti-Martingale-Modus multipliziert die Positionsgröße nach einem Gewinn und setzt sie nach einem Verlust auf das Basisvolumen zurück.
5. **Lot-Rundung** – die berechnete Größe wird auf den nächsten Volumenschritt des Instruments abgeschnitten, bevor eine Marktorder gesendet wird.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|---------|
| `FirstPositionDirection` | Richtung des allerersten Trades (Buy/Sell). | Buy |
| `Mode` | Sizing-Modus: Martingale (Erhöhung nach Verlusten) oder AntiMartingale (Erhöhung nach Gewinnen). | Martingale |
| `BaseVolume` | Initiales Positionsvolumen. Wird beim Zurücksetzen einer Sequenz verwendet. | 0.1 |
| `StopLossPips` | Abstand zum Stop-Loss in Pips. | 30 |
| `TakeProfitPips` | Abstand zum Take-Profit in Pips. | 90 |
| `LotMultiplier` | Multiplikator, der beim Erweiterungsschritt angewendet wird (nach Verlust für Martingale, nach Gewinn für Anti-Martingale). | 1.6 |

## Risikomanagement
- Verwendet `StartProtection`, um Stop-Loss- und Take-Profit-Orders für jeden Einstieg zu verknüpfen.
- Stop- und Zielabstände sind absolute Preisoffsets, abgeleitet aus den konfigurierten Pip-Werten.
- Es wird keine zusätzliche Trailing-Logik angewendet, sodass das Risiko vollständig durch die Schutzorders und die Positionsumkehrregeln kontrolliert wird.

## Betriebshinweise
- Die Strategie basiert nicht auf Kerzen oder Indikatoren; sie reagiert ausschließlich auf Trade-Bestätigungen (`OnOwnTradeReceived`).
- Wenn eine Schutzorder teilweise ausgeführt wird, akkumuliert die Strategie den verbleibenden Betrag, bis die Position flat ist, bevor sie erneut handelt.
- Provisions- und Swap-Werte sind in StockSharp-Trades nicht verfügbar, daher verwendet der Gewinnvergleich nur die Preisdifferenz. Erwägen Sie, Stops oder Multiplikatoren zu erweitern, wenn Ihr Broker erhebliche Gebühren berechnet.
- Funktioniert mit jedem Instrument, das Preis- und Volumenschrittmetadaten bereitstellt; überprüfen Sie beide vor dem Einsatz in der Produktion.
