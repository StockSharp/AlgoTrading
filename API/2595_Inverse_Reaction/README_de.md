# Umkehr-Reaktions-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Umkehr-Reaktions-Strategie ist ein Mean-Reversion-System, inspiriert vom ursprünglichen MetaTrader-Expertenberater "IREA". Sie reagiert auf ungewöhnlich große Einzelbar-Bewegungen und antizipiert eine inverse Reaktion in der nächsten Bar. Die Strategie berechnet ein dynamisches Konfidenzlevel aus jüngsten Kerzenbereichen und handelt nur, wenn Kursschwankungen dieses Level überschreiten, aber innerhalb benutzerdefinierter Grenzen bleiben. Es kann jeweils nur eine Position offen sein.

## Handelsstrategie
1. **Umkehr-Reaktions-Indikator** – Für jede abgeschlossene Kerze misst die Strategie die Eröffnungs-/Schlusskursänderung und speist ihren Absolutwert in einen einfachen gleitenden Durchschnitt der Länge `MaPeriod` ein. Die gemittelte Änderung wird mit `Coefficient` multipliziert, um eine dynamische Schwelle ähnlich dem Dynamic Confidence Level (DCL) des ursprünglichen Indikators zu bilden.
2. **Signalvalidierung** – Die absolute Eröffnungs-/Schlusskursänderung der letzten Kerze muss größer als die dynamische Schwelle, größer als `MinCriteriaPoints * PriceStep` und kleiner als `MaxCriteriaPoints * PriceStep` sein. Signale werden ignoriert, wenn die vorherige Kerze bereits dieselbe Bedingung erfüllte, was den ursprünglichen Expertenberater widerspiegelt.
3. **Richtung** – Eine negative Änderung (bärische Kerze) deutet auf einen Rebound nach oben hin, daher wird eine Long-Position eröffnet. Eine positive Änderung impliziert eine Erwartung einer bärischen Umkehr und löst eine Short-Position aus. Neue Trades werden nur gesendet, wenn keine bestehende Position vorhanden ist.
4. **Risikomanagement** – Nach dem Einstieg überwacht die Strategie die folgenden Kerzen. Wenn der Preis die vordefinierten Stop-Loss- oder Take-Profit-Levels berührt (von Punkten in absolute Preise unter Verwendung des `PriceStep` des Instruments umgerechnet), schließt sie die offene Position sofort mit Marktorders. `StartProtection()` wird ebenfalls aktiviert, um die integrierten StockSharp-Schutzfunktionen zu unterstützen.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `StopLossPoints` | Stop-Loss-Abstand in Punkten (multipliziert mit `PriceStep`). |
| `TakeProfitPoints` | Take-Profit-Abstand in Punkten. |
| `TradeVolume` | Für jede Marktorder verwendetes Volumen. |
| `SlippagePoints` | Informationseinstellung, die die MQL-Version widerspiegelt; derzeit nicht auf Orders angewendet. |
| `MinCriteriaPoints` | Minimale Eröffnungs-/Schlusskursdistanz (in Punkten) für ein gültiges Signal. |
| `MaxCriteriaPoints` | Maximal zulässige Eröffnungs-/Schlusskursdistanz (in Punkten). |
| `Coefficient` | Multiplikator zum Aufbau der dynamischen Konfidenzschwelle. |
| `MaPeriod` | Länge des im Indikator verwendeten gleitenden Durchschnitts. Muss mindestens 3 sein. |
| `CandleType` | Zeitrahmen der verarbeiteten Kerzen (Standard: 1 Stunde). |

## Verwendungsrichtlinien
- Stellen Sie sicher, dass das ausgewählte Instrument einen gültigen `PriceStep` hat. Wenn nicht verfügbar, fällt die Strategie auf einen Schritt von 1.0 zurück, was Schwellen verzerren kann.
- Passen Sie `MinCriteriaPoints` und `MaxCriteriaPoints` an die Volatilität des gewählten Zeitrahmens an. Ein zu enges Fenster filtert die meisten Signale heraus, während ein zu weites Fenster extrem große Bewegungen zulässt, die möglicherweise nicht umkehren.
- Der Standard-`Coefficient` von 1.618 repliziert die Goldene-Ratio-Skalierung des ursprünglichen Indikators. Höhere Werte erfordern größere Ausreißerkerzen vor dem Handel.
- Da Positionen durch Marktorders beim nächsten Kerzenschluss geschlossen werden, der die Stop- oder Ziellevels verletzt, kann die tatsächliche Ausführung von den genauen Limitlevels abweichen. Erwägen Sie Tests mit Intraday-Daten für präzisere Kontrolle, wenn nötig.
- Es wird jeweils nur eine Position gehalten. Die Strategie wartet, bis der aktuelle Trade geschlossen ist, bevor sie auf ein neues Signal reagiert.

## Hinweise
- Backtesten Sie die Konfiguration auf historischen Daten, bevor Sie sie live verwenden. Der ursprüngliche EA wurde für FX-Märkte konzipiert; Parameteranpassungen können für andere Assets erforderlich sein.
- Der `SlippagePoints`-Parameter wird aus Vollständigkeitsgründen beibehalten, aber absichtlich nicht verwendet, weil StockSharp Slippage anders als MetaTrader handhabt.
- Stellen Sie sicher, dass `MaPeriod` bei oder über 3 bleibt; kleinere Werte waren in der ursprünglichen Implementierung verboten und können zu instabilen Schwellen führen.
