# Estratégia completa aleatória
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia At Random Full** é uma conversão fiel do consultor especialista MetaTrader 5 "At random Full". Ele mantém o
ideia original de abrir negociações com base em um gerador aleatório enquanto expõe as mesmas opções de gerenciamento de dinheiro: direção
filtros, espaçamento de grade, janelas de tempo opcionais e um botão liga/desliga para cálculo da média. A porta StockSharp usa o API de alto nível,
portanto, todo o ciclo de decisão é conduzido por assinaturas de velas e ajudantes `StartProtection` padrão para ordens de proteção.

## Lógica de negociação
1. Em cada vela finalizada a estratégia verifica se a negociação é permitida (filtro de sessão, estado do portfólio e opcional
sinalizador "apenas uma posição").
2. Um gerador pseudo-aleatório decide entre uma entrada longa ou curta. O parâmetro `ReverseSignals` pode inverter o resultado para
emular o modo reverso MQL.
3. Filtros de direção (`TradeMode`) bloqueiam sinais indesejados. O código também impõe a regra original EA de uma única negociação por
barra em cada direção, lembrando o tempo de abertura da vela do último sinal.
4. As opções de gerenciamento de grade refletem o comportamento MetaTrader:
   - `MaxPositions` limita o número médio de entradas por lado.
   - `MinStepPoints` requer uma distância mínima (convertida em preço usando a etapa de preço de segurança) entre entradas consecutivas.
   - `CloseOpposite` força o fechamento da exposição oposta existente antes que uma nova negociação seja enviada.
5. As ordens de mercado são emitidas através de `BuyMarket` / `SellMarket` com um volume normalizado definido por `OrderVolume`.

## Gestão de Posição e Risco
- `StartProtection` anexa ordens de stop-loss e take-profit que correspondem às entradas de MetaTrader. Se `TrailingStopPoints` for
maior que zero, o modo de rastreamento StockSharp integrado está ativado. Os parâmetros `TrailingActivatePoints` e
`TrailingStepPoints` são convertidos em distâncias de preços e registrados para transparência, mas o rastreamento real é tratado pelo
plataforma.
- Todos os cálculos de volume respeitam os metadados de troca (mínimo, máximo e passo) exatamente como as rotinas auxiliares MQL.
- O controle de tempo emula o bloco `InpTimeControl` do script. Quando habilitado, as negociações são permitidas apenas dentro do configurado
janela `[SessionStart, SessionEnd]`; sessões noturnas são suportadas.

## Parâmetros
| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Série de velas usada para agendar o ciclo de decisão. | `15 minute timeframe` |
| `OrderVolume` | Volume base de ordens de mercado em lotes. | `0.1` |
| `MaxPositions` | Número máximo de entradas médias por direção (0 = ilimitado). | `5` |
| `MinStepPoints` | Distância mínima entre entradas expressa em MetaTrader pontos. | `150` |
| `StopLossPoints` | Distância de stop-loss em pontos. | `150` |
| `TakeProfitPoints` | Distância de lucro em pontos. | `460` |
| `TrailingActivatePoints` | Limite de lucro (em pontos) registrado para fins informativos quando o rastreamento está ativado. | `70` |
| `TrailingStopPoints` | Distância de parada final passada para `StartProtection`. | `250` |
| `TrailingStepPoints` | Passo entre os ajustes finais, registrados junto com a distância de ativação. | `50` |
| `OnlyOnePosition` | Bloqueia novas negociações até que a posição líquida atual seja fechada. | `false` |
| `CloseOpposite` | Fecha a exposição oposta antes de abrir uma negociação. | `false` |
| `ReverseSignals` | Inverte a decisão aleatória para que as compras se tornem vendas e vice-versa. | `false` |
| `UseTimeControl` | Habilita o filtro de horário da sessão de negociação. | `false` |
| `SessionStart` | Hora de início da sessão (inclusive) quando `UseTimeControl` for `true`. | `10:01` |
| `SessionEnd` | Hora de término da sessão (inclusive) quando `UseTimeControl` for `true`. | `15:02` |
| `Mode` | Direção comercial permitida (`Both`, `BuyOnly`, `SellOnly`). | `Both` |
| `RandomSeed` | Semente determinística opcional para o gerador pseudo-aleatório (0 = contagem de ticks do ambiente). | `0` |

## Notas de implementação
- Todos os comentários são escritos em inglês e o código usa recuo de tabulação, correspondendo às diretrizes do repositório.
- O processamento de velas depende de `SubscribeCandles().Bind(...)`, garantindo que a lógica seja executada uma vez por barra concluída, como em EA.
- A estratégia acompanha os últimos preços de compra e venda para impor a restrição de espaçamento mínimo, mesmo durante o cálculo da média.
- As instruções de registro refletem os diagnósticos detalhados impressos pelo script original: cada entrada anuncia a direção escolhida,
preço de entrada, volume e configuração final na inicialização.

## Dicas de uso
- Como o sinal de negociação é aleatório, a estratégia é mais adequada para testar infraestruturas ou demonstrar controlos de risco.
- Ajuste `OrderVolume`, `StopLossPoints` e `TakeProfitPoints` para alinhar com o tamanho do tick e a volatilidade do instrumento que você
planeja negociar.
- Ative `UseTimeControl` se o EA deve operar apenas durante uma sessão específica (por exemplo, a sessão de Londres ou Nova York).
- Use `RandomSeed` durante as execuções de otimização para obter sequências reproduzíveis de decisões aleatórias.
