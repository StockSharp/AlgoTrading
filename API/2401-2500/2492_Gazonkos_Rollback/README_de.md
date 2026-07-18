# Gazonkos Rücksetzer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Gazonkos Rücksetzer-Strategie ist eine Konvertierung des ursprünglichen MetaTrader 5 Expert Advisors **gazonkos**. Der Ansatz handelt den EUR/USD-Stunden-Chart und sucht nach starkem Momentum zwischen zwei historischen Schlusskursen. Nachdem dieses Momentum erkannt wurde, wartet er auf einen Rücksetzer einer vordefinierten Größe und steigt dann in Richtung der ursprünglichen Bewegung ein. Die StockSharp-Implementierung behält dieselbe schrittweise Zustandsmaschine wie der Quellcode bei und verwendet die High-Level-API mit Kerzenabonnements und Schutzorders.

## Handelslogik
1. **Zulässigkeitsprüfung** – nur eine Position pro Stunde ist erlaubt. Wenn ein weiterer Trade in derselben Uhrstunde eröffnet wurde oder die konfigurierte Anzahl gleichzeitiger Trades bereits läuft, wartet die Strategie.
2. **Momentum-Erkennung** – vergleicht die Schlusskurse zweier vergangener Kerzen (`SecondShift` minus `FirstShift`). Wenn die Differenz `Delta` überschreitet, notiert die Strategie die vorgesehene Richtung (Long, wenn der neuere Schlusskurs höher ist, sonst Short).
3. **Rücksetzer-Tracking** – ab dem Moment, wenn das Momentum erscheint, überwacht der Code das höchste Hoch (für Long-Setups) oder das tiefste Tief (für Short-Setups), das während dieser Stunde erreicht wurde. Wenn der Preis mindestens `Rollback` zurücksetzt, wird das Setup ausführungsbereit. Wenn sich die Stunde ändert, bevor der Rücksetzer erfolgt, wird das Signal verworfen.
4. **Order-Ausführung** – sobald die Rücksetzer-Bedingung erfüllt ist, platziert die Strategie eine Marktorder mit festen Take-Profit- und Stop-Loss-Abständen. Die Positionsgrößenbestimmung wird über den `TradeVolume`-Parameter gesteuert, und der eingebaute `StartProtection`-Helper verwaltet die Schutzorders.

Diese Sequenz spiegelt die MT5-Version eng wider, die `STATE`- und `Trade`-Variablen zur Koordination des Workflows verwendete.

## Risikomanagement
* `StartProtection` konfiguriert feste Take-Profit- und Stop-Loss-Abstände in absoluten Preiseinheiten, ähnlich wie der Expert TP/SL an jede Order anhängte.
* `ActiveTrades` begrenzt das maximale Gesamtengagement, indem der absolute Positionswert mit dem Produkt aus konfiguriertem Volumen und erlaubter Trade-Anzahl verglichen wird.
* Die Kombination aus stündlicher Steuerung und Rücksetzerbestätigung reduziert Überhandel bei Seitwärtsbedingungen.

## Parameter
| Name | Standard | Beschreibung |
| ---- | -------- | ------------ |
| `TakeProfit` | `0.0016` | Absoluter Abstand (in Preiseinheiten) für den Take Profit. Entspricht 16 Punkten bei einem 5-stelligen EUR/USD-Kurs. |
| `Rollback` | `0.0016` | Erforderlicher Rücksetzer vom nach dem Momentum-Signal erreichten Extrempunkt. |
| `StopLoss` | `0.0040` | Absoluter Abstand für den Schutz-Stop-Loss. Entspricht 40 Punkten auf EUR/USD. |
| `Delta` | `0.0040` | Mindestdifferenz zwischen den zwei historischen Schlusskursen, die eine starke Bewegung definiert. |
| `TradeVolume` | `0.1` | Standard-Ordervolumen, das an `BuyMarket()` und `SellMarket()` übergeben wird. |
| `FirstShift` | `3` | Älterer Balkenindex (Anzahl der Kerzen zurück) für den Schlusskursvergleich. |
| `SecondShift` | `2` | Neuerer Balkenindex für den Schlusskursvergleich. |
| `ActiveTrades` | `1` | Maximale Anzahl gleichzeitiger Trades. Auf null setzen, um das Limit zu deaktivieren. |
| `CandleType` | Zeitrahmen `1 Stunde` | Für die Analyse verwendete Kerzenserie; standardmäßig Stundenkerzen wie der Quell-EA. |

## Hinweise
* Die Strategie funktioniert mit jedem Instrument mit einer angemessenen Tick-Größe; `Delta`, `Rollback`, `TakeProfit` und `StopLoss` an den Punktwert des Instruments anpassen.
* Alle Inline-Kommentare sind auf Englisch geschrieben, wie von den Projektrichtlinien verlangt.
* Ein Python-Port für diese Strategie ist noch nicht verfügbar.
