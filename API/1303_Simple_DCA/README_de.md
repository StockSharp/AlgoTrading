# Einfache DCA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie platziert eine Basisorder und fügt Sicherheitsorders hinzu, wenn der Preis um einen bestimmten Prozentsatz abweicht. Sie beendet die Position, sobald der Preis ein Take-Profit-Niveau erreicht, das vom durchschnittlichen Einstiegspreis berechnet wird. Die Größe jeder Sicherheitsorder wird mit einem Faktor multipliziert.

## Parameter
- Kerzentyp
- Basisordergröße (Kurswährung)
- Preisabweichung für Sicherheitsorder (%)
- Maximale Anzahl an Sicherheitsorders
- Take Profit (%)
- Ordergrößenmultiplikator
