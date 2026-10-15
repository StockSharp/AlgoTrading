# Einfache Roboterstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Easy Robot ist ein dem Momentum folgender Expert Advisor, der einmal pro abgeschlossener stündlicher Kerze handelt. Wenn die vorherige Kerze bullisch schließt, eröffnet die Strategie eine neue Long-Position; Wenn es bärisch schließt, eröffnet es einen Short. Es kann jeweils nur eine Position aktiv sein, was die ursprüngliche Logik von MetaTrader 4 vollständig widerspiegelt.

## Handelsregeln
1. Abonnieren Sie den stündlichen Kerzentyp, der durch den Parameter **CandleType** ausgewählt wird (standardmäßig H1).
2. Sobald eine Kerze fertig ist, vergleichen Sie ihren Schlusskurs mit dem Eröffnungskurs:
   - Schließen > Öffnen: Senden Sie eine Marktkauforder, wenn keine Position offen ist.
   - Schließen < Öffnen: Senden Sie einen Marktverkaufsauftrag, wenn der Kurs unverändert bleibt.
3. Die Positionsgröße verwendet die Strategieeigenschaft `Volume`, genau wie die MQL-Version, die auf `CheckVolumeValue` basierte, mit einem Standardwert von 0,01 Lots.
4. Stop-Loss- und Take-Profit-Level basieren auf einem **Average True Range**-Indikator mit der Periode **AtrPeriod** (Standard 14):
   - Stoppdistanz = `ATR * StopFactor`.
   - Nehmen Sie Abstand = `ATR * TakeFactor`.
   - Beide Abstände werden durch den minimalen Tick-/Pip-Abstand normalisiert, sodass Schutzaufträge niemals näher platziert werden, als der Broker zulässt.
5. Schutzaufträge werden unmittelbar nach der Marktorder über `SetStopLoss` und `SetTakeProfit` registriert und bieten das gleiche Verhalten wie `OrderSend` mit den Parametern `sl` und `tp`.
6. Optionales Trailing wird aktiviert, wenn **UseTrailingStop** wahr ist. Nachdem der Handel **TrailingStartPips**-Gewinne angehäuft hat (MetaTrader Pips, d. h. Punkte angepasst an 3/5 Dezimalkurse), wird der Stop um **TrailingStepPips** näher gerückt und nur dann weiter verschoben, wenn neue Gewinnextremwerte erreicht werden. Beim Trailing wird der minimale Stoppabstand des Brokers berücksichtigt, um ungültige Änderungen zu vermeiden.
7. Quotes für Stop-Berechnungen verwenden den besten Geld-/Briefkurs, sofern verfügbar, und fallen auf den letzten Preis oder Kerzenschluss zurück, der mit den ursprünglichen `Bid`/`Ask`-Referenzen übereinstimmt.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `TakeFactor` | 4.2 | ATR-Multiplikator für Take-Profit-Distanz (entspricht der `TakeFactor`-Eingabe in MQL). |
| `StopFactor` | 4.9 | ATR-Multiplikator für die Stop-Loss-Distanz (entspricht `StopFactor`). |
| `UseTrailingStop` | wahr | Aktiviert das Nachstellen im MetaTrader-Stil (`UseTstop`). |
| `TrailingStartPips` | 40 | Profitieren Sie in Pips, bevor das Trailing beginnen kann (`Tstart`). |
| `TrailingStepPips` | 19 | Beim Nachstellen von Aktualisierungen wird ein Pip-Schritt angewendet (`Tstep`). |
| `AtrPeriod` | 14 | ATR Berechnungszeitraum für die Volatilitätsgröße. |
| `CandleType` | H1 | Kerzenserie, die für Signale und den ATR-Eingang verwendet wird. |

## Notizen
- Die Strategie setzt die gespeicherten Einstiegs- und Stop-Preise jedes Mal zurück, wenn die Position auf Null zurückkehrt, und stellt so einen sauberen Zustand für das nächste Signal sicher.
- Der minimale Stoppabstand wird über die Pip-Größe des Instruments geschätzt (oder über den Preisschritt, wenn die Pip-Größe nicht verfügbar ist). Dadurch wird der `SC`-Helper aus der MQL-Include-Datei reproduziert.
- `StartProtection()` wird beim Start einmal aufgerufen, damit die Plattform bei Bedarf Notausgänge verwalten kann.
