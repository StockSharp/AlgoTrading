# Liquiditäts-Interne-Marktstruktur-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erkennt interne Marktstrukturverschiebungen, die mit Liquiditäts-Sweeps an kürzlichen Hochs oder Tiefs zusammenfallen. Ein Trade wird eröffnet, wenn der Kurs eine Liquiditätslinie berührt und anschließend die Struktur in die entgegengesetzte Richtung verschiebt. Der Handel kann auf bullische oder bärische Setups oder beide beschränkt werden.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurs schließt über der vorherigen bärischen Struktur und hat die Liquiditätslinie des jüngsten Tiefs berührt.
  - **Short**: Kurs schließt unter der vorherigen bullischen Struktur und hat die Liquiditätslinie des jüngsten Hochs berührt.
- **Long/Short**: Beide Richtungen oder wählbar Nur Bullisch / Nur Bärisch.
- **Ausstiegskriterien**:
  - Gegensignal nach dem Einstieg.
  - Stop-Loss bei `StopLossPips` Pips.
  - Optionaler Take-Profit bei `TakeProfitPips` Pips.
- **Stops**: Ja, konfigurierbarer Stop-Loss und optionaler Take-Profit.
- **Filter**:
  - Handel nur innerhalb des angegebenen Zeitbereichs.
  - Signalsperre verhindert wiederholte Einstiege für mehrere Bars.
