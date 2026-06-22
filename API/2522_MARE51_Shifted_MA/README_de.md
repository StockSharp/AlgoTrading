# MARE5.1 Verschobener Gleitender Durchschnitt Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **MARE5.1 Verschobener Gleitender Durchschnitt Strategie** ist ein direkter Port des originalen MetaTrader 5 Expertenberaters "MARE5.1" in die StockSharp High-Level-API. Das System überwacht Einminuten-Kerzen (konfigurierbar) und vergleicht zwei einfache gleitende Durchschnitte (SMA), die einen konfigurierbaren Vorwärts-Shift teilen. Die Logik sucht nach Kreuzungsmustern, die durch historische SMA-Beziehungen und die Richtung der letzten abgeschlossenen Kerze bestätigt werden.

## Handelslogik

- Die Strategie verwendet zwei SMAs: einen schnellen SMA und einen langsamen SMA. Beide werden um dieselbe Anzahl von Bars vorwärts verschoben, was das Verhalten des ursprünglichen Expertenberaters repliziert.
- Eine **Short-Position** wird eröffnet, wenn alle folgenden Bedingungen erfüllt sind:
  1. Der langsame SMA liegt auf der aktuellen Kerze mindestens einen Preisschritt über dem schnellen SMA.
  2. Zwei Kerzen zurück lag der schnelle SMA mindestens einen Preisschritt über dem langsamen SMA.
  3. Fünf Kerzen zurück lag der schnelle SMA mindestens einen Preisschritt über dem langsamen SMA.
  4. Die zuletzt abgeschlossene Kerze (vorheriger Bar) ist bärisch.
- Eine **Long-Position** wird eröffnet, wenn das entgegengesetzte Muster auftritt:
  1. Der schnelle SMA liegt auf der aktuellen Kerze mindestens einen Preisschritt über dem langsamen SMA.
  2. Zwei Kerzen zurück lag der langsame SMA mindestens einen Preisschritt über dem schnellen SMA.
  3. Fünf Kerzen zurück lag der langsame SMA mindestens einen Preisschritt über dem schnellen SMA.
  4. Die zuletzt abgeschlossene Kerze (vorheriger Bar) ist bullisch.
- Es kann jeweils nur eine Position offen sein. Die Standard-Ordergröße kommt vom `TradeVolume`-Parameter.
- Das Trading ist nur zwischen den konfigurierten Session-Stunden (einschließlich) erlaubt. Dieses Fenster repliziert den stundenbasierten Filter des ursprünglichen Expertenberaters.

## Risikomanagement

Die Strategie spiegelt die ursprünglichen festen Take-Profit- und Stop-Loss-Abstände wider. Sie werden in "Pips" (Punkte angepasst für Drei- und Fünf-Digit-Instrumente) definiert und beim Start der Strategie in absolute Preiseinheiten umgewandelt. Schutzorders werden durch `StartProtection` mit Marktorder-Ausstiegen verwaltet.

## Indikatoren und Daten

- **Schneller SMA** – Länge definiert durch `FastPeriod`.
- **Langsamer SMA** – Länge definiert durch `SlowPeriod`.
- **Datenquelle** – standardmäßig Einminuten-Kerzen, aber jeder von StockSharp unterstützte Kerzentyp kann über den `CandleType`-Parameter ausgewählt werden.

## Parameter

| Name | Standardwert | Beschreibung |
|------|--------------|--------------|
| `TradeVolume` | 0.01 | Ordervolumen für Einstiege. |
| `TakeProfitPips` | 35 | Take-Profit-Abstand in angepassten Pips. Auf null setzen zum Deaktivieren. |
| `StopLossPips` | 55 | Stop-Loss-Abstand in angepassten Pips. Auf null setzen zum Deaktivieren. |
| `FastPeriod` | 14 | Periode des schnellen SMA. |
| `SlowPeriod` | 79 | Periode des langsamen SMA. |
| `MovingAverageShift` | 4 | Vorwärts-Shift (in Bars), der auf beide SMAs angewendet wird. |
| `SessionOpenHour` | 2 | Beginn des erlaubten Handelsfensters (0–23, einschließlich). |
| `SessionCloseHour` | 3 | Ende des erlaubten Handelsfensters (0–23, einschließlich). Muss größer als `SessionOpenHour` sein. |
| `CandleType` | 1-Minuten-Kerzen | Kerzendatentyp der Strategie. |

## Hinweise

- Signale werden auf abgeschlossenen Kerzen ausgewertet. Historische SMA-Werte werden intern gepuffert, um die indexbasierten Vergleiche aus dem originalen MQL-Code zu replizieren.
- Der Preisschrittswert des aktiven Wertpapiers wird beim Vergleich von SMA-Unterschieden verwendet, um sicherzustellen, dass der erforderliche Abstand mindestens einem Tick entspricht.
- Stop-Loss- und Take-Profit-Niveaus basieren auf dem Wertpapier-Preisschritt. Bei Drei- und Fünf-Dezimal-Instrumenten wird die Pip-Größe automatisch um das Zehnfache erhöht, was dem MetaTrader-Verhalten entspricht.
- Kein automatisches Positionsskalierung ist implementiert. Die Strategie wartet, bis alle offenen Positionen geschlossen sind, bevor sie nach dem nächsten Eintrittssignal sucht.
- Dieses Repository enthält nur die C#-Implementierung; es gibt keinen Python-Port für diese Strategie.
