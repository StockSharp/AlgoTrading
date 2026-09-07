# Diagramm einer Limit-Grid-Straddle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm beginnt jeden Grid-Zyklus mit zwei symmetrischen Limit-Orders um die letzte abgeschlossene Fünf-Minuten-Kerze. Die Ausführung einer Startorder plant eine zusätzliche Stufe in derselben Richtung, der absolute Take-Profit-Schutz schließt die entstandene Position und eine verzögerte Massenstornierung entfernt die verbleibenden Limits.

![schema](schema.svg)

## Strategieübersicht

- Bei einer neutralen Position wird ein Kauflimit 100 Preiseinheiten unter und ein Verkaufslimit 100 Einheiten über dem Kerzenschluss platziert.
- Wird eine der Startorders ausgeführt, bleibt die gegenüberliegende Startorder aktiv und eine gleichgerichtete Grid-Stufe wird für die nächste abgeschlossene Kerze geplant.
- Die zusätzliche Kaufstufe liegt 350 Einheiten unter ihrer Startausführung; die zusätzliche Verkaufsstufe liegt 350 Einheiten darüber.
- Jede Start- oder Grid-Ausführung geht an Position protection mit einem absoluten Take-Profit-Abstand von 300 und ohne Stop-Loss.
- Ein Schutzausstieg plant die Massenstornierung für die nächste abgeschlossene Kerze. Deren Bestätigung gibt den nächsten Grid-Zyklus frei.

## Einstiegs- und Ausstiegsregeln

- **Kaufseite**: Zu Beginn eines Zyklus mit neutraler Position ein Kauflimit bei `Close - Start Offset` registrieren. Nach seiner Ausführung auf der nächsten abgeschlossenen Kerze einen weiteren Kauf bei `Average Fill Price - Grid Distance - Step Distance` registrieren.
- **Verkaufsseite**: Zu Beginn eines Zyklus mit neutraler Position ein Verkaufslimit bei `Close + Start Offset` registrieren. Nach seiner Ausführung auf der nächsten abgeschlossenen Kerze einen weiteren Verkauf bei `Average Fill Price + Grid Distance + Step Distance` registrieren.
- **Offene Orders**: Eine Startausführung storniert das gegenüberliegende Limit nicht. Verbleibende Start- und Grid-Limits bleiben bis zur Massenstornierung aktiv.
- **Ausstieg**: Position protection sendet eine Market-Order zum Schließen, wenn der Kerzenschluss ein Ziel 300 Preiseinheiten von einer geschützten Ausführung entfernt erreicht. Es ist keine Stop-Loss-Grenze aktiviert.

## Parameter

| Parameter | Standardwert | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der abgeschlossenen Kerzen, die den Zyklus steuern. |
| Start Offset | 100 | Abstand zwischen Kerzenschluss und jedem Startlimit in Preiseinheiten. |
| Grid Distance | 300 | Basisabstand von einer Startausführung zur zusätzlichen Stufe derselben Seite. |
| Step Distance | 50 | Zuschlag zu Grid Distance für die zusätzliche Stufe. |
| Take Profit | 300 | Absoluter Abstand von einer geschützten Ausführung zu ihrem Gewinnziel. |
| Stop Loss | 0 | Absoluter Stop-Abstand; null deaktiviert die Stop-Loss-Grenze. |
| Trailing Stop Loss | false | Lässt die Nachführung des Stop-Loss deaktiviert. |
| Use Market Orders | true | Sendet Schutzausstiege als Market-Orders. |
| Volume | 1 | Volumen jeder Start- und Grid-Limit-Order. |

## Diagrammdetails

- Die Position wird mit jeder abgeschlossenen Kerze abgetastet und mit null verglichen. Ein Flag-Block erlaubt nur ein symmetrisches Startpaar pro Grid-Zyklus.
- Vier Order-registering-Blöcke senden Kauf- und Verkaufslimits für Start und Grid. Zwischen den beiden Startorders gibt es keine gezielten Stornierungsblöcke.
- Eine Startausführung wird in einem Variable-Block gespeichert. Ein Delay über zwei Ereignisse verbraucht die auslösende Kerze und gibt den gespeicherten Trade mit der folgenden abgeschlossenen Kerze frei, sodass die neue Registrierung außerhalb des Ausführungs-Callbacks erfolgt.
- Der gespeicherte Trade wird über `Order.AveragePrice` konvertiert; anschließend wenden Formula-Blöcke `Grid Distance + Step Distance` an, was mit den Standardwerten 350 ergibt.
- Position protection behandelt jede eingehende Ausführung einzeln. Dieses begrenzte Beispiel berechnet kein gemeinsames volumengewichtetes Ziel für mehrere Grid-Ausführungen.
- Eine Schutzausführung aktiviert ein weiteres Delay über zwei Ereignisse. Dessen Ausgang fordert Order mass cancellation an; nur ein erfolgreicher Rückgabewert setzt das Zyklus-Flag zurück.
- Das Chart zeigt Fünf-Minuten-Kerzen, alle vier Orderströme sowie jede Ausführung und jeden Ausstieg der Strategie.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, führen Sie das Diagramm im Tester mit historischen Daten aus und passen Sie Abstände und Volumen vor dem Live-Handel an Preisskala und Volatilität des Instruments an.
