# DCA Doppel-Trailing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht long, wenn ein schneller EMA über einen langsamen EMA kreuzt. Bis zu zwei Sicherheitsorders werden platziert, wenn der Preis um ATR-basierte oder prozentuale Schwellenwerte fällt. Positionen sind durch einen Standard-Trailing Stop und einen sekundären Lock-in-Trailing Stop geschützt, der nach einem Gewinnschwellenwert aktiviert wird.

## Parameter
- Kerzentyp
- Länge des schnellen EMA
- Länge des langsamen EMA
- Datumsfilter verwenden
- Startdatum
- ATR-Abstand verwenden
- ATR-Länge
- ATR-SO1-Multiplikator
- ATR-SO2-Multiplikator
- Fallback-SO1-Prozent
- Fallback-SO2-Prozent
- Abkühlungskerzen
- Basisordergröße USD
- Sicherheitsorder 1 Größe USD
- Sicherheitsorder 2 Größe USD
- Trailing-Stop-Prozent
- Lock-in-Auslöseprozent
- Lock-in-Trail-Prozent
