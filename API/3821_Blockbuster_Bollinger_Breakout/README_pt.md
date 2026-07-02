# Estratégia de grande sucesso Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Blockbuster Bollinger Breakout é uma versão direta do MetaTrader 4 consultor especialista "BLOCKBUSTER EA". O sistema original procurou reversões agressivas depois que o preço foi empurrado além de uma banda Bollinger por uma distância configurável. Esta versão StockSharp mantém a mesma lógica enquanto adota o API de alto nível para assinaturas de velas, vinculação de indicadores e gerenciamento de posições.

## Ideia Central

1. Crie faixas Bollinger com período e desvio definidos pelo usuário.
2. Meça quando o fechamento da vela atual ultrapassa a banda superior ou abaixo da banda inferior por um deslocamento extra (em pontos).
3. Digite short se o fechamento exceder a banda superior mais o deslocamento. Insira longo se o fechamento cair abaixo da banda inferior menos o deslocamento.
4. Gerencie a posição com limites de lucros e perdas baseados em pontos idênticos às configurações de MQL.

A distância, parada e alvo são expressos em pontos do instrumento. Eles se adaptam à etapa de preço do instrumento, portanto, um valor de `3` significa três `PriceStep` unidades, independentemente do símbolo subjacente.

## Lógica detalhada

- **Cálculo do Indicador**
  - Indicador: Bollinger Bandas.
  - Entradas: preços de fechamento de velas (o código MT4 usado `PRICE_OPEN`; esta porta mantém preços de fechamento para melhor compatibilidade StockSharp enquanto preserva o comprimento da banda e os parâmetros de desvio).
  - Parâmetros:
    - `BollingerPeriod`: número de velas utilizadas na média móvel e desvio padrão.
    - `BollingerDeviation`: multiplicador de desvio padrão para as faixas superior e inferior.
  - Compensação adicional `DistancePoints` (convertida em preço usando o instrumento `PriceStep`).

- **Condições de entrada**
  - **Longo**: `Close < LowerBand - Distance` e a posição líquida atual é estável ou curta.
  - **Short**: `Close > UpperBand + Distance` e a posição líquida atual é estável ou longa.
  - Qualquer posição oposta aberta é achatada pelo tamanho da ordem de mercado `TradeVolume + |Position|` para espelhar o comportamento MT4 "Apenas uma ordem".

- **Exit Conditions**
  - As posições são monitoradas em cada vela finalizada. O lucro não realizado em pontos é calculado usando o instrumento `PriceStep`.
  - **Take Profit**: se o lucro atingir ou exceder `ProfitTargetPoints`.
  - **Stop Loss**: se a perda atingir ou exceder `LossLimitPoints`.
  - As saídas são realizadas com ordens de mercado que fecham toda a posição.

- **Gerenciamento de risco e dinheiro**
  - `TradeVolume` define o tamanho base do pedido. Corresponder a entrada MetaTrader "Lotes" é tão simples quanto definir o mesmo valor numérico.
  - Tanto a parada quanto o destino podem ser desativados definindo o respectivo parâmetro como `0`.
  - Quando ambos os limites estão ativados, o stop é avaliado após a meta, exatamente como o EA original verificou primeiro o ramo de lucro.

- ** Rastreamento de estado **
  - A estratégia registra o preço de entrada no momento do sinal e o utiliza para todos os cálculos subsequentes de lucros/perdas.
  - Se uma ordem de saída nivelar a posição, o estado será redefinido automaticamente.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `BollingerPeriod` | 20 | Número de velas na média móvel das bandas Bollinger. |
| `BollingerDeviation` | 2,0 | Multiplicador de desvio padrão. |
| `DistancePoints` | 3 | Distância extra além da banda antes de uma negociação ser realizada (pontos de instrumento). |
| `ProfitTargetPoints` | 3 | Limite de lucro em pontos de instrumento. Defina como 0 para desativar. |
| `LossLimitPoints` | 20 | Limite de stop-loss em pontos do instrumento. Defina como 0 para desativar. |
| `TradeVolume` | 1 | Volume para novas entradas. |
| `CandleType` | Período de 1 minuto | Tipo de vela usado para cálculos. |

## Notas de uso

- Funciona em qualquer instrumento que forneça velas e um `PriceStep` diferente de zero. Pares Forex, CFDs de índices e futuros líquidos refletem melhor o ambiente EA original.
- Como o indicador agora depende dos preços de fechamento, recomenda-se testar no período pretendido para garantir um comportamento semelhante ao da versão MT4.
- A estratégia usa ajudantes `CreateChartArea` para visualizar velas, as bandas Bollinger e negociações executadas quando um gráfico está disponível na IU.
- A lógica pressupõe avaliação contínua das velas finalizadas, garantindo comportamento determinístico em backtesting e negociação ao vivo.

## Etiquetas

- Categoria: Breakout contra-tendência
- Direção: Ambos
- Indicadores: Bollinger Bandas
- Paradas: Sim (configurável)
- Prazo: Curto prazo (padrão 1 minuto)
- Complexidade: Simples
