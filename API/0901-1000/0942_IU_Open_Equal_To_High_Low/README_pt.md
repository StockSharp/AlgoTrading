# Estratégia IU Abertura Igual ao Máximo ou Mínimo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Entra comprado no primeiro candle do dia quando a abertura é igual à mínima, e entra vendido quando a abertura é igual à máxima. O stop-loss usa o candle anterior e o take profit é baseado na relação `RiskReward`.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: a abertura do primeiro candle é igual à sua mínima.
  - **Vendido**: a abertura do primeiro candle é igual à sua máxima.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Stop-loss na mínima do candle anterior para comprado, máxima do candle anterior para vendido.
  - Take profit calculado a partir do preço de entrada usando `RiskReward`.
- **Stops**: Sim.
- **Valores padrão**:
  - `RiskReward` = 2.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Ação do preço
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
