# Exp UltraFATL Duplex Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Exp UltraFATL Duplex Strategie** ist eine C#-Konvertierung des MetaTrader-5-Experten-Beraters `Exp_UltraFatl_Duplex`. Das System betreibt zwei unabhängige UltraFATL-Indikator-Pipelines: eine für Long-Gelegenheiten und eine für Short-Setups. Jede Pipeline bewertet eine Leiter geglätteter FATL-Werte und zählt, wie viele Stufen steigen oder fallen. Die Balance zwischen bullischen und bärischen Zählern definiert die Richtung des nächsten Trades.

## Handelslogik
1. Das konfigurierte Kerzen-Zeitrahmen für jeden Richtungsblock abonnieren.
2. Den angewendeten Preis mit dem FATL-Kernel (39-tap Digital-Filter) filtern.
3. Die gefilterte Serie durch eine Leiter von gleitenden Durchschnitten leiten, deren Längen um den konfigurierten Schritt zunehmen. Die Leiter verwendet die vom Benutzer angegebene Glättungsmethode.
4. Aufeinanderfolgende Werte innerhalb der Leiter vergleichen, um bullische und bärische Stimmen zu zählen. Beide Zähler mit einem zweiten gleitenden Durchschnitt glätten.
5. Die Zähler beim ausgewählten Signal-Versatz auswerten (Standard: eine vollständig geschlossene Kerze):
   - Der **Long-Block** öffnet eine Position, wenn die vorherige Kerze bullische Dominanz zeigte, aber die aktuelle Kerze zeigt, dass die Zähler abwärts kreuzen (Bullen ≤ Bären). Er schließt die Long-Position, wenn Bären die Bullen auf der vorherigen Kerze übersteigen.
   - Der **Short-Block** arbeitet in entgegengesetzter Richtung: Er öffnet einen Short, wenn die vorherige Kerze bärisch dominiert ist und die aktuelle Kerze aufwärts kreuzt (Bullen ≥ Bären). Er schließt den Short, wenn Bullen auf der vorherigen Kerze führen.
6. Optionale Stop-Loss- und Take-Profit-Niveaus werden auf Kerzendaten mit dem Instrumentenpreisschritt ausgewertet.

Die Strategie erzwingt eine Nettoposition: Short-Signale schließen bestehende Longs vor dem Öffnen, und umgekehrt. Marktorders werden für Einstiege und Ausstiege verwendet.

## Parameter
### Long-Block
- **Long Volume** – Ordergröße beim Öffnen eines Long-Trades.
- **Allow Long Entries** – neue Long-Positionen aktivieren oder deaktivieren.
- **Allow Long Exits** – Schließen von Longs bei entgegengesetzten Signalen erlauben.
- **Long Candle Type** – Zeitrahmen für die Long-UltraFATL-Pipeline.
- **Long Applied Price** – Preisquelle (Schluss, typisch, DeMark usw.) für den FATL-Kernel.
- **Long Trend Method / Start Length / Phase / Step / Steps** – Leiter-Glättungskonfiguration.
- **Long Counter Method / Counter Length / Counter Phase** – Glättungseinstellungen für die bullischen/bärischen Zähler.
- **Long Signal Bar** – Anzahl abgeschlossener Kerzen als Signal-Versatz (Werte unter 1 werden als 1 behandelt).
- **Long Stop (pts)** – optionaler Stop-Loss-Abstand in Preisschritten.
- **Long Target (pts)** – optionaler Take-Profit-Abstand in Preisschritten.

### Short-Block
Symmetrische Einstellungen für die Short-Pipeline: **Short Volume**, **Allow Short Entries**, **Allow Short Exits**, **Short Candle Type**, **Short Applied Price**, **Short Trend Method / Start Length / Phase / Step / Steps**, **Short Counter Method / Counter Length / Counter Phase**, **Short Signal Bar**, **Short Stop (pts)**, **Short Target (pts)**.

## Implementierungshinweise
- Die Glättungsmethoden werden auf StockSharp-Indikatoren abgebildet. Jurik-basierte Optionen verwenden `JurikMovingAverage`; Methoden wie `Parabolic` und `T3` werden mit exponentiellen oder Jurik-gleitenden Durchschnitten angenähert, da die ursprünglichen benutzerdefinierten Kernel nicht verfügbar sind.
- Stop-Loss- und Take-Profit-Niveaus werden auf Kerzen-Hochs/Tiefs ausgewertet; sie sind keine serverseitigen Schutzorders.
- Signal-Versätze unter einer Kerze können nicht reproduziert werden, da der StockSharp-Port nur auf abgeschlossene Kerzen reagiert. Daher verhält sich das Setzen der Signal-Kerze auf null identisch zu einem Versatz von eins.
- Beide Indikator-Pipelines zeichnen ihre geglätteten Zähler in dedizierten Diagrammbereichen zur visuellen Inspektion.

## Verwendung
Die Strategie zur StockSharp-Lösung hinzufügen, die Richtungsblöcke gemäß Ihrem Handelsplan konfigurieren und sie im Designer, Shell oder Runner ausführen. Sicherstellen, dass das Instrument die erforderliche Kerzenserie bereitstellt und dass die Parameter `LongVolume`/`ShortVolume` auf die gewünschte Ordergröße gesetzt sind.
