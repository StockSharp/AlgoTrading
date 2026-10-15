# Estratégia de Rompimento 04
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera rompimentos do intervalo do dia anterior.
Compra quando o preço supera a máxima do dia anterior e vende quando cai abaixo da mínima do dia anterior.
Utiliza trailing stop e take-profit fixo com dimensionamento de posição opcional baseado no saldo da conta.
A operação é desabilitada antes de um horário de início configurado na segunda-feira e após um horário de corte na sexta-feira.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Preço > Máxima anterior`
  - Vendido: `Preço < Mínima anterior`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Trailing stop ou take-profit
- **Stops**: Trailing e stop loss fixo
- **Valores padrão**:
  - `MondayHour` = 18
  - `FridayHour` = 14
  - `TrailingStop` = 21
  - `TakeProfit` = 550
  - `StopLoss` = 124
  - `UseMoneyManagement` = false
  - `PercentMM` = 8m
  - `Volume` = 0.1m
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
