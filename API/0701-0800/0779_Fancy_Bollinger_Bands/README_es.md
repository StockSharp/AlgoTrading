# Bollinger Bands Elegantes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas de Bollinger Bands. Compra cuando el precio de cierre cruza por encima de la banda superior y vende cuando el precio de cierre cruza por debajo de la banda inferior.

## Detalles

- **Condiciones de entrada**:
  - **Largo**: el cierre cruza por encima de la banda superior
  - **Corto**: el cierre cruza por debajo de la banda inferior
- **Condiciones de salida**: cruce inverso
- **Tipo**: Ruptura
- **Indicadores**: Bollinger Bands
- **Marco temporal**: 1 minuto (por defecto)
- **Stops**: ninguno

**Regla verificable y valores predeterminados.** Las bandas de Bollinger usan los últimos 20 cierres completados, **incluida la vela de señal**, y dos desviaciones estándar poblacionales. `BollingerPeriod=20`, `DeviationMultiplier=2`, `CandleType=1 minuto`. Sin posición se abre largo cuando el cierre cruza desde abajo hacia arriba de la banda superior, y corto cuando cruza desde arriba hacia abajo de la inferior. Se cierra **todo el largo** al volver a cruzar hacia el interior de la banda superior, y **todo el corto** al volver al interior de la inferior. Las bandas previa y actual se calculan con los cierres de sus respectivas velas completadas. Sin stops, objetivos ni reversión en la misma vela.
