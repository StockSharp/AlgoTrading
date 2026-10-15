# Estrategia MTC Combo v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertida del script de MetaTrader "MTC Combo v2 (barabashkakvn's edition)".

## Lógica
- Usa la pendiente de una media móvil para determinar la tendencia básica.
- El filtro de perceptrón opcional calcula la suma ponderada de las diferencias recientes de precios de apertura con rezagos configurables.
- El parámetro `Pass` selecciona qué ramas del perceptrón se utilizan:
  - 4: requiere perceptron3 > 0 y perceptron2 > 0 para largo; perceptron3 <= 0 y perceptron1 < 0 para corto.
  - 3: usa perceptron2 > 0 para largo.
  - 2: usa perceptron1 < 0 para corto.
  - otros valores: opera solo en base a la pendiente de MA.

Los niveles de stop loss y take profit se toman de los parámetros `Sl*` y `Tp*`.

## Parámetros
- `MaPeriod` – longitud de la media móvil.
- `P2`, `P3`, `P4` – rezagos para los perceptrones.
- `Pass` – modo de decisión.
- `Sl1`/`Tp1`, `Sl2`/`Tp2`, `Sl3`/`Tp3` – stop y objetivo para cada rama.
- `CandleType` – series de velas a procesar.

## Notas
La estrategia mantiene una sola posición a la vez y la cierra cuando se alcanza el stop loss o el take profit.

## Aviso
Solo para uso educativo. No constituye asesoramiento de inversión.
