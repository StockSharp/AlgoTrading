# Padrão Estrela Cadente (Shooting Star Pattern)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A vela estrela cadente (Shooting Star) frequentemente aparece após uma alta e avisa sobre uma reversão. Esta estratégia busca uma sombra superior longa em relação ao corpo com pouca sombra inferior.

Os testes indicam um retorno anual médio de aproximadamente 67%. Funciona melhor no mercado de ações.

Se a confirmação for necessária, a vela seguinte deve fechar abaixo antes de entrar vendido. Caso contrário, a operação pode ser aberta imediatamente. Os stops são colocados acima da máxima do padrão.

## Detalhes

- **Critérios de entrada**: Estrela cadente detectada e confirmação se habilitada.
- **Comprado/Vendido**: Somente vendido.
- **Critérios de saída**: Stop-loss ou saída discricionária.
- **Stops**: Sim.
- **Valores padrão**:
  - `ShadowToBodyRatio` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
  - `ConfirmationRequired` = true
- **Filtros**:
  - Categoria: Padrão
  - Direção: Somente vendido
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
