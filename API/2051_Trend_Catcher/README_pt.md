# Estratégia de Captura de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Trend Catcher** combina o Parabolic SAR com múltiplas médias móveis simples para capturar movimentos direcionais. Aguarda que o preço cruze o Parabolic SAR na direção das médias rápidas predominantes e, em seguida, gerencia a posição usando regras dinâmicas de stop-loss e trailing.

Uma operação é aberta quando a última vela fecha no lado oposto do Parabolic SAR em comparação com a vela anterior, enquanto as médias rápidas confirmam o movimento. O stop-loss inicial é calculado a partir da distância ao ponto SAR e é limitado por valores mínimos e máximos. Os alvos de lucro são definidos como um múltiplo da distância do stop. Após o preço avançar uma quantidade especificada, o stop é movido para o ponto de equilíbrio com um pequeno deslocamento e depois acompanha o preço.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Close[0] > SAR && Close[1] < SAR_prev && FastMA > SlowMA && Close > FastMA2`.
  - **Vendido**: `Close[0] < SAR && Close[1] > SAR_prev && FastMA < SlowMA && Close < FastMA2`.
- **Critérios de saída**:
  - Níveis de stop-loss ou take-profit são atingidos.
  - Trailing stop ativado após o limite de lucro.
  - Sinal oposto fecha a posição existente.
- **Stops**: Stop-loss dinâmico baseado no SAR com ajustes opcionais de ponto de equilíbrio e trailing.
- **Valores padrão**:
  - `SlowMaPeriod = 200`
  - `FastMaPeriod = 50`
  - `FastMa2Period = 25`
  - `SarStep = 0.004`
  - `SarMax = 0.2`
  - `SlMultiplier = 1`
  - `TpMultiplier = 1`
  - `MinStopLoss = 10`
  - `MaxStopLoss = 200`
  - `ProfitLevel = 500`
  - `BreakevenOffset = 1`
  - `TrailingThreshold = 500`
  - `TrailingDistance = 10`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR, SMA
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
