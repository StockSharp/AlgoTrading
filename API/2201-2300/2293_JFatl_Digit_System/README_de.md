# JFATL Digit-System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die auf der Steigung des Jurik-Gleitenden Durchschnitts (JFATL) aufbaut. Long-Positionen werden eröffnet, wenn der gleitende Durchschnitt nach oben dreht, Short-Positionen, wenn er nach unten dreht. Die Idee imitiert das farbkodierte digitale System aus der originalen MQL-Version.

## Details
- **Einstiegskriterien**: Die Steigung des Jurik-Gleitenden Durchschnitts ändert das Vorzeichen. Aufwärtssteigung eröffnet eine Long-Position, Abwärtssteigung eröffnet eine Short-Position.
- **Long/Short**: Beide Richtungen werden gehandelt.
- **Ausstiegskriterien**: Die Position wird bei entgegengesetzter Steigung umgekehrt oder durch Risikomanagement geschlossen.
- **Stops**: Prozentualer Take-Profit und optionaler Stop-Loss, konfiguriert über `StartProtection`.
- **Standardwerte**: Length = 5, Phase = -100, Timeframe = 4 hours.
- **Filter**: Keine. Die Strategie basiert ausschließlich auf der JMA-Steigung.
