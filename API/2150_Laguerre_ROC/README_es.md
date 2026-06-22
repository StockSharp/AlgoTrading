# Estrategia Laguerre ROC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el oscilador de tasa de cambio Laguerre para capturar reversiones de tendencia.

El oscilador Laguerre ROC suaviza la tasa de cambio mediante un filtro Laguerre de cuatro etapas.
Los valores se normalizan entre 0 y 1. Dos umbrales definen las zonas de sobrecompra y sobreventa:

- **Up Level** – los valores por encima de este nivel indican un fuerte impulso alcista.
- **Down Level** – los valores por debajo de este nivel indican un fuerte impulso bajista.

Lógica de trading:

1. Cuando el oscilador cae desde la zona de sobrecompra (valor anterior por encima de Up Level
   y el valor actual por debajo) la estrategia entra en una posición larga.
2. Cuando el oscilador sube desde la zona de sobreventa (valor anterior por debajo de Down Level
   y el valor actual por encima) la estrategia entra en una posición corta.
3. Si hay una posición larga abierta y el oscilador se vuelve bajista (valor anterior por debajo
   del nivel neutro de 0.5) la posición se cierra.
4. Si hay una posición corta abierta y el oscilador se vuelve alcista (valor anterior por encima
   de 0.5) la posición se cierra.

Parámetros:

- **Period** – longitud del período de retroceso para el cálculo de la tasa de cambio.
- **Gamma** – factor de suavizado para el filtro Laguerre.
- **Up Level** – umbral de sobrecompra.
- **Down Level** – umbral de sobreventa.
- **Candle Type** – marco temporal utilizado para los datos de velas.

El ejemplo demuestra cómo se puede recrear la lógica de un indicador personalizado dentro de una
estrategia StockSharp de alto nivel usando la tasa de cambio incorporada y el filtrado Laguerre manual.
