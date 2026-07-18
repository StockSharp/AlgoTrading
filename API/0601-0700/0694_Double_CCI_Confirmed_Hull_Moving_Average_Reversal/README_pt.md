# Estratégia de Reversão com Hull MA Confirmada por Duplo CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprado quando o preço cruza acima da Hull Moving Average com confirmação dos indicadores CCI rápido e lento. Uma EMA de rastreamento gerencia o lucro após uma ativação baseada em ATR.

Os testes mostram retorno anual moderado. Funciona melhor em mercados mistos.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Preço cruza acima do HMA, fechamento acima do HMA, CCI rápido > 0, CCI lento > 0
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - **Comprado**: Fechamento abaixo da EMA de rastreamento após ativação ou mínima atinge o stop ATR
- **Stops**: Sim.
- **Valores padrão**:
  - `StopLossAtrMultiplier` = 1.75
  - `TrailingActivationMultiplier` = 2.25
  - `FastCciPeriod` = 25
  - `SlowCciPeriod` = 50
  - `HullMaLength` = 34
  - `TrailingEmaLength` = 20
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão
  - Direção: Somente comprado
  - Indicadores: CCI, HMA, EMA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
