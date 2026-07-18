# Estratégia de cruzamento de turnos MARE5.1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia MARE5.1 é uma porta C# do MetaTrader 4 consultor especialista `MARE5_1.mq4`. O robô original negociou com dados M1 e contou com um par de médias móveis simples avaliadas em três compensações históricas diferentes para detectar mudanças de regime. Esta implementação StockSharp reproduz o comportamento com parâmetros configuráveis, anexa ordens de proteção no estilo MetaTrader e expõe um filtro detalhado da janela de negociação.

## Lógica de negociação
1. **Dados de mercado**
   - Uma única assinatura de vela definida por `CandleType` (padrão: 1 minuto) alimenta os cálculos.
   - Cada vela é processada somente após seu fechamento para evitar o uso de barras meio formadas.
2. **Indicadores**
   - Duas instâncias `SimpleMovingAverage` representam os componentes rápido (`FastPeriod`) e lento (`SlowPeriod`).
   - Ambas as médias são deslocadas para frente em `MovingAverageShift`, exatamente como o argumento `ma_shift` na função MQL `iMA`.
   - Cópias atrasadas adicionais de cada média são obtidas com mudanças de `MovingAverageShift + 2` e `MovingAverageShift + 5` para espelhar as chamadas `iMA(..., shift=2/5)` originais.
3. **Detecção de sinal**
   - A diferença entre as médias deve exceder pelo menos uma etapa de preço (`Point` em termos de MetaTrader). Se o instrumento tiver zero `PriceStep`, qualquer diferença positiva será suficiente.
   - **Configuração de venda:**
     - A vela anterior deve ser de baixa (`Close < Open`).
     - A média lenta deslocada atual é maior que a média rápida.
     - Duas e cinco velas atrás, a média rápida ainda estava acima da média lenta, sinalizando uma mudança de impulso.
   - **Configuração de compra:**
     - A vela anterior deve ser de alta (`Close > Open`).
     - A média rápida deslocada atual é maior que a média lenta.
     - Duas e cinco velas atrás, a média lenta ainda estava liderando, confirmando uma transição de condições de baixa para condições de alta.
   - Apenas uma posição pode ser aberta por vez, replicando a guarda `OrdersTotal() < 1` do EA.
4. **Filtro de tempo**
   - A negociação é permitida somente quando o horário de fechamento da vela avaliada estiver dentro do intervalo `[TimeOpenHour, TimeCloseHour]`.
   - Se a hora final for menor que a hora inicial, a janela será tratada como noturna (por exemplo, `22` a `5`).

## Gestão de risco
- `StartProtection` é configurado com uma distância de stop-loss e take-profit convertida de MetaTrader pontos em compensações de preço absoluto usando o instrumento `PriceStep`.
- Nenhum ponto final é implementado porque o código original declarou `TrailingStop` mas nunca o usou.
- Os pedidos são enviados com o volume definido por `TradeVolume`. A estratégia não faz pirâmide nem amplia posições.

## Parâmetros
| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `TradeVolume` | Tamanho do lote para entradas no mercado. | `7.8` | Arredondado de acordo com as regras de troca pelo conector StockSharp. |
| `FastPeriod` | Período da média móvel simples rápida. | `13` | Controla a rapidez com que a estratégia reage às mudanças de preço. |
| `SlowPeriod` | Período da média móvel simples lenta. | `55` | Fornece a referência de tendência de longo prazo. |
| `MovingAverageShift` | Mudança para frente aplicada a ambas as médias móveis. | `2` | Corresponde ao parâmetro `ma_shift` da função MQL `iMA`. |
| `StopLossPoints` | Distância de parada protetora em MetaTrader pontos. | `80` | Convertido em um deslocamento absoluto por meio do instrumento `PriceStep`. |
| `TakeProfitPoints` | Distância alvo de lucro em MetaTrader pontos. | `110` | Defina como `0` para desativar o take-profit. |
| `TimeOpenHour` | Início da janela de negociação permitida (hora, 0–23). | `8` | Avaliado em relação ao tempo de fechamento da vela. |
| `TimeCloseHour` | Fim da janela de negociação permitida (hora, 0–23). | `14` | Pode ser inferior a `TimeOpenHour` para abranger a meia-noite. |
| `CandleType` | Prazo usado para assinatura de velas. | `1 minute` | Qualquer outro valor `TimeFrame()` pode ser fornecido. |

## Notas de implementação
- O indicador `Shift` é usado internamente para reproduzir as compensações históricas exatas da implementação MQL sem acessar diretamente os buffers do indicador.
- `IsDifferenceSatisfied` encapsula a comparação ponto-limiar, mantendo a estratégia compatível com instrumentos que possuem tamanhos de ticks variados.
- A verificação da janela de negociação usa tempos de fechamento de velas, que é a melhor aproximação de `Hour()` de MetaTrader quando apenas velas finalizadas são processadas.
- Todos os comentários são escritos em inglês e o código depende exclusivamente do API (`SubscribeCandles().Bind(...)`) de alto nível, conforme exigido pelas diretrizes do projeto.

## Diferenças em comparação com a versão MQL
- Os sinais são avaliados em velas fechadas, eliminando possíveis repinturas que poderiam ocorrer em ticks intra-barras em MetaTrader.
- As ordens stop-loss e take-profit são tratadas por `StartProtection` em vez de serem anexadas manualmente a cada chamada `OrderSend`.
- A entrada `TrailingStop` não utilizada foi omitida intencionalmente para evitar a exposição de um parâmetro não funcional.
- O filtro de tempo suporta sessões noturnas por design, enquanto o EA original assumiu implicitamente `TimeOpen <= TimeClose`.
