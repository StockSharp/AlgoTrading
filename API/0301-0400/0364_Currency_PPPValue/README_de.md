# Währungs-PPP-Wert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Währungs-PPP-Wert-Strategie sucht nach Fehlbewertungen im Verhältnis zur Kaufkraftparität (PPP). Währungen, die unter ihrem PPP-Wert handeln, werden gekauft, während solche, die darüber handeln, leerverkauft werden. Das Portfolio wird monatlich neu gewichtet, um das Long/Short-Exposure zu erhalten.

Da PPP-Daten selten aktualisiert werden, werden Trades nur dann platziert, wenn die erforderliche Anpassung einen Mindest-USD-Betrag überschreitet. Der Beispielcode liefert das Gerüst zur Währungsranglistenerstellung, lässt jedoch die eigentliche PPP-Berechnung als Platzhalter offen.

## Details

- **Universum**: Satz von Währungspaaren mit verfügbaren PPP-Schätzungen.
- **Signal**: Long die `K` am stärksten unterbewerteten Währungen und Short die `K` am stärksten überbewerteten.
- **Rebalancing**: Monatlich.
- **Positionierung**: Long/Short, gleichgewichtet.
- **Parameter**:
  - `Universe` – handelbare Währungen.
  - `K` – Anzahl der Long- und Short-Währungen.
  - `MinTradeUsd` – Mindesthandelsgröße in USD.
  - `CandleType` – Kerzen-Zeitrahmen (Standard: 1 Tag).
- **Hinweis**: Die PPP-Abweichungsermittlung (`TryGetPPPDeviation`) ist nicht implementiert und muss vom Benutzer bereitgestellt werden.
