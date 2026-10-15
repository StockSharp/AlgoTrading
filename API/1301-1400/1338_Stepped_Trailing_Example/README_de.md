# Beispiel einer gestuften Trailing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Beispielstrategie, die ein dreistufiges Trade-Management mit optionalem Trailing-Stop demonstriert.

Die Strategie geht long, wenn der 14-Perioden-SMA den 28-Perioden-SMA nach oben kreuzt. Das Risiko wird durch einen Stop-Loss und drei Gewinnziele kontrolliert:
- Nach dem ersten Ziel wird der Stop auf Break-even verschoben.
- Nach dem zweiten Ziel wird der Stop auf das erste Ziel verschoben.
- Im dritten Schritt verlässt die Position entweder beim dritten Ziel oder startet einen Trailing-Stop.

Dieses Beispiel zeigt, wie Gewinne gestaffelt gesichert und Positionen im Verlauf geschützt werden.
