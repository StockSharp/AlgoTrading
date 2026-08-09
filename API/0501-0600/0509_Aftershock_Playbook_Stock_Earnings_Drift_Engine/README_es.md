# Estrategia Aftershock Playbook
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Aftershock Playbook** interpreta un movimiento de precio inusualmente grande en una sola vela como aproximación de una sorpresa de resultados y sigue la deriva posterior. Solo utiliza velas de mercado y no requiere una fuente externa de resultados.

- **Señal**: En cada vela `CandleType` finalizada, el cambio entre cierres se compara con el ATR calculado durante `AtrLength` períodos.
- **Entrada o inversión**: Una subida superior a `ATR × SurpriseThreshold` abre o invierte a una posición larga; una caída equivalente abre o invierte a una posición corta.
- **Salida**: Un movimiento adverso superior a `ATR × AtrMultiplier` cierra la posición actual. Si el movimiento también alcanza el umbral de entrada, la inversión tiene prioridad.
- **Pausa**: Después de una entrada, inversión o salida, se omiten todas las señales durante `CooldownBars` velas finalizadas.
