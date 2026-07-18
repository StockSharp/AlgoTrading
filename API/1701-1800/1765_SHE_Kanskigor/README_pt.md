# Estratégia SHE Kanskigor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia diária abre uma única posição por dia com base na direção da vela do dia anterior. No horário configurado, compra se o dia anterior fechou abaixo da abertura e vende se fechou acima. Um take-profit e stop-loss fixos medidos em passos de preço gerenciam o risco. Apenas uma operação é permitida por dia.

## Detalhes

- **Critérios de entrada**: Em `StartTime` comparar a abertura e o fechamento do dia anterior; comprar quando `open > close`, vender quando `open < close`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: take profit ou stop loss
- **Stops**: Sim
- **Valores padrão**:
  - `Volume` = 0.1
  - `StartTime` = 00:05
  - `TakeProfit` = 350
  - `StopLoss` = 550
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
