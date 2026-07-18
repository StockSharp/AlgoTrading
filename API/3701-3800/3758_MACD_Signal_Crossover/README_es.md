# MACD Estrategia de cruce de señales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Este ejemplo convierte el MetaTrader 4 asesor experto original `MACD_v1.mq4` en una estrategia de alto nivel StockSharp. El algoritmo rastrea los cruces de convergencia y divergencia del promedio móvil (MACD) y opera en la dirección de la nueva tendencia. Las salidas protectoras opcionales replican el comportamiento del asesor original: un stop-loss, una toma de ganancias distante y un objetivo de ganancias parcial que liquida la mitad de la posición actual.

## Lógica de trading
1. **Fuente de datos**: la estrategia se suscribe a la serie de velas configuradas (velas de 5 minutos de forma predeterminada) y vincula un indicador `MovingAverageConvergenceDivergenceSignal`.
2. **Condiciones de entrada**:
   - Ingrese **long** cuando la línea MACD cruce por encima de la línea de señal. Si una posición corta está activa, se cierra antes de abrir la larga.
   - Ingrese **short** cuando la línea MACD cruce por debajo de la línea de señal. Si existe una posición larga, se cierra primero.
3. **Condiciones de salida**:
   - El cruce opuesto MACD cierra la posición actual y abre una nueva posición en la dirección opuesta.
   - Una toma de ganancias y un stop-loss configurables administrados por `StartProtection` reflejan los parámetros originales de TP/SL (medidos en puntos del instrumento).
   - Un objetivo de beneficio parcial cierra la mitad de la posición abierta una vez que el precio avanza una cantidad específica desde el nivel de entrada. La salida parcial se activa sólo una vez por posición.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| **Período rápido** | 23 | Longitud rápida de EMA para el cálculo MACD (refleja el parámetro MQL `a = 2300`). |
| **Período lento** | 40 | Longitud lenta de EMA para el cálculo MACD (`b = 4000` en la fuente). |
| **Período de señal** | 8 | Longitud de la línea de señal (`c = 800` en la fuente). |
| **Obtener ganancias** | 500 | Distancia en puntos de precio para la orden protectora de toma de ganancias. Establezca en `0` para desactivar. |
| **Detener pérdidas** | 80 | Distancia en puntos de precio para la orden protectora de stop-loss. Establezca en `0` para desactivar. |
| **Beneficio parcial** | 70 | Distancia en puntos de precio para cerrar la mitad de la posición abierta. Establezca en `0` para deshabilitar las salidas parciales. |
| **Tipo de vela** | marco de tiempo de 5 minutos | Serie de velas utilizadas para los cálculos de indicadores.

## Notas
- Los parámetros del indicador se escalaron a longitudes típicas de MACD (23/40/8) porque el script MQL los expresaba como centésimas (2300/4000/800).
- La estrategia restaura automáticamente la bandera de salida parcial cada vez que se acumula una nueva posición.
- Los ayudantes de dibujo de gráficos resaltan velas, valores MACD y las operaciones de la estrategia cuando hay un panel de gráfico disponible.
- El manejo de volúmenes se basa en la propiedad de la estrategia base `Volume`. Ajústelo antes de comenzar la estrategia para que coincida con el tamaño de su instrumento.
