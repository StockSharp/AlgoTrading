# Estrategia Aftershock Playbook
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Aftershock Playbook** opera la deriva post-ganancias basada en sorpresas de BPA.

- **Entrada**: En una publicación de ganancias, entrar largo cuando la sorpresa ≥ `PositiveSurprise` o corto cuando la sorpresa ≤ `NegativeSurprise`. Las señales se pueden invertir con `ReverseSignals`.
- **Stop**: Stop ATR opcional (`AtrLength`, `AtrMultiplier`) aplicado a posiciones cortas.
- **Salida**: Opcionalmente cerrar posiciones después de `HoldDays` días calendario (`UseTimeExit`).
- **Reentrada**: Después de una salida rentable, la estrategia vuelve a entrar una vez en la misma dirección. Las operaciones con pérdidas bloquean nuevas entradas hasta la próxima publicación de ganancias.

*Se requiere una fuente de datos de ganancias externa.*
