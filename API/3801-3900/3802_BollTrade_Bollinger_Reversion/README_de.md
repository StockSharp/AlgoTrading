# BollTrade Bollinger Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **BollTrade Bollinger-Reversionsstrategie** ist eine hochrangige StockSharp-Strategie, die vom klassischen BollTrade MetaTrader-Expertenberater abgeleitet wurde. Es handelt ein einzelnes Instrument mithilfe von Bollinger-Bändern und wartet auf Preisausschläge über die Bänder hinaus sowie einen zusätzlichen Pip-Puffer. Wenn eine Kerze über dem oberen Band schließt, eröffnet die Strategie eine Short-Position, und wenn eine Kerze unter dem unteren Band schließt, eröffnet sie eine Long-Position. Alle Entscheidungen werden anhand fertiger Kerzen getroffen, um eine Reaktion auf unvollständige Daten zu vermeiden.

## Handelslogik

1. Abonnieren Sie den konfigurierten Kerzentyp und berechnen Sie Bollinger Bänder mit der ausgewählten Periode und Abweichung.
2. Berechnen Sie einen zusätzlichen Preisversatz, ausgedrückt in Pip-Einheiten, um den ursprünglichen Puffer nachzuahmen, der die Geschäfte tiefer in den überkauften/überverkauften Bereich zwang.
3. Wenn der Schlusskurs einer abgeschlossenen Kerze unter dem unteren Band abzüglich des Offsets liegt, eröffnen Sie eine Long-Position. Wenn es über dem oberen Band plus dem Offset liegt, eröffnen Sie eine Short-Position.
4. Für jeden eröffneten Trade speichert die Strategie Stop-Loss- und Take-Profit-Level, die in Pip-Einheiten definiert sind. Diese Exits emulieren den ursprünglichen Expertenberater, der Positionen schloss, wenn der schwankende Gewinn oder Verlust vordefinierte Pip-Abstände überschritt.
5. Positionen werden geschlossen, wenn die Kerzenspanne entweder die Stop-Loss- oder Take-Profit-Schwelle überschreitet. Es wird keine zusätzliche Skalierung oder Pyramidenbildung durchgeführt.

## Money-Management

* Der Parameter `Lots` definiert die Basispositionsgröße.
* Wenn `LotIncrease` aktiviert ist, skaliert das Volumen proportional zum aktuellen Portfoliowert im Verhältnis zum zu Beginn der Strategie beobachteten Wert, bis zu einer Sicherheitsobergrenze von 500 Lots. Dies reproduziert die Balance-Linked-Sizing-Logik aus der MetaTrader-Version.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| **Gewinnmitnahme (Pips)** | Abstand in Pips, der zur Berechnung des Take-Profit-Levels aus dem Einstiegspreis verwendet wird. Auf Null setzen, um den Take-Profit-Exit zu deaktivieren. |
| **Stop-Loss (Pips)** | Abstand in Pips, der zur Berechnung des Stop-Loss-Levels aus dem Einstiegspreis verwendet wird. Auf Null setzen, um den Stop-Loss-Ausstieg zu deaktivieren. |
| **Bandversatz** | Zusätzlicher Pip-Abstand, der über das Bollinger-Band hinaus hinzugefügt wird, bevor ein Handel eröffnet wird. |
| **Bollinger Zeitraum** | Anzahl der Kerzen, die für den gleitenden Durchschnitt der Bollinger-Bänder verwendet werden. |
| **Bollinger Abweichung** | Standardabweichungsmultiplikator für die Bandbreite Bollinger. |
| **Grundvolumen** | Basishandelsvolumen in Lots. |
| **Volumen skalieren** | Wenn diese Option aktiviert ist, erhöht sich das Auftragsvolumen basierend auf dem Wachstum des Portfoliowerts. |
| **Kerzentyp** | Kerzentyp (Zeitrahmen), der zur Signalgenerierung verwendet wird. |

## Notizen

* Die Strategie funktioniert nur mit fertigen Kerzen und benötigt daher historische Daten zum Aufwärmen vor dem Live-Handel.
* Stop-Loss- und Take-Profit-Niveaus werden anhand von Kerzenbereichen bewertet, was der ursprünglichen Tick-basierten Logik nahekommt und gleichzeitig mit dem hohen Niveau API kompatibel bleibt.
* Schutzfunktionen aus dem StockSharp-Framework (`StartProtection`) sind aktiviert, um vor versehentlicher Positionsfreilegung zu schützen, wenn die Strategie unerwartet stoppt.
