# Estratégia de Alerta de Sinal do Oscilador Aroon
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o Oscilador Aroon para gerar sinais de negociação quando o oscilador cruza níveis predefinidos. Uma posição comprada é aberta quando o oscilador cruza para cima o nível baixo (padrão -50). Uma posição vendida é aberta quando cruza para baixo o nível alto (padrão +50). Sinais opostos fecham ou revertem a posição.

## Detalhes

- **Critérios de entrada:**
  - **Comprado**: O oscilador Aroon cruza para cima o nível baixo.
  - **Vendido**: O oscilador Aroon cruza para baixo o nível alto.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - O sinal inverso fecha ou reverte automaticamente a posição atual.
- **Stops**: Nenhum.
- **Filtros**: Nenhum.
- **Período**: Candles de 4 horas por padrão (configurável).

## Parâmetros

- `AroonPeriod` – período de lookback para o oscilador Aroon (padrão 9).
- `UpLevel` – limiar superior para sinais de venda (padrão +50).
- `DownLevel` – limiar inferior para sinais de compra (padrão -50).
- `CandleType` – período de candles para os cálculos (padrão 4 horas).
