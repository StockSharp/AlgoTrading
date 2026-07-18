# Estratégia FuzzyLogic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia FuzzyLogic replica o consultor especialista do MT5 **Fuzzy logic (edição de barabashkakvn)** usando a API de alto nível do StockSharp. O sistema mede a força da tendência e o esgotamento do momentum com osciladores de Bill Williams e indicadores de momentum, converte essas leituras em graus de pertinência difusa e as agrega em uma única pontuação de decisão entre 0 e 1.

As ações de trading são acionadas quando a pontuação difusa cruza limites calibrados:

- **Decision &gt; 0.75** – abrir uma posição vendida (forte esgotamento / condições de sobrecompra).
- **Decision &lt; 0.25** – abrir uma posição comprada (configuração de reversão altista forte).

As posições são gerenciadas com distâncias fixas de take-profit e stop-loss expressas em passos de preço. Quando uma distância de trailing stop é fornecida, o stop protetor é convertido em um trailing.

## Conjunto de indicadores

| Componente | Finalidade |
| --- | --- |
| **Oscilador Gator** (construído a partir das linhas Alligator) | Mede a soma dos spreads mandíbula–dentes e dentes–lábios para avaliar a expansão ou contração da tendência. |
| **Williams %R (14)** | Detecta níveis de sobrecompra / sobrevenda. |
| **Acceleration/Deceleration Oscillator (AC)** | Conta mudanças consecutivas de momentum para estimar a aceleração da tendência. |
| **DeMarker (14)** | Confirma o esgotamento por meio de comparações de máximas/mínimas. Implementado diretamente dentro da estratégia. |
| **RSI (14)** | Rastreia oscilações clássicas de momentum. |

As linhas Alligator são calculadas com médias móveis suavizadas e deslocadas para frente exatamente como no consultor especialista original para reproduzir o oscilador Gator. Os valores de AC são derivados do Awesome Oscillator (diferença SMA 5/34) menos sua média móvel de 5 períodos, fornecendo leituras idênticas ao indicador `iAC` do MT5.

## Lógica de trading

1. O valor de cada indicador é mapeado para cinco conjuntos de pertinência difusa (muito baixista → muito altista). Funções lineares por partes replicam os arrays originais do MT5.
2. Os cinco grupos de pertinência são ponderados (0.133, 0.133, 0.133, 0.268, 0.333) e agregados em quatro compartimentos de resumo.
3. A pontuação de decisão difusa é calculada como `Σ summary[x] * (0.2 * (x + 1) - 0.1)`, produzindo valores no intervalo `[0, 1]`.
4. Os sinais são avaliados uma vez por vela fechada. A estratégia permanece sem posição a menos que a decisão ultrapasse os limites de entrada.
5. O tamanho da ordem depende da propriedade `Volume` (padrão 1). Os stops protetores são registrados por meio de `StartProtection`.

## Gestão de risco

- **StopLossPoints** – distância absoluta (em passos de preço) para o stop protetor. Usado quando `TrailingStopPoints` é zero.
- **TrailingStopPoints** – se &gt; 0, a distância do stop-loss muda para este valor e o modo trailing é ativado.
- **TakeProfitPoints** – distância absoluta para o alvo de lucro.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Período / tipo de vela usado para cálculos. |
| `BuyThreshold` | Pontuação difusa abaixo da qual uma entrada comprada é aberta. Padrão 0.25. |
| `SellThreshold` | Pontuação difusa acima da qual uma entrada vendida é aberta. Padrão 0.75. |
| `StopLossPoints` | Distância do stop-loss em passos de preço do instrumento. Padrão 60. |
| `TakeProfitPoints` | Distância de take-profit em passos de preço. Padrão 20. |
| `TrailingStopPoints` | Distância do trailing stop em passos de preço. Padrão 0 (desativado). |
| `WilliamsPeriod` | Lookback para Williams %R. Padrão 14. |
| `RsiPeriod` | Lookback para RSI. Padrão 14. |
| `DeMarkerPeriod` | Lookback para o cálculo integrado de DeMarker. Padrão 14. |

## Notas de implementação

- O oscilador DeMarker é implementado manualmente porque o StockSharp não expõe uma versão integrada. Os deltas de máximas e mínimas são enfileirados para reproduzir as somas do MT5.
- O histórico de AC armazena os cinco valores concluídos mais recentes para que a lógica difusa possa verificar sequências de aceleração consecutivas, assim como `iAC(..., shift)` no MT5.
- Os buffers de mandíbula/dentes/lábios do Alligator introduzem o mesmo deslocamento para frente (8/5/3 barras) antes de derivar os valores do histograma Gator.
- A estratégia só abre uma nova posição quando `Position == 0`, respeitando o comportamento de posição única do consultor especialista original.

## Passos de uso

1. Vincule a estratégia a uma carteira e um ativo no Designer/Backtester.
2. Configure a série de velas desejada via `CandleType`.
3. Ajuste os limites ou as distâncias de stop se necessário.
4. Inicie a estratégia; ela operará automaticamente quando a pontuação difusa cruzar os níveis configurados.
