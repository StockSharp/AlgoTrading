# Estrategia Supertrend - SSL con Alternancia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el indicador Supertrend con el canal SSL.
Un interruptor permite requerir confirmación de ambos indicadores antes de entrar en una operación.
Si la confirmación está activada, la señal del primer indicador espera al segundo antes de ejecutarse.
Las posiciones se cierran cuando aparece una señal opuesta de cualquiera de los indicadores.

## Detalles

- **Indicadores**: Supertrend (ATR 10, factor 2.4), canal SSL (período 13)
- **Entrada**: Cruce SSL o cambio de dirección Supertrend con confirmación opcional
- **Salida**: Señal opuesta de SSL o Supertrend
- **Dirección**: Largo y Corto
- **Marco temporal**: Cualquiera
