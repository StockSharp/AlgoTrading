# Globaler Stop-Loss & Handelsfenster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie repliziert das Verhalten des MetaTrader-Experten **Exp_GStopLoss_Tm** und stellt eine Risikoüberlagerung bereit, die das kombinierte Ergebnis aller von der Strategieinstanz eröffneten Trades überwacht. Das Modul generiert selbst keine Einstiegssignale; stattdessen verfolgt es die Gewinne und Verluste bestehender Positionen und erzwingt sowohl einen globalen Stop-Loss-Schwellenwert als auch ein optionales Handelssessionfenster. Wenn die Verluste das konfigurierte Limit überschreiten oder der Markt sich außerhalb des erlaubten Zeitrahmens bewegt, liquidiert die Strategie das aktuelle Exposure und blockiert weitere Trades, bis das Buch wieder ausgeglichen ist.

## Handelslogik
1. Beim Start zeichnet die Strategie den aktuellen realisierten PnL als Basisreferenz auf. Dies ermöglicht es, den schwebenden Gewinn relativ zum zuletzt ausgeglichenen Zustand zu messen.
2. Jede abgeschlossene Kerze, die vom konfigurierten Kerzentop produziert wird, löst eine Risikoprüfung aus. Der Standard-Zeitrahmen ist eine Minute, um eine Tick-Level-Überwachung zu emulieren, ohne das System zu überlasten.
3. Das Modul berechnet den unrealisierten Gewinn als Differenz zwischen dem aktuellen Strategie-PnL und dem Basiswert. Positiver PnL wird ignoriert, solange die Strategie innerhalb des Handelsfensters bleibt, entsprechend dem originalen Expert Advisor.
4. Wenn der Verlustmodus auf **Percent** gesetzt ist, vergleicht die Strategie den absoluten Verlustprozentsatz mit dem Konto-Eigenkapital aus `Portfolio.CurrentValue`. Im **Currency**-Modus erfolgt der Vergleich in absoluten Währungseinheiten.
5. Sobald der Verlustschwellenwert überschritten ist, wird das Stop-Flag gesetzt und die Strategie beginnt in der nächsten Iteration damit, die offene Position zu schließen. Das Flag wird erst freigegeben, nachdem die Positionsgröße auf null zurückgekehrt ist und der Basis-PnL aktualisiert wurde.
6. Wenn das optionale Handelsfenster aktiviert ist, prüft die Risikokontrolle auch, ob die Kerzenabschlusszeit innerhalb des erlaubten Intervalls liegt. Das Fenster unterstützt Intraday-Sessions, die über Mitternacht hinausgehen, entsprechend der MetaTrader-Logik.
7. Wenn das Stop-Flag aktiv ist oder der Session-Filter erkennt, dass der Markt außerhalb der erlaubten Stunden ist, sendet das Modul eine Marktorder in der entgegengesetzten Richtung, um die Position zu glätten. Informative Protokolleinträge beschreiben den Grund für jeden Ausstieg.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| `LossMode` | Legt fest, wie der Verlustschwellenwert interpretiert wird: Prozentsatz des aktuellen Konto-Eigenkapitals oder absolute Kontowährung. |
| `StopLoss` | Verlustschwellenwert. Im Prozentmodus repräsentiert die Zahl den Prozentsatz, im Währungsmodus wird die Kontowährung verwendet. |
| `UseTimeFilter` | Aktiviert das Intraday-Handelsfenster. Wenn deaktiviert, ignoriert die Strategie den Zeitfilter vollständig. |
| `StartTime` | Inklusiver Start des Handelsfensters in UTC. Funktioniert zusammen mit `EndTime`, um die gültige Session zu definieren. |
| `EndTime` | Exklusives Ende des Handelsfensters in UTC. Unterstützt Wrap-Around-Sessions, wenn die Endzeit früher als die Startzeit ist. |
| `CandleType` | Kerzen-Abonnement für die periodische Risikobewertung. Standard ist ein 1-Minuten-Zeitrahmen. |

## Implementierungshinweise
- Der Basis-PnL wird neu berechnet, wenn die Positionsgröße auf null zurückkehrt, damit nachfolgende Trades mit einer sauberen Basis beginnen.
- Eigenkapitalwerte werden aus dem Live-Portfolio abgerufen, daher passt sich der Prozentmodus sowohl an realisierte als auch unrealisierte Änderungen im Kontowert an.
- Alle Kommentare im Quellcode sind wie von den Projektkonventionen gefordert auf Englisch verfasst.
- Die Strategie zeichnet Kerzen und eigene Trades im Standard-Diagrammbereich, wenn einer verfügbar ist, um das Verhalten während des Tests zu visualisieren.

## Verwendungsrichtlinien
1. Hängen Sie die Strategie an das Instrument an, das Sie überwachen möchten. Die Ordergenerierung anderer Strategien kann noch stattfinden; dieses Modul überwacht nur und schließt Positionen.
2. Konfigurieren Sie den Verlustmodus und Schwellenwert entsprechend Ihrem Risikoappetit. Zum Beispiel werden `LossMode = Percent` und `StopLoss = 5` die Position nach einem 5% unrealisierten Drawdown relativ zum aktuellen Eigenkapital schließen.
3. Setzen Sie die Parameter `StartTime` und `EndTime`, um das Trading auf eine bestimmte Intraday-Session zu beschränken. Um ein Übernacht-Fenster abzudecken, geben Sie eine Startzeit an, die später als die Endzeit ist (zum Beispiel 20:00 bis 06:00).
4. Führen Sie den Backtest oder die Live-Session aus. Die Strategie setzt das Stop-Flag automatisch zurück, sobald alle Positionen ausgeglichen sind, und überwacht dann weiterhin nachfolgende Trades.
