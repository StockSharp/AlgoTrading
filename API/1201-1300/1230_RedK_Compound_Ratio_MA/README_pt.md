# Estratégia RedK de MA de Razão Composta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera comprado quando a média móvel de razão composta (CoRa Wave) sobe e vendido quando cai.

## Detalhes

- **Critérios de entrada**:
  - Comprado: O valor da CoRa Wave sobe acima do valor anterior
  - Vendido: O valor da CoRa Wave cai abaixo do valor anterior
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Sinal oposto
- **Stops**: Nenhum
- **Valores padrão**:
  - `Length` = 20
  - `RatioMultiplier` = 2m
  - `AutoSmoothing` = true
  - `ManualSmoothing` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Compound Ratio MA, Weighted Moving Average
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Nenhum
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
