# Estratégia EXP FIBO ZZ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia EXP FIBO ZZ é uma porta C# do MetaTrader 4 consultor especialista `EXP_FIBO_ZZ_V1en`. Ele reproduz o rompimento original
lógica: monitorar o último corredor ZigZag confirmado, colocar um stop de compra acima da máxima oscilante e um stop de venda abaixo da mínima oscilante, e
anexar ordens de stop-loss e take-profit baseadas em Fibonacci. A versão StockSharp expõe todas as entradas configuráveis por meio de
`StrategyParam` objetos, adiciona validação extensiva e mantém as opções originais de gerenciamento de dinheiro, incluindo risco baseado em saldo
dimensionamento e ajuste do ponto de equilíbrio.

## Lógica de negociação
1. **Preparação de dados**
   - A estratégia assina o `CandleType` configurado (padrão: velas de 1 minuto) e alimenta a série em `Highest` e
`Lowest` indicadores com comprimento igual a `ZigZagDepth`.
   - Um detector ZigZag leve rastreia os três preços de pivô mais recentes. Um novo pivô é registrado somente quando:
     * A máxima/baixa da vela é igual à saída do indicador.
     * Pelo menos `ZigZagBackstep` barras se passaram desde o ponto de viragem anterior.
     * O desvio de preço do pivô atual excede `ZigZagDeviationPips` (expresso em MetaTrader pips).

2. **Validação de corredor**
   - Uma vez disponíveis três pivôs, os dois mais antigos definem o corredor. A negociação continua apenas se a altura do corredor estiver entre
`MinCorridorPips` e `MaxCorridorPips` e o pivô mais recente ficam estritamente dentro da banda com um pequeno buffer estilo corretor.
   - Fora da janela de negociação especificada pelo usuário (`StartHour/StartMinute` a `StopHour/StopMinute`) todas as ordens pendentes são canceladas.

3. **Colocação de pedido**
   - Os preços stop de compra e venda são calculados como os limites do corredor mais/menos `EntryOffsetPips`.
   - A distância de stop-loss é igual a `corridor * FiboStopLoss / 100`. A distância de lucro segue a fórmula MetaTrader
`corridor * (FiboTakeProfit / 100 - 1)` com valores negativos fixados em zero.
   - Antes de fazer pedidos, a estratégia calcula o volume de negociação. Se `RiskPercent > 0`, o código multiplica a capital selecionada
fonte (patrimônio líquido quando `UseBalanceForRisk` é `true`, caso contrário, patrimônio líquido menos margem bloqueada) pela porcentagem de risco e divide
o resultado pelo preço de referência. O volume é ajustado à grade do lote de troca e cortado aos limites de troca. Quando
as informações necessárias não estão disponíveis, o algoritmo volta para `FixedVolume`.
   - As ordens de entrada ativas são modificadas sempre que o preço alvo ou o volume mudam; caso contrário, novos pedidos serão enviados.

4. **Gerenciamento de posição**
   - Assim que uma posição é aberta, o algoritmo cancela a ordem pendente oposta e registra ordens de proteção:
     * Stop-loss via `SellStop`/`BuyStop` na distância pré-calculada.
     * Take-profit opcional via `SellLimit`/`BuyLimit`.
   - O módulo de ponto de equilíbrio opcional (`EnableBreakEven`) reflete a rotina `MovingInWL` original. Depois de acumular
`BreakEvenTriggerPips` de lucro o stop é movido para o preço de entrada mais/menos `BreakEvenOffsetPips`, garantindo pelo menos
um pequeno ganho, evitando ajustes repetidos.

5. **Manutenção da sessão**
   - Sair da janela de negociação ou achatar a posição cancela quaisquer ordens pendentes ou de proteção pendentes. O método
`OnStopped` também limpa todos os pedidos quando a estratégia termina.

