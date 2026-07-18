# Estrategia Force DiverSign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Force DiverSign opera basándose en señales de divergencia entre dos indicadores Force Index calculados con diferentes períodos de suavizado.
Busca un patrón de reversión de tres velas junto con oscilaciones opuestas en los valores rápido y lento del Force. Cuando aparece una divergencia alcista,
la estrategia compra; cuando aparece una divergencia bajista, vende.

## Parámetros
- `Period1` – período para el Force Index rápido.
- `Period2` – período para el Force Index lento.
- `MaType1` – tipo de media móvil usada para suavizar el Force Index rápido.
- `MaType2` – tipo de media móvil usada para suavizar el Force Index lento.
- `CandleType` – marco temporal de las velas para los cálculos.

## Lógica de operación
1. Calcular el Force Index bruto como el volumen multiplicado por el cambio del precio de cierre.
2. Suavizar el valor bruto con dos medias móviles para obtener las series Force rápida y lenta.
3. Almacenar los últimos cinco valores de Force y las últimas cuatro velas.
4. **Comprar** cuando:
   - Las tres velas anteriores forman un patrón bajo–alto–bajo.
   - Ambas series Force forman un mínimo local y luego suben.
   - El Force rápido y el lento se mueven en direcciones opuestas entre la primera y la tercera vela.
5. **Vender** cuando:
   - Las tres velas anteriores forman un patrón alto–bajo–alto.
   - Ambas series Force forman un máximo local y luego caen.
   - El Force rápido y el lento se mueven en direcciones opuestas entre la primera y la tercera vela.

Las posiciones se invierten en cada señal: una compra cierra un corto existente y una venta cierra un largo.
