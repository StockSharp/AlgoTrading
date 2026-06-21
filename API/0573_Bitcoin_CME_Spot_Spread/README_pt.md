# Spread Bitcoin CME-Spot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera o spread entre os futuros de Bitcoin da CME e o spot BTCUSD da Bitfinex usando Bandas de Bollinger.
Comprado quando o spread cai abaixo da banda inferior, vendido quando sobe acima da banda superior.
As posições são reduzidas escalonadamente em quatro níveis de take-profit e fechadas após um número fixo de barras.

## Detalhes

- **Dados**: Futuros de Bitcoin da CME e spot Bitfinex BTCUSD.
- **Entrada**: Comprado em spread sobrevendido, vendido em spread sobrecomprado.
- **Saída**: Take-profits escalonados ou fechamento após barras de retenção.
- **Instrumentos**: Futuros de Bitcoin.
- **Risco**: Saídas parciais e fechamento temporizado.
