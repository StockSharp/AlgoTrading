# Estratégia de Armadilha de Volume por Captura de Liquidez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aguarda uma captura de liquidez baixista com volume plano que forma uma lacuna de valor justo. Quando o preço fecha acima da parte superior da lacuna enquanto o volume permanece próximo de sua média móvel, coloca uma ordem limitada de compra na parte inferior da lacuna com stop loss e take profit simétricos.

## Detalhes

- **Condição de entrada**: `Close[2] < Open[1]` && `Close > High[1]` && rompimento baixista com volume plano
- **Critérios de saída**: stop loss abaixo do fundo da lacuna pela altura da lacuna, take profit em `High[1]`
- **Tipo**: Reversão
- **Indicadores**: SMA de Volume
- **Período**: 1 minuto (padrão)
