# Yuri Garcia Smart-Money-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Smart-Money-Konzept-Strategie sucht nach Preisreaktionen innerhalb von Hochvolumenzonen und Vier-Stunden-Support-/Resistenzbereichen. Sie bestätigt Einstiege mit kumulativem Delta und Wick-Pullbacks, mit dem Ziel, dem institutionellen Orderflow zu folgen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 42%. Es funktioniert am besten bei BTC und wichtigen Indizes.

Das System berechnet ATR-basierten Stop Loss und Take Profit mit einem konfigurierbaren Risiko-Rendite-Verhältnis. Trades sind long, short oder beides erlaubt, und Positionen werden nur geöffnet, wenn der Kurs innerhalb der Zone liegt, ein Wick-Pullback auftritt und das Delta die Bewegung unterstützt.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurs innerhalb der gepufferten Hoch-/Tief-Zone, bullischer Wick-Pullback, kumulatives Delta steigend.
  - **Short**: Kurs innerhalb der Zone, bärischer Wick-Pullback, kumulatives Delta fallend.
- **Long/Short**: Konfigurierbar (beide, nur Kauf oder nur Verkauf).
- **Ausstiegskriterien**:
  - ATR-basierter Stop Loss oder Take Profit.
- **Stops**: Ja, ATR-basiert.
- **Filter**:
  - HTF-Zone, kumulative Delta-Bestätigung, Wick-Pullback.
