# Estrategia CAi de Desviación Estándar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port a StockSharp del experto MQL5 original **Exp_i-CAi_StDev**. Combina una media móvil con bandas de desviación estándar para detectar rupturas y posteriores reversiones.

## Lógica de la estrategia

1. Calcular una media móvil simple (SMA) durante el período especificado.
2. Calcular la desviación estándar de los precios de cierre durante el mismo período.
3. Construir dos conjuntos de bandas alrededor de la SMA:
   - **Bandas de entrada**: SMA ± `OpenMultiplier` × StdDev.
   - **Bandas de salida**: SMA ± `CloseMultiplier` × StdDev.
4. Abrir una posición larga cuando el precio cierra por encima de la banda de entrada superior.
5. Abrir una posición corta cuando el precio cierra por debajo de la banda de entrada inferior.
6. Cerrar una posición larga existente cuando el precio cae por debajo de la banda de salida superior.
7. Cerrar una posición corta existente cuando el precio sube por encima de la banda de salida inferior.

## Parámetros

| Nombre | Descripción | Por defecto |
| --- | --- | --- |
| `MaLength` | Longitud del cálculo de la media móvil y la desviación estándar | 12 |
| `StdDevPeriod` | Período para el indicador de desviación estándar | 9 |
| `OpenMultiplier` | Multiplicador para las bandas de entrada | 2.5 |
| `CloseMultiplier` | Multiplicador para las bandas de salida | 1.5 |
| `CandleType` | Tipo de velas utilizadas por la estrategia | Velas de 5 minutos |

## Notas

- La estrategia usa la API de alto nivel con `Bind` para recibir los valores del indicador.
- Solo se procesan velas completadas para evitar señales prematuras.
- Todos los comentarios en el código fuente están en inglés.
