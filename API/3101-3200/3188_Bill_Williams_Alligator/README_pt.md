# Estratégia Bill Williams Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o expert advisor do MetaTrader 5 **"Bill Williams.mq5"** de Vladimir Karputov para a API de alto nível do StockSharp. Ela assina uma única série de velas, reconstrói os pontos fractais de Bill Williams e avalia rompimentos em relação às linhas Alligator deslocadas. Quando a vela atual fecha além do fractal ascendente ou descendente mais próximo e esse fractal está fora de todas as três curvas do Alligator (Mandíbula, Dentes, Lábios), o sistema abre uma posição. As funcionalidades opcionais de gestão monetária reproduzem os inputs originais como stop-loss, take-profit, trailing stop, reversão de sinais e fechamento automático de posições opostas.

## Lógica de negociação

1. **Detecção de fractais** – cada vela concluída atualiza buffers contínuos de máximos e mínimos. O algoritmo varre até `FractalsLookback` barras concluídas e encontra os fractais ascendentes e descendentes de Bill Williams mais recentes confirmados (padrão de cinco barras).
2. **Reconstrução do Alligator** – o Preço Mediano `(High + Low) / 2` alimenta três instâncias de `SmoothedMovingAverage` representando a mandíbula, os dentes e os lábios. Seus valores são deslocados para frente pelo número configurado de barras para corresponder às regras de plotagem do MetaTrader.
3. **Validação de rompimento** – uma configuração longa requer que o último fractal ascendente permaneça acima da mandíbula, dentes e lábios deslocados enquanto a vela mais recente fecha acima do preço do fractal. Uma configuração curta espelha a lógica abaixo do Alligator.
4. **Execução de ordens** – por padrão, a estratégia abre uma única ordem de mercado com `OrderVolume` quando o rompimento é detectado e nenhuma posição é mantida. Se `CloseOppositePositions` estiver habilitado, uma posição oposta é zerada antes de abrir uma nova. Definir `ReverseSignals = true` troca os lados de rompimento para reproduzir o modo reverso do EA.
5. **Gestão de risco** – os níveis configuráveis de stop-loss e take-profit são armazenados internamente e avaliados em cada vela. O trailing stop é ativado assim que o mercado se move `TrailingStopPips + TrailingStepPips` na direção do trade e continua avançando conforme o preço avança. Os stops são expressos em "pips" derivados do `PriceStep` do instrumento, incluindo o ajuste de 3 ou 5 dígitos do MetaTrader.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `OrderVolume` | Tamanho do trade em lotes ou contratos para entradas de mercado. | `0.1` |
| `StopLossPips` | Distância de stop-loss inicial em pips. Defina como `0` para desabilitar. | `50` |
| `TakeProfitPips` | Distância de take-profit em pips. Defina como `0` para desabilitar. | `50` |
| `TrailingStopPips` | Distância de trailing stop em pips. `0` desabilita a lógica de trailing. | `10` |
| `TrailingStepPips` | Ganho pip extra necessário antes de o trailing stop se mover novamente. Deve ser positivo quando o trailing estiver habilitado. | `5` |
| `JawPeriod` | Comprimento da média móvel suavizada para a mandíbula do Alligator (azul). | `13` |
| `JawShift` | Deslocamento para frente dos valores da mandíbula, medido em barras. | `8` |
| `TeethPeriod` | Comprimento da média móvel suavizada dos dentes (vermelho). | `8` |
| `TeethShift` | Deslocamento para frente dos valores dos dentes. | `5` |
| `LipsPeriod` | Comprimento da média móvel suavizada dos lábios (verde). | `5` |
| `LipsShift` | Deslocamento para frente dos valores dos lábios. | `3` |
| `FractalsLookback` | Número de velas concluídas varridas ao procurar os fractais confirmados mais recentes. | `100` |
| `ReverseSignals` | Quando `true`, os sinais de compra vêm de rompimentos de fractal descendente e os sinais de venda vêm de rompimentos de fractal ascendente. | `false` |
| `CloseOppositePositions` | Quando `true`, a estratégia fecha uma posição oposta existente antes de entrar em um novo trade. | `false` |
| `CandleType` | Série de velas usada para cálculos e sinais. | `TimeFrame(1h)` |

## Notas

- A estratégia opera estritamente em **velas concluídas** e ignora ticks intrabarra, correspondendo ao fluxo de trabalho barra a barra do Expert Advisor original.
- Para emular o cálculo de pip do MetaTrader 5, a estratégia multiplica o `PriceStep` da bolsa por 10 quando o instrumento tem 3 ou 5 casas decimais.
- Ordens protetoras e o trailing stop são gerenciados internamente. Quando uma condição de stop ou alvo é atendida dentro da próxima vela, a posição é fechada a mercado para imitar o gerenciamento de ordens do EA.
- Os indicadores Alligator são desenhados automaticamente se uma área de gráfico estiver disponível, permitindo comparação visual entre o port do StockSharp e o modelo do MetaTrader.
- Projetos Python e de teste são intencionalmente omitidos de acordo com as diretrizes do repositório para novas conversões.
