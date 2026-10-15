# Lilith Goes To Hollywood-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie stellt das Verhalten der MetaTrader-Expertin „Lilith geht nach Hollywood“ innerhalb der StockSharp hohen Ebene API nach. Es implementiert ein Absicherungsnetz, das in zwei sehr unterschiedlichen Modi arbeiten kann:

* **Automatisierter Modus** – Parabolic SAR löst sofortige Markteintritte aus, sobald der Preis den Stop-and-Reverse-Wert überschreitet.
* **Manueller Modus** – Ausstehende Stop-/Limit-Orders werden um benutzerdefinierte Referenzpreise herum geparkt und zur Ausführung überlassen.

In beiden Fällen verfolgt die Strategie das Long- und Short-Engagement getrennt, berechnet den schwebenden PnL des offenen Netzes und nutzt diese Informationen, um zu entscheiden, wann zusätzliche Wiederherstellungsanordnungen eingesetzt werden sollen.

## Betriebsarten
* **Automatisiert** – Wenn keine Position offen ist, abonniert die Strategie den Indikator Parabolic SAR (0,02/0,2 Faktoren). Liegt der Schlusskurs der Kerze über dem Indikator, wird zum Marktwert gekauft, liegt er darunter, wird verkauft. Der ausgeführte Preis wird zum neuen **Fokus** und Erholungsstopps werden in einem konfigurierbaren Ankerabstand um ihn herum aktiviert.
* **Manuell** – Wenn keine Position offen ist, übermittelt die Strategie eine einzelne ausstehende Order pro Seite. Wenn der Markt unterhalb des Kaufniveaus handelt, wird ein Kaufstopp erstellt, andernfalls wird ein Kauflimit übermittelt. Die Verkaufsseite spiegelt die gleiche Logik rund um das `PriceDown`-Niveau wider. Sobald eine der Orders ausgeführt wird, bleibt die andere Seite aktiv, bis sie manuell oder durch die Strategie storniert wird.

## Logik der Auftragsverwaltung
* Das Raster führt weiterhin die Gesamtsumme der erfüllten Long-/Short-Volumina und ausstehenden Kauf-/Verkaufsaufträge aus. Dies ermöglicht es der Strategie, Ungleichgewichte zwischen beiden Seiten des Buches zu messen.
* Immer wenn der variable Gewinn das dynamische Ziel (`account value / 1000`) erreicht, schließt die Strategie jede Position und storniert alle ausstehenden Aufträge.
* Wenn der variable PnL unter `-AccountValue * RiskPercent / 100` fällt, wird eine Notfallabsicherung eingesetzt, indem Marktaufträge eröffnet werden, die den Netto-Short- oder Long-Überschuss abdecken.
* Wiederherstellungsaufträge werden als Stop-Orders ausgedrückt, die um den Fokuspreis (automatischer Modus) oder um die konfigurierten manuellen Preise herum platziert werden. Ihre Größe wird als `(opposite exposure * XFactor) - current exposure` berechnet und ahmt die MT4-Logik nach, die nächste Bestellung zu überdimensionieren, um das Raster wieder auszugleichen.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `Automated` | Ermöglicht Parabolic SAR gesteuerte Markteintritte. Deaktivieren Sie die Funktion, um im manuellen Pending-Order-Modus zu arbeiten. |
| `PriceUp` | Referenzpreis, der zum Erstellen von Kauf-Stop/Limit-Orders im manuellen Modus verwendet wird. |
| `PriceDown` | Referenzpreis, der zum Erstellen von Verkaufs-Stopp-/Limit-Orders im manuellen Modus verwendet wird. |
| `AnchorSteps` | Abstand, ausgedrückt in Preisschritten, der zum Ausgleich von Wiederherstellungsaufträgen vom Fokuspreis verwendet wird. |
| `ManualVolume` | Basislosgröße bei manueller Bedienung oder wenn die dynamische Positionsgrößenbestimmung Null ergibt. |
| `XFactor` | Bei der Dimensionierung von Einziehungsanordnungen wird ein Multiplikator auf das gegnerische Risiko angewendet. |
| `RiskPercent` | Maximaler variabler Verlust (Prozentsatz des Kontowerts), der toleriert wird, bevor die Strategie eine Notfallabsicherung einsetzt. |
| `CandleType` | Zeitrahmen, der zur Steuerung der Parabolic SAR und der allgemeinen Verwaltungslogik verwendet wird. |

## Risk controls
* Die Gewinnmitnahme ist dynamisch und skaliert mit dem Kontowert, sodass das Ziel automatisch erhöht werden kann, wenn das Konto wächst.
* Eine Notfallabsicherung kann extreme Verluste neutralisieren, indem die am stärksten gefährdete Seite des Netzes abgeflacht wird, sobald der schwebende Verlust den Schwellenwert `RiskPercent` überschreitet.
* Alle ausstehenden Aufträge werden auf die Tick-Größe des Instruments gerundet und die Volumina werden angepasst, um die Wechselkurslimits einzuhalten, die den typischen Schutzmaßnahmen des ursprünglichen MetaTrader-Experten entsprechen.

## Konvertierungshinweise
* MetaTrader Häkchen werden durch fertige Kerzen ersetzt. Der standardmäßige Zeitrahmen von einer Minute hält die Strategie reaktiv, kann jedoch über den Parameter `CandleType` angepasst werden.
* Die Einstellung `Anchor` aus der Quelle MQL drückte die Entfernung in Punkten aus. Hier ist es in mehreren Preisschritten konfiguriert, sodass es sich automatisch an die Tick-Größe des Instruments anpasst.
* Die ursprüngliche „Kommentar“-Ausgabe wurde in Strategieprotokollmeldungen (`LogInfo`) umgewandelt, sodass das Plattformjournal dasselbe Feedback enthält, ohne auf Diagrammanmerkungen angewiesen zu sein.
