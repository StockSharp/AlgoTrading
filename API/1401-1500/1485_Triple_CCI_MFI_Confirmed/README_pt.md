# Estratégia Triple CCI MFI Confirmada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprado quando o CCI rápido cruza acima de zero enquanto o CCI médio e o lento permanecem positivos, o preço está acima da EMA e o MFI ultrapassa 50. O lucro é acompanhado pela EMA após uma ativação baseada em ATR.

Os testes mostram desempenho moderado; funciona melhor em mercados em tendência.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: CCI rápido cruza acima de 0, CCI médio > 0, CCI lento > 0, MFI > 50, fechamento acima da EMA
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - **Comprado**: Fechamento abaixo da EMA de rastreamento após ativação ou mínimo toca o stop ATR
- **Stops**: Sim.
- **Valores padrão**:
  - `StopLossAtrMultiplier` = 1.75
  - `TrailingActivationMultiplier` = 2.25
  - `FastCciPeriod` = 14
  - `MiddleCciPeriod` = 25
  - `SlowCciPeriod` = 50
  - `MfiLength` = 14
  - `EmaLength` = 50
  - `TrailingEmaLength` = 20
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: CCI, MFI, EMA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
