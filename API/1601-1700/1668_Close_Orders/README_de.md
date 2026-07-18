# Strategie zum Schließen von Orders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Hilfsstrategie schließt sofort bestehende Positionen und storniert ausstehende Orders gemäß benutzerdefinierten Filtern. Sie kann entweder nur auf das angehängte Instrument oder auf alle Portfolio-Instrumente angewendet werden. Optionale Zeitfenster- und Preisbereichsbeschränkungen ermöglichen eine präzise Kontrolle darüber, welche Orders betroffen sind.

## Details

- **Zweck**: Risikomanagement und manuelle Liquidation.
- **Betrieb**:
  - Beim Start prüft die Strategie das optionale Zeitfenster.
  - Falls erlaubt, schließt sie Positionen und storniert Orders, die den Filtern entsprechen.
  - Nach der Verarbeitung stoppt die Strategie automatisch.
- **Filter**:
  - `CloseAllSecurities` – alle Portfolio-Instrumente statt nur des angehängten Instruments einbeziehen.
  - `CloseOpenLongOrders` / `CloseOpenShortOrders` – bestehende Long- oder Short-Positionen schließen.
  - `ClosePendingLongOrders` / `ClosePendingShortOrders` – ausstehende Kauf- oder Verkaufsorders stornieren.
  - `SpecificOrderId` – nur Orders mit der angegebenen Transaktions-ID berühren, wenn ungleich null.
  - `CloseOrdersWithinRange`, `CloseRangeHigh`, `CloseRangeLow` – nach Einstiegspreisbereich begrenzen.
  - `EnableTimeControl`, `StartCloseTime`, `StopCloseTime` – nur während eines bestimmten Zeitfensters anwenden.
- **Standardwerte**:
  - Alle Schließoptionen aktiviert.
  - `SpecificOrderId` = 0.
  - `CloseOrdersWithinRange` = false.
  - `CloseRangeHigh` = 0.
  - `CloseRangeLow` = 0.
  - `EnableTimeControl` = false.
  - `StartCloseTime` = 02:00.
  - `StopCloseTime` = 02:30.
- **Hinweise**:
  - Die Strategie eröffnet keine neuen Positionen.
  - Preisbereichsfilter werden ignoriert, wenn die Grenzen null oder negativ sind.
  - Bei aktiviertem `CloseAllSecurities` werden Positionen im gesamten Portfolio verarbeitet.
