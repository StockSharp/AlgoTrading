# Estratégia Exp RSIOMA V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Exp RSIOMA V2 é uma conversão do consultor especialista original do MetaTrader 5 que opera no oscilador RSIOMA (Índice de Força Relativa da Média Móvel). A estratégia reproduz as mesmas ideias dentro da API de alto nível do StockSharp: os dados de preço são suavizados, convertidos em uma série de momentum e alimentados em um acumulador estilo RSI. As decisões de trading são tomadas quando o oscilador muda de direção ou cruza zonas predefinidas.

## Lógica de trading
1. **Pré-processamento de preço** – o preço de candle selecionado (fechamento por padrão) é suavizado com uma das quatro famílias de médias móveis (simples, exponencial, suavizada ou linear ponderada).
2. **Cálculo de momentum** – o preço suavizado é comparado com o valor de `MomentumPeriod` barras atrás para obter o impulso de momentum.
3. **Computação RSIOMA** – os componentes de momentum positivo e negativo são acumulados com uma suavização exponencial de comprimento `RsiomaLength`, produzindo o valor RSIOMA no intervalo `[0; 100]`.
4. **Avaliação de sinais** – os candles fechados mais recentes são inspecionados de acordo com o `Mode` escolhido:
   - **Breakdown** – reage quando RSIOMA sai dos níveis de tendência principal (`MainTrendLong` / `MainTrendShort`). Quando o oscilador sai da zona superior, as vendidas são fechadas e entradas compradas são permitidas; sair da zona inferior executa a ação oposta.
   - **Twist** – procura pontos de virada. Uma compra ocorre quando a inclinação do RSIOMA muda de descendente para ascendente, enquanto as vendas reagem a uma transição de ascendente para descendente.
   - **CloudTwist** – emula a lógica de nuvem colorida do indicador MT5. Os trades são abertos quando RSIOMA retorna de extremos de sobrecompra/sobrevenda de volta para dentro do canal, e posições opostas são fechadas ao mesmo tempo.

Os sinais são avaliados na barra especificada por `SignalBar` (padrão: o candle completamente fechado anterior), garantindo que apenas dados confirmados sejam usados.

## Parâmetros
| Nome | Descrição | Valor padrão |
|------|-----------|--------------|
| `OrderVolume` | Volume de ordem padrão usado por ordens de mercado. | `1` |
| `CandleType` | Série de dados de candle processada pela estratégia. | Período de `4 horas` |
| `EnableLongEntries` / `EnableShortEntries` | Permitir a abertura de novas posições compradas/vendidas. | `true` |
| `EnableLongExits` / `EnableShortExits` | Permitir o fechamento de posições compradas/vendidas existentes. | `true` |
| `Mode` | Lógica de trading (Breakdown, Twist ou CloudTwist). | `Breakdown` |
| `PriceSmoothing` | Média móvel aplicada ao preço antes do RSIOMA. | `Exponential` |
| `RsiomaLength` | Período de média RSIOMA. | `14` |
| `MomentumPeriod` | Atraso entre amostras ao computar momentum. | `1` |
| `AppliedPrice` | Preço de candle usado para o oscilador (fechamento, abertura, mediana, DeMark, etc.). | `Close` |
| `MainTrendLong` / `MainTrendShort` | Níveis RSIOMA que definem zonas de sobrecompra/sobrevenda. | `60` / `40` |
| `SignalBar` | Número de barras fechadas atrás que devem ser analisadas. | `1` |

## Notas de implementação
- Apenas as famílias de suavização disponíveis no StockSharp são suportadas (simples, exponencial, suavizada e linear ponderada). Os modos avançados da versão MT5 (JJMA, VIDYA, AMA, …) não estão incluídos.
- As médias RSI são inicializadas usando os primeiros `RsiomaLength` valores de momentum para espelhar a inicialização do MetaTrader. Depois disso uma atualização exponencial é aplicada, correspondendo ao comportamento original do consultor especialista.
- Posições são sempre fechadas antes que uma entrada oposta seja emitida. As permissões de entrada (`EnableLongEntries`, `EnableShortEntries`) e as permissões de saída (`EnableLongExits`, `EnableShortExits`) fornecem controle total sobre as direções permitidas.
- `SignalBar = 0` pode ser usado para reagir ao candle finalizado atual; valores mais altos reproduzem a capacidade do MT5 de aguardar várias barras antes de agir.

## Uso
1. Adicionar a estratégia a um projeto StockSharp e atribuir o instrumento que deseja operar.
2. Configurar a assinatura de candle através de `CandleType` (padrão são candles de 4 horas) e ajustar limites se o símbolo usar características de volatilidade diferentes.
3. Selecionar o modo de sinal preferido dependendo de se deseja entradas de estilo rompimento (`Breakdown`), viradas de momentum (`Twist`) ou mudanças de cor de nuvem (`CloudTwist`).
4. Iniciar a estratégia. Durante a execução a estratégia se inscreve na série de candles escolhida, computa a cadeia RSIOMA e emite ordens de mercado quando as condições são satisfeitas.
