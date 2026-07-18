# Estratégia de Volatilidade de Stochastic Chaikin's
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port em StockSharp do consultor especialista MetaTrader `Exp_Stochastic_Chaikins_Volatility`. Analisa a amplitude entre os preços máximo e mínimo, suaviza essa volatilidade com uma média móvel configurável e depois normaliza o resultado usando um oscilador semelhante ao Stochastic. As decisões de trading seguem a lógica contra-tendência original: a estratégia procura pontos de virada no oscilador para operar os extremos de curto prazo enquanto fecha opcionalmente as posições existentes quando o momentum se reverte.

## Construção do indicador
1. **Volatilidade estilo Chaikin** – a diferença entre o máximo e o mínimo do candle é suavizada com a média móvel *primária*. Os métodos de suavização suportados são:
   - Simples (SMA)
   - Exponencial (EMA)
   - Suavizada/Wilder (SMMA)
   - Ponderada linearmente (LWMA)
   - Jurik (aproximação JMA)
2. **Normalização estocástica** – os `Stochastic Length` valores suavizados mais recentes definem o intervalo mais alto e mais baixo. O valor suavizado atual é normalizado em um intervalo de 0–100 usando essa janela.
3. **Suavização secundária** – uma segunda média móvel (método selecionável da mesma lista) é aplicada ao valor normalizado para obter a linha principal do oscilador. Internamente a linha de sinal é simplesmente o valor do oscilador do candle concluído anterior, replicando o comportamento do buffer do indicador MQL.

## Lógica de trading
- **Entrada**
  - *Comprar*: quando o oscilador principal formou um máximo mais baixo (valor anterior maior que seu próprio valor precedente, valor atual cruza abaixo desse valor anterior). Isso espelha o gatilho de compra contrário do EA original.
  - *Vender*: quando o oscilador formou um mínimo mais alto (valor anterior menor que seu próprio valor precedente, valor atual cruza acima desse valor anterior).
- **Saída**
  - Posições compradas fecham quando o valor do oscilador anterior se move abaixo de seu valor mais antigo (momentum descendente reaparece).
  - Posições vendidas fecham quando o valor do oscilador anterior sobe acima de seu valor mais antigo.
- A avaliação de sinais usa o parâmetro `Signal Shift` para inspecionar candles concluídos. Os padrões emulam a configuração MQL de 1 barra.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `Candle Type` | Período usado para todos os cálculos (padrão: candles temporais de 4 horas). |
| `Primary Method` / `Primary Length` | Tipo e comprimento de média móvel para suavizar a amplitude máximo–mínimo. |
| `Secondary Method` / `Secondary Length` | Tipo e comprimento de média móvel para suavizar o oscilador normalizado. |
| `Stochastic Length` | Janela de retrospectiva para o intervalo mais alto/mais baixo usado no passo de normalização. |
| `Signal Shift` | Número de candles concluídos entre a barra atual e a barra usada para avaliação do sinal. Deve permanecer ≥1. |
| `Allow Long/Short Entry` | Habilitar ou desabilitar a abertura de operações compradas ou vendidas. |
| `Allow Long/Short Exit` | Habilitar ou desabilitar o fechamento de posição quando o oscilador se reverte. |
| `High/Middle/Low Level` | Níveis guias visuais reproduzidos do indicador original (sem efeito direto no trading). |

## Notas de uso
- O port do StockSharp mantém o comportamento contra-tendência original mas usa as médias móveis do StockSharp. Métodos exóticos da biblioteca MQL (ParMA, VIDYA, AMA, etc.) são mapeados para a opção de suavização disponível mais próxima; escolha Jurik para uma aproximação mais próxima quando necessário.
- O dimensionamento de posição segue a propriedade `Volume` da estratégia base. O gerenciamento de stop-loss e take-profit da biblioteca auxiliar MQL não é replicado; as saídas dependem de reversões do oscilador ou gerenciamento de risco externo como `StartProtection`.
- Os sinais são calculados apenas em candles terminados. Certifique-se de que o feed de dados fornece o `Candle Type` selecionado com histórico suficiente para que ambas as etapas de suavização e a janela estocástica possam se aquecer.
