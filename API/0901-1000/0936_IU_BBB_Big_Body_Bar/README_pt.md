# Estratégia IU BBB de Barra de Grande Corpo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra quando o corpo da vela atual é várias vezes maior que o tamanho médio do corpo das últimas 20 velas. Uma grande vela de alta abre uma posição comprada, enquanto uma grande vela de baixa abre uma posição vendida. As posições são protegidas com um trailing stop baseado em ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: corpo > corpo médio * BigBodyThreshold e fechamento > abertura.
  - **Vendido**: corpo > corpo médio * BigBodyThreshold e fechamento < abertura.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Trailing stop ATR.
- **Stops**: Trailing stop usando ATR * AtrFactor.
- **Valores padrão**:
  - `BigBodyThreshold` = 4
  - `AtrLength` = 14
  - `AtrFactor` = 2
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: SMA, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

