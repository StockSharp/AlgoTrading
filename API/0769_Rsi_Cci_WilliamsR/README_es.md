# RSI CCI Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina RSI, CCI y Williams %R para capturar oportunidades de reversión. Compra cuando los tres indicadores alcanzan niveles de sobreventa y vende cuando todos alcanzan niveles de sobrecompra. Cada operación utiliza take profit y stop loss basados en porcentaje.

## Detalles

- **Condiciones de entrada**:
  - **Largo**: `RSI < RSI sobreventa` && `CCI < CCI sobreventa` && `Williams %R < Williams sobreventa`
  - **Corto**: `RSI > RSI sobrecompra` && `CCI > CCI sobrecompra` && `Williams %R > Williams sobrecompra`
- **Condiciones de salida**: Las posiciones salen mediante take profit o stop loss.
- **Tipo**: Reversión
- **Indicadores**: RSI, CCI, Williams %R
- **Marco temporal**: 45 minutos (por defecto)
- **Stops**: Take profit y stop loss basados en porcentaje
