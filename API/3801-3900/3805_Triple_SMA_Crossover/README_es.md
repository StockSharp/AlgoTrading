# Triple estrategia de cruce SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Triple SMA Crossover replica el MQL asesor experto `3sma.mq4` original. El sistema analiza tres promedios móviles simples (SMA) calculados sobre el precio de cierre y opera cuando la tendencia a corto plazo se alinea con los promedios a mediano y largo plazo. La conversión mantiene las reglas comerciales originales y las adapta a la StockSharp estrategia de alto nivel API.

## Lógica de trading
1. Calcule tres SMA con períodos configurables.
2. Salga de las posiciones largas existentes cuando el SMA rápido caiga por debajo del SMA medio.
3. Salga de las posiciones cortas existentes cuando el SMA rápido supere el SMA medio.
4. Ingrese una nueva posición larga cuando:
   - El SMA rápido está por encima del SMA medio al menos en el spread configurado.
   - El SMA medio está por encima del SMA lento al menos en el spread configurado.
   - Actualmente no hay ninguna posición larga abierta.
5. Ingrese una nueva posición corta cuando:
   - El SMA rápido está por debajo del SMA medio al menos en el spread configurado.
   - El SMA medio está por debajo del SMA lento al menos en el margen configurado.
   - Actualmente no hay ninguna posición corta abierta.

## Parámetros
- **Tipo de vela**: período de tiempo principal utilizado para calcular los promedios móviles.
- **Duración del SMA rápido**: período para el SMA rápido (MQL entrada `SMA1`).
- **Longitud del medio SMA** – Período para el medio SMA (MQL entrada `SMA2`).
- **Duración del SMA lento**: período para el SMA lento (MQL entrada `SMA3`).
- **SMA Pasos de diferencial**: filtro adicional que requiere que las SMA diverjan en una cantidad de incrementos de precio (MQL ingrese `SMAspread`).
- **Volumen comercial**: volumen de órdenes utilizado al abrir posiciones (MQL entrada `lots`).

## Notas
- El manejo de stop loss de la versión MQL se omite porque estaba deshabilitado en el script fuente.
- Todas las salidas son órdenes de mercado para alinearse con el comportamiento directo del experto original.
