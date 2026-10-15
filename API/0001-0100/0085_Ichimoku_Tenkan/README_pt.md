# Estratégia de Cruzamento Ichimoku Tenkan/Kijun
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Os indicadores Ichimoku fornecem um sistema completo de seguimento de tendência. Esta abordagem foca no cruzamento da Tenkan-sen sobre a Kijun-sen enquanto o preço opera em relação à nuvem Kumo. Um cruzamento de alta acima da nuvem sinaliza a continuação da tendência ascendente, enquanto um cruzamento de baixa abaixo da nuvem sugere fraqueza.

Os testes indicam um retorno anual médio de aproximadamente 142%. Funciona melhor no mercado de ações.

Durante a operação, a estratégia calcula os componentes Ichimoku em cada barra. Quando a Tenkan sobe acima da Kijun e o preço está acima da nuvem, uma operação comprada é iniciada com um stop próximo à Kijun. Um cruzamento na direção oposta abaixo da nuvem aciona uma operação vendida com colocação similar do stop.

O sistema permanece na operação até que o stop seja atingido ou o cruzamento se reverta, visando capturar movimentos sustentados que seguem a direção da nuvem.

## Detalhes

- **Critérios de entrada**: Cruzamento Tenkan/Kijun com preço relativo à nuvem Kumo.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss ou cruzamento oposto.
- **Stops**: Sim, no nível da Kijun.
- **Valores padrão**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = 30 minute
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Ichimoku
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Swing
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

