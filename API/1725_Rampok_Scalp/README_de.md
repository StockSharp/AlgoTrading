# Rampok Scalp-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Scalping-System, das handelt, wenn der Preis die gleitenden Durchschnittshüllen durchbricht.
Die Strategie geht Long, wenn der Preis das untere Band nach oben kreuzt, und
Short, wenn der Preis das obere Band nach unten kreuzt. Positionen werden durch
optionale Take-Profit-, Stop-Loss- und Trailing-Stop-Parameter geschützt.

## Details

- **Einstiegskriterien**:
  - **Kauf**: vorheriger Schlusskurs unter dem unteren Band und aktueller Schlusskurs darüber.
  - **Verkauf**: vorheriger Schlusskurs über dem oberen Band und aktueller Schlusskurs darunter.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Take-Profit, Stop-Loss oder Trailing-Stop.
- **Stops**: Konfigurierbares SL/TP und Trailing.
- **Filter**: keine.
