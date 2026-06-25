# Estrategia Slime Mold RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una conversión directa del asesor experto MQL4 "Slime_Mold_RSI_v1.1". La estrategia construye un único perceptrón combinando cuatro lecturas de RSI (12, 36, 108 y 324) calculadas sobre el precio mediano. Cada valor de RSI se normaliza del rango original 0–100 a -1…+1 y se multiplica por un peso configurable. Un cruce por cero de la suma ponderada invierte la posición.

## Cómo Funciona
- Calcular el precio mediano de cada vela finalizada y alimentarlo en cuatro indicadores de Índice de Fuerza Relativa con longitudes de 12, 36, 108 y 324.
- Normalizar cada valor de RSI al intervalo -1…+1 y aplicar el peso correspondiente. Los valores predeterminados (-100) reproducen los coeficientes originales del perceptrón (`x - 100`).
- Sumar las cuatro entradas ponderadas para producir la salida del perceptrón de la vela actual.
- Comparar el valor más reciente con la salida del perceptrón de la vela anterior para detectar cruces por cero y generar señales de trading.

## Reglas de Trading
- **Entrada larga**: El valor anterior del perceptrón es inferior a cero y el valor actual sube por encima de cero. La estrategia cierra cualquier exposición corta y establece una posición larga de tamaño `Volume`.
- **Entrada corta**: El valor anterior del perceptrón es superior a cero y el valor actual cae por debajo de cero. La estrategia sale de cualquier posición larga y abre una posición corta de tamaño `Volume`.
- **Gestión de posiciones**: No hay objetivos de beneficio explícitos ni órdenes de stop-loss. Las posiciones solo se cambian cuando ocurre un nuevo cruce por cero.

## Parámetros
- `Weight1` – coeficiente aplicado a la entrada de RSI normalizada de 12 períodos.
- `Weight2` – coeficiente aplicado a la entrada de RSI normalizada de 36 períodos.
- `Weight3` – coeficiente aplicado a la entrada de RSI normalizada de 108 períodos.
- `Weight4` – coeficiente aplicado a la entrada de RSI normalizada de 324 períodos.
- `CandleType` – marco temporal de las velas suministradas a la estrategia. El valor predeterminado es velas de 1 hora.

## Detalles
- **Criterios de entrada**: Cruce por cero del perceptrón RSI ponderado.
- **Largo/Corto**: Ambos (siempre en el mercado después de la primera señal).
- **Criterios de salida**: El cruce opuesto por cero invierte la posición.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Weight1` = -100
  - `Weight2` = -100
  - `Weight3` = -100
  - `Weight4` = -100
  - `CandleType` = velas de 1 hora
- **Filtros**:
  - Categoría: Perceptrón / Oscilador
  - Dirección: Bidireccional
  - Indicadores: RSI (precio mediano)
  - Stops: No
  - Complejidad: Intermedio (requiere cuatro indicadores de largo horizonte)
  - Marco temporal: Configurable (predeterminado intradía por hora)
  - Estacionalidad: No
  - Redes neuronales: Perceptrón lineal
  - Divergencia: No
  - Nivel de riesgo: Depende del volumen y pesos elegidos

## Notas
- La implementación mantiene el registro de la salida previa del perceptrón incluso cuando el trading está deshabilitado para garantizar la continuidad del estado una vez que el trading se reanuda.
- El precio mediano se utiliza para coincidir con la configuración `PRICE_MEDIAN` del script original de MetaTrader.
- La estrategia invierte las posiciones instantáneamente, así que tenga en cuenta el deslizamiento potencial al elegir pesos y volumen.
