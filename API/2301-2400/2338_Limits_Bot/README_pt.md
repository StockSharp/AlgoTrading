# Estratégia Limits Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Coloca ordens limitadas simétricas ao redor do preço de abertura de cada vela e protege as posições com stop-loss, take-profit e trailing opcional.

## Detalhes

- **Entrada**:
  - Compra limitada em `Open - StopOrderDistance * PriceStep` se o trading comprado estiver habilitado.
  - Venda limitada em `Open + StopOrderDistance * PriceStep` se o trading vendido estiver habilitado.
- **Saída**: Fechamento a mercado ao acionar stop-loss, take-profit ou trailing stop.
- **Comprado/Vendido**: Ambos.
- **Stops**: Stop-loss fixo com opção de trailing.
- **Valores padrão**:
  - `StopOrderDistance` = 5
  - `TakeProfit` = 35
  - `StopLoss` = 8
  - `TrailingStart` = 40
  - `TrailingDistance` = 30
  - `TrailingStep` = 1
  - `CandleType` = 1 minuto
- **Sessão**: Opera apenas entre `StartTime` e `EndTime`.
- **Filtros**:
  - Categoria: Price action
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
