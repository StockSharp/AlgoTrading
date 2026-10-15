# Estratégia Ultra MFI de Recontagem de Gestão Monetária
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Ultra MFI MMRec** é um port direto do consultor especialista MetaTrader 5 `Exp_UltraMFI_MMRec`. Ela combina um oscilador de Índice de Fluxo de Dinheiro (MFI) suavizado em múltiplas etapas com gestão monetária baseada em sequências. Dois contadores internos acumulam quantas camadas de suavização apontam para cima ou para baixo. Cruzamentos entre esses contadores geram sinais de negociação, enquanto os resultados recentes das operações determinam se a próxima posição usa o tamanho de posição normal ou reduzido.

## Lógica de trading
1. **Indicador base** – um Índice de Fluxo de Dinheiro com comprimento configurável é calculado no tipo de vela selecionado.
2. **Suavização em escada** – o valor MFI é passado por uma escada de médias móveis. Cada etapa aumenta o comprimento de suavização em um incremento fixo. Os métodos de suavização suportados são Simple, Exponential, Smoothed, Linear Weighted e médias móveis Jurik (outros modos específicos do MT5 não estão disponíveis no StockSharp).
3. **Contadores direcionais** – para cada barra a estratégia compara a saída atual e anterior de cada etapa de suavização. Se a etapa está subindo, o contador de alta aumenta, caso contrário o de baixa aumenta. Ambos os contadores são suavizados novamente por uma média móvel final.
4. **Deslocamento de sinal** – as regras de negociação operam em barras terminadas. Um `SignalShift` configurável informa à estratégia quantas velas fechadas olhar para trás ao comparar os contadores, imitando o comportamento do MT5 ao usar `SignalBar=1`.
5. **Entradas e saídas** –
   * Se a barra anterior mostrou touros mais fortes (`bulls > bears`) e a última barra mostra um cruzamento para `bulls < bears`, a estratégia abre uma posição comprada. A mesma condição também fecha qualquer posição vendida aberta.
   * Se a barra anterior mostrou ursos mais fortes e a última barra muda para `bulls > bears`, a estratégia abre uma posição vendida e fecha qualquer posição comprada aberta.
   * O stop-loss e take-profit opcionais (baseados em porcentagem) podem ser gerenciados através de `StartProtection`.
6. **Gestão monetária** – o próximo tamanho de ordem depende dos últimos resultados de operações por direção. Após fechar cada posição o PnL realizado é inspecionado:
   * A estratégia armazena as operações de compra mais recentes `BuyTotalTrigger` e conta quantas foram perdas. Quando a contagem atinge `BuyLossTrigger`, a próxima ordem de compra usa `ReducedVolume`, caso contrário usa `NormalVolume`.
   * A mesma lógica é aplicada independentemente para operações de venda com `SellTotalTrigger` e `SellLossTrigger`.

## Parâmetros
- **CandleType** – tipo de dados do instrumento (período) usado para geração de sinais.
- **MfiPeriod** – comprimento do oscilador Índice de Fluxo de Dinheiro.
- **StepSmoothing / FinalSmoothing** – tipo de média móvel para as etapas da escada e os contadores finais.
- **StartLength / StepSize / StepsTotal** – geometria da escada de suavização (primeiro comprimento, incremento, número de etapas).
- **FinalSmoothingLength** – comprimento da etapa de suavização do contador.
- **SignalShift** – número de barras completadas para olhar para trás ao avaliar sinais.
- **NormalVolume / ReducedVolume** – tamanho de operação para condições normais e após uma sequência de perdas.
- **BuyTotalTrigger / BuyLossTrigger** – profundidade do histórico e limiar de perda para mudar a próxima operação comprada para tamanho reduzido.
- **SellTotalTrigger / SellLossTrigger** – configurações análogas para operações vendidas.
- **AllowLongEntries / AllowShortEntries / AllowLongExits / AllowShortExits** – habilitar ou desabilitar entradas e saídas para cada direção.
- **TakeProfitPercent / StopLossPercent** – níveis de proteção opcionais baseados em porcentagem.

## Notas de uso
- A suavização em escada requer velas históricas suficientes para preencher cada média móvel. Aguarde até que a estratégia esteja totalmente formada antes de confiar nos sinais.
- Como o StockSharp não fornece suavizadores específicos do MT5 como JurX, Parabolic, VIDYA ou AMA, as alternativas suportadas mais próximas são utilizadas. A suavização Jurik é um bom padrão que reproduz a sensação original do indicador UltraMFI.
- A gestão monetária é baseada no PnL realizado. Certifique-se de que seus backtests incluam a execução de ordens para que o PnL realizado seja atualizado após cada fechamento de posição.
- Este port mantém o comportamento de apenas entrar em novas posições quando a posição atual está zerada. Quando um sinal de reversão aparece enquanto a posição oposta é mantida, a estratégia primeiro sai da operação existente e entrará na próxima barra elegível uma vez zerada.
