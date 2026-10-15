# Estratégia Lavika100 (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **estratégia Lavika100** é uma versão fiel do expert advisor do MetaTrader 5 "Lavika  cent". O sistema combina um filtro de momentum RAVI de uma hora (H1) e de quatro horas (H4) para decidir quando abrir operações. Ele mantém as escolhas originais de gestão de dinheiro (lote fixo ou percentual de risco), disciplina de uma posição, reversão opcional de sinal e gestão automática de stops. A versão StockSharp segue as diretrizes da API de alto nível: assinaturas de candles conduzem o fluxo, indicadores são acessados por binders e ordens de proteção são configuradas com `StartProtection`.

## Fluxo de trabalho
1. **Assinaturas de dados** - a estratégia assina candles H1 para o período de execução e candles H4 para o filtro de tendência. O indicador `SimpleMovingAverage` é aplicado aos preços de abertura para emular as chamadas MT5 `iMA(..., PRICE_OPEN)`.
2. **Momentum RAVI** - duas médias móveis em cada período (rápida/lenta) geram uma porcentagem "RAVI": `(fast - slow) / slow * 100`. O valor H1 precisa ser positivo antes que qualquer operação seja considerada.
3. **Detecção do padrão de tendência** - os quatro valores RAVI mais recentes em H4 são inspecionados:
   - Uma sequência ascendente (`r0 > r1`, `r1 < r2`, `r2 < r3`) aciona um sinal comprado.
   - Uma sequência descendente (`r0 < r1`, `r1 > r2`, `r2 > r3`) aciona um sinal vendido. Isso espelha o comportamento do código original, embora o expert só invertesse direção pelo flag `Reverse`.
4. **Reversão de sinais e zeragem** - dependendo dos parâmetros `ReverseSignals` e `CloseOpposite`, o algoritmo abre na direção detectada ou a reverte, fechando antes qualquer posição oposta.
5. **Gestão de dinheiro** - o volume vem de `FixedVolume` ou é escalado por risco via método `RiskPercent` (valor do portfólio * percentual / distância do stop).
6. **Proteção** - stop-loss, take-profit, trailing stop e passo trailing são ativados via `StartProtection` assim que a estratégia inicia e os parâmetros são diferentes de zero.

## Regras de negociação
- **Entrada comprada** - RAVI H1 é positivo e a série H4 mostra um padrão ascendente. A estratégia fecha uma posição vendida existente quando `CloseOpposite=true` antes de comprar.
- **Entrada vendida** - RAVI H1 é positivo e a série H4 mostra um padrão descendente. Quando `ReverseSignals=true`, as direções são trocadas, correspondendo ao seletor "Reverse" do MT5.
- **Posição única** - com `OnlyOnePosition=true`, qualquer exposição não zerada bloqueia entradas adicionais até que a posição seja fechada.
- **Dimensionamento de volume** - o modo de percentual de risco usa o par `PriceStep`/`StepPrice` do instrumento para converter distância de preço em valor monetário, respeitando `VolumeStep`, `VolumeMin` e `VolumeMax`.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `H1CandleType` | Período para a lógica de execução (padrão 1 hora). |
| `H4CandleType` | Período mais alto usado pelo filtro de tendência (padrão 4 horas). |
| `H1FastPeriod` / `H1SlowPeriod` | Comprimentos de média móvel para o RAVI H1. |
| `H4FastPeriod` / `H4SlowPeriod` | Comprimentos de média móvel para o RAVI H4. |
| `StopLossPoints` | Distância de stop-loss em pontos baseados em pips. |
| `TakeProfitPoints` | Distância de take-profit em pontos baseados em pips. |
| `TrailingStopPoints` | Distância do trailing stop. Defina como zero para desabilitar trailing. |
| `TrailingStepPoints` | Passo mínimo para atualizações de trailing. Deve ser positivo quando trailing está habilitado. |
| `FixedVolume` | Tamanho do lote usado no modo fixo. |
| `RiskPercent` | Percentual do valor do portfólio a arriscar quando `MoneyMode` é igual a `RiskPercent`. |
| `MoneyMode` | Alterna entre `FixedLot` e `RiskPercent`. |
| `OnlyOnePosition` | Permite apenas uma única posição aberta. |
| `ReverseSignals` | Inverte ações compradas/vendidas (padrão true para corresponder à configuração do EA). |
| `CloseOpposite` | Fecha uma posição oposta antes de colocar uma nova ordem. |

## Notas de conversão
- A conversão de pips imita o expert MT5: cotações de três e cinco dígitos multiplicam `PriceStep` por dez para obter um incremento do tamanho de um pip.
- O histórico RAVI é armazenado sem coleções personalizadas - apenas quatro campos anuláveis - respeitando as restrições do repositório contra buffers manuais.
- A gestão de dinheiro evita chamadas `GetValue` de indicadores e usa metadados de mercado do StockSharp para mapear risco percentual para volume.
- `StartProtection` só é chamado quando pelo menos uma das distâncias de proteção é positiva, garantindo execução segura durante backtests e negociação ao vivo.

## Dicas de uso
- Forneça um instrumento no estilo Forex com `PriceStep`, `StepPrice`, `VolumeStep`, `VolumeMin` e `VolumeMax` corretamente configurados.
- Ao usar dimensionamento baseado em risco, defina `StopLossPoints` diferente de zero; caso contrário, o volume calculado será zero.
- Como o EA original continha uma peculiaridade lógica em que ambos os padrões definiam o flag de compra, mantenha `ReverseSignals=true` se precisar reproduzir suas operações exatas.
