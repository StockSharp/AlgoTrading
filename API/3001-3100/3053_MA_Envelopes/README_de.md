# MA-Envelopes-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert aus dem MetaTrader 5-Experten "MA Envelopes". Die Strategie sucht nach Preiskorrekturen in Richtung eines gleitenden Durchschnitts, der von einem Envelope-Kanal umgeben ist. Wenn eine abgeschlossene Kerze zwischen dem gleitenden Durchschnitt und einer der Envelope-Bänder während des konfigurierten Handelsfensters schließt, platziert die Strategie Limit-Einstiege beim gleitenden Durchschnitt mit aus dem Envelope abgeleiteten Schutzausstiegsorders.

## Handelslogik

1. Ein gleitender Durchschnitt wird mit der ausgewählten Methode, Preisquelle und Periode berechnet. Derselbe Wert wird verwendet, um symmetrische Envelope-Bänder mithilfe des Deviationsparameters aufzubauen.
2. Wenn eine abgeschlossene Kerze über dem gleitenden Durchschnitt, aber unter dem oberen Envelope-Band schließt und der aktuelle Ask-Preis über dem gleitenden Durchschnitt bleibt, wird eine gestaffelte Sequenz von Kauf-Limit-Orders zum gleitenden Durchschnittspreis vorbereitet.
   * Jedes Kauf-Limit verwendet den unteren Envelope als Stop-Loss-Level und den oberen Envelope plus einem zusätzlichen Pip-Offset als Take-Profit.
   * Bis zu drei unabhängige Orders werden verwaltet, jede mit ihrem eigenen Take-Profit-Offset (`First`, `Second`, `Third` SL/TP-Parameter).
3. Wenn eine abgeschlossene Kerze unter dem gleitenden Durchschnitt, aber über dem unteren Envelope-Band schließt und der aktuelle Bid-Preis unter dem gleitenden Durchschnitt bleibt, wird die Logik für Verkaufs-Limit-Orders gespiegelt.
4. Das Handelsfenster wird durch `StartHour` und `EndHour` (Terminalzeit) gesteuert. Nach der Endstunde werden alle noch aktiven Einstiegsorders storniert.
5. Das Risiko pro Trade wird über `MaximumRisk` geschätzt und nach aufeinanderfolgenden Verlusten mittels `DecreaseFactor` reduziert. Das Ordervolumen wird an den Volumenschritt und die Limits des Instruments angepasst.
6. Sobald eine Einstiegsorder vollständig gefüllt ist, werden sofort Schutz-Stop-Loss- und Take-Profit-Orders registriert. Wenn eine Ausstiegsorder ausgelöst wird, wird die Gegenorder storniert und, falls noch Positionsvolumen übrig ist, werden neue Schutzorders für den Rest ausgegeben.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `MaximumRisk` | Anteil des verfügbaren Eigenkapitals, das pro Position riskiert wird. |
| `DecreaseFactor` | Reduziert die Positionsgröße nach aufeinanderfolgenden Verlust-Trades. |
| `First/Second/ThirdStopTakeProfitPips` | Pip-Abstände, die zu den Envelope-Bändern für die drei gestaffelten Orders hinzugefügt werden. |
| `StartHour`, `EndHour` | Handelssitzungsgrenzen in Terminalzeit (0–23). |
| `MaPeriod`, `MaShift`, `MaMethodType`, `AppliedPrice` | Konfiguration des gleitenden Durchschnitts. |
| `EnvelopeDeviation` | Breite des Envelope-Kanals in Prozent. |
| `CandleType` | Zeitrahmen der für die Berechnungen verwendeten Kerzen. |

## Hinweise

* Schutzorders werden neu erstellt, wenn nur ein Teil einer Position geschlossen wurde, um die verbleibende Größe abzudecken.
* Ausstehende Einstiegsorders werden am Ende der Sitzung storniert; offene Positionen bleiben von ihren Schutzorders verwaltet.
* Die Strategie verlässt sich auf Order-Book-Updates, um die neuesten Bid/Ask-Preise zu erfassen; Kerzenschlusspreise werden als Fallback verwendet, wenn Order-Book-Daten nicht verfügbar sind.
