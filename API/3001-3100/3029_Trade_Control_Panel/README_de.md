# Strategie Trading-Steuerungspanel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Strategie Trading-Steuerungspanel** portiert das manuelle Handelspanel aus dem originalen MQL5-Skript in die StockSharp-High-Level-API. Die Klasse stellt Hilfsmethoden bereit, die jeden Button des Panels replizieren: Volumen-Preset-Umschalter, Markt-Kauf-/Verkaufsaktionen, Schließen der aktuellen Position, Umkehren des Exposures und eine dedizierte Break-Even-Routine. Schützende Stop-Loss- und Take-Profit-Orders können automatisch um den durchschnittlichen Einstiegspreis generiert werden, was die Sicherheitsfeatures des Quell-Expertenberaters widerspiegelt.

Anstatt Chart-Controls zu zeichnen, bietet die StockSharp-Implementierung eine stark typisierte Schnittstelle, die aus UI-Code, Skripten oder automatisierten Workflows aufgerufen werden kann. Die Strategie verfolgt ausgewählte Volumen-Presets, rundet Volumina auf den nächsten Exchange-Schritt und gibt Markt-/Stop-/Limit-Orders über die integrierten `Strategy`-Helpers wie `BuyMarket`, `SellMarket`, `SellStop` und `BuyLimit` aus.

## Parameter
- **VolumeList** – durch Semikolon getrennte Volumen-Presets, die sich wie die originalen Checkboxen verhalten. Nur die ersten neun Werte werden verwendet, um Kompatibilität mit dem MQL-Layout zu gewährleisten. Leerzeichen werden ignoriert und ungültige Zahlen werden übersprungen.
- **CurrentVolume** – aggregiertes Volumen basierend auf den aktuell umgeschalteten Presets. Der Setter rundet den Wert mit `Security.VolumeStep` (wenn verfügbar) oder zwei Dezimalstellen (Forex-Lots). Sie können diesen Parameter auch manuell setzen, wenn Sie ihn in eine externe Benutzeroberfläche integrieren.
- **BreakEvenSteps** – Anzahl der Preisschritte, die beim Verschieben des Schutz-Stops auf Break-Even über `ApplyBreakEven()` zum Einstiegspreis addiert werden. Wenn das Instrument keinen `PriceStep` hat, wird der Wert als direkter Preisversatz behandelt.
- **StopLossSteps** – anfängliche Stop-Loss-Distanz in Preisschritten. Ein Wert von null deaktiviert automatische Stops, wenn eine Position eröffnet oder geändert wird.
- **TakeProfitSteps** – anfängliche Take-Profit-Distanz in Preisschritten. Funktioniert genauso wie der Stop-Loss-Parameter.

## Manuelle Steuerungen
Alle Laufzeitaktionen werden durch öffentliche Methoden bereitgestellt, sodass die Hostanwendung sie mit Buttons, Hotkeys oder Skripten verbinden kann:

- `ToggleVolumeSelection(int index)` – imitiert die Preset-Checkboxen durch Hinzufügen oder Entfernen eines Volumens aus dem aggregierten Betrag. Ungültige Indizes werfen eine Ausnahme, um stille Fehler zu vermeiden.
- `ResetVolumeSelection()` – löscht jeden Preset und setzt `CurrentVolume` auf null zurück.
- `ExecuteBuy()` / `ExecuteSell()` – senden Marktorders mit dem aktuellen Volumen. Beide Methoden geben `false` zurück, wenn kein Volumen ausgewählt ist.
- `CloseAllPositions()` – sendet eine Marktorder entgegengesetzt zur aktuellen Positionsgröße (`BuyMarket` für Shorts, `SellMarket` für Longs).
- `ReversePosition()` – schließt die bestehende Position und öffnet sofort eine neue in der entgegengesetzten Richtung mit dem aggregierten Volumen, genau wie der "Reverse"-Button im MQL-Panel.
- `ApplyBreakEven()` – berechnet den Schutz-Stop als `durchschnittlicher Einstieg ± BreakEvenSteps * PriceStep` neu und platziert eine neue Stop-Order (`SellStop` für Longs, `BuyStop` für Shorts). Gibt `true` nur zurück, wenn die Strategie eine offene Position hält und ein Offset größer als null angegeben wird.

Wenn sich die Positionsgröße ändert, baut `OnPositionChanged` die Schutzorders neu auf. Zuerst storniert es das vorherige Stop/Ziel-Paar, dann erstellt es sie mit dem neuesten durchschnittlichen Einstiegspreis und den konfigurierten Offsets neu. Das Schließen der Position (manuell oder durch Stop/Ziel-Ausführungen) entfernt alle aktiven Schutzorders, um verwaiste Anweisungen an der Börse zu vermeiden.

## Verwendungsablauf
1. Konfigurieren Sie die gewünschten Volumen-Presets in **VolumeList** (zum Beispiel `0.05; 0.10; 0.25; 0.50; 1.00`).
2. Schalten Sie einen oder mehrere Presets mit `ToggleVolumeSelection` ein. Der `CurrentVolume`-Parameter zeigt den akkumulierten Wert nach dem Runden.
3. Rufen Sie `ExecuteBuy` oder `ExecuteSell` auf, um in den Markt einzusteigen. Wenn **StopLossSteps** oder **TakeProfitSteps** größer als null sind, platziert die Strategie automatisch `SellStop`/`BuyStop`- und `SellLimit`/`BuyLimit`-Orders relativ zum durchschnittlichen Einstiegspreis.
4. Verwenden Sie `ApplyBreakEven`, wenn sich der Preis zu Ihren Gunsten bewegt, um den Stop über (für Longs) oder unter (für Shorts) dem Einstieg um den konfigurierten Offset zu ziehen.
5. `CloseAllPositions` verlässt sofort den Markt, während `ReversePosition` sowohl schließt als auch das Exposure umdreht und dabei die aktuell ausgewählte Lot-Größe wiederverwendet.
6. `ResetVolumeSelection` bereitet das Panel für den nächsten Trade vor, indem alle Presets gelöscht werden.

## Hinweise und Empfehlungen
- Die Break-Even- und Schutzlogik basiert auf `PositionAvgPrice` und dem aktuellen `Security.PriceStep`. Stellen Sie sicher, dass die Instrument-Metadaten vor dem Starten der Strategie gefüllt sind.
- `StartProtection()` wird während `OnStarted` aufgerufen, damit die eingebaute Schutz-Engine Stop/Ziel-Orders verfolgen kann, die diese Strategie registriert.
- Die Hilfsmethoden sind synchrone Wrapper um StockSharp-Order-Helpers. Exchanges oder Adapter, die asynchrone Bestätigung erfordern, sollten auf Order-Events warten, bevor sie den nächsten Befehl ausgeben, wenn strenge Sequenzierung erforderlich ist.
- Die Klasse kann in benutzerdefinierte WPF/WinForms-Panels, REST-Dienste oder Konsolentools eingebettet werden, indem UI-Ereignisse den bereitgestellten Methoden zugeordnet werden.
