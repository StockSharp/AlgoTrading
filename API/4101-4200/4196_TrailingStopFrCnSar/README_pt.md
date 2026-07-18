# Estratégia Trailing Stop FrCnSar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Trailing Stop FrCnSar transporta o kit de ferramentas MetaTrader fornecido como **TrailingStopFrCnSARen_v4.mq4** e **OrderBalansEN_v3_4.mq4**. O consultor especialista gerenciou as posições existentes ajustando seus stop-loss usando diversas técnicas (velas anteriores, fractais, velocidade do preço ou Parabolic SAR), enquanto o indicador complementar exibia o saldo da conta atual e os pedidos abertos. A conversão StockSharp concentra-se nas posições líquidas e reimplementa a lógica final com primitivas API de alto nível. Ele também fornece um registrador opcional de resumo de pedidos para que a sobreposição de informações do indicador original permaneça disponível em formato textual.

A estratégia não abre novas negociações automaticamente. Em vez disso, ele observa continuamente a posição atual em `Strategy.Security`, atualiza o nível de trailing stop desejado de acordo com o modo selecionado e os filtros definidos pelo usuário e fecha a exposição quando o preço atinge a barreira móvel. Como StockSharp trabalha com posições líquidas em vez de tickets discretos, todos os cálculos se aplicam à quantidade agregada.

## Lógica de negociação
1. Assine o `CandleType` configurado e processe apenas velas concluídas para evitar ajustes de parada prematuros.
2. Mantenha buffers rolantes curtos com máximos e mínimos de velas para que fractais e extremos recentes possam ser recuperados sem chamar métodos de indicadores proibidos.
3. Opcionalmente, calcule uma velocidade suavizada de fechamento em pontos quando o modo de rastreamento de velocidade estiver ativo.
4. Para cada vela concluída, produza o preço do trailing stop candidato com base no modo selecionado:
   - A mínima mais baixa do histórico recente de velas menos o deslocamento de `DeltaPoints`.
   - Último fractal confirmado ajustado por `DeltaPoints`.
   - Preço de fechamento deslocado por uma distância dependente da velocidade.
   - Valor atual de Parabolic SAR compensado por `DeltaPoints`.
   - Uma distância fixa expressa em pontos do instrumento.
5. Verifique o candidato em relação aos filtros de gestão de dinheiro: exija stops existentes, permita apenas o trailing lucrativo, pare quando o ponto de equilíbrio for alcançado ou baseie o teste de lucro no preço médio de entrada.
6. Substitua o nível de stop armazenado quando o candidato melhorar o existente em pelo menos `StepPoints`.
7. Se a vela ultrapassar o nível armazenado (mínimo para posições compradas, alta para posições vendidas) e a negociação for permitida, feche a posição líquida com uma ordem de mercado.
8. Opcionalmente, registre um resumo textual com saldo, tamanho da posição, preço de entrada, stop atual e PnL não realizado, emulando o indicador MetaTrader OrderBalans.

