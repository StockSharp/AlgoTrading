# Bill Williams Wise Man 2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert das zweite „Weise-Mann"-Muster aus dem Handelssystem von Bill Williams.
Sie analysiert das Histogramm des Awesome Oscillators (AO), um Impulswechsel zu erkennen:

- **Kauf** wenn der AO über null liegt und einen Gipfel bildet, dem drei aufeinanderfolgend niedrigere Balken folgen.
- **Verkauf** wenn der AO unter null liegt und ein Tal bildet, dem drei aufeinanderfolgend höhere Balken folgen.

Sobald ein Signal erscheint, schließt die Strategie die entgegengesetzte Position und eröffnet eine neue in
der Signalrichtung. Standardmäßig werden Vier-Stunden-Kerzen verwendet, der Zeitrahmen kann jedoch über
einen Parameter geändert werden.

Es ist keine Stop-Loss- oder Take-Profit-Logik enthalten; Positionen werden nur umgekehrt, wenn ein
entgegengesetztes Muster auftritt. Die Strategie zeichnet außerdem Kerzen, den AO-Indikator und ausgeführte
Trades auf einem Chart zur visuellen Analyse.
