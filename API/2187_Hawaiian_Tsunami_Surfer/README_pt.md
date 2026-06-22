# Hawaiian Tsunami Surfer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia procura picos repentinos de momentum e opera contra eles. Ela calcula a variação percentual do preço de fechamento em uma barra usando um indicador Momentum. Quando a variação percentual excede um pequeno limite, o movimento é considerado um "tsunami". A estratégia vende após um forte pico de alta e compra após um forte pico de baixa. Stop-loss e take-profit de proteção são aplicados em passos de preço através do StartProtection.

## Detalhes

- **Critérios de entrada**:
  - Vender quando o percentual de momentum > `TsunamiStrength`.
  - Comprar quando o percentual de momentum < `-TsunamiStrength`.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop-loss ou take-profit de proteção.
- **Stops**: Sim, através do StartProtection.
- **Valores padrão**:
  - `MomentumPeriod` = 1
  - `TsunamiStrength` = 0.24
  - `TakeProfitPoints` = 500
  - `StopLossPoints` = 700
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Momentum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
