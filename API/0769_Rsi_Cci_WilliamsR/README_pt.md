# RSI CCI Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina RSI, CCI e Williams %R para capturar oportunidades de reversão. Compra quando os três indicadores atingem níveis de sobrevenda e vende quando todos atingem níveis de sobrecompra. Cada operação utiliza take profit e stop loss baseados em percentual.

## Detalhes

- **Condições de entrada**:
  - **Comprado**: `RSI < RSI sobrevenda` && `CCI < CCI sobrevenda` && `Williams %R < Williams sobrevenda`
  - **Vendido**: `RSI > RSI sobrecompra` && `CCI > CCI sobrecompra` && `Williams %R > Williams sobrecompra`
- **Condições de saída**: As posições saem via take profit ou stop loss.
- **Tipo**: Reversão
- **Indicadores**: RSI, CCI, Williams %R
- **Período**: 45 minutos (padrão)
- **Stops**: Take profit e stop loss baseados em percentual
