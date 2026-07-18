# Estrategia MasterMind 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera reversiones extremas usando cuatro indicadores **Williams %R** con diferentes períodos. Cuando todos los indicadores caen a valores profundos de sobreventa, la estrategia entra en una posición larga. Cuando todos los indicadores suben a valores fuertes de sobrecompra, entra en una posición corta.

## Lógica de trading

1. Suscribirse a velas del marco temporal seleccionado.
2. Calcular cuatro indicadores Williams %R con períodos 26, 27, 29 y 30.
3. **Comprar** cuando todos los indicadores estén por debajo de `-99.99`.
4. **Vender** cuando todos los indicadores estén por encima de `-0.01`.
5. Las señales se procesan solo en velas completadas.

El volumen de la orden se toma de la propiedad `Volume` de la estrategia. Las posiciones opuestas existentes se cierran automáticamente enviando una orden de mercado del tamaño requerido.

## Parámetros

| Nombre | Descripción | Predeterminado |
|--------|-------------|----------------|
| `WprPeriod1` | Longitud del primer indicador Williams %R | 26 |
| `WprPeriod2` | Longitud del segundo indicador Williams %R | 27 |
| `WprPeriod3` | Longitud del tercer indicador Williams %R | 29 |
| `WprPeriod4` | Longitud del cuarto indicador Williams %R | 30 |
| `CandleType` | Tipo y marco temporal de velas | Velas de 1 minuto |

## Notas

* La estrategia utiliza la API de alto nivel con `Bind` para el procesamiento de indicadores.
* No incluye niveles de stop loss o take profit; la posición se invierte en señales opuestas.
