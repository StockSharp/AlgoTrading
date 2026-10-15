# XMA Ichimoku-Kanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie implementiert ein Kanal-Ausbruchssystem auf Basis des XMA Ichimoku-Konzepts. Sie baut einen dynamischen Kanal um einen geglätteten Durchschnitt der jüngsten Hochs und Tiefs auf und generiert Trades, wenn die Preisbewegung einen Ausbruch mit einem Rücksetzer bestätigt.

## Funktionsweise

1. **Höchst- und Tiefstwerte**: Für jede abgeschlossene Kerze berechnet die Strategie das höchste Hoch und das niedrigste Tief über konfigurierbare Rückblickperioden.
2. **Geglättete Mittellinie**: Der Mittelpunkt zwischen den Höchst- und Tiefstwerten wird mit einem einfachen gleitenden Durchschnitt geglättet.
3. **Kanalaufbau**: Obere und untere Bänder werden aus der geglätteten Mittellinie durch Anwendung prozentualer Abstände abgeleitet.
4. **Handelslogik**:
   - Wenn der vorherige Schlusskurs über dem vorherigen oberen Band lag und der aktuelle Schlusskurs unter das aktuelle obere Band zurückkehrt, öffnet die Strategie eine Long-Position und schließt eine bestehende Short-Position.
   - Wenn der vorherige Schlusskurs unter dem vorherigen unteren Band lag und der aktuelle Schlusskurs über das aktuelle untere Band zurückkehrt, öffnet die Strategie eine Short-Position und schließt eine bestehende Long-Position.

## Parameter

- **Up Period** – Rückblickperiode für den höchsten Preis.
- **Down Period** – Rückblickperiode für den niedrigsten Preis.
- **MA Length** – Länge des glättenden gleitenden Durchschnitts.
- **Up Percent** – Prozentualer Aufschlag auf die Mittellinie zur Bildung des oberen Bandes.
- **Down Percent** – Prozentualer Abzug von der Mittellinie zur Bildung des unteren Bandes.
- **Candle Type** – Zeitrahmen der für Berechnungen verwendeten Kerzen.

## Hinweise zur Verwendung

- Trades werden mit Marktaufträgen ausgeführt.
- Es werden nur abgeschlossene Kerzen verarbeitet, um Fehlsignale zu vermeiden.
- Die Strategie schließt Gegenpositionen, bevor eine neue eröffnet wird.

## Haftungsausschluss

Dieses Beispiel dient ausschließlich zu Bildungszwecken. Testen Sie es gründlich, bevor Sie es im Live-Trading einsetzen.