## Modos de rastreamento
- **Vela** – trilhas atrás do extremo significativo da vela mais recente. Os deslocamentos são aplicados via `DeltaPoints` para manter o stop ligeiramente afastado do suporte/resistência.
- **Fractal** – usa o último fractal de cinco barras detectado no período processado. Isso imita a implementação padrão MetaTrader, mas opera em posições líquidas.
- **Velocidade** – estima a velocidade do preço calculando a média das alterações de fechamento em `VelocityPeriod`. Quando o momento acelera na direção da posição, a parada é apertada proporcionalmente à diferença de velocidade escalonada por `VelocityMultiplier`.
- **Parabolic** – segue o indicador Parabolic SAR gerenciado por StockSharp. A parada abraça os pontos SAR e herda os parâmetros de passo e aceleração máxima.
- **Pontos fixos** – impõe uma distância constante do preço, refletindo efetivamente o comportamento “>4 pips” do script original.
- **Desligado** – desativa o trailing e mantém a parada atual inalterada.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `Mode` | `TrailingStopMode` | `Candle` | Determina qual algoritmo final está ativo. |
| `CandleType` | `DataType` | Velas de 15 minutos | Período usado para analisar velas e calcular dados finais. |
| `DeltaPoints` | `int` | `0` | Distância adicional (em pontos de instrumento) adicionada abaixo/acima do preço bruto. |
| `StepPoints` | `int` | `0` | Melhoria mínima, em pontos, necessária antes de atualizar um trailing stop existente. |
| `FixedDistancePoints` | `int` | `50` | Distância para o modo de rastreamento fixo. Ignorado por outros modos. |
| `TrailOnlyProfit` | `bool` | `true` | Quando `true`, o trailing começa somente após o stop terminar em lucro relativo ao preço de entrada. |
| `TrailOnlyBreakEven` | `bool` | `false` | Pare de atualizar quando o stop armazenado ultrapassar o ponto de equilíbrio. |
| `RequireExistingStop` | `bool` | `false` | Ignore as atualizações finais até que um nível de parada já tenha sido calculado. |
| `UseGeneralBreakEven` | `bool` | `false` | Avalie o filtro de lucratividade usando o preço médio de entrada da posição líquida (equivalente ao auxiliar `TProfit` original). |
| `VelocityPeriod` | `int` | `30` | Número de fechamentos usados para calcular a velocidade média no modo de velocidade. |
| `VelocityMultiplier` | `decimal` | `1` | Dimensiona o ajuste de velocidade aplicado à distância de fuga. |
| `ParabolicStep` | `decimal` | `0.02` | Etapa de aceleração para o indicador Parabolic SAR. |
| `ParabolicMaximum` | `decimal` | `0.2` | Aceleração máxima para o indicador Parabolic SAR. |
| `LogOrderSummary` | `bool` | `true` | Ativa o registro textual semelhante ao painel OrderBalans. |
| `TradeVolume` | `decimal` | `1` | Volume padrão usado ao nivelar posições por meio de métodos auxiliares. |

## Diferenças dos scripts MetaTrader originais
- A conversão funciona com StockSharp posições líquidas em vez de tickets individuais. Portanto, as atualizações de interrupção aplicam-se a toda a posição, independentemente de como ela foi construída.
- Os filtros de números mágicos e multisímbolos foram removidos. A estratégia monitora apenas `Strategy.Security` e assume que o dimensionamento da posição é tratado externamente.
- O indicador personalizado MetaTrader `Velocity` é aproximado por meio de uma diferença média de fechamento a fechamento medida em pontos do instrumento. Isso mantém o comportamento intuitivo, mas pode não corresponder exatamente ao indicador proprietário.
- Objetos gráficos visuais (linhas de tendência, setas, rótulos) foram substituídos por entradas de registro textuais. O parâmetro `LogOrderSummary` recria o painel informativo produzido por *OrderBalansEN_v3_4.mq4* sem depender do desenho manual do gráfico.
- As modificações de parada usam métodos auxiliares StockSharp (`BuyMarket`, `SellMarket`) porque a plataforma não expõe um equivalente direto ao MetaTrader de `OrderModify` em tickets individuais.

## Dicas de uso
- Anexe a estratégia a um gráfico para visualizar o efeito de cada modo de rastreamento. Para Parabolic SAR, ative a área do gráfico para visualizar pontos e negociações simultaneamente.
- Ajuste `DeltaPoints` e `StepPoints` de acordo com o tamanho do tick do instrumento. A implementação converte pontos automaticamente usando `Security.PriceStep` ou `Security.MinPriceStep`.
- Mantenha `TrailOnlyProfit` ativado ao imitar o comportamento original, pois o script MetaTrader evitou restringir stops antes que as posições se tornassem lucrativas.
- Desative `LogOrderSummary` se preferir uma saída mais silenciosa ou estiver executando centenas de estratégias simultaneamente.
- Teste o modo de velocidade com diferentes valores `VelocityMultiplier`; multiplicadores mais altos fazem com que o trailing stop reaja mais rapidamente a explosões repentinas de impulso.

## Indicadores
- Parabolic SAR (`ParabolicSar`)
- Máximos e mínimos de velas rolantes (buffers de dados nativos)
- Velocidade média opcional de fechamento a fechamento derivada de fechamentos de velas
