# Strategie Handelspanel Mit Autopilot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert das MQL5-Beispiel **Trade panel with autopilot** auf das StockSharp-Framework.
Sie berechnet bullischen und bärischen Druck über mehrere Zeitrahmen. Eine Position wird eröffnet, wenn der entsprechende Prozentsatz den *Open %*-Schwellenwert überschreitet, und geschlossen, wenn er unter den *Close %*-Level fällt. Optional kann ein fraktalbasierter Stop-Loss mit 10-Minuten-Kerzen angewendet werden.

## Parameter

- **Autopilot** – automatisierten Handel aktivieren oder deaktivieren.
- **Open %** – Stimmen-Schwellenwert zum Öffnen einer Position.
- **Close %** – Schwellenwert zum Schließen einer bestehenden Position.
- **Use Fixed Volume** – wenn wahr, den Wert aus *Fixed Volume* verwenden.
- **Fixed Volume** – absolutes Ordervolumen.
- **Volume %** – Portfolio-Prozentsatz bei dynamischem Volumen.
- **Use Stop Loss** – Stop-Loss basierend auf jüngsten Fraktalen aktivieren.

## Logik

Für jeden Zeitrahmen von 1 Minute bis 1 Monat vergleicht die Strategie die letzte Kerze mit der vorherigen. Jeder Vergleich von Eröffnung, Hoch, Tief und abgeleiteten Durchschnittswerten fügt eine Stimme für Kauf oder Verkauf hinzu. Die Prozentsätze der Kauf- und Verkaufsstimmen steuern die Orderplatzierung. Wenn aktiviert, dient das letzte Fraktal der 10-Minuten-Kerzen als Trailing-Stop.

Dieses Beispiel dient ausschließlich Bildungszwecken und stellt keine Anlageberatung dar.
