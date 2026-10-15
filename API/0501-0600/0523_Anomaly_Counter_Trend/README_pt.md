# Estratégia de Contra-Tendência por Anomalia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O algoritmo detecta movimentos percentuais bruscos em uma janela curta e opera contra eles. Quando o preço sobe acima do limiar, vende; quando o preço cai abaixo do limiar, compra. Stop-loss e take-profit são definidos em ticks.

## Detalhes

- **Critérios de entrada**: A variação percentual na janela de lookback supera o limiar.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop-loss ou take-profit.
- **Stops**: Sim.
- **Valores padrão**:
  - `PercentageThreshold` = 1
  - `LookbackMinutes` = 30
  - `StopLossTicks` = 100
  - `TakeProfitTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Contra-tendência
  - Direção: Ambos
  - Indicadores: Preço
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
