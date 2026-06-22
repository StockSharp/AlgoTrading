# Kerzen-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie eröffnet Positionen basierend auf der Richtung aufeinanderfolgender Kerzen.
Eine Long-Position wird eröffnet, nachdem eine bestimmte Anzahl bullischer Kerzen in Folge erscheint, während eine Short-Position nach der gleichen Anzahl bärischer Kerzen eröffnet wird.
Bestehende Positionen können geschlossen werden, wenn das gegenteilige Signal auftritt.

## Parameter

- **Candle Type**: Zeitrahmen der für die Analyse verwendeten Kerzen.
- **Trend Candles**: Anzahl aufeinanderfolgender Kerzen in eine Richtung, die erforderlich sind, um eine Aktion auszulösen.
- **Take Profit %**: Optionaler Take-Profit ausgedrückt als Prozentsatz des Einstiegspreises.
- **Stop Loss %**: Optionaler Stop-Loss ausgedrückt als Prozentsatz des Einstiegspreises.
- **Enable Long Entry**: Erlaubt das Öffnen von Long-Positionen.
- **Enable Short Entry**: Erlaubt das Öffnen von Short-Positionen.
- **Enable Long Exit**: Erlaubt das Schließen von Long-Positionen bei gegenteiligem Signal.
- **Enable Short Exit**: Erlaubt das Schließen von Short-Positionen bei gegenteiligem Signal.

## Logik

1. Kerzendaten des ausgewählten Zeitrahmens abonnieren.
2. Die Anzahl aufeinanderfolgender bullischer und bärischer Kerzen verfolgen.
3. Wenn der bullische Zähler die erforderliche Anzahl erreicht:
   - Short-Positionen schließen, falls erlaubt.
   - Eine Long-Position öffnen, falls erlaubt.
4. Wenn der bärische Zähler die erforderliche Anzahl erreicht:
   - Long-Positionen schließen, falls erlaubt.
   - Eine Short-Position öffnen, falls erlaubt.
5. Optionale Schutzorders werden mit `StartProtection` gesetzt.

## Hinweise

- Signale werden nur bei abgeschlossenen Kerzen verarbeitet.
- Die Strategie verwendet `BuyMarket` und `SellMarket` für Ein- und Ausstiege.
- Alle Kommentare im Code sind wie erforderlich auf Englisch verfasst.
