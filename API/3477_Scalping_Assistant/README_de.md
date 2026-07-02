# Scalping-Assistent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Scalping Assistant** ist eine direkte Umsetzung des MetaTrader 4 Expertenberaters „Scalper Assistant v1.0“. Es generiert keine eigenen Einträge. Stattdessen überwacht es offene Positionen auf dem konfigurierten Wertpapier und verwaltet Schutzaufträge auf MetaTrader-ähnliche Weise.

## Wie es funktioniert

1. Wenn eine neue Position erkannt wird, registriert die Strategie sofort Stop-Loss- und Take-Profit-Orders unter Verwendung der konfigurierten Distanzen (ausgedrückt in Preisschritten).
2. Die Strategie nutzt Level-1-Daten und verfolgt kontinuierlich den besten Geld-/Briefkurs, um den aktuellen Gewinn der Position abzuschätzen.
3. Sobald der nicht realisierte Gewinn `BreakEvenTriggerPoints` erreicht, wird der anfängliche Stopp aufgehoben und zum Break-Even-Preis zuzüglich des konfigurierten Offsets neu registriert.
4. Das Stop-Level bleibt beim Break-Even; Es wird kein weiteres Nachziehen durchgeführt. Die Take-Profit-Order bleibt davon unberührt.
5. Sobald die Position geschlossen ist, werden alle Schutzaufträge gelöscht und der interne Status zurückgesetzt, bereit für den nächsten manuellen Handel.

## Nutzungshinweise

- Hängen Sie die Strategie an einen Connector/Portfolio an und eröffnen Sie Trades manuell oder über einen anderen Algorithmus. Der Assistent übernimmt die Vertretung dieser Positionen.
- Die Logik basiert auf Anführungszeichen der Ebene 1; Stellen Sie sicher, dass der ausgewählte Connector die besten Bid/Ask-Updates bietet.
- Der Begriff *Punkte* bezieht sich auf die Preisstufe des Instruments (`Security.PriceStep`). Bei Forex-Symbolen mit fünf Dezimalstellen entspricht dies einem Pip.

## Parameter

| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `StopLossPoints` | `decimal` | `30` | Abstand (in Preisschritten), der beim Platzieren des ersten Schutzstopps verwendet wird. Auf `0` setzen, um das Senden einer Stop-Order zu überspringen. |
| `TakeProfitPoints` | `decimal` | `100` | Distanz (in Preisschritten), die bei der Platzierung der ersten Take-Profit-Order verwendet wird. Auf `0` setzen, um den Take-Profit zu überspringen. |
| `BreakEvenTriggerPoints` | `decimal` | `15` | Profitieren Sie von Preisschritten, die erreicht werden müssen, bevor der Stop auf die Gewinnschwelle verschoben wird. |
| `BreakEvenOffsetPoints` | `decimal` | `5` | Zusätzlicher Abstand (in Preisschritten), der über/unter dem Einstiegspreis hinzugefügt wird, wenn der Stop auf die Gewinnschwelle verschoben wird. |

## Konvertierungsstatus

- ✅ Kernlogik: Break-Even-Stop-Verarbeitung basierend auf MetaTrader-Eingabeparametern.
- ✅ API-Nutzung auf hoher Ebene: `SubscribeLevel1()` mit Delegatenbindung.
- ✅ Schutzbefehle: erstellt über die Helfer `SellStop`, `BuyStop`, `SellLimit` und `BuyLimit`.
- ❌ Kein Python-Port – es wird nur die C#-Strategie entsprechend der Anfrage bereitgestellt.
