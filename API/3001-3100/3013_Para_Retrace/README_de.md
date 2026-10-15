# Para Retracement-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Para Retracement-Strategie** ist eine C#-Konvertierung des ursprünglichen MetaTrader 4-Experten-Advisors `Para_Retrace.mq4`. Sie reproduziert die Idee, den Parabolic-SAR-Indikator als dynamischen Anker zu verwenden und auf Kursrückläufe zu diesem Niveau zu warten, bevor man in den Markt eintritt. Die Konvertierung nutzt die StockSharp-High-Level-API zur Verwaltung von Marktdatenabonnements, Indikatoraktualisierungen und Orderausführung.

## Handelslogik
1. Den Parabolic-SAR-Wert auf jeder abgeschlossenen Kerze mit dem konfigurierten Beschleunigungsschritt und der maximalen Beschleunigung berechnen.
2. Den vorherrschenden Trend bestimmen, indem geprüft wird, ob die gesamte Kerze unter oder über dem SAR-Wert liegt:
   - **Bärischer Kontext:** wenn sowohl das Kerzenhoch als auch das Kerzentief unter dem SAR-Wert liegen.
   - **Bullischer Kontext:** andernfalls (der Kurs berührt oder liegt über dem SAR-Niveau).
3. Einen Auslösepreis ableiten, indem der SAR-Wert um eine benutzerdefinierte Anzahl von Pips versetzt wird:
   - In einem bärischen Kontext subtrahiert die Strategie den Versatz und wartet auf einen Rücklauf nach oben.
   - In einem bullischen Kontext addiert die Strategie den Versatz und wartet auf einen Rückzug nach unten.
4. Sobald der Kurs den Auslöser berührt (Kerzenhoch kreuzt nach oben für Shorts, Kerzentief kreuzt nach unten für Longs), eröffnet die Strategie eine Marktorder in Trendrichtung.
5. Schützende Stop-Loss- und Take-Profit-Orders werden automatisch über die `StartProtection`-Funktion von StockSharp angehängt und entsprechen den Abständen des ursprünglichen Skripts.

Im Gegensatz zum ursprünglichen Experten-Advisor handelt die StockSharp-Version nach dem Öffnen einer Position weiter; es ist nicht notwendig, den Versatzwert manuell zurückzusetzen. Alle Aktionen werden nur auf abgeschlossenen Kerzen durchgeführt, um Intrabar-Neuzeichnungsprobleme zu vermeiden.

## Indikatoren
- **Parabolic SAR** – treibt sowohl die Trenderkennung als auch die Einstiegsniveaus an.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `SarStep` | Anfangsbeschleunigungsfaktor für den Parabolic SAR. | `0.01` |
| `SarMax` | Maximaler Beschleunigungsfaktor für den Parabolic SAR. | `0.2` |
| `RetraceOffsetPips` | Abstand (in Pips) zwischen dem SAR-Wert und dem Einstiegsauslöser. | `30` |
| `StopLossPips` | Stop-Loss-Abstand in Pips (in absoluten Preis umgerechnet). Auf `0` setzen, um zu deaktivieren. | `30` |
| `TakeProfitPips` | Take-Profit-Abstand in Pips (in absoluten Preis umgerechnet). Auf `0` setzen, um zu deaktivieren. | `30` |
| `CandleType` | Zeitrahmen für Kerzen und Indikatorberechnungen. | `5 Minute` |

> **Hinweis:** Die Strategie schätzt die Pip-Größe aus den Sicherheitsmetadaten. Wenn das Instrument fünf Dezimalstellen verwendet (typisch für Forex), entspricht ein Pip zehn minimalen Preisschritten.

## Order-Management
- Orders werden zu Marktpreisen platziert, sobald die Rücklaufbedingung erfüllt ist.
- Die Standard-Handelsgröße beträgt ein Lot (`Volume = 1`), kann aber über die Basis-Eigenschaft `Strategy.Volume` vor dem Start der Strategie angepasst werden.
- `StartProtection` verwaltet automatisch Stop-Loss- und Take-Profit-Platzierungen unter Verwendung absoluter Preisversätze, die aus den Pip-Einstellungen abgeleitet werden.

## Verwendungstipps
- Passen Sie den Pip-Versatz, Stop und Ziel an die Volatilität des gehandelten Instruments an.
- Erwägen Sie, die Strategie mit zusätzlichen Filtern (Tageszeit, Volatilität usw.) zu kombinieren, wenn Sie sie in ein breiteres Handelsframework integrieren.
- Führen Sie immer einen Backtest durch, bevor Sie live einsetzen, da die Profitabilität stark von den Marktbedingungen und der Brokerausführung abhängt.

## Unterschiede zum Original-Skript
- Kontinuierlicher Handel ohne manuelle globale Variablen.
- Verwendet abgeschlossene Kerzen anstelle von Tick-für-Tick-Überprüfungen, was deterministisches Verhalten für Backtests bietet.
- Integriertes Risikomanagement durch StockSharp's Protective-Order-Modul.

## Haftungsausschluss
Diese Strategie wird zu Bildungszwecken bereitgestellt. Testen Sie gründlich mit historischen Daten und Demo-Daten, bevor Sie echtes Kapital einsetzen.
