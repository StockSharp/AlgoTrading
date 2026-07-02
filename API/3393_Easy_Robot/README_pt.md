# Estratégia Fácil de Robô
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Easy Robot é um Expert Advisor que segue o impulso e negocia uma vez por vela horária concluída. Quando a vela anterior fecha em alta a estratégia abre uma nova posição comprada; quando fecha em baixa, abre uma posição curta. Apenas uma posição pode estar ativa a qualquer momento, espelhando totalmente a lógica MetaTrader 4 original.

## Regras comerciais
1. Assine o tipo de vela horária selecionado pelo parâmetro **CandleType** (o padrão é H1).
2. Assim que a vela terminar, compare seu fechamento com a abertura:
   - Fechar > Aberto: envia uma ordem de compra a mercado se nenhuma posição estiver aberta.
   - Fechar <Abrir: envia uma ordem de venda a mercado se estiver estável.
3. O tamanho da posição usa a propriedade da estratégia `Volume`, exatamente como a versão MQL que dependia de `CheckVolumeValue` com um padrão de 0,01 lote.
4. Os níveis de stop-loss e take-profit dependem de um indicador **Average True Range** com período **AtrPeriod** (padrão 14):
   - Distância de parada = `ATR * StopFactor`.
   - Tome distância = `ATR * TakeFactor`.
   - Ambas as distâncias são normalizadas pela distância mínima de tick/pip, de modo que as ordens de proteção nunca são colocadas mais próximas do que a corretora permite.
5. As ordens protetoras são registradas imediatamente após a ordem de mercado através de `SetStopLoss` e `SetTakeProfit`, proporcionando o mesmo comportamento de `OrderSend` com os parâmetros `sl` e `tp`.
6. O rastreamento opcional é ativado quando **UseTrailingStop** é verdadeiro. Depois que a negociação acumula lucro **TrailingStartPips** (MetaTrader pips, ou seja, pontos ajustados para cotações decimais de 3/5), o stop é movido para mais perto por **TrailingStepPips** e é empurrado ainda mais somente quando novos extremos de lucro são alcançados. O trailing respeita a distância mínima de parada da corretora para evitar modificações inválidas.
7. As cotações para cálculos de stop usam o melhor bid/ask quando disponível, voltando ao último preço ou fechamento da vela, correspondendo às referências originais `Bid`/`Ask`.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `TakeFactor` | 4.2 | Multiplicador de ATR para distância de lucro (mapeia para `TakeFactor` entrada em MQL). |
| `StopFactor` | 4.9 | Multiplicador ATR para distância de stop-loss (mapeia para `StopFactor`). |
| `UseTrailingStop` | verdade | Ativa o estilo de rastreamento MetaTrader (`UseTstop`). |
| `TrailingStartPips` | 40 | Lucro em pips antes do início do trailing (`Tstart`). |
| `TrailingStepPips` | 19 | Etapa Pip aplicada ao rastrear atualizações (`Tstep`). |
| `AtrPeriod` | 14 | período ATR de cálculo para dimensionamento de volatilidade. |
| `CandleType` | H1 | Série de velas usada para sinais e entrada ATR. |

## Notas
- A estratégia redefine os preços de entrada e stop armazenados sempre que a posição retorna a zero, garantindo um estado limpo para o próximo sinal.
- A distância mínima de parada é estimada através do tamanho do pip do instrumento (ou da etapa de preço quando o tamanho do pip não está disponível). Isso reproduz o auxiliar `SC` do arquivo de inclusão MQL.
- `StartProtection()` é chamado uma vez no início para que a plataforma possa gerenciar saídas de emergência, se necessário.
