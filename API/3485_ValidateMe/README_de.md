# ValidateMe-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die ValidateMe-Strategie portiert das grundlegende Validierungs-Framework des ursprünglichen MQL4-Expertenberaters. Die Logik konzentriert sich darauf, die Verfügbarkeit von Geldern zu prüfen, zu überprüfen, ob die Stop-Loss- und Take-Profit-Abstände den Wechselkursbeschränkungen entsprechen, und dann eine einzelne Marktorder in die gewählte Richtung auszulösen. Die Strategie überwacht kontinuierlich Handelsausführungsereignisse und eröffnet nur dann eine neue Position, wenn keine Positionen oder aktiven Aufträge vorhanden sind.

## Handelslogik

1. Die Strategie abonniert Tickdaten des konfigurierten Wertpapiers.
2. Wenn die Strategie online ist, erstellt wurde und der Handel zulässig ist, wird überprüft, ob keine offenen Positionen und keine aktiven Aufträge vorhanden sind.
3. Anschließend wird eine Marktorder in der konfigurierten Richtung (Kauf oder Verkauf) unter Verwendung der definierten Losgröße gesendet.
4. Ein Schutzmodul fügt sofort Take-Profit- und Stop-Loss-Orders hinzu, die aus Pip-Abständen berechnet werden, und stellt so die Einhaltung der Stop-Levels des Brokers sicher (angepasst an Bruchpreise).
5. Sobald die Position geschlossen ist, wartet die Strategie auf den nächsten Tick und wiederholt die Validierung, bevor sie eine neue Order sendet.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| **Gewinnmitnahme (Pips)** | Abstand vom Einstiegspreis zum Take-Profit in Pips. Muss größer als Null sein. |
| **Stop-Loss (Pips)** | Abstand vom Einstiegspreis zum Stop-Loss in Pips. Muss größer als Null sein. |
| **Viele** | Handelsvolumen in Lots, das für jede Marktorder verwendet wird. |
| **Richtung** | Richtung der Marktorder (Kauf oder Verkauf). |

## Risikomanagement

* Die Strategie verwendet `StartProtection` mit absoluten Offsets, um sowohl Take-Profit- als auch Stop-Loss-Orders zu registrieren.
* Die Pip-Größe wird aus dem Wertpapierpreisschritt und der Dezimalgenauigkeit berechnet, um das MetaTrader-Verhalten nachzuahmen (5- und 3-stellige Symbole verwenden eine zehnfache Punktgröße).
* Die Strategie löst neue Aufträge nur dann aus, wenn keine bestehenden Aufträge aktiv sind, wodurch eine Stapelung von Aufträgen vermieden wird.

## Nutzungshinweise

* Befestigen Sie die Strategie an einem Wertpapier und stellen Sie das gewünschte Volumen und die gewünschte Richtung ein.
* Konfigurieren Sie die Take-Profit- und Stop-Loss-Abstände in Pips entsprechend den Anforderungen des Brokers.
* Die Strategie basiert nicht auf Indikatoren und ist als Validierungsrahmen und nicht als vollständiges Handelssystem gedacht.
* Die Portfolio-Risikokontrolle (z. B. Max Drawdown) kann bei Bedarf extern kombiniert werden.
