# Estratégia E TurboFx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de reversão de momentum adaptada do especialista MQL5 "e-TurboFx". O sistema observa uma série de velas cujos corpos crescem em tamanho na mesma direção. Após várias velas de baixa com corpos em expansão, a estratégia compra esperando uma recuperação. Após várias velas de alta com corpos crescentes, vende. Stop-loss e take-profit opcionais são definidos em pontos de preço brutos.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `N` velas de baixa consecutivas e cada corpo maior que o anterior
  - Vendido: `N` velas de alta consecutivas e cada corpo maior que o anterior
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop-loss ou take-profit
- **Stops**: Pontos via `StartProtection`
- **Valores padrão**:
  - `BarsCount` = 3
  - `StopLossPoints` = 700
  - `TakeProfitPoints` = 1200
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Price Action
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
