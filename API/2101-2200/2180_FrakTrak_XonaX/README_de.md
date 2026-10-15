# FrakTrak XonaX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

FrakTrak XonaX ist eine Ausbruch-Strategie, die auf Fraktal-Niveaus basiert, die in einem höheren Zeitrahmen berechnet werden. Wenn der Preis das jüngste Fraktal um einen kleinen Offset überschreitet, steigt die Strategie in Richtung des Ausbruchs ein. Ein fester Take-Profit und ein Trailing-Stop verwalten die offene Position.

## Parameter
- **Volume** – Ordergröße.
- **Take Profit** – Abstand in Punkten für das Take-Profit-Niveau.
- **Trailing Stop** – Abstand in Punkten für den Trailing-Stop-Loss.
- **Trailing Correction** – zusätzlicher Abstand zum Trailing-Stop.
- **Candle Type** – Zeitrahmen zur Erstellung von Kerzen und Fraktalen.

## Handelsregeln
1. Obere und untere Fraktale anhand der letzten abgeschlossenen Kerzen berechnen.
2. Kaufen, wenn der Schlusskurs das obere Fraktal plus 15 Punkte überschreitet und keine Long-Position besteht. Stop-Loss wird beim letzten unteren Fraktal gesetzt, Take-Profit über *Take Profit* konfiguriert.
3. Verkaufen, wenn der Schlusskurs unter das untere Fraktal minus 15 Punkte fällt und keine Short-Position besteht. Stop-Loss wird beim letzten oberen Fraktal gesetzt, Take-Profit über *Take Profit* konfiguriert.
4. Wenn eine Position mehr als *Trailing Stop* Punkte im Gewinn liegt, folgt der Stop-Loss dem Preis mit einem zusätzlichen *Trailing Correction* Offset.
