# Estratégia Autotrade com Stops Pendentes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão em C# do consultor especializado MetaTrader *Autotrade (edição de barabashkakvn)*. Ela mantém continuamente duas ordens de entrada stop simétricas ao redor do preço de mercado atual. Sempre que o mercado permanece flat e não há posição aberta, a estratégia atualiza ambas as ordens pendentes. Quando uma ordem stop é executada, a posição é monitorada ativamente: as saídas são acionadas quando a ação do preço se estabiliza ou quando um limiar absoluto de lucro/perda é atingido. A implementação usa a API de alto nível do StockSharp conforme exigido pelas diretrizes do projeto.

## Mapeamento dos parâmetros originais
| Parâmetro StockSharp | Parâmetro MQL5 | Descrição |
| --- | --- | --- |
| `IndentTicks` | `InpIndent` | Distância (em passos de preço) entre o preço atual e as ordens de entrada stop. |
| `MinProfit` | `MinProfit` | Lucro flutuante mínimo (moeda da conta) necessário para sair durante uma fase tranquila do mercado. |
| `ExpirationMinutes` | `ExpirationMinutes` | Tempo de vida das ordens stop pendentes antes de serem canceladas e recriadas. |
| `AbsoluteFixation` | `AbsoluteFixation` | Nível absoluto de lucro ou perda (moeda) que força o fechamento da posição. |
| `StabilizationTicks` | `InpStabilization` | Tamanho máximo do corpo da vela anterior que é tratado como zona de consolidação. |
| `OrderVolume` | `Lots` | Volume usado tanto para o buy stop quanto para o sell stop. |
| `CandleType` | `Period()` | Série de velas que impulsiona a lógica (período de 1 minuto por padrão). |

Todos os parâmetros numéricos que representam distâncias de preço são convertidos de "pontos" para passos de preço reais através do valor `Security.PriceStep`. Os limiares baseados em lucro são calculados usando `Security.StepPrice`, o que reflete os cálculos de lucro do MQL que operam na moeda do depósito.

## Lógica de trading
### Implantação de ordens pendentes
1. A estratégia reage apenas a velas terminadas (`CandleStates.Finished`).
2. A primeira vela é usada para semear dados históricos (open/close anterior) e imediatamente agendar ordens pendentes.
3. Quando não há posição aberta, referências inativas são limpas e:
   - Um buy stop é colocado em `Close + IndentTicks * PriceStep`.
   - Um sell stop é colocado em `Close - IndentTicks * PriceStep`.
4. Cada ordem pendente recebe um timestamp de expiração igual a `CloseTime + ExpirationMinutes` minutos. Quando esse tempo é atingido, a ordem é cancelada e recriada na próxima vela.

### Gerenciamento de posição
1. Uma vez executada qualquer ordem stop, a ordem pendente contrária é cancelada para evitar hedge indesejado no modelo de conta baseado em netting do StockSharp.
2. A estratégia mantém o corpo da vela anterior (`|Open - Close|`) para detectar condições tranquilas do mercado.
3. Para cada vela com posição aberta:
   - O lucro não realizado é estimado em moeda usando a diferença de preço em relação a `PositionAvgPrice`, escalada por `Security.PriceStep` e `Security.StepPrice`.
   - Se o lucro exceder `MinProfit` **e** o corpo da vela anterior estiver abaixo de `StabilizationTicks * PriceStep`, a posição é fechada a mercado.
   - Independentemente da estabilização, se o lucro ou perda absoluta exceder `AbsoluteFixation`, a posição também é fechada a mercado.
4. Quando a posição retorna a flat, todas as ordens pendentes restantes são eliminadas.

### Comportamentos adicionais
- Apenas uma posição é permitida por vez; os volumes de ordens são compensados usando `OrderVolume`.
- Como o StockSharp não expõe bid/ask durante backtests da mesma forma que o MetaTrader, o preço de fechamento da vela completada é usado como nível de referência para novas ordens stop.
- A estratégia atualiza automaticamente o valor `Volume` em cache quando `OrderVolume` é ajustado via parâmetros ou otimização.

## Notas de implementação e diferenças
- Os cálculos de lucro dependem de `Security.PriceStep` e `Security.StepPrice`. Garanta que esses campos estejam preenchidos nos metadados do instrumento; caso contrário, o valor `1` é usado como fallback.
- A versão MQL original permitia hedge temporário (múltiplas ordens em direções opostas). O port StockSharp cancela o stop não utilizado imediatamente após uma execução para cumprir com o modelo de netting da plataforma.
- A expiração de ordens pendentes usa o `CloseTime` da vela. Se os dados históricos não tiverem timestamps de fechamento, ajuste o feed para fornecê-los ou estenda o código adequadamente.
- A estratégia funciona com qualquer tipo de dado de velas ajustando `CandleType`. As velas padrão são baseadas em período (`TimeSpan.FromMinutes(1).TimeFrame()`).

## Recomendações de uso
1. Configure a série de velas que corresponda ao período do gráfico usado no MetaTrader.
2. Defina `IndentTicks`, `StabilizationTicks` e os limiares de lucro em relação ao tamanho do tick e ao valor do tick do instrumento.
3. Verifique se o portfólio usa hedge ou netting conforme desejado. A estratégia assume netting e fechará o livro antes de reativar as ordens stop.
4. Use os parâmetros fornecidos para otimização no StockSharp Designer ou Backtester para adaptar o comportamento a diferentes mercados.
5. Monitore a saída do log: o código depende de velas terminadas e disponibilidade do mercado (`IsFormedAndOnlineAndAllowTrading()`) antes de enviar novas ordens.

## Aviso de risco
O trading automatizado envolve riscos substanciais. Faça backtests completos, valide os parâmetros em dados históricos e confirme os requisitos específicos do corretor (como distâncias mínimas para ordens stop) antes de implantar a estratégia em uma conta real.
