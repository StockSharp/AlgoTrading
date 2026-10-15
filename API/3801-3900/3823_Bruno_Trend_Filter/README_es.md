# Estrategia de tendencia de Bruno
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Bruno Trend Strategy es una StockSharp versión del MetaTrader asesor experto "Bruno_v1". La estrategia opera con velas de 30 minutos y se centra en señales alcistas sincronizadas de varios indicadores clásicos de seguimiento de tendencias y de impulso. Sólo se abren posiciones largas, imitando al experto original que se concentraba en las rupturas alcistas confirmadas por la alineación de los indicadores.

## Lógica de trading

1. **Plazo**: velas de 30 minutos.
2. **Indicadores**:
   - Media móvil simple (SMA) con longitud 4 utilizada como indicador de impulso a corto plazo.
   - Medias móviles exponenciales (EMA) con longitudes 8 y 21 para definir la dirección de la tendencia principal.
   - Índice direccional promedio (ADX) con período 13 para garantizar la fuerza direccional a través de los componentes +DI y -DI.
   - Stochastic Oscilador con parámetros %K=21, %D=3, desaceleración=3 para confirmar el impulso y evitar niveles de sobrecompra.
   - MACD (13, 34, 8) para confirmación de histograma y línea de señal.
   - Parabolic SAR (paso 0.055, máximo 0.21) para verificar la aceleración ascendente y gestionar las salidas.
3. **Reglas de entrada**:
   - EMA(8) debe estar por encima de EMA(21).
   - Filtro ADX: +DI mayor que -DI y superior a 20.
   - Filtro Stochastic: %K por encima de %D pero aún por debajo de 80 para mantenerse alejado de los extremos de sobrecompra.
   - MACD histograma por encima de cero y por encima de la línea de señal.
   - Parabolic SAR aumentando (el SAR actual más alto que la lectura anterior).
   - La posición actual debe ser plana o corta. Cualquier posición corta se cierra antes de ingresar a la nueva operación larga.
4. **Reglas de salida**:
   - Cierre la posición larga cuando el cierre de la vela anterior caiga por debajo del valor Parabolic SAR anterior, replicando el activador de salida MetaTrader.

## Gestión del riesgo

- Tamaño de lote predeterminado: 0,1 lotes.
- Protección opcional estilo MetaTrader: toma de ganancias de 50 pips y stop-loss de 30 pips, configurada con `StartProtection`. Las paradas finales están deshabilitadas de forma predeterminada para reflejar el script original.

## Notas

- La estrategia ignora la configuración corta no utilizada del código MetaTrader, coincidiendo con el comportamiento original donde las operaciones cortas se desactivaron efectivamente.
- Los valores del indicador se procesan a través del nivel alto API de StockSharp para evitar el almacenamiento en búfer manual y mantenerse alineados con las pautas del proyecto.
