# Ausstehende Limit-Grid-Strategie (MQL/8147 Conversion)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Pending Limit Grid Strategy** reproduziert das Verhalten des MetaTrader-Experten
gespeichert in `MQL/8147`. Die Strategie baut ein symmetrisches Raster aus ausstehenden Limit-Orders auf
um die aktuellen Geld-/Briefkurse herum. Es hält das Netz aktiv, während der Gewinn schwankt
bleibt innerhalb eines konfigurierten Gewinnziels und einer Drawdown-Schwelle. Wenn einer der
Schwellenwerte werden überschritten, alle Aufträge werden storniert, offene Positionen werden abgeflacht und
Das Raster wird unter Verwendung des neuen Kontokapitals als Basis neu aufgebaut.

## Handelslogik

1. Abonnieren Sie Level-One-Daten, um die besten Geld- und Briefkurse zu verfolgen.
2. Erfassen Sie das Kontoguthaben beim ersten Empfang von Live-Daten und speichern Sie es als
die Sitzungsbasislinie.
3. Platzieren Sie `LevelsPerSide` Verkaufslimits über dem Markt und die gleiche Anzahl an Käufen
Grenzen unterhalb des Marktes. Der Abstand zwischen den Gitterebenen wird durch gesteuert
`GridStepPoints` in den Instrumentenpreisschritt umgerechnet.
4. Halten Sie die ausstehenden Aufträge zurück, ohne neue Aufträge erneut auszugeben, wenn sie ausgeführt werden. Die
Das Raster wird erst nach einem vollständigen Reset neu erstellt.
5. Floating PnL kontinuierlich überwachen:
   - Wenn der Gewinn `ProfitTargetCurrency` erreicht, alle Engagements schließen und zurücksetzen.
   - Wenn der Drawdown `MaxDrawdownCurrency` überschreitet, glätten Sie das Buch und setzen Sie es zurück.
6. Nach jedem Zurücksetzen wird das Grundkapital erneut erfasst und das Raster neu aufgebaut
unter Verwendung des aktuellsten Bid/Ask-Snapshots.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `ProfitTargetCurrency` | Nettogewinn (in Kontowährung), der einen vollständigen Reset des Rasters auslöst. |
| `MaxDrawdownCurrency` | Maximal tolerierter Floating-Verlust, bevor die gesamte Exposition geschlossen wird. |
| `GridStepPoints` | Abstand zwischen aufeinanderfolgenden Rasterebenen, ausgedrückt in Brokerpunkten. |
| `LevelsPerSide` | Anzahl der ausstehenden Aufträge, die über und unter dem Marktwert erstellt wurden. |
| `OrderVolume` | Das jeder ausstehenden Limit-Order zugewiesene Volumen. |

## Risikomanagement

Die Strategie sieht keine Stopps oder Ziele pro Order vor. Stattdessen überwacht es die
aggregierter Gewinn und Verlust. Der `RequestFlatten`-Helfer storniert ausstehende Orders und
verwendet Marktaufträge (über `ClosePosition`), um alle offenen Positionen zu entfernen. Nach dem
Wenn die Abflachung abgeschlossen ist, werden der Gitterstatus und die Grundliniengerechtigkeit vor dem Platzieren zurückgesetzt
neue Aufträge.

## Notizen

- Die Preise werden bis `Security.ShrinkPrice` normalisiert, um den Umtausch zu berücksichtigen
Preisschritt.
- Der „Punkt“-Wert MetaTrader wird durch Analyse des Instruments `PriceStep` emuliert.
um vier- und fünfstellige Anführungszeichen abzugleichen.
- Die Strategie vermeidet das erneute Senden von Grid-Bestellungen, sobald sie platziert wurden, und ahmt dies nach
ursprünglicher Experte, der sich auf Flag-Variablen stützte, um jedes Level bis dahin einzigartig zu halten
Es erfolgt ein manueller oder automatischer Reset.
