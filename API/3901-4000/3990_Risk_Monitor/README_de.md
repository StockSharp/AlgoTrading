# Risikoüberwachungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Risk Monitor Strategy ist eine Portierung des MetaTrader 4 Expert Advisors `risk.mq4`. Das Originalskript eröffnete nie Trades; stattdessen es
ermittelte anhand des Kontostands und eines benutzerdefinierten Risikoprozentsatzes, wie viele Lots der Händler sicher einsetzen konnte. Dies
Die StockSharp-Version behält den gleichen Geist bei: Sie führt eine kontinuierliche Kontodiagnose durch, berechnet empfohlene Handelsgrößen und überwacht
variable und realisierte Gewinne und veröffentlicht die Ergebnisse direkt im Strategiekommentar für eine schnelle Entscheidungsfindung.

Im Gegensatz zu herkömmlichen Strategien versendet die Risk Monitor Strategy keine Aufträge automatisch. Seine Rolle ist die Aufsicht: Es gibt die
Händler eine Momentaufnahme des aktuellen Engagements, der verfügbaren Kapazität gemäß dem gewählten Risikobudget und der Rentabilität der geschlossenen Geschäfte
Positionen. Die Kommentarzeile wird jedes Mal aktualisiert, wenn sich Positionen, PnL oder Trades ändern, sodass die Informationen immer auf dem neuesten Stand sind
Portfoliostatus.

## Berechnungen
Die Strategie leitet die im Kommentar angezeigten Zahlen aus drei Datengruppen ab:

1. **Grundlosgröße** – berechnet als `AccountBalance / 1000` und abgestimmt auf den Sicherheitsvolumenschritt. Dies spiegelt das Original wider
MT4-Logik, bei der alle 1000 Guthabeneinheiten einem Standardlos entsprechen.
2. **Risikolosgröße** – multipliziert die Basislose mit `Risk % / 100`, richtet das Ergebnis auf den Volumenschritt aus und gibt an, wie viele
Lose können unter Einhaltung des konfigurierten Risikobudgets eröffnet werden.
3. **Offene Lots & Differenz** – vergleicht die absolute Nettoposition mit der Risikolosgröße. Liegt der Händler unter dem Schwellenwert,
Die Differenz zeigt an, wie viele Lose noch verfügbar sind, bevor das Limit erreicht wird. Eine winzige negative Differenz, die kleiner ist als
Der Lautstärkeschritt wird auf Null gerundet, um verwirrendes Rauschen zu vermeiden.

Bei Gewinnen unterscheidet die Strategie zwischen variablen und realisierten Werten:

* **Floating PnL** – aus der Strategieeigenschaft `PnL` gelesen und sowohl in Preiseinheiten als auch als Prozentsatz des aktuellen Wertes ausgedrückt
Portfoliowert.
* **Realisierter Gewinn** – angesammelt aus eigenen Geschäften. Die Komponente teilt jede abschließende Füllung in positive und negative Teile auf,
wendet die gemeldete Provision an und führt eine laufende Summe. Die endgültige Zahl wird auch in einen Prozentsatz des Eigenkapitals umgerechnet
mit der MT4-Anzeige übereinstimmen.

## Parameter
* **Risiko %** – Teil des Kontostands, der für neue Positionen reserviert werden kann. Standard: `10`. Der Parameter ist verfügbar für
Optimierung, sodass unterschiedliche Risikobudgets schnell rückgetestet werden können.

## Kommentarformat
Die Strategie aktualisiert den Kommentar mit drei Zeilen:

1. `Base lots`, `Risk lots`, `Open lots`, `Lots to adjust` – Schnellansicht der Positionsgrößenmetriken.
2. `Risk`, `Floating PnL` – Risikoeinstellung, variabler Gewinn in Währungseinheiten und variabler Gewinn in Prozent des Saldos.
3. `Realized profit` – kumulierter geschlossener Gewinn und sein Prozentsatz.

Alle Werte werden ähnlich wie beim MT4-Skript gerundet, wobei der Sicherheitslosschritt berücksichtigt und zwei Dezimalstellen für den Geldwert verwendet werden
Zahlen. Da sich die Ausgabe im Kommentar befindet, ist sie sofort im Diagramm oder im Strategieraster sichtbar, ohne dass sie geöffnet werden muss
zusätzliche Panels.

## Nutzungshinweise
* Hängen Sie die Strategie an das Instrument an, dessen Balance und Position Sie überwachen möchten. Es funktioniert mit Nettopositionen (kein MT4-Stil).
Absicherung) genau wie StockSharp selbst.
* Die Strategie toleriert den manuellen Handel: Sie reagiert auf alle Handelsbestätigungen, um die Statistiken synchron zu halten.
* Der Kommentar wird automatisch gelöscht, wenn die Strategie beendet oder zurückgesetzt wird, wodurch verhindert wird, dass veraltete Werte über Sitzungen hinweg bestehen bleiben.
* Es wird keine Python-Implementierung bereitgestellt. Das Paket API enthält nur die C#-Version.