## Parâmetros
| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `CandleType` | Série de dados usada para construir os pivôs ZigZag. | `1m TimeFrame()` | Suporta qualquer tipo de vela StockSharp. |
| `ZigZagDepth` | Número mínimo de velas entre oscilações do ZigZag. | `12` | Corresponde à entrada MT4 `ExtDepth`. |
| `ZigZagDeviationPips` | Desvio mínimo (em MetaTrader pips) antes de aceitar um novo pivô. | `5` | Espelhos `ExtDeviation`. |
| `ZigZagBackstep` | Contagem mínima de barras antes que o ZigZag possa reverter novamente. | `3` | Equivalente a `ExtBackstep`. |
| `EntryOffsetPips` | Distância em pips adicionada acima/abaixo do corredor ao colocar ordens de parada. | `5` | Espelhos `n_pips`. |
| `MinCorridorPips` | Limite inferior para o tamanho do corredor. | `20` | Espelhos `Min_Corridor`. |
| `MaxCorridorPips` | Limite superior para o tamanho do corredor. | `100` | Espelhos `Max_Corridor`. |
| `FiboStopLoss` | Razão Fibonacci aplicada ao corredor para derivar a distância de stop-loss. | `61.8` | Espelhos `Fibo_StopLoss`. |
| `FiboTakeProfit` | Proporção de Fibonacci aplicada para calcular a meta de lucro. | `161.8` | Espelhos `Fibo_TakeProfit`. |
| `StartHour` / `StartMinute` | Início do pregão permitido. | `00:01` | Os pedidos são cancelados fora da janela. |
| `StopHour` / `StopMinute` | Fim do pregão. | `23:59` | Suporta sessões noturnas que terminam à meia-noite. |
| `UseBalanceForRisk` | Escolha patrimônio (`true`) ou dinheiro disponível (`false`) para dimensionamento de risco. | `true` | Espelhos `Choice_method`. |
| `RiskPercent` | Fração de capital alocada para a próxima negociação. | `1` | Defina como `0` para desativar o dimensionamento baseado em risco. |
| `FixedVolume` | Tamanho do lote usado quando o dimensionamento de risco está desabilitado ou indisponível. | `0.1` | Espelha a entrada `Lots`. |
| `EnableBreakEven` | Habilita o ajuste do ponto de equilíbrio. | `true` | Espelhos `MovingInWL`. |
| `BreakEvenTriggerPips` | Lucro em pips necessário antes de mover o stop. | `13` | Espelhos `LevelProfit`. |
| `BreakEvenOffsetPips` | Compensação em pips aplicada ao ponto de equilíbrio. | `2` | Espelhos `LevelWLoss`. |
| `DrawCorridorLevels` | Trace o corredor ativo no gráfico. | `false` | Espelha o sinalizador de desenho de linha opcional. |

## Notas de implementação
- A conversão de pip respeita as convenções MetaTrader multiplicando `PriceStep` por 10 para símbolos Forex de três e cinco dígitos.
- Os preços e volumes dos pedidos são arredondados para o incremento válido mais próximo usando os metadados de troca (`PriceStep`, `VolumeStep`,
`MinVolume`, `MaxVolume`).
- O dimensionamento do risco recua normalmente quando faltam dados da carteira ou preços de referência, garantindo que a estratégia ainda funciona com
o lote fixo configurado.
- A rotina de ponto de equilíbrio cancela e registra novamente o stop de proteção apenas uma vez por negociação e nunca coloca o stop além do
preço de entrada.
- Quando `DrawCorridorLevels` está ativado, a estratégia desenha um segmento vertical entre os pivôs alto e baixo do atual
corredor, permitindo rápida confirmação visual da faixa de negociação.

## Diferenças versus a versão MetaTrader
- Objetos gráficos, sons e comentários na tela do script MT4 foram omitidos; StockSharp primitivos de registro e gráfico cobrem o
mesmas necessidades.
- O dimensionamento de risco usa o patrimônio do portfólio e os últimos preços conhecidos em vez de `MarketInfo` valores de margem, porque esses detalhes são do corretor
específico e indisponível de maneira independente de plataforma.
- O gerenciamento de pedidos usa o StockSharp API (`BuyStop`, `SellStop`, `SellLimit`, `BuyLimit`) de alto nível em vez do ticket manual
manuseio. O comportamento permanece equivalente, embora exija menos código clichê.
- O detector ZigZag reimplementa a lógica de profundidade/desvio/retrocesso com indicadores integrados para permanecer compatível com
Modelo de vela de streaming de StockSharp.
