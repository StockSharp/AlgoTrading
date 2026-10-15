# Estratégia SAW System 1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de rompimento coloca ordens stop no início de cada dia de negociação. Ela mede o intervalo diário médio ao longo de um número configurável de dias e usa esse valor para derivar os níveis de stop-loss e take-profit. As ordens são posicionadas em ambos os lados do preço atual e espera-se que apenas um lado seja acionado.

Na `OpenHour` especificada, a estratégia calcula os preços de buy stop e sell stop à metade da distância de stop-loss do preço de mercado atual. Os níveis de stop-loss e take-profit são definidos como percentuais do intervalo médio. Quando uma ordem stop é executada, a ordem oposta pode ser cancelada ou mantida para reversão da posição. Um recurso opcional de martingale multiplica o volume da ordem restante após uma execução.

As ordens de entrada pendentes que permanecerem não executadas até `CloseHour` são removidas para evitar exposição overnight. Após uma entrada, a estratégia coloca imediatamente ordens protetoras de stop-loss e take-profit relativas ao preço de execução.

## Detalhes

- **Critérios de entrada:**
  - Calcular o intervalo diário médio usando um ATR por `VolatilityDays` dias.
  - Calcular as distâncias de stop-loss e take-profit como `StopLossRate` e `TakeProfitRate` por cento desse intervalo.
  - Na `OpenHour` colocar ordens buy e sell stop a `offset = stopLoss/2` do preço de mercado.
- **Critérios de saída:**
  - Ordens protetoras de stop-loss e take-profit fecham posições.
  - Ordens de entrada pendentes são canceladas na `CloseHour`.
- **Modo de reversão:**
  - Se `Reverse` for verdadeiro, a ordem stop oposta permanece para reverter a posição.
  - Se `UseMartingale` também for verdadeiro, a ordem restante é re-registrada com volume multiplicado por `MartingaleMultiplier`.
- **Comprado/Vendido:** Ambas as direções.
- **Stops:** Stop-loss e take-profit fixos baseados no intervalo diário.
- **Valores padrão:**
  - `VolatilityDays` = 5
  - `OpenHour` = 7
  - `CloseHour` = 10
  - `StopLossRate` = 15%
  - `TakeProfitRate` = 30%
  - `Reverse` = false
  - `UseMartingale` = false
  - `MartingaleMultiplier` = 2.0

Esta abordagem tenta capturar rompimentos após sessões noturnas tranquilas, limitando o risco por meio de alvos ajustados à volatilidade.
