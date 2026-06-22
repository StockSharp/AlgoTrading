# Estratégia UP3x1 Premium
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia UP3x1 Premium é um port em C# do consultor especializado do MetaTrader *up3x1_premium_v2M*. Combina cruzamentos de EMA rápida/lenta com filtros de velas de grande amplitude e um filtro de contexto diário para capturar rompimentos de momentum enquanto mantém o risco gerenciado através de alvos fixos e trailing stops.

## Como Funciona

1. **Detecção de Tendência**
   - Calcula duas EMAs no período de trabalho (padrão 12 e 26 períodos).
   - Rastreia os dois valores EMA anteriores para identificar cruzamentos altistas ou baixistas semelhante à lógica MQL.
   - Mantém uma EMA diária para entender o viés mais amplo.

2. **Lógica de Entrada**
   - **Setups comprados** se ativam quando qualquer um dos seguintes ocorre:
     - A EMA rápida cruza acima da EMA lenta e as duas aberturas de velas anteriores mostram progresso ascendente.
     - A vela anterior forma uma barra altista de grande amplitude cujo corpo excede o limite de corpo configurado.
     - À meia-noite, se a vela diária anterior fechou notavelmente abaixo de sua abertura (capitulação), um sinal de repique é permitido.
     - O preço negocia acima da EMA diária atual, favorecendo o lado comprado.
   - **Setups vendidos** se ativam quando as condições espelho valem (cruzamento EMA baixista, barra baixista de grande amplitude, ou reversão de meia-noite na direção oposta).
   - Quando os gatilhos comprado e vendido se ativam simultaneamente, a estratégia segue o relacionamento EMA prevalecente para decidir.

3. **Gerenciamento de Saída**
   - Uma posição aberta é fechada quando:
     - As EMAs convergem dentro de ±0.1%, sinalizando perda de convicção direcional.
     - O preço toca os offsets de take-profit ou stop-loss definidos em unidades de preço absolutas.
     - O trailing stop (se habilitado) é puxado atrás do preço e subsequentemente atingido.

4. **Manipulação de Posição**
   - Trades são abertos apenas quando a estratégia está zerada, correspondendo ao comportamento original do EA.
   - O volume é controlado via o parâmetro `OrderVolume` e aplicado a cada ordem de mercado.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `OrderVolume` | Tamanho de ordem em lotes/contratos para cada trade. |
| `FastEmaLength` / `SlowEmaLength` | Períodos para as EMAs rápida e lenta no período de trabalho. |
| `DailyEmaLength` | Período para a EMA calculada nas velas diárias. |
| `TakeProfit` | Alvo de lucro absoluto em unidades de preço (definir como zero para desabilitar). |
| `StopLoss` | Distância de stop absoluta em unidades de preço (definir como zero para desabilitar). |
| `TrailingStop` | Distância de trailing que segue o preço assim que o movimento excede o limiar. |
| `RangeThreshold` | Amplitude total mínima que a vela anterior deve exceder para qualificar como barra de grande amplitude. |
| `BodyThreshold` | Tamanho mínimo do corpo da vela que define barras de impulso altistas/baixistas. |
| `DailyReversalThreshold` | Tamanho da reversão diária anterior necessária durante o filtro de meia-noite. |
| `CandleType` | Período de trabalho para a lógica principal de EMA e preço. |
| `DailyCandleType` | Período superior usado para o contexto EMA diário. |

## Notas de Uso

- Os padrões imitam as constantes numéricas encontradas no EA original (convertidas de valores em pontos para offsets de preço decimal).
- Ajuste os limiares baseados em preço (`TakeProfit`, `StopLoss`, `TrailingStop`, limiares de amplitude/corpo) para corresponder ao tamanho do tick do instrumento negociado.
- O filtro EMA diário substitui o viés comprado incondicional presente no script MQL, mantendo os trades alinhados com a tendência do período superior prevalecente.
- Sempre faça backtesting em dados históricos e testes forward em ambiente demo antes de habilitar o trading ao vivo.
