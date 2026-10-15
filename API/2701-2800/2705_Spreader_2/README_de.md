# Spreader 2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **Spreader 2-Strategie** ist ein Pair-Trading-System, das aus dem MetaTrader Expert Advisor "Spreader 2" konvertiert wurde. Sie beobachtet zwei korrelierte Instrumente auf einem Ein-Minuten-Zeitrahmen und sucht nach kurzfristigen Abweichungen zwischen ihren Preisbewegungen. Wenn beide Beine innerhalb kontrollierter Volatilitätsgrenzen divergieren und dabei positive Korrelation aufrechterhalten, eröffnet die Strategie einen marktneutralen Spread durch eine Long-Position in einem Symbol und eine Short-Position im anderen. Die kombinierte Position wird geschlossen, wenn der gesamte schwebende Gewinn das konfigurierte Ziel erreicht oder wenn Korrelationsregeln verletzt werden.

## Kernlogik

1. Fertige Kerzen für das primäre und sekundäre Symbol empfangen und nach Schließzeit ausrichten.
2. Rollierende Listen von Schlusspreisen pflegen, damit der Algorithmus auf Werte verweisen kann, die `ShiftLength`, `2 * ShiftLength` und `1440` Balken in der Vergangenheit liegen.
3. Erste Differenzen berechnen (`x1`, `x2` für das primäre Symbol und `y1`, `y2` für das sekundäre Symbol), um lokale Schwankungen zu erkennen.
4. Handel überspringen, wenn ein Instrument zwei aufeinanderfolgende Bewegungen in die gleiche Richtung zeigt (Trendfilter) oder wenn die Produkte `x1 * y1` negative Korrelation anzeigen.
5. Das Volatilitätsverhältnis `a / b` auswerten, wobei `a = |x1| + |x2|` und `b = |y1| + |y2|`. Nur fortfahren, wenn das Verhältnis zwischen `0.3` und `3.0` bleibt.
6. Das sekundäre Beinvolumen proportional zum Volatilitätsverhältnis skalieren und an Volumen-Schritt, Minimum und Maximum des Kontrakts anpassen.
7. Die beabsichtigte Handelsrichtung mit dem 1440-Balken-Rückblick (ungefähr ein Handelstag) bestätigen. Der Spread wird nur eröffnet, wenn die Tagesbewegung das kurzfristige Signal unterstützt.
8. Die Strategie eröffnet beide Beine gleichzeitig: das primäre Symbol handelt mit dem konfigurierten `PrimaryVolume`, während das sekundäre Symbol die angepasste Größe in entgegengesetzter Richtung handelt.
9. Während Positionen offen sind, verfolgt das System kontinuierlich den schwebenden Gewinn beider Beine. Wenn der kombinierte Gewinn `TargetProfit` übersteigt, schließt es den Spread und setzt die Einstiegsreferenzen zurück.
10. Sicherheitschecks schließen automatisch verwaiste Positionen, wenn ein Bein unerwartet aussteigt, und eröffnen fehlende Beine wenn möglich neu, um die Absicherung ausgewogen zu halten.

## Parameter

- **SecondSecurity** – sekundäres Instrument, das am Spread teilnimmt. Dieser Parameter ist erforderlich.
- **PrimaryVolume** – Handelsvolumen (in Lots/Kontrakten) für das primäre Symbol. Standard ist `1`.
- **TargetProfit** – absolutes monetäres Gewinnziel für das kombinierte Paar. Standard ist `100`.
- **ShiftLength** – Anzahl der Kerzen zwischen Vergleichspunkten, die in Erste-Differenz-Berechnungen verwendet werden. Standard ist `30`.
- **CandleType** – Datentyp für Kerzen-Subskriptionen. Standardmäßig arbeitet die Strategie mit Ein-Minuten-Zeitrahmen-Kerzen.

## Handelsregeln

- Nur fertige Kerzen werden verarbeitet, um Aktionen auf unvollständigen Daten zu vermeiden.
- Trendfilter müssen für beide Symbole über die letzten zwei `ShiftLength`-Fenster entgegengesetzte Bewegungen zeigen.
- Die Korrelation muss positiv sein, und das Volatilitätsverhältnis muss im Band `[0.3, 3.0]` bleiben.
- Die Bestätigungsprüfung gegen den 1440-Balken-Rückblick verhindert Trades, die der längerfristigen Richtung widersprechen.
- Aufträge werden mit `OrderTypes.Market` gesendet. Das sekundäre Bein wird explizit mit dem sekundären Wertpapier und Portfolio registriert, um das MetaTrader-Verhalten widerzuspiegeln.
- Der offene Gewinn wird anhand der letzten Kerzenschlüsse und gespeicherten Einstiegspreise berechnet, um zu bestimmen, wann der Spread zu schließen ist.

## Hinweise

- Die Strategie setzt voraus, dass beide Instrumente kompatible Kontraktspezifikationen teilen. Wenn Multiplikatoren abweichen, wird der Handel deaktiviert und eine Warnung protokolliert.
- Da der ursprüngliche Algorithmus auf einem vollständigen Tag historischer Daten basiert, wartet auch die StockSharp-Version, bis mindestens 1440 Kerzen für den ersten Einstieg angesammelt sind.
- Alle Risikomanagement-Logik (Gewinnziel, Behandlung verwaister Beine) ist in der Strategie enthalten. Zusätzliche Schutzmaßnahmen wie Stop-Losses können bei Bedarf extern hinzugefügt werden.
