# Strategie zur Volumenberechnung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Volume Calculator Strategy** reproduziert die Logik des ursprünglichen MetaTrader-Expertenberaters, der ein empfohlenes Handelsvolumen basierend auf Stop-Loss- und Take-Profit-Niveaus berechnet. Wenn die Strategie startet, liest sie die konfigurierten Stop-Preise, bewertet den aktuellen Marktpreis des ausgewählten Wertpapiers und leitet die Risikokennzahlen anhand des verfügbaren Portfoliokapitals ab.

Die Strategie erteilt keine Aufträge. Sein einziger Zweck besteht darin, detaillierte Statistiken zur Geldverwaltung im Protokoll bereitzustellen und die berechneten Werte über schreibgeschützte Eigenschaften offenzulegen. Dies macht es für manuelle Händler nützlich, die die Regeln zur Positionsgröße validieren möchten, bevor sie einen Handel senden.

## Parameter
- **Stop-Loss-Preis** – absolutes Preisniveau des Schutzstopps, der für die geplante Position verwendet wird.
- **Take-Profit-Preis** – absolutes Preisniveau des Take-Profit-Ziels.
- **Max. Verlust %** – maximaler Anteil des Portfoliowerts, der bei einem einzelnen Trade riskiert werden kann. Die Strategie multipliziert diesen Prozentsatz mit dem Portfolioeigenkapital, um den maximal akzeptablen Währungsverlust zu erhalten.
- **Ist Long-Position** – bestimmt, ob die geplante Position Long (`true`) oder Short (`false`) ist. Die Richtung ist erforderlich, um den Abstand zwischen dem aktuellen Preis und den Stop-/Zielniveaus zu berechnen.

Alle Parameter außer *Max Loss %* sind von der Optimierung ausgeschlossen, um sie bei rein manuellen Eingaben zu belassen und das Verhalten des ursprünglichen Experten widerzuspiegeln.

## Berechnungsdetails
1. **Portfoliowert** – die Strategie ruft `Portfolio.CurrentValue` ab (und greift auf `Portfolio.BeginValue` zurück), um das verfügbare Kapital zu schätzen. Wenn der Wert nicht angegeben wird, wird die Berechnung mit einer Warnung abgebrochen.
2. **Preisstufenvalidierung** – die Werte `Security.PriceStep` und `Security.StepPrice` müssen definiert werden, da sie Preisabstände in Vertragsstufen und Barbeträge umwandeln. Fehlende Metadaten verhindern die Berechnung.
3. **Aktuelle Preiserkennung** – die Strategie sucht nach dem letzten Handelspreis. Wenn es nicht verfügbar ist, nähert es sich dem Preis an, indem es die besten Geld-/Briefkurse mittelt und schließlich auf den letzten bekannten Preis zurückgreift.
4. **Abstand in Schritten** – Sowohl die Stop-Loss- als auch die Take-Profit-Abstand werden in Preisschritten gemessen. Die Abstände werden aufgerundet (`decimal.Ceiling`), um konservativ zu bleiben, genauso wie das MetaTrader-Skript auf `MathCeil` basiert.
5. **Geld in Gefahr** – der maximal akzeptable Verlust beträgt `PortfolioValue * MaxLoss% / 100`.
6. **Empfohlenes Volumen** – Verlust pro Schritt beträgt `MaxLoss / StopSteps`. Die Division dieses Werts durch `StepPrice` ergibt das Positionsvolumen, das den Verlust unter Kontrolle hält.
7. **Erwarteter Gewinn** – Multiplikation der Take-Profit-Schritte mit `StepPrice` und dem vorgeschlagenen Volumen ergibt den prognostizierten Cash-Gewinn, wenn das Ziel erreicht wird.
8. **Risiko-Ertrags-Verhältnis** – Verhältnis zwischen Take-Profit- und Stop-Loss-Schrittzahlen, äquivalent zur ursprünglichen Pip-basierten Berechnung.

Jeder berechnete Wert wird in der Strategie gespeichert und mit informativen englischen Meldungen im Protokoll ausgedruckt. Wenn das Risiko-Ertrags-Verhältnis größer oder gleich 3 ist, signalisiert die Strategie „Sie können handeln“; Andernfalls wird eine Warnung ausgegeben, dass der Handel zu riskant ist.

## Nutzungsworkflow
1. Hängen Sie die Strategie an das gewünschte Wertpapier und Portfolio in der StockSharp-Umgebung an.
2. Konfigurieren Sie die Stop-Loss- und Take-Profit-Preise, die dem geplanten manuellen Handel entsprechen.
3. Legen Sie den akzeptablen Risikoprozentsatz und die beabsichtigte Richtung fest.
4. Starten Sie die Strategie – die Ausgabe mit allen Metriken erscheint sofort im Protokoll.
5. Überprüfen Sie das vorgeschlagene Volumen und das Risiko-Ertrags-Verhältnis, bevor Sie den Handel manuell ausführen.

## Notizen
- Wenn eines der erforderlichen Sicherheitsmetadatenfelder (Preisschritt oder Stufenpreis) fehlt, fordern Sie es bei der Börse an oder passen Sie die Sicherheitseinstellungen manuell an.
- Die Berechnung ist statisch; Es wird nach dem Start nicht automatisch aktualisiert. Starten Sie die Strategie neu, wenn sich die Marktbedingungen oder Risikoparameter ändern.
- Da die Strategie keine Aufträge sendet, kann sie sowohl in Backtesting- als auch in Live-Umgebungen ausschließlich zu Analysezwecken ausgeführt werden.
