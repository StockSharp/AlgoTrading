# Estratégia de Gestão de Risco na Saída Long/Short
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia modelo que mostra como gerenciar posições compradas e vendidas com controles de risco baseados em percentual. Utiliza gatilhos simples de igualdade de preço e saídas opcionais por tempo.

## Detalhes

- **Critérios de entrada**: O preço de fechamento é igual ao valor comprado ou vendido configurado.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop loss, take profit ou saída temporal após N barras.
- **Stops**: Stop loss e take profit percentuais com trailing opcional.
- **Valores padrão**:
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 3
  - `ExitBars` = 10
  - `BarsToWait` = 10
  - `MaxTradesPerDay` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Gestão de risco
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
