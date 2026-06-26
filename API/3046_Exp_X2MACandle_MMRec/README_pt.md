# Estratégia Exp X2MA Candle MM Recovery
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão em C# do expert do MetaTrader **Exp_X2MACandle_MMRec**. Ela observa a cor de um candle duplamente suavizado, produzido pelo indicador personalizado X2MA original, para decidir quando abrir ou fechar posições. A versão StockSharp recria o pipeline de suavização dupla e mantém uma camada leve de gestão de dinheiro que reduz o volume de trading após um número configurável de perdas recentes.

O algoritmo processa apenas candles completados. Ele assina um período configurável, aplica duas médias móveis encadeadas aos valores OHLC do candle, deriva uma cor sintética de candle (verde, cinza ou vermelho) e usa transições de cor com um deslocamento de barra selecionável pelo usuário para acionar ações. Operações compradas são abertas quando a cor muda de altista para qualquer outra coisa. Operações vendidas seguem a condição simétrica. As saídas de posição estão alinhadas com as mesmas verificações de cor e podem ser habilitadas ou desabilitadas separadamente para cada lado.

## Lógica do indicador
1. Cada candle é suavizado duas vezes. Ambas as etapas podem usar diferentes métodos e comprimentos.
2. As opções de suavização mapeiam para indicadores do StockSharp:
   - `Simple` → `SimpleMovingAverage`
   - `Exponential` → `ExponentialMovingAverage`
   - `Smoothed` → `SmoothedMovingAverage` (RMA)
   - `Weighted` → `WeightedMovingAverage`
   - `Jurik` → `JurikMovingAverage` (o parâmetro Phase é respeitado quando disponível).
3. O corpo do candle sintético é achatado quando a diferença absoluta abertura/fechamento está abaixo de `GapPoints * Security.StepPrice`.
4. As cores são atribuídas da seguinte forma: abertura < fechamento → `2` (altista), abertura > fechamento → `0` (baixista), caso contrário → `1` (neutro).
5. Os sinais são avaliados na barra `SignalBar + 1` (duas barras atrás com a configuração padrão) para que as ordens sejam enviadas apenas após um candle completo confirmar a mudança de cor.

## Gestão de dinheiro
- O expert original reduzia dinamicamente o tamanho da posição após uma série de perdas usando estatísticas históricas de negociações. O StockSharp não expõe o histórico exato do MetaTrader, então o port mantém uma fila interna de negociações fechadas recentes.
- O comprimento da fila é controlado por `HistoryDepth` e o volume cai para `ReducedVolume` uma vez que `LossTrigger` ou mais perdas sejam detectadas dentro da janela.
- A estratégia registra os resultados das negociações usando preços de fechamento de candles quando uma saída manual é acionada. As ordens stop-loss/take-profit da versão MetaTrader não são recriadas. Você pode adicionar suas próprias regras de proteção através dos gestores de risco do StockSharp se necessário.

## Parâmetros
| Nome | Descrição |
|------|-----------|
| `CandleType` | Período dos candles usados para suavização e trading. |
| `FirstMethod`, `FirstLength`, `FirstPhase` | Método de suavização primário, comprimento e fase Jurik. |
| `SecondMethod`, `SecondLength`, `SecondPhase` | Método de suavização secundário, comprimento e fase Jurik. |
| `GapPoints` | Limiar de achatamento do corpo em passos de preço. |
| `SignalBar` | Deslocamento (0 = último candle terminado) ao ler os buffers de cor. |
| `AllowLongEntry` / `AllowShortEntry` | Habilitar abertura de posições compradas ou vendidas. |
| `AllowLongExit` / `AllowShortExit` | Habilitar fechamento de posições compradas ou vendidas. |
| `NormalVolume` | Tamanho de ordem padrão (lotes, ações, contratos). |
| `ReducedVolume` | Tamanho de ordem usado após o número configurado de perdas. |
| `HistoryDepth` | Número de negociações recentes inspecionadas para perdas (0 desativa o rastreamento do histórico). |
| `LossTrigger` | Contagem de perdas que ativa o volume reduzido (0 desativa o interruptor). |

## Notas de uso
- A estratégia opera em um único instrumento retornado por `GetWorkingSecurities()`.
- Sinais e saídas são processados uma vez por candle terminado para evitar ordens duplicadas.
- Definir `ReducedVolume` igual a `NormalVolume` se quiser desativar a redução de volume mantendo as estatísticas do histórico.
- Como o port depende de preços de fechamento de candles para classificar as negociações, o contador de perdas pode diferir ligeiramente do MetaTrader quando ocorrem deslizamentos ou execuções parciais. A documentação deve ajudá-lo a ajustar os parâmetros para obter comportamento similar.
- Stops e take-profits da versão MQL não são recriados automaticamente. Usar gestores de risco do StockSharp (`StartProtection`) se precisar de proteção a nível de plataforma.
