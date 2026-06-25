# Renko-Niveau-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Renko-Niveau-Strategie ist eine originalgetreue Konvertierung des MetaTrader 5-Expertenberaters "Renko Level EA". Sie rekonstruiert die indikatorgesteuerte Logik innerhalb von StockSharp und handelt, wenn das gerundete Renko-Niveau auf einen neuen Block springt. Die Strategie interpretiert jede Niveau-Verschiebung als Ausbruch aus dem synthetischen Renko-Stein und tritt in die Ausbruchsrichtung ein oder in die entgegengesetzte Richtung, wenn der Umkehrmodus aktiviert ist.

Das System verwendet reguläre zeitbasierte Kerzen (standardmäßig 1 Minute) nur als Datenzuführung. Kerzenschlusskurse werden auf eine konfigurierbare Blockgröße gerundet, die Renko-Steine emuliert, ohne dass Renko-Datenabonnements erforderlich sind. Jedes Mal, wenn sich der gerundete Block ändert, schließt die Strategie alle entgegengesetzten Engagements und öffnet eine neue Position, die mit der erkannten Bewegung übereinstimmt.

## Handelslogik
1. **Initialisierung**
   - Pip-Größe vom Instrument ermitteln (`PriceStep`).
   - Den Parameter `Block Size` von Pips in Preiseinheiten umrechnen (3-stellige und 5-stellige Instrumente multiplizieren den Pip-Wert automatisch mit 10).
   - Den Schlusskurs der ersten abgeschlossenen Kerze auf den nächsten Block runden, um die anfänglichen oberen und unteren Renko-Niveaus zu erstellen.
2. **Niveau-Pflege**
   - Bei jeder abgeschlossenen Kerze wird der Schlusskurs auf die nächste Blockgröße gerundet.
   - Wenn der Schlusskurs innerhalb des aktuellen Blocks bleibt, bleiben die gespeicherten Niveaus unverändert.
   - Wenn der Schlusskurs unter die untere Grenze bricht, rundet der Algorithmus den Preis nach unten und verschiebt den Block nach unten (`lower = round`, `upper = round + size`).
   - Wenn der Schlusskurs über die obere Grenze bricht, wird der Block nach oben verschoben (`upper = round`, `lower = round - size`).
3. **Signalgenerierung**
   - Ein steigendes oberes Niveau deutet auf einen bullischen Ausbruch des Renko-Blocks hin. Ein fallendes oberes Niveau deutet auf einen bärischen Ausbruch hin.
   - Wenn `Reverse` deaktiviert ist, kauft die Strategie bei bullischen Verschiebungen und verkauft bei bärischen Verschiebungen. Wenn `Reverse` aktiviert ist, werden die Aktionen vertauscht.
   - Wenn ein Signal ausgelöst wird, wird das bestehende Engagement in der entgegengesetzten Richtung automatisch geschlossen (Kauforder schließt Shorts, Verkaufsorder schließt Longs). Wenn `Allow Increase` deaktiviert ist, weigert sich die Strategie, Größe auf eine bereits offene Position in der gleichen Richtung hinzuzufügen.
4. **Orderausführung**
   - Orders werden mit der `Volume`-Einstellung der Strategie gesendet. Bei der Umkehrung einer bestehenden Position entspricht die Ordergröße der absoluten Position plus dem konfigurierten Volumen, sodass der Wechsel sofort erfolgt.
   - `StartProtection()` wird beim Start aufgerufen, damit die in Designer oder über Komposition konfigurierten Risikoschutzmechanismen aktiv sind.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `Block Size` | Renko-Blockgröße in Pips. Die Strategie multipliziert sie mit dem Pip-Wert des Instruments, um das tatsächliche Preisinkrement zu erhalten. Größere Werte reduzieren die Handelshäufigkeit. | 30 |
| `Reverse` | Wenn `true`, werden alle Handelssignale invertiert (Kauf bei bärischer Verschiebung, Verkauf bei bullischer Verschiebung). | `false` |
| `Allow Increase` | Wenn `true`, erlaubt die Pyramidisierung durch Hinzufügen weiterer Orders in der gleichen Richtung bei jedem Signal. Wenn `false`, wird eine neue Order nur gesendet, wenn die Nettoposition nach dem Schließen der Gegenseite flach ist. | `false` |
| `Candle Type` | Quell-Kerzendaten. Jeder unterstützte `DataType` kann verwendet werden; standardmäßig abonniert die Strategie 1-Minuten-Kerzen. | `TimeFrame(1m)` |
| `Volume` *(geerbt)* | Ordergröße beim Senden von Marktorders. Setzen Sie diese Eigenschaft auf der Strategieinstanz, bevor Sie sie starten. | Hängt vom Portfolio ab |

## Verwendungshinweise
- Wählen Sie die Blockgröße entsprechend der Instrumentenvolatilität. Für wichtige FX-Paare emulieren 30–50 Pips das Verhalten des ursprünglichen EA. Bei Indizes oder Krypto-Assets verwenden Sie größere Blockgrößen.
- Die Strategie funktioniert mit jedem Kerzenfeed (Tick, Zeitrahmen, Range), solange der Kerzenschlusskurs das gewünschte Preis-Sampling widerspiegelt. Für einen reinen Renko-Feed können Sie den Kerzentyp auf eine Renko-Datenserie umstellen.
- Aktivieren Sie `Reverse`, um das Ausbruchssystem in ein Mean-Reversion-System umzuwandeln, das jeden Renko-Niveau-Wechsel auslöscht.
- `Allow Increase` kann eingeschaltet werden, um den ursprünglichen "Increase"-Parameter des EA zu imitieren, der Kontrakte bei jedem neuen Niveau in der gleichen Richtung hinzufügt.
- Risiko- und Geldmanagement (Stop-Loss, Take-Profit, Drawdown-Kontrolle) können über StockSharp-Schutzmaßnahmen oder Wrapper-Strategien konfiguriert werden. Das Beispiel behält die identische Logik des MT5-Experten bei und erzwingt keine festen Ausstiege über Niveau-Wechsel hinaus.

## Datenanforderungen
- Historische und Echtzeit-Kerzendaten für den konfigurierten `Candle Type`.
- Die Instrument-Metadaten müssen `PriceStep` und `Decimals` liefern, damit die Pip-Konvertierung korrekt funktioniert. Wenn diese Werte nicht verfügbar sind, greift die Strategie auf einen Standard-Schritt von 0.0001 zurück.

## Empfohlener Arbeitsablauf
1. Fügen Sie die Strategie zu Designer hinzu oder erstellen Sie sie programmatisch über die StockSharp-API.
2. Setzen Sie `Security`, `Portfolio`, `Volume` und passen Sie optional die oben aufgeführten Parameter an.
3. Starten Sie die Strategie. Sie wartet auf die erste abgeschlossene Kerze, um den anfänglichen Renko-Block zu erstellen.
4. Überwachen Sie das integrierte Trades-Diagramm oder abonnieren Sie Logs, um zu überprüfen, dass Orders nur ausgelöst werden, wenn sich das gerundete Niveau ändert.

Diese Dokumentation spiegelt das Verhalten des ursprünglichen Renko Level EA wider und erklärt, wie es innerhalb von StockSharp implementiert ist, damit Sie es weiter anpassen oder erweitern können.
