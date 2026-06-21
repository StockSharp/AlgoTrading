# Estratégia Ichimoku Clouds Comprado e Vendido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o cruzamento de Tenkan-sen e Kijun-sen do indicador Ichimoku. Os cruzamentos são classificados como fortes, neutros ou fracos dependendo do valor de Tenkan em relação à nuvem. Dependendo do modo de negociação selecionado, abre posições compradas ou vendidas quando a força de sinal escolhida ocorre. Take profit e stop loss opcionais baseados em percentual podem fechar posições ou sinais opostos conforme configurado.

## Detalhes

- **Critérios de entrada**:
  - Tenkan-sen cruza acima de Kijun-sen e a força do sinal corresponde às opções compradas selecionadas.
  - Tenkan-sen cruza abaixo de Kijun-sen e a força do sinal corresponde às opções vendidas selecionadas.
- **Comprado/Vendido**: Configurável, padrão comprado.
- **Critérios de saída**:
  - Sinais opostos conforme definido pelas opções de saída.
  - Percentuais opcionais de take profit ou stop loss.
- **Stops**: Take profit e stop loss percentuais.
- **Valores padrão**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `TakeProfitPct` = 0
  - `StopLossPct` = 0
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Ichimoku
  - Stops: Opcional
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
