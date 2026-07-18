# Strategie Status-Mail und Benachrichtigung bei Orderabschluss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie überwacht das Konto und meldet wichtige Ereignisse:

- Sendet täglich eine Statusbenachrichtigung zu einer festgelegten Minute.
- Meldet jede geschlossene Order mit grundlegenden Trade-Informationen.

Sie basiert auf dem MQL-Experten *StatusMailandAlertOnOrderClose.mq4* und zeigt, wie Benachrichtigungen in StockSharp gehandhabt werden.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `SendReportEmail` | Tägliche Statusbenachrichtigung aktivieren. |
| `StatusEmailMinute` | Minute der Stunde zum Senden der Statusnachricht. |
| `SendClosedEmail` | Benachrichtigungen beim Schließen von Orders aktivieren. |
| `StartBalance` | Anfangssaldo des Kontos für die Gewinnberechnung. |
| `CandleType` | Zeitrahmen zur Uhrzeitprüfung. Normalerweise auf 1 Minute eingestellt. |

## Logik

1. Kerzen des gewählten Zeitrahmens abonnieren.
2. Wenn eine Kerze endet, prüfen ob es die angegebene Minute ist und eine Berichtsnachricht senden.
3. Bei jedem neuen Trade benachrichtigen, wenn eine Order geschlossen wurde.

Diese Nachrichten werden über `AddInfo` protokolliert, können aber durch einen beliebigen Benachrichtigungsmechanismus ersetzt werden.
