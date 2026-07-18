# VR Smart Grid Lite-Mittelungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die VR Smart Grid Lite Averaging-Strategie ist ein Grid-Averaging-System, das dem ursprünglichen MetaTrader 5 Expert Advisor folgt. Der Algorithmus öffnet Marktaufträge in Richtung der letzten bullischen oder bärischen Kerze und baut eine Leiter im Martingal-Stil auf, wenn sich der Preis gegen die Position bewegt. Entfernungen, Volumina und Ausgangslogik können so angepasst werden, dass sie der ursprünglichen MQL-Implementierung entsprechen.

## Handelslogik
- Bei jeder abgeschlossenen Kerze überprüft die Strategie ihre Richtung.
  - Eine bullische Kerze ermöglicht eine neue Kauforder, wenn der aktuelle Preis mindestens `Order Step (pips)` unter dem niedrigsten bestehenden Kaufeinstieg liegt.
  - Eine bärische Kerze ermöglicht einen neuen Verkaufsauftrag, wenn der aktuelle Preis mindestens `Order Step (pips)` über dem höchsten bestehenden Verkaufseintrag liegt.
- Die erste Bestellung für jede Seite verwendet `Start Volume`. Jede weitere Ordnung verdoppelt das Volumen der am weitesten entfernten Ordnung auf dieser Seite, während `Max Volume` die absolute Größe begrenzt.
- Wenn auf einer Seite nur eine einzige Position vorhanden ist, wird der Handel geschlossen, sobald der Preis die Distanz `Take Profit (pips)` erreicht.
- Bei zwei oder mehr Positionen hängt die Abschlusslogik vom ausgewählten `Close Mode` ab:
  - **Durchschnitt** – schließt die höchsten und niedrigsten Orders, sobald der Preis ihren gewichteten Durchschnitt plus `Minimal Profit (pips)` erreicht.
  - **PartialClose** – schließt die niedrigste Order vollständig und reduziert die höchste Order um `Start Volume`, wenn der Preis das gemischte Ziel erreicht.

## Risikomanagement
- Die Volumina werden an die `MinVolume`, `MaxVolume` und `StepVolume` des Brokers angepasst, um eine Ablehnung zu vermeiden.
- Der integrierte `StartProtection()`-Aufruf stellt sicher, dass der Kontoschutz von StockSharp vor dem Handel aktiviert wird.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| `Take Profit (pips)` | Zieldistanz für einzelne offene Positionen. |
| `Start Volume` | Volumen für die Erstbestellung in jede Richtung. |
| `Max Volume` | Maximal zulässiges Volumen pro Bestellung (0 deaktiviert das Limit). |
| `Close Mode` | Wählen Sie zwischen durchschnittlichen Exits oder teilweisen Schließungen. |
| `Order Step (pips)` | Minimale Gegenbewegung vor dem Hinzufügen einer neuen Bestellung. |
| `Minimal Profit (pips)` | Zusätzlicher Gewinnpuffer zum durchschnittlichen Ausstieg hinzugefügt. |
| `Candle Type` | Kerzenserie zur Signalerzeugung. |

## Notizen
- Die Strategie verwendet nur Marktaufträge; Ausstehende Aufträge des ursprünglichen EA werden durch die Auswertung der Bedingungen für jede Kerze emuliert.
- Die Implementierung behält den Status pro Bestellung bei, um die Ticket-basierte Verwaltung von MetaTrader nachzuahmen, einschließlich teilweiser Schließungen und selektiver Ausgänge.
- Konfigurieren Sie den Kerzentyp und die Symbol-Pip-Größe so, dass sie mit dem im MQL-Skript verwendeten Zeitrahmen übereinstimmen, um ein konsistentes Verhalten zu gewährleisten.
