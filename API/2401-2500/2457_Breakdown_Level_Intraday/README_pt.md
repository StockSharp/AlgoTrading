# Estratégia de Rompimento de Nível Intradiário
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Coloca ordens de rompimento ao redor da máxima e mínima do dia anterior em um horário especificado. Entra comprado quando o preço cruza acima da máxima mais um delta e vendido quando o preço cai abaixo da mínima menos o delta. O gerenciamento de posição inclui stop loss opcional, take-profit, break-even e trailing stop.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o preço cruza acima da máxima do dia anterior + `Delta`
  - Vendido: o preço cruza abaixo da mínima do dia anterior − `Delta`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Stop loss ou take-profit atingido
  - Ativação do trailing stop ou ajuste de break-even
- **Stops**: Pontos a partir do preço de entrada
- **Valores padrão**:
  - `OrderTime` = TimeSpan.Zero
  - `Delta` = 6
  - `StopLoss` = 120
  - `TakeProfit` = 90
  - `NoLoss` = 0
  - `Trailing` = 0
  - `Volume` = 1m
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
