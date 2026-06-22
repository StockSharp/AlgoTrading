# Estratégia Exp Super Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia convertida do script MQL **Exp_Super_Trend.mq5** (ID 14269). Ela segue a direção do indicador SuperTrend e inverte posições sempre que a tendência muda. A implementação usa a API de alto nível do StockSharp e o indicador SuperTrend integrado.

O indicador calcula uma linha dinâmica de suporte ou resistência baseada em ATR. Quando o preço permanece acima dessa linha, a tendência é considerada de alta; caso contrário, de baixa. A estratégia abre uma posição comprada durante os períodos de alta e muda para uma posição vendida durante os períodos de baixa. Cada mudança do indicador provoca uma inversão imediata de posição.

Essa abordagem funciona melhor em mercados com tendência, onde grandes movimentos seguem um rompimento. Também é útil como modelo educacional mostrando como conectar um indicador usando `BindEx` e executar ordens de mercado em velas concluídas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: SuperTrend sinaliza uma tendência de alta.
  - Vendido: SuperTrend sinaliza uma tendência de baixa.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto do SuperTrend (a posição é invertida).
- **Stops**: Sem stop loss explícito; a linha do indicador atua como trailing stop.
- **Valores padrão**:
  - `AtrPeriod` = 10
  - `Multiplier` = 3m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SuperTrend
  - Stops: Baseado em indicador
  - Complexidade: Básico
  - Período: Médio (1 hora por padrão)
  - Sazonalidade: Nenhum
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
